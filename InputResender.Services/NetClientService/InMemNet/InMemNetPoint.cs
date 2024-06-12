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
			if ( BoundedPoints.Remove ( GetKey ( ID, _port ) ) )
				throw new InvalidOperationException ( $"Failed to remove {this}" );
			ListeningDevice = null;
		}

		private static string GetAddress (int id) => $"IMN#{id}";
		private static string GetKey (int id, int port) => $"{GetAddress ( id )}:{port}";
		public override string ToString () => GetKey ( ID, _port ) + (string.IsNullOrWhiteSpace ( DscName ) ? "" : $" ({DscName})");

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