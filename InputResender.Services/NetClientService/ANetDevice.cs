using System;
using System.Collections.Generic;
using System.Threading;

namespace InputResender.Services.NetClientService {
	public abstract class ANetDevice<EPT> : INetDevice where EPT : class, INetPoint {
		private Dictionary<EPT, NetworkConnection.NetworkInfo> Connections = new ();
		private Action<NetworkConnection> ConnAccepter;

		public string DeviceName { get; set; } = string.Empty;
		public INetPoint EP { get => locEP; }
		public event Action<INetDevice, INetPoint> OnClosed;

		public readonly List<(NetMessagePacket, string, INetDevice.ProcessResult)> LastErrors = new ();

		protected EPT locEP { get; private set; }
		protected ANetDeviceLL<EPT> boundedLLDevice { get; private set; }
		protected abstract ANetDeviceLL<EPT> BindLL ( EPT ep );

		protected virtual INetDevice.ProcessResult LogRecvError ( NetMessagePacket msg, string dsc, INetDevice.ProcessResult err = INetDevice.ProcessResult.Skiped ) {
			LastErrors.Add ( (msg, dsc, err) );
			return err;
		}

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
			boundedLLDevice = BindLL ( locEP );
		}

		public void Close () => throw new NotImplementedException ();

		public NetworkConnection Connect ( INetPoint ep, INetDevice.MessageHandler recvAct, int timeout = 1000 ) {
			// Argument check
			if ( ep == null ) throw new ArgumentNullException ( nameof ( ep ) );
			if ( locEP == null ) throw new InvalidOperationException ( "Not bound" );
			if ( ep is not EPT inMemEP ) throw new ArgumentException ( $"{nameof ( ep )} is not of type {nameof ( EPT )}" );

			var connInfo = Connector<EPT>.Connect ( this, boundedLLDevice, inMemEP, Send, new CancellationTokenSource ( timeout ).Token );
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

		protected INetDevice.ProcessResult ReceiveMsg ( NetMessagePacket msg ) {
			if ( msg == null ) return LogRecvError ( msg, "Given message is null" );
			if ( msg.Data == null ) return LogRecvError ( msg, "Data of given message is null" );
			if ( msg.TargetEP == null ) return LogRecvError ( msg, "TargetEP of given message is null" );
			if ( msg.TargetEP != EP ) return LogRecvError ( msg, "TargetEP of given message is not this device" );
			if ( msg.SourceEP == null ) return LogRecvError ( msg, "SourceEP of given message is null" );

			NetworkConnection.NetworkInfo connInfo;
			if (msg.SignalType == INetDevice.SignalMsgType.Connect ) {
				var remoteEP = msg.SourceEP as EPT;
				if ( ConnAccepter == null ) return LogRecvError ( msg, "Not accepting connections" );
				if ( Connections.ContainsKey ( remoteEP ) ) return LogRecvError ( msg, "Connection already exists" );
				connInfo = NetworkConnection.Create ( this, remoteEP, Send );
				var accMsg = NetMessagePacket.CreateSignal ( INetDevice.SignalMsgType.Confirm, EP, msg.SourceEP );
				boundedLLDevice.Send ( accMsg.Data, remoteEP );
				ConnAccepter.Invoke ( connInfo.Connection );
				Connections.Add ( remoteEP, connInfo );
			}

			if ( Connections.TryGetValue ( msg.SourceEP as EPT, out connInfo ) ) {
				connInfo.Receiver ( msg );
				return INetDevice.ProcessResult.Accepted;
			} else return LogRecvError ( msg, "No connection to source" );
		}

		public override string ToString () {
			if ( string.IsNullOrEmpty ( DeviceName ) ) return $"NetDevice@{locEP}";
			return $"(NetDevice '{DeviceName}' @{locEP})";
		}
	}
}