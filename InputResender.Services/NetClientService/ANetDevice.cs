using System;
using System.Collections.Generic;
using System.Threading;

namespace InputResender.Services.NetClientService {
	internal abstract class ANetDevice<EPT> : INetDevice where EPT : class, INetPoint {
		private Dictionary<EPT, NetworkConnection.NetworkInfo> Connections = new ();
		private Action<NetworkConnection> ConnAccepter;

		public string DeviceName { get; set; } = string.Empty;
		public INetPoint EP { get => locEP; }
		public event Action<INetDevice, INetPoint> OnClosed;

		protected EPT locEP { get; private set; }
		protected ANetDeviceLL<EPT> boundedLLDevice { get; private set; }
		protected Func<EPT, ANetDeviceLL<EPT>> LLDeviceCreator { get; }

		public void AcceptAsync ( Action<NetworkConnection> callback, CancellationToken ct ) {
			if ( EP == null ) throw new InvalidOperationException ( "Not bound" );
			if ( callback == null ) throw new ArgumentNullException ( nameof ( callback ) );
			if ( ConnAccepter != null ) throw new InvalidOperationException ( "Already accepting" );

			ConnAccepter = callback;
			ct.Register ( () => ConnAccepter = null );
		}

		public void Bind ( INetPoint ep ) {
			if ( ep == null ) throw new ArgumentNullException ( nameof ( ep ) );
			if ( EP != null ) throw new InvalidOperationException ( $"Already bounded to {EP}" );
			if ( ep is not EPT ) throw new ArgumentException ( $"{nameof ( ep )} is not of type {nameof ( EPT )}" );
			locEP = ep as EPT;
			boundedLLDevice = LLDeviceCreator ( locEP );

			System.Net.Sockets.UdpClient udpClient = new System.Net.Sockets.UdpClient ();
		}

		public void Close () => throw new NotImplementedException ();

		public NetworkConnection Connect ( INetPoint ep, INetDevice.MessageHandler recvAct, int timeout = 1000 ) {
			// Argument check
			if ( ep == null ) throw new ArgumentNullException ( nameof ( ep ) );
			if ( locEP == null ) throw new InvalidOperationException ( "Not bound" );
			if ( ep is not EPT inMemEP ) throw new ArgumentException ( $"{nameof ( ep )} is not of type {nameof ( EPT )}" );

			var connInfo = Connector<EPT>.Connect ( this, boundedLLDevice, inMemEP, Send );
			Connections.Add ( inMemEP, connInfo );
			return connInfo.Connection;
		}
		public void UnregisterConnection ( NetworkConnection connection ) => throw new NotImplementedException ();

		private bool Send (NetMessagePacket msg) {
			if ( msg == null ) return false;
			if ( msg.TargetEP == null ) return false;
			if ( msg.SourceEP != EP ) return false;
			return boundedLLDevice.Send ( msg.Data, msg.TargetEP as EPT );
		}
	}
}