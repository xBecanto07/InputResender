#define USE_UDP
#if !USE_UDP
#define USE_TCP
#endif

using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace InputResender.Services {
#if USE_UDP
	public class UDPDevice : INetDevice {
		//private  UdpClient Client;
		private IPNetPoint ipEP;
		private UDPDeviceLL LLDevice;
		private readonly List<NetworkConnection> Connections = new ();
		private readonly BlockingCollection<(IPEndPoint, NetMessagePacket)> UnconnectedMessages = new ();
		private volatile ManualResetEvent Closing;
		private readonly List<SignalMessage> SignalMessages = new ();

		public INetPoint EP => ipEP;
		/// <inheritdoc/>
		public string DeviceName { get; set; } = string.Empty;

		public event Action<INetDevice, INetPoint> OnClosed;
		public event EventHandler<NetworkConnection> OnConnectionClosed;

		public bool Listening { get => LLDevice != null; }

		public UDPDevice () {
			ipEP = null;
			Connections = new List<NetworkConnection> ();
		}
		public void Bind ( INetPoint ep ) {
			if ( ep == null ) throw new ArgumentNullException ( nameof ( ep ) );
			if ( ipEP != null ) throw new InvalidOperationException ( "Already bound" );
			if (Closing != null) { Closing.Dispose (); Closing = null; }
			ipEP = ep as IPNetPoint;
			//Client = new UdpClient ( ipEP.LowLevelEP () );
			LLDevice = new UDPDeviceLL ( this, ipEP.LowLevelEP () );
		}
		public void Close () {
			if ( Closing != null ) return;
			if ( ipEP == null ) return;
			Closing = new ManualResetEvent ( false );
			Signal ( INetDevice.SignalMsgType.Close );
			Closing.WaitOne ();
			Closing.Dispose ();
			Closing = null;
		}

		public NetworkConnection Connect ( INetPoint ep, INetDevice.MessageHandler recvAct, int timeout = 1000 ) {
			if ( Closing != null ) throw new InvalidOperationException ( "Device is closing" );
			if ( ipEP == null ) throw new InvalidOperationException ( "Device not bound to local NetPoint!" );
			if ( ep == null ) throw new ArgumentNullException ( nameof ( ep ) );
			if ( recvAct == null ) throw new ArgumentNullException ( nameof ( recvAct ) );
			if ( ep is not IPNetPoint ) throw new ArgumentException ( $"{nameof ( ep )} is not of type {nameof ( IPNetPoint )}" );

			var signalResponse = Signal ( INetDevice.SignalMsgType.Connect, ep, timeout );
			if ( signalResponse.Response.IsError ) throw new InvalidOperationException ( $"Signal message failed ({signalResponse.Response.Error})" ) { Data = { { INetDevice.ExResponseData, signalResponse.Response } } };

			throw new NotImplementedException ( "No constructor is currently implemented for NetworkConnection. :(" );
			//return new NetworkConnection ( this, ep as IPNetPoint, ipEP, Send, recvAct );
		}
		public void AcceptAsync ( Action<NetworkConnection> callback, CancellationToken ct ) => throw new NotImplementedException ();
		public void UnregisterConnection ( NetworkConnection connection ) => throw new NotImplementedException ();

		private bool Send ( byte[] data, INetPoint remoteEP ) {
			if ( Closing != null ) throw new InvalidOperationException ( "Device is closing" );
			var remIPEP = remoteEP as IPNetPoint;
			if ( remIPEP == null ) throw new ArgumentException ( $"{nameof ( remoteEP )} is not of type {nameof ( IPNetPoint )}" );
			int sent = LLDevice.Client.Send ( data, data.Length, remIPEP.LowLevelEP () );
			return sent >= data.Length;
		}

		private SignalMessage Signal ( INetDevice.SignalMsgType msgType, INetPoint ep = null, int timeout = 1000 ) {
			if ( LLDevice == null ) return null;
			if (msgType == INetDevice.SignalMsgType.None ) return null;
			byte[] msgData = new byte[INetDevice.SignalMsgSize];
			msgData[0] = 0xAA;
			msgData[1] = (byte)msgType;
			for ( int i = 2; i < INetDevice.SignalMsgSize; i++ ) msgData[i] = 0xFF;

			IPNetPoint target;
			if ( ep == null ) target = null;
			else if ( ep is IPNetPoint ipEP ) target = ipEP;
			else throw new ArgumentException ( $"{nameof ( ep )} is not of type {nameof ( IPNetPoint )}" );

			SignalMessage signalMessage = new ( INetDevice.SignalMsgType.Connect, msgData, target, ipEP, this, timeout );
			return signalMessage;
		}

		public override bool Equals ( object obj ) {
			if ( obj == null ) return false;
			if ( obj is not UDPDevice tcp ) return false;
			bool ret = ipEP == null ? tcp.ipEP == null : ipEP.Equals ( tcp.ipEP );
			ret &= LLDevice == null ? tcp.LLDevice == null : LLDevice.Equals ( tcp.LLDevice );
			ret &= Connections.Count == tcp.Connections.Count;
			return ret;
		}
		public override int GetHashCode () => HashCode.Combine ( ipEP, LLDevice, Connections );

		bool INetDevice.SharedWith ( INetDevice device ) {
			if ( device is not UDPDevice tcp ) return false;
			if ( ipEP == null || tcp.ipEP == null ) return false;
			return ipEP.Equals ( tcp.ipEP );
		}



		protected class SignalMessage {
			public readonly INetDevice.SignalMsgType SignalType, AnswerType;
			public readonly byte[] SentData;
			public readonly DateTime SentTime;
			public readonly IPNetPoint TargetEP, SourceEP;
			public readonly ManualResetEvent ConfirmWaiter;
			public readonly UDPDevice Device;
			public NetMessagePacket Response;

			public bool Status => Response != null;

			public SignalMessage ( INetDevice.SignalMsgType signalType, byte[] sentData, IPNetPoint targetEP, IPNetPoint sourceEP, UDPDevice device, int timeout = 1000 ) {
				SignalType = signalType;
				AnswerType = signalType switch {
					INetDevice.SignalMsgType.Connect => INetDevice.SignalMsgType.Confirm,
					INetDevice.SignalMsgType.Close => INetDevice.SignalMsgType.Confirm,
					_ => INetDevice.SignalMsgType.None,
				};
				SentData = sentData;
				TargetEP = targetEP;
				SourceEP = sourceEP;
				SentTime = DateTime.Now;
				Device = device;
				lock ( Device.SignalMessages )
					Device.SignalMessages.Add ( this );
				ConfirmWaiter = new ManualResetEvent ( false );

				device.Send ( sentData, targetEP );
				if ( !ConfirmWaiter.WaitOne ( timeout ) ) {
					Response = new ( INetDevice.NetworkError.TimedOut, sourceEP, targetEP );
				}
			}

			public bool TryConfirm ( NetMessagePacket msg ) {
				if ( ConfirmWaiter.WaitOne ( 0 ) ) return false;
				if ( !msg.IsError && !msg.IsFrom ( TargetEP ) ) return false;
				if ( !msg.IsError && msg.SignalType != AnswerType ) return false;
				if ( msg.IsError && msg.IsFrom ( TargetEP, false ) ) return false;

				Response = msg;
				ConfirmWaiter.Set ();
				return true;
			}
		}

		/// <summary>Low-level components for UDPDevice</summary>
		private class UDPDeviceLL {
			private enum RecvStatus { None, Close, Data }
			readonly private UDPDevice Owner;
			readonly public UdpClient Client;
			readonly private IPEndPoint LocalEP;
			readonly private Task Listener;

			public UDPDeviceLL (UDPDevice owner, IPEndPoint locEP) {
				LocalEP = locEP;
				Owner = owner;
				Client = new UdpClient ( LocalEP );
				Listener = Task.Run ( RecvLoop );
			}

			private void RecvLoop () {
				while ( true ) {
					RecvStatus status = Receive ();
					if ( status == RecvStatus.Data ) continue;
					if ( status == RecvStatus.Close ) break;
					if ( status == RecvStatus.None ) break;
				}
				// Close socket (all sockets, i.e. senders and receivers, are expected to have listeners opened, and since the closing signal has to be sent (even if only through loopback), so this is the part that will be waited for when closing socket)
				if ( Client == null ) return;
				Client.Close ();
				Client.Dispose ();
				foreach ( var conn in Owner.Connections ) conn.Close ();
				Owner.Connections.Clear ();
				Owner.LLDevice = null;
				Owner.OnClosed?.Invoke ( Owner, Owner.ipEP );
				Owner.ipEP = null;
				Owner.Closing?.Set ();
			}

			private RecvStatus Receive () {
				IPEndPoint remoteEP = new ( IPAddress.Any, 0 );
				if ( Client == null ) return RecvStatus.None;
				NetMessagePacket msg;
				try {
					byte[] data = Client.Receive ( ref remoteEP );
					msg = new ( data, new IPNetPoint ( remoteEP ), Owner.ipEP );
				} catch (SocketException e) {
					var remote = new IPNetPoint ( remoteEP );
					msg = e.SocketErrorCode switch {
						SocketError.ConnectionAborted => new ( INetDevice.NetworkError.HostUnreachable, Owner.ipEP, remote ),
						SocketError.ConnectionReset => new ( INetDevice.NetworkError.HostUnreachable, Owner.ipEP, remote ),
						_ => new ( INetDevice.NetworkError.Unknown, Owner.ipEP, remote ),
					};
				}

				if ( msg.SignalType == INetDevice.SignalMsgType.Close ) {
					return RecvStatus.Close;
				} else {
					//OnDataReceived?.Invoke ( this, data, new IPNetPoint ( remoteEP ) );
					foreach ( var conn in Owner.Connections ) {
						if ( conn.TargetEP.Equals ( remoteEP ) ) {
							throw new NotImplementedException ("This is waiting for proper NetworkConnection.ReceiveMessage implementation. This method should than handle incoming message from remote EP and either call NetworkConnection.OnReceive or store it in UnconnectedMessages.");
							//conn.Receive ( msg );
							return RecvStatus.Data;
						}
					}
					lock ( Owner.SignalMessages ) {
						foreach ( var signalMsg in Owner.SignalMessages ) {
							if ( signalMsg.TryConfirm ( msg ) ) break;

						}
					}
					Owner.UnconnectedMessages.Add ( (remoteEP, msg) );
					return RecvStatus.Data;
				}
			}
		}
	}
#endif
}