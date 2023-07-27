using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Components.Interfaces;
using Components.Library;
using InputResender.Services;
using NetClient = System.Net.Sockets.UdpClient;
using ClientType = InputResender.Services.INetClientService.ClientType;
using NetList = InputResender.Services.NetworkFinderService;

namespace Components.Implementations {
	public class VPacketSender : DPacketSender {
		public const int MaxBufferSize = 32;
		public static int DefPort = 45256;
		public readonly int Port;
		readonly NetList NetworkList;
		public NetClientList Clients;
		SetRelation<IPEndPoint, NetList.Node, IPAddress, INetClientService> Targets;
		public IPEndPoint[] Listenings { get { return Targets.SetAKeys.ToArray (); } }
		List<(string msg, Exception e)> errors;
		public readonly CustomWaiter.WaiterList WaiterList;
		public delegate void ReceiveHandler ( byte[] data );
		public event ReceiveHandler OnReceiveEvent;
		private List<byte[]> PacketBuffer;
		private readonly ClientType ClientType;
		public Task ReceiverTask;

		public VPacketSender ( CoreBase owner, int port = -1, ClientType clientType = ClientType.UDP ) : base ( owner ) {
			WaiterList = new CustomWaiter.WaiterList ( nameof ( Recv ), nameof ( ReceiveAsync ), nameof ( Disconnect ) );
			if ( !Owner.IsRegistered ( nameof ( DLogger ) ) ) new VLogger ( Owner );
			Targets = new SetRelation<IPEndPoint, NetList.Node, IPAddress, INetClientService> (
				( val ) => NetworkList.FindNetwork ( val.Address ),
				( val ) => Clients.Add ( new IPEndPoint ( val, Port ), clientType ),
				( key, val ) => { },
				( key, val ) => Clients.Remove ( new IPEndPoint ( key, Port ), clientType )
				);
			errors = new List<(string msg, Exception e)> ();
			Port = port < 0 ? DefPort++ : port;
			NetworkList = new NetList ();
			ClientType = clientType;
			Clients = new NetClientList ();
			PacketBuffer = new List<byte[]> ( MaxBufferSize );
		}

		public override int ComponentVersion => 1;
		public override int Connections => Targets.Count;
		public override IReadOnlyCollection<(string msg, Exception e)> Errors => errors.AsReadOnly ();
		public override IReadOnlyCollection<IReadOnlyCollection<IPEndPoint>> EPList => NetworkList.GetAllEPs ( Port ).AsReadonly2D ();

		public override IPEndPoint OwnEP ( int TTL, int network = 0 ) => new IPEndPoint ( NetworkList[network][TTL].IPAddress, Port );

		public override void Connect ( object epObj ) => Targets.Add ( ParseEP ( epObj ) );
		public override void Disconnect ( object epObj ) => Targets.Remove ( ParseEP  ( epObj ) );

		private (IPEndPoint, IPAddress) ParseEP ( object epObj ) {
			if ( !(epObj is IPEndPoint) ) throw new InvalidCastException ( $"Wrong EP object type. Expected {typeof ( IPEndPoint ).Name} but is {epObj.GetType ().Name}!" );
			var ep = (IPEndPoint)epObj;
			var netNode = NetworkList.FindNetwork ( ep.Address );
			return (ep, netNode.IPAddress);
		}
		public override void ReceiveAsync ( Func<byte[], bool> callback ) {
			if ( callback == null ) return;
			if ( !Clients.Active ) Clients.Start ();
			ReceiverTask = Task.Run ( () => {
				while ( true ) {
					if ( Clients.Count == 0 ) return;
					var recv = Clients.WaitAny ();
					var res = recv.task.Result;
					if ( !callback ( recv.task.Result ) ) return;
				}
			} );
		}
		public override void Recv ( byte[] data ) => Clients.Direct ( data );
		public override void Send ( byte[] data ) {
			Targets.ForEach ( ( ep, valA, netNode, netClient ) => netClient.Send ( data, ep ) );
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

		public override void Destroy () { Clients.Dispose (); Targets.Clear (); }
	}
}