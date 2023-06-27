using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Components.Interfaces;
using Components.Library;
using NetClient = System.Net.Sockets.UdpClient;

namespace Components.Implementations {
	public class VPacketSender : DPacketSender<IPEndPoint> {
		public const int DefPort = 45256;
		public readonly int Port;
		Dictionary<IPEndPoint, NetClient> TCPs;
		readonly IPEndPoint[][] thisEPs;
		readonly NetClient[][] thisClients;
		RecvProcessor RecvHandler;
		Func<byte[], bool> Callback;
		public IPEndPoint[] Listenings { get { return TCPs.Keys.ToArray (); } }

		public VPacketSender ( CoreBase owner, int port = -1 ) : base ( owner ) {
			Port = port < 0 ? DefPort : port;
			thisEPs = AddressesToEPs ( FindNetworks (), Port );
			thisClients = new NetClient[thisEPs.Length][];
			for ( int i = 0; i < thisEPs.Length; i++ ) thisClients[i] = new NetClient[thisEPs[i].Length];
			TCPs = new Dictionary<IPEndPoint, NetClient> ();
			RecvHandler = new RecvProcessor ( this );
		}

		public override int ComponentVersion => 1;
		public override int Connections => TCPs.Count;

		public override IReadOnlyCollection<IReadOnlyCollection<IPEndPoint>> EPList => thisEPs.AsReadonly2D ();

		public override IPEndPoint OwnEP ( int TTL, int network = 0 ) {
			if ( TTL < 0 ) return thisEPs[network][0];
			int N = thisEPs.Length - 1;
			if ( TTL > N ) return thisEPs[network][N];
			return thisEPs[network][TTL];
		}

		public override void Connect ( IPEndPoint ep ) {
			var nt = FindNetwork ( ep.Address );
			NetClient newTCP = thisClients[nt.network][nt.ttl];
			if ( newTCP == null ) {
				newTCP = thisClients[nt.network][nt.ttl] = new NetClient ( thisEPs[nt.network][nt.ttl] );
			}

			TCPs.Add ( ep, newTCP );
		}
		/// <summary>WARNING! Only very basic implementation!</summary>
		private (int network, int ttl) FindNetwork ( IPAddress addr ) {
			if ( IPAddress.IsLoopback ( addr ) ) return (0, 0);
			byte[] abAr = addr.GetAddressBytes ();
			if ( abAr[0] == 192 && abAr[1] == 168 ) {
				for ( int i = 0; i < thisEPs.Length; i++ ) {
					if ( abAr[2] == thisEPs[i][1].Address.GetAddressBytes ()[2] ) return (i, 1);
				}
			}
			return (0, 2);
		}
		public override void Disconnect ( IPEndPoint ep ) {
			if ( TCPs.TryGetValue ( ep, out NetClient newTCP ) ) {
				RecvHandler.RemoveConn ( newTCP );
				TCPs.Remove ( ep );
				newTCP.Close ();
				newTCP.Dispose ();
			}
		}
		public override void ReceiveAsync ( Func<byte[], bool> callback ) {
			Callback = callback;
			foreach ( var conn in TCPs )
				RecvHandler.AddConn ( conn.Value );
		}
		public override void Recv ( byte[] data ) {
			if ( !Callback ( data ) ) {
				RecvHandler.Pause ( false );
			}
		}
		public override void Send ( byte[] data ) {
			foreach ( var conn in TCPs ) conn.Value.Send ( data, conn.Key );
		}

		public override string ToString () {
			var SB = new System.Text.StringBuilder ();
			var lst = Listenings;
			int N = lst.Length;
			SB.Append ( $"(IP:{Port}=>{{" );
			if ( N < 1 ) SB.Append ( "none}" );
			else {
				SB.Append ( $"{N}] {lst[0]}" );
				for ( int i = 1; i < N; i++ ) SB.Append ( $", {lst[i]}" );
			}
			SB.Append ( "}" );
			return SB.ToString ();
		}

		public static IPAddress IPv4 ( byte A, byte B, byte C, byte D ) => new IPAddress ( new byte[] { A, B, C, D } );

		public static IPEndPoint[][] AddressesToEPs ( IPAddress[][] addresses, int port = DefPort ) {
			int N = addresses.Length;
			IPEndPoint[][] ret = new IPEndPoint[N][];
			for ( int i = 0; i < N; i++ ) {
				int C = addresses[i].Length;
				ret[i] = new IPEndPoint[C];
				for ( int a = 0; a < C; a++ ) {
					ret[i][a] = new IPEndPoint ( addresses[i][a], port );
				}
			}
			return ret;
		}
		public static IPAddress[][] FindNetworks () {
			List<List<IPAddress>> semiRet = new List<List<IPAddress>> ();
			var interfaces = NetworkInterface.GetAllNetworkInterfaces ();
			foreach ( var inf in interfaces ) {
				var info = inf.GetIPProperties ();
				var gateways = info.GatewayAddresses;
				var unicasts = info.UnicastAddresses;
				IPAddress newAddr = null;
				foreach ( var addr in unicasts ) {
					if ( addr.Address.AddressFamily != AddressFamily.InterNetwork ) continue;
					if ( addr.Address.Equals ( IPAddress.Loopback ) ) continue;
					newAddr = addr.Address;
				}
				if ( newAddr == null ) continue;
				List<IPAddress> network = new List<IPAddress> () { IPAddress.Loopback, newAddr };
				if ( gateways.Count > 0 ) semiRet.Insert ( 0, network );
				else semiRet.Add ( network );
			}
			return semiRet.ToArray2D ();
		}

		public override void Destroy () {
			for (int net = 0; net < thisClients.Length; net ++ ) {
				for (int ttl = 0; ttl < thisClients[net].Length;ttl++) {
					thisClients[net][ttl].Close ();
					thisClients[net][ttl].Dispose ();
				}
			}
			Callback = null;
			RecvHandler.Dispose ();
		}

		public class RecvProcessor : IDisposable {
			ArDict<NetClient, Task<UdpReceiveResult>> Receivers;
			Thread RecvProcess;
			readonly AutoResetEvent NewConnSignal, PauseSignal;
			readonly ManualResetEvent OpFinished;
			VPacketSender Owner;
			public bool Cont = false, Stop = false;

			public override string ToString () {
				var SB = new System.Text.StringBuilder ();
				int N = Receivers.Count;
				SB.Append ( $"({(Cont?"cont":"stop")}:{{" );
				if ( N < 1 ) SB.Append ( "none}" );
				else {
					SB.Append ( $"{N}] {Line ( 0 )}" );
					for ( int i = 1; i < N; i++ ) SB.Append ( $", {Line ( i )}" );
				}
				SB.Append ( '}' );
				return SB.ToString ();

				string Line ( int ID ) {
					char c = '-';
					if ( Receivers[ID] != null ) {
						switch ( Receivers[ID].Status ) {
						case TaskStatus.Running: c = 'R'; break;
						case TaskStatus.RanToCompletion: c = 'F'; break;
						case TaskStatus.Created: c = 'N'; break;
						default: c = 'U'; break;
						}
					}
					return $"{ClientEP ( ID )}=>{c}";
				}
				string ClientEP (int ID) {
					var client = Receivers.GetKey ( ID );
					return client == null ? "Null" : Receivers.GetKey ( ID ).Client.LocalEndPoint.ToString ();
				}
			}

			public void Dispose () {
				Stop = true;
				NewConnSignal.Set ();
				Thread.MemoryBarrier ();
				Wait ();
				for (int i = 0; i < Receivers.Count; i++ ) {
					Receivers[i] = null;
				}
				Receivers.Clear ();
				Owner = null;
				RecvProcess = null;
			}

			public RecvProcessor ( VPacketSender owner ) {
				Owner = owner;
				OpFinished = new ManualResetEvent ( true );
				NewConnSignal = new AutoResetEvent ( false );
				PauseSignal = new AutoResetEvent ( true );
				Receivers = new ArDict<NetClient, Task<UdpReceiveResult>> {
					{ null, new Task<UdpReceiveResult> ( AddRecv ) }
				};
				ProcessSingleData ( 0 );
				RecvProcess = new Thread ( Process );
				RecvProcess.Start ();
			}

			public void Wait () { OpFinished.WaitOne (); }
			public void AddConn ( NetClient conn ) {
				if ( Receivers.ContainsKey ( conn ) ) return;
				Task<UdpReceiveResult> task = Task.Run ( () => RecvAsync ( conn ) );
				Receivers.Add ( conn, task );
				OpFinished.Reset ();
				NewConnSignal.Set ();
				PauseSignal.Set ();
				Wait ();
			}
			public void RemoveConn ( NetClient conn ) {
				if ( Receivers.Remove ( conn ) ) {
					OpFinished.Reset ();
					NewConnSignal.Set ();
					PauseSignal.Set ();
					Wait ();
				}
			}
			public void Pause ( bool cont ) { Cont = cont; NewConnSignal.Set (); }

			private UdpReceiveResult AddRecv () {
				NewConnSignal.WaitOne ();
				return default;
			}
			private UdpReceiveResult RecvAsync (NetClient conn) {
				IPEndPoint EP = new IPEndPoint ( IPAddress.Any, 0 );
				byte[] data = conn.Receive ( ref EP );
				return new UdpReceiveResult ( data, EP );
			}

			private void Process () {
				while ( true ) {
					if ( !Cont ) PauseSignal.WaitOne ();
					PauseSignal.Reset ();
					int ID = Task.WaitAny ( Receivers.Values.ToArray () );
					if (Stop) {
						for ( int i = 0; i < Receivers.Count; i++ ) {
							var client = Receivers.GetKey ( i );
							if (client != null ) {
								if ( client.Client != null ) client.Close ();
								if ( client.Client != null ) client.Dispose ();
								client = null;
							}
							Receivers[i].Dispose ();
						}
						return;
					}
					Cont &= ProcessSingleData ( ID );
				}
			}

			private bool ProcessSingleData ( int ID ) {
				OpFinished.Reset ();
				bool ret;
				if ( ID == 0 ) {
					Receivers[ID] = new Task<UdpReceiveResult> ( AddRecv );
					if ( Cont ) PauseSignal.Set ();
					Receivers[0].Start ();
					ret = true;
				} else {
					byte[] data = Receivers[ID].Result.Buffer;
					ret = Owner.Callback ( data );
					Receivers[ID] = Task.Run ( () => RecvAsync ( Receivers.GetKey ( ID ) ) );
				}
				OpFinished.Set ();
				return ret;
			}
		}
	}
}