#define USE_UDP
#if !USE_UDP
#define USE_TCP
#endif

using System;
using System.Collections.Generic;

namespace InputResender.Services {
	public interface INetDevice {
		/// <summary>Name of the device. Used for logging and debugging.</summary>
		public string DeviceName { get; set; }
		public const string ExResponseData = "Response";
		delegate ProcessResult MessageHandler ( NetMessagePacket message );
		public const int SignalMsgSize = 4;
		public enum SignalMsgType {
			/// <summary>Not a signal, just a data message</summary>
			None = 0,
			/// <summary>Notification or command to close underlying device
			Close = 0xF0,
			/// <summary>Incoming connection request</summary>
			Connect = 0xA1,
			/// <summary>Request or notify connection close</summary>
			Disconnect = 0xA2,
			/// <summary>Generic confirmation</summary>
			Confirm = 0x33
		}
		public enum NetworkError { None = 0, Unknown = 1, ConnectionRefused = 2, ConnectionReset = 3, HostUnreachable = 4, TimedOut = 5 }
		public enum ProcessResult { Accepted, Skiped, Closed, Confirmed }
		void Bind ( INetPoint ep );
		void Close ();
		NetworkConnection Connect ( INetPoint ep, MessageHandler recvAct, int timeout = 1000 );
		void AcceptAsync ( Action<NetworkConnection> callback, System.Threading.CancellationToken ct );
		INetPoint EP { get; }
		void UnregisterConnection ( NetworkConnection connection );
		/// <summary>Checks, if this device is using same resources as <paramref name="device"/></summary>
		/// <returns>True when internal client is shared. False when at least one is null or are different.</returns>
		bool SharedWith ( INetDevice device ) => Equals ( device );
		//event EventHandler<NetworkConnection> OnConnectionClosed;
		event Action<INetDevice, INetPoint> OnClosed;
	}

	public abstract class ANetDeviceLL<T> where T : INetPoint {
		public T LocalEP { get; }
		public abstract bool Send ( byte[] data, T ep );

		private List<Func<NetMessagePacket, bool>> Receivers;
		public event Func<NetMessagePacket, bool> OnReceive {
			add => Receivers.Insert ( 0, value );
			remove => Receivers.Remove ( value );
		}
	}

#if USE_TCP
	public class TCPDevice : INetDevice {
		public INetPoint EP => ipEP;
		private TcpClient Client;
		private TcpListener Listener;
		private IPNetPoint ipEP;
		private List<TCPConnection> connections = new();

		public event Action<INetDevice, INetPoint> OnClosed;
		public event EventHandler<INetworkConnection> OnConnectionClosed;
		public bool Listening { get; private set; } = false;

		public TCPDevice () {
			ipEP = null;
			Listener = null;
		}

		public void Bind ( INetPoint ep ) {
			if ( ep == null ) throw new ArgumentNullException ( nameof ( ep ) );
			if ( ipEP != null ) throw new InvalidOperationException ( "Already bound" );
			ipEP = ep as IPNetPoint;
			Client = new TcpClient ();
			Client.Client.Bind ( ipEP.LowLevelEP () );
			Listener = new TcpListener ( ipEP.LowLevelEP () );
		}

		public void Close () {
			if ( ipEP == null ) return;
			Client.Close ();
			Client.Dispose ();
			Client = null;
			if ( Listener != null ) Listener.Stop ();
			OnClosed?.Invoke ( this, ipEP );
			ipEP = null;
		}

		public INetworkConnection Connect ( INetPoint ep ) {
			if ( ep == null ) throw new ArgumentNullException ( nameof ( ep ) );
			if ( ipEP == null ) throw new InvalidOperationException ( "Not bound" );
			if ( ep is not IPNetPoint ) throw new ArgumentException ( $"{nameof ( ep )} is not of type {nameof ( IPNetPoint )}" );
			return new TCPConnection ( this, ep as IPNetPoint );
		}

		public void AcceptAsync ( Action<INetworkConnection> callback, CancellationToken ct ) {
			if ( ipEP == null ) throw new InvalidOperationException ( "Not bound" );
			if ( callback == null ) throw new ArgumentNullException ( nameof ( callback ) );

			Listener.BeginAcceptTcpClient ( ( ar ) => {
				var client = Listener.EndAcceptTcpClient ( ar );
				callback ( new TCPConnection ( this, new IPNetPoint ( client.Client.RemoteEndPoint as IPEndPoint ) ) );
			}, null );
		}

		public void Dispose () {
			if ( ipEP != null ) Close ();
			if ( Client != null ) Client.Dispose ();
		}

		public void RegisterConnection ( INetworkConnection connection ) {
			if ( connection is not TCPConnection tcp ) throw new ArgumentException ( $"{nameof ( connection )} is not of type {nameof ( TCPConnection )}" );

		}

		public override bool Equals ( object obj ) {
			if ( obj == null ) return false;
			if ( obj is not TCPDevice tcp ) return false;
			bool ret = ipEP == null ? tcp.ipEP == null : ipEP.Equals ( tcp.ipEP );
			ret &= Client == null ? tcp.Client == null : Client.Equals ( tcp.Client );
			ret &= Listener == null ? tcp.Listener == null : Listener.Equals ( tcp.Listener );
			ret &= connections.Count == tcp.connections.Count;
			return ret;
		}
		public override int GetHashCode () => HashCode.Combine ( ipEP, Client, Listener, connections );

		bool INetDevice.SharedWith ( INetDevice device ) {
			if ( device is not TCPDevice tcp ) return false;
			if ( ipEP == null || tcp.ipEP == null ) return false;
			return ipEP.Equals ( tcp.ipEP );
		}
	}
#endif
}