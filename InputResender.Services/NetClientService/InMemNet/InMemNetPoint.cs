using System;
using System.Collections.Generic;

namespace InputResender.Services.NetClientService.InMemNet {
	public class InMemNetPoint : INetPoint {
		private static Dictionary<string, InMemNetPoint> ReservedPoints = new ();
		private static Dictionary<string, InMemNetPoint> BoundedPoints = new ();
		public InMemDevice ListeningDevice { get; private set; }
		public string DscName { get; set; }

		public readonly int ID;
		private readonly int _port;

		/// <inheritdoc />
		public string Address => GetAddress ( ID );
		/// <inheritdoc />
		public int Port => _port;
		/// <inheritdoc />
		public string NetworkAddress => $"IMN#{ID}";
		/// <inheritdoc />
		public int PrefixLength => 24;

		private InMemNetPoint ( int id, int port ) {
			ID = id;
			_port = port;
			lock ( ReservedPoints ) {
				if ( !ReservedPoints.ContainsKey ( GetKey (id, port) ) )
					ReservedPoints.Add ( GetKey ( id, port ), this );
			}
		}

		public void Bind ( InMemDevice device ) {
			if ( device == null ) throw new ArgumentNullException ( nameof ( device ) );
			if ( ListeningDevice != null ) throw new InvalidOperationException ( $"Already bounded to {ListeningDevice}" );
			ListeningDevice = device;
			BoundedPoints.Add ( GetKey ( ID, _port ), this );
		}
		public void Close (InMemDevice owner) {
			if ( ListeningDevice != owner ) throw new InvalidOperationException ( $"Not bounded to {owner}" );
			if ( ListeningDevice == null ) throw new InvalidOperationException ( $"Not bounded" );
			var key = GetKey ( ID, _port );
			if ( !BoundedPoints.ContainsKey ( key ) ) throw new InvalidOperationException ( $"Not bounded" );
			if ( !BoundedPoints.Remove ( key ) )
				throw new InvalidOperationException ( $"Failed to remove {this}" );
			ListeningDevice = null;
		}

		private static string GetAddress (int id) => $"IMN#{id}";
		private static string GetKey (int id, int port) => $"{GetAddress ( id )}:{port}";
		public override string ToString () => GetKey ( ID, _port ) + (string.IsNullOrWhiteSpace ( DscName ) ? "" : $" ({DscName})");

		public bool SendHere ( byte[] data, InMemNetPoint senderEP ) {
			if ( ListeningDevice == null ) throw new InvalidOperationException ( $"Not listening" );
			if ( ListeningDevice.BoundedInMemDeviceLL == null ) throw new InvalidOperationException ( $"Listening device not bound properly" );
			NetMessagePacket msg = new ( data, this, senderEP );
			var status = ListeningDevice.BoundedInMemDeviceLL.ReceiveMsg ( msg );
			switch (status) {
			case INetDevice.ProcessResult.Accepted: return true;
			case INetDevice.ProcessResult.Confirmed: return true;
			case INetDevice.ProcessResult.Closed: return true;
			case INetDevice.ProcessResult.Skiped: return false;
			default: throw new InvalidOperationException ( $"Unknown status {status}" );
			}
		}

		/// <summary>Finds next available InMemNetPoint. That is a combination of <paramref name="id"/> and <paramref name="port"/> that is not yet used by any created InMemNetPoint. The returned InMemNetPoint is reserved and will not be returned by this method again. This method is thread-safe.</summary>
		public static InMemNetPoint NextAvailable ( int id = 0 ) {
			int port = 1;
			InMemNetPoint ret;
			lock ( ReservedPoints ) {
				while ( ReservedPoints.ContainsKey ( GetKey ( id, port ) ) ) port++;
				ret = new InMemNetPoint ( id, port );
			}
			return new InMemNetPoint ( id, port );
		}
	}
}