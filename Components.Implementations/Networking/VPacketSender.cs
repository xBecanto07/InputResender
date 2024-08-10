using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Components.Interfaces;
using Components.Library;
using InputResender.Services;
using NetClient = System.Net.Sockets.UdpClient;
using ClientType = InputResender.Services.ClientType;
using NetList = InputResender.Services.NetworkFinderService;
using InputResender.Services.NetClientService.InMemNet;
using System.Collections.Concurrent;

namespace Components.Implementations {
	/// <summary>This serves mostly as a translation layer between the DPacketSender and the NetClientList</summary>
	public class VPacketSender : DPacketSender {
		public const int MaxBufferSize = 32;
		public static int DefPort = 45256;
		public readonly int Port;
		readonly NetClientList Clients;
		List<(string msg, Exception e)> errors;
		public IReadOnlyDictionary<INetPoint, NetworkConnection> ActiveConns;
		private readonly BlockingCollection<NetMessagePacket> PacketBuffer;
		private readonly List<Func<byte[], bool, CallbackResult>> OnReceiveHandlers = new ();
		private List<INetPoint[]> NetList;

		private event Action<string, Exception> OnErrorLocal;
		public override event Action<string, Exception> OnError { add => OnErrorLocal += value; remove => OnErrorLocal -= value; }

		public VPacketSender ( CoreBase owner, int port = -1, Action<string, Exception> onErrorSub = null ) : base ( owner ) {
			if ( !Owner.IsRegistered ( nameof ( DLogger ) ) ) new VLogger ( Owner );
			errors = new List<(string msg, Exception e)> ();
			if ( onErrorSub != null ) OnError += onErrorSub;
			Port = port < 0 ? DefPort++ : port;
			NetList = new NetList ().ToList ( Port );
			NetList.Insert ( 0, INetPoint.NextAvailable<InMemNetPoint> ( 1, Port, DefPort.ToString () ) );
			PacketBuffer = new ( MaxBufferSize );
			Clients = new ();
			ActiveConns = Clients.Connections;
			//Clients.AddEP ( InMemNetPoint.NextAvailable ( DefPort ) );

			foreach ( var network in NetList ) {
				// Assuming that NetworkList returns only local addresses (i.e. bindable)
				foreach ( var node in network ) {
					// Skip if already added
					if ( Clients.OwnedDevices.Keys.Any ( ep => node.Equals ( ep ) ) ) continue;
					Owner.Fetch<DLogger> ().Log ( $"Trying to add {node} '{node.DscName}' as a valid local EP" );
					try {
						Clients.AddEP ( node );
					} catch ( Exception e ) {
						PrintError ( $"Failed to add {node} as a valid local EP", e );
					}
				}
			}
			Clients.AcceptAcync ( OnNewConnection );
		}

		private void OnNewConnection (NetworkConnection conn) {
			conn.OnReceive += LocalReceiver;
		}
		private void PrintError (string msg, Exception e) {
			errors.Add ( (msg, e) );
			OnErrorLocal?.Invoke ( msg, e );
		}

		public override int ComponentVersion => 1;
		public override int Connections => Clients.Connections.Count;
		public override IReadOnlyCollection<(string msg, Exception e)> Errors => errors.AsReadOnly ();
		public override IReadOnlyList<IReadOnlyList<INetPoint>> EPList => NetList.AsReadOnly ();
		public override bool IsEPConnected ( object ep ) => ep is INetPoint iep ? Clients.Connections.ContainsKey ( iep ) : false;

		public override INetPoint OwnEP ( int TTL, int network = 0 ) => NetList[network][TTL];

		public override void Connect ( object epObj ) {
			if ( epObj == null ) throw new ArgumentNullException ( nameof ( epObj ) );
			var INetPoint = ParseEP ( epObj );
			NetworkConnection conn = null;
			for (int i = 0; i < 5; i++ ) {
				try {
					conn = Clients.Connect ( INetPoint );
					break;
				} catch (OperationCanceledException e) {
					if ( i == 4 ) throw new InvalidOperationException ( $"Failed to connect to {epObj}", e );
				}
			}
			
			if ( conn == null ) PrintError ( "Failed to connect", null );
			//else ActiveConns.Add ( epObj, conn );
			conn.OnReceive += LocalReceiver;
		}
		public override void Disconnect ( object epObj ) {
			if ( epObj == null ) throw new ArgumentNullException ( nameof ( epObj ) );
			var INetPoint = ParseEP ( epObj );
			if ( !Clients.Connections.TryGetValue ( INetPoint, out var conn ) )
				throw new InvalidOperationException ( $"No active connection to {epObj}" );
			conn.OnReceive -= LocalReceiver;
			conn.Close ();
			//ActiveConns.Remove ( epObj );
		}

		private INetPoint ParseEP ( object epObj ) {
			if ( epObj is INetPoint INP ) return INP;
			if ( epObj is IPEndPoint IP ) return new IPNetPoint ( IP );
			if ( epObj is string sEP ) {
				if ( InMemNetPoint.TryParse ( sEP, out var IMEP ) ) return IMEP;
				if ( IPAddress.TryParse ( sEP, out var IPAddr ) ) return new IPNetPoint ( IPAddr, Port );
				if ( IPNetPoint.TryParse ( sEP, out var IPP ) ) return IPP;
			}
			throw new InvalidCastException ( $"Unexpected object type: {epObj.GetType ().Name}. Currently supported: {nameof ( INetPoint )}, {nameof ( IPEndPoint )}." );
		}

		private INetDevice.ProcessResult LocalReceiver ( NetMessagePacket msg ) {
			lock ( PacketBuffer ) {
				//if ( Receiver != null ) return Receiver ( msg.Data ) ? INetDevice.ProcessResult.Accepted : INetDevice.ProcessResult.Skiped;
				foreach ( var handler in OnReceiveHandlers ) {
					var ret = handler ( msg.Data, false );
					if ( ret.HasFlag ( CallbackResult.Skip ) ) return INetDevice.ProcessResult.Skiped;
					if ( !ret.HasFlag ( CallbackResult.Stop ) ) return INetDevice.ProcessResult.Accepted;
				}
				if ( PacketBuffer.Count >= MaxBufferSize ) PacketBuffer.Take ();
				PacketBuffer.Add ( msg );
				return INetDevice.ProcessResult.Accepted;
			}
		}

		public override event Func<byte[], bool, CallbackResult> OnReceive {
			add {
				if ( value == null ) return;
				lock (PacketBuffer) {
					List<NetMessagePacket> declined = new ();
					while ( PacketBuffer.Count > 0 ) {
						var msg = PacketBuffer.Take ();
						var ret = value ( msg.Data, false );
						if ( !ret.HasFlag ( CallbackResult.Skip) ) declined.Add ( msg );
						if ( ret.HasFlag( CallbackResult.Stop) ) return; // Caller does no longer want to receive (might be due to a full buffer, error or received specific message)
					}
					foreach ( var msg in declined ) PacketBuffer.Add ( msg );
					OnReceiveHandlers.Add ( value );
				}
			}
			remove {
				if ( value == null ) return;
				lock ( PacketBuffer ) {
					OnReceiveHandlers.Remove ( value );
				}
			}
		}
		public override void Recv ( byte[] data ) => throw new NotImplementedException ();
		public override void Send ( byte[] data ) {
			if ( Owner.LogFcn != null ) {
				System.Text.StringBuilder SB = new ();
				SB.AppendLine ( $"Sending data[{data.Length}]" );
				foreach ( var conn in ActiveConns ) SB.AppendLine ( $"  {conn.Value}" );
				Owner.LogFcn.Invoke ( SB.ToString () );
			}
			foreach ( var conn in ActiveConns ) conn.Value.Send ( data );
		}

		public override string ToString () {
			var SB = new System.Text.StringBuilder ();
			var lst = ActiveConns;
			int N = lst.Count;
			SB.Append ( $"(IP:{Port}=>{{" );
			var targs = ActiveConns.Values.Select ( ep => ep.TargetEP.ToString () ).ToArray ();
			SB.Append ( targs.Any () ? string.Join ( ", ", targs ) : "none" );
			SB.Append ( '}' );
			return SB.ToString ();
		}

		public static IPAddress IPv4 ( byte A, byte B, byte C, byte D ) => new IPAddress ( new byte[] { A, B, C, D } );

		public override void Destroy () {
			foreach ( var conn in ActiveConns ) conn.Value.Close ();
			//ActiveConns.Clear ();
			Clients.Close ();
			PacketBuffer.Dispose ();
			errors.Clear ();
		}

		public override StateInfo Info => new VStateInfo ( this );

		public class VStateInfo : DStateInfo {
			public new VPacketSender Owner => (VPacketSender)base.Owner;
			public VStateInfo ( VPacketSender owner ) : base ( owner ) {
				Clients = owner.Clients.ToString ();
			}

			public readonly string Clients;

			protected override string[] GetBuffers () {
				lock ( Owner.PacketBuffer ) {
					NetMessagePacket[] bufferCopy = Owner.PacketBuffer.ToArray ();
					return bufferCopy.Where ( p => p != null )
						.Select ( p => $"{p.SourceEP}->{p.TargetEP}[t:{p.SignalType}|e:{p.Error}] {p.Data.ToHex ()}" ).ToArray ();
				}
			}
			protected override string[] GetConnections () {
				List<string> ret = new ();
				foreach ( var conn in Owner.ActiveConns ) {
					ret.Add ( $"{conn.Key}: {conn.Value}" );
				}
				return ret.ToArray ();
			}
			public override string AllInfo () => $"{base.AllInfo ()}{BR}Clients: {Clients}";
		}
	}
}