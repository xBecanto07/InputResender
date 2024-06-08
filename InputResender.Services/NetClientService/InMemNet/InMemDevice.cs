using System;
using System.Collections.Generic;

namespace InputResender.Services.NetClientService.InMemNet {
	public class InMemDevice : INetDevice {
		private InMemNetPoint locEP;
		private Dictionary<InMemNetPoint, NetworkConnection.NetworkInfo> Connections = new ();

		public INetPoint EP => locEP;
		/// <inheritdoc/>
		public string DeviceName { get; set; } = string.Empty;

		//public event EventHandler<NetworkConnection> OnConnectionClosed;
		public event Action<INetDevice, INetPoint> OnClosed;

		private Action<NetworkConnection> ConnAccepter;
		/// <summary>Start listening for incoming connections until <paramref name="ct"/> is cancelled.</summary>
		public void AcceptAsync ( Action<NetworkConnection> callback, System.Threading.CancellationToken ct ) {
			if ( locEP == null ) throw new InvalidOperationException ( "Not bound" );
			if ( callback == null ) throw new ArgumentNullException ( nameof ( callback ) );
			if ( ConnAccepter != null ) throw new InvalidOperationException ( "Already accepting" );

			ConnAccepter = callback;
			ct.Register ( () => ConnAccepter = null );
		}

		public void Bind ( INetPoint ep ) {
			if ( ep == null ) throw new ArgumentNullException ( nameof ( ep ) );
			if ( locEP != null ) throw new InvalidOperationException ( $"Already bounded to {locEP}" );
			if ( ep is not InMemNetPoint ) throw new ArgumentException ( $"{nameof ( ep )} is not of type {nameof ( InMemNetPoint )}" );
			locEP = ep as InMemNetPoint;
			locEP.Bind ( this );
		}

		public void Close () => locEP.Close ( this );

		/// <summary>Attempts to connect to remote device. Will throw exception on failure.</summary>
		public NetworkConnection Connect ( INetPoint ep, INetDevice.MessageHandler recvAct, int timeout = 1000 ) {
			// Argument check
			if ( ep == null ) throw new ArgumentNullException ( nameof ( ep ) );
			if ( locEP == null ) throw new InvalidOperationException ( "Not bound" );
			if ( ep is not InMemNetPoint inMemEP ) throw new ArgumentException ( $"{nameof ( ep )} is not of type {nameof ( InMemNetPoint )}" );

			// Remote device check
			if (inMemEP.ListeningDevice == null) throw new InvalidOperationException ( "Remote device is not listening" );
			if (inMemEP.ListeningDevice == this) throw new InvalidOperationException ( "Cannot connect to self" );
			if (inMemEP.ListeningDevice is not InMemDevice remoteDevice) throw new InvalidOperationException ( "Remote device is not InMemDevice" );
			if (remoteDevice.ConnAccepter == null) throw new InvalidOperationException ( "Remote device is not accepting new connections" );

			/*
			// Should use direct connection, but send/receive signals
			var connFromRemoteInfo = NetworkConnection.Create ( remoteDevice, locEP, remoteDevice.Send );
			remoteDevice.ConnAccepter.Invoke ( connFromRemoteInfo.Connection );

			var connToRemoteInfo = NetworkConnection.Create ( this, ep, Send );
			Connections.Add ( inMemEP, connToRemoteInfo );
			remoteDevice.Connections.Add ( locEP, connFromRemoteInfo );
			return connToRemoteInfo.Connection;*/

		}

		public void UnregisterConnection ( NetworkConnection connection ) {
			if ( locEP == null ) throw new InvalidOperationException ( "Not bound" );
			var targEP = connection.ValidTarget<InMemNetPoint> ();

			if (!Connections.TryGetValue ( targEP, out var conn ))
				throw new InvalidOperationException ( $"No connection to {targEP}" );
				conn.Connection.Close ( Send );
				Connections.Remove ( targEP );
		}

		private InMemDevice FindRemote ( NetworkConnection conn ) {
			var inMemEP = conn.ValidTarget<InMemNetPoint> ();
			if (inMemEP.ListeningDevice == null) throw new InvalidOperationException ( $"No listening device at {inMemEP}" );
			return inMemEP.ListeningDevice;
		}

		private bool Send ( NetMessagePacket data ) {
			if (locEP == null) throw new InvalidOperationException ( "Not bound" );

			var inMemEP = data.ValidReceiver<InMemNetPoint> ();
			if (!Connections.TryGetValue ( inMemEP, out var conn )) throw new InvalidOperationException ( $"No connection to {inMemEP}" );

			// Break reference to 1) allow modification of original data, 2) to prevent modification of data after it was sent and 3) to simulate network behavior
			var newDataArr = new byte[data.Data.Length];
			data.Data.CopyTo ( newDataArr, 0 );
			NetMessagePacket newData = new ( newDataArr, data.TargetEP, data.SourceEP );

			var remote = FindRemote ( conn.Connection );
			var status = remote.Receive ( newData );
			return status == INetDevice.ProcessResult.Accepted || status == INetDevice.ProcessResult.Confirmed;
		}

		private INetDevice.ProcessResult Receive ( NetMessagePacket message ) {
			if (locEP == null) throw new InvalidOperationException ( "Not bound" );
			var inMemEP = message.ValidSender<InMemNetPoint> ();
			if (!Connections.TryGetValue ( inMemEP, out var conn )) throw new InvalidOperationException ( $"No connection from {inMemEP}" );

			return conn.Receiver ( message );
		}

		public override string ToString () {
			if (string.IsNullOrEmpty(DeviceName)) return $"IND@{locEP}";
			return $"(IND '{DeviceName}' @{locEP})";
		}
	}
}