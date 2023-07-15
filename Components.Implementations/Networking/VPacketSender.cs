using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Components.Interfaces;
using Components.Library;
using InputResender.Services;
using NetClient = System.Net.Sockets.UdpClient;

namespace Components.Implementations {
	public class VPacketSender : DPacketSender {
		public const int MaxBufferSize = 32;
		public static int DefPort = 45256;
		public readonly int Port;
		Dictionary<IPEndPoint, NetClient> TCPs;
		readonly IPEndPoint[][] thisEPs;
		readonly NetClient[][] thisClients;
		public RecvProcessor RecvHandler;
		Func<byte[], bool> Callback;
		public IPEndPoint[] Listenings { get { return TCPs.Keys.ToArray (); } }
		List<(string msg, Exception e)> errors;
		public readonly CustomWaiter.WaiterList WaiterList;
		public delegate void ReceiveHandler ( byte[] data );
		public event ReceiveHandler OnReceiveEvent;
		private List<byte[]> PacketBuffer;

		public VPacketSender ( CoreBase owner, int port = -1 ) : base ( owner ) {
			WaiterList = new CustomWaiter.WaiterList ( nameof ( Recv ), nameof ( ReceiveAsync ), nameof ( Disconnect ) );
			if ( !Owner.IsRegistered ( nameof ( DLogger ) ) ) new VLogger ( Owner );
			errors = new List<(string msg, Exception e)> ();
			Port = port < 0 ? DefPort++ : port;
			thisEPs = AddressesToEPs ( FindNetworks (), Port );
			thisClients = new NetClient[thisEPs.Length][];
			for ( int i = 0; i < thisEPs.Length; i++ ) thisClients[i] = new NetClient[thisEPs[i].Length];
			TCPs = new Dictionary<IPEndPoint, NetClient> ();
			RecvHandler = new RecvProcessor ( this );
			PacketBuffer = new List<byte[]> ( MaxBufferSize );
		}

		public override int ComponentVersion => 1;
		public override int Connections => TCPs.Count;
		public override IReadOnlyCollection<(string msg, Exception e)> Errors => errors.AsReadOnly ();
		public override IReadOnlyCollection<IReadOnlyCollection<IPEndPoint>> EPList => thisEPs.AsReadonly2D ();

		public override IPEndPoint OwnEP ( int TTL, int network = 0 ) {
			if ( TTL < 0 ) return thisEPs[network][0];
			int N = thisEPs.Length - 1;
			if ( TTL > N ) return thisEPs[network][N];
			return thisEPs[network][TTL];
		}

		public override void Connect ( object epObj ) {
			Owner.Fetch<DLogger> ().Log ( $"Connecting {epObj}" );
			IPEndPoint ep = (IPEndPoint)epObj;
			var nt = FindNetwork ( ep.Address );
			NetClient newTCP = thisClients[nt.network][nt.ttl];
			if ( newTCP == null ) {
				newTCP = thisClients[nt.network][nt.ttl] = new NetClient ( thisEPs[nt.network][nt.ttl] );
			}

			TCPs.Add ( ep, newTCP );
			// No wait because no async execs
		}
		/// <summary>WARNING! Only very basic implementation!</summary>
		private (int network, int ttl) FindNetwork ( IPAddress addr ) {
			Owner.Fetch<DLogger> ().Log ( $"Finding network of {addr}" );
			if ( IPAddress.IsLoopback ( addr ) ) return (0, 0);
			byte[] abAr = addr.GetAddressBytes ();
			if ( abAr[0] == 192 && abAr[1] == 168 ) {
				for ( int i = 0; i < thisEPs.Length; i++ ) {
					if ( abAr[2] == thisEPs[i][1].Address.GetAddressBytes ()[2] ) return (i, 1);
				}
			}
			return (0, 2);
		}
		public override void Disconnect ( object epObj ) {
			var waiter = WaiterList.Register ( nameof ( Disconnect ) );
			Owner.Fetch<DLogger> ().Log ( $"Disconnecting {epObj}" );
			IPEndPoint ep = (IPEndPoint)epObj;
			if ( TCPs.TryGetValue ( ep, out NetClient newTCP ) ) {
				RecvHandler.RemoveConn ( newTCP );
				TCPs.Remove ( ep );
				newTCP.Close ();
				newTCP.Dispose ();
			}
			//waiter.Wait ();
		}
		public override void ReceiveAsync ( Func<byte[], bool> callback ) {
			var waiter = WaiterList.Register ( nameof ( ReceiveAsync ) );
			Owner.Fetch<DLogger> ().Log ( $"Starting async recv" );
			Callback = callback;
			foreach ( var conn in TCPs )
				RecvHandler.AddConn ( conn.Value );
			//waiter.Wait ();
		}
		public override void Recv ( byte[] data ) {
			var waiter = WaiterList.Register ( nameof ( Recv ) );
			Owner.Fetch<DLogger> ().Log ( $"Directly receiving {data.Length} bytes of data" );
			if ( !Callback ( data ) ) {
				RecvHandler.Pause ( false );
			}
			waiter.Wait ();
		}
		public override void Send ( byte[] data ) {
			Owner.Fetch<DLogger> ().Log ( $"Sending {data.Length} bytes of data" );
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

		public bool OnReceive ( byte[] data ) {
			if ( OnReceiveEvent != null ) OnReceiveEvent.Invoke ( data );
			else {
				PacketBuffer.Insert ( 0, data );
				if ( PacketBuffer.Count > MaxBufferSize ) PacketBuffer.RemoveAt ( MaxBufferSize );
			}
			return true;
		}

		public static IPAddress IPv4 ( byte A, byte B, byte C, byte D ) => new IPAddress ( new byte[] { A, B, C, D } );

		public static IPEndPoint[][] AddressesToEPs ( IPAddress[][] addresses, int port ) {
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
			Owner.Fetch<DLogger> ().Log ( $"Destroying" );
			RecvHandler.Pause ( false );
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
			VPacketSender Owner;
			NetClientList ClientList;

			public override string ToString () => ClientList.ToString ();

			public void Dispose () => ClientList.Dispose ();

			public RecvProcessor ( VPacketSender owner ) {
				Owner = owner;
				ClientList = new NetClientList ();
			}

			public void Wait () => ClientList.WaitAny ();
			public void AddConn ( NetClient conn ) => ClientList.AddClient ( null );
			public void RemoveConn ( NetClient conn ) => ClientList.RemoveClient ( null );
			public void Pause ( bool cont ) { if ( cont ) ClientList.Start (); else ClientList.Interrupt (); }
		}
	}
}