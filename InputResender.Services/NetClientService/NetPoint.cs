﻿using Components.Library;
using InputResender.Services.NetClientService.InMemNet;
using System;
using System.Net;
using System.Net.Sockets;

namespace InputResender.Services {
	public enum ClientType { Unknown, UDP }

	public interface INetPoint {
		string DscName { get; set; }
		string Address { get; }
		int Port { get; }
		string NetworkAddress { get; }
		string FullNetworkPath { get; }
		int PrefixLength { get; }

		public static INetPoint NextAvailable<T> ( int wantedPort, string addr = null ) where T : INetPoint => NextAvailable<T> ( 0, wantedPort, addr )[0];

		public static INetPoint[] NextAvailable<T> ( int N, int wantedPort, string addr = null ) where T : INetPoint {
			var ar = new INetPoint[N];
			NextAvailable<T> ( ar, wantedPort, addr );
			return ar;
		}

		public static void NextAvailable<T> ( INetPoint[] ar, int wantedPort, string addr = null ) where T : INetPoint {
			if ( ar == null ) throw new ArgumentNullException ( nameof ( ar ) );
			if ( ar.Length == 0 ) return;

			int N = ar.Length;
			if ( typeof ( T ) == typeof ( InMemNetPoint ) ) {
				if ( !int.TryParse ( addr, out int id ) ) id = 0;
				for ( int i = 0; i < N; i++ ) ar[i] = InMemNetPoint.NextAvailable ( id );

			} else if ( typeof ( T ) == typeof ( IPNetPoint ) ) {
				if ( !IPAddress.TryParse ( addr, out IPAddress ip ) ) ip = IPAddress.Loopback;
				NetStatsServise.PrepareNextPort ( N, wantedPort, ClientType.UDP );
				for ( int i = 0; i < N; i++ ) {
					int port = NetStatsServise.FindNextPort ( wantedPort, ClientType.UDP );
					ar[i] = new IPNetPoint ( ip, port );
				}

			} else {
				throw new ArgumentException ( $"Type {typeof ( T ).Name} is not supported" );
			}
		}
	}
	public class IPNetPoint : INetPoint {
		readonly IPEndPoint ep;

		public IPNetPoint ( IPEndPoint ep ) {
			if ( ep == null ) throw new ArgumentNullException ( nameof ( ep ) );
			this.ep = ep;
		}
		public IPNetPoint ( IPAddress addr, int port ) {
			if ( addr == null ) throw new ArgumentNullException ( nameof ( addr ) );
			if ( port < 1 || port > 65535 ) throw new ArgumentOutOfRangeException ( nameof ( port ) );
			ep = new IPEndPoint ( addr, port );
		}

		public string DscName { get; set; }
		public string Address => ep.Address.ToString ();
		public int Port { get => ep.Port; private set => ep.Port = value; }
		public int PrefixLength { get; private set; } = 24;
		public string NetworkAddress => ep.Address.GetNetworkAddr ( PrefixLength ).ToString ();
		public string FullNetworkPath => $"{NetworkAddress}:{Port}";
		public IPEndPoint LowLevelEP () => new ( ep.Address, ep.Port );
		public override bool Equals ( object obj ) => obj is IPNetPoint point && ep.Equals ( point.ep );
		public override int GetHashCode () => HashCode.Combine ( ep );
		public override string ToString () => ep.ToString ();
		public static bool TryParse ( string ss, out IPNetPoint IPP ) {
			IPP = null;
			if ( string.IsNullOrWhiteSpace ( ss ) ) return false;
			if ( !IPEndPoint.TryParse ( ss, out IPEndPoint ep ) ) return false;
			IPP = new IPNetPoint ( ep );
			return true;
		}
		public static bool operator == ( IPNetPoint left, IPNetPoint right ) {
			if ( left is null ) return right is null;
			return left.Equals ( right );
		}
		public static bool operator != ( IPNetPoint left, IPNetPoint right ) => !(left == right);
	}
}