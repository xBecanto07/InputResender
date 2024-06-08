using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net;
using Protocol = InputResender.Services.INetClientService.ClientType;

namespace InputResender.Services {
	public static class NetStatsServise {
		private static List<int> reservedPorts = new ();

		public static void PrepareNextPort (int cnt, int wantedPort, Protocol protocol, int maxPortOffset = 100 ) {
			lock ( reservedPorts ) {
				for ( int i = 0; i < cnt; i++ ) {
					int port = FindNextPortNonlocked ( wantedPort, protocol, maxPortOffset );
					if ( port == -1 ) throw new InvalidOperationException ( "No more ports available!" );
					reservedPorts.Add ( port );
				}
				reservedPorts.Sort ();
			}
		}
		public static int FindNextPort ( int wantedPort, Protocol protocol, int maxPortOffset = 100 ) {
			lock ( reservedPorts ) {
				int resID = -1;
				for (int i = 0; i < reservedPorts.Count; i++ ) {
					if ( (reservedPorts[i] < wantedPort) | (reservedPorts[i] > wantedPort + maxPortOffset) ) continue;
					resID = i;
					break;
				}
				if ( resID >= 0 ) {
					var tmp = reservedPorts[resID];
					reservedPorts.RemoveAt ( resID );
					return tmp;
				} else return FindNextPortNonlocked ( wantedPort, protocol, maxPortOffset );
			}
		}

		private static int FindNextPortNonlocked ( int wantedPort, Protocol protocol, int maxPortOffset = 100 ) {
			var globIP = IPGlobalProperties.GetIPGlobalProperties ();
			IPEndPoint[] activePorts;
			switch ( protocol ) {
			case Protocol.UDP: activePorts = globIP.GetActiveUdpListeners (); break;
			case Protocol.Unknown: return -1;
			default: throw new ArgumentException ( "Incorrect protocol!" );
			}
			int N = wantedPort + maxPortOffset;
			HashSet<int> usedPorts = new HashSet<int> ();
			foreach ( var resPort in reservedPorts ) usedPorts.Add ( resPort );
			foreach ( var port in activePorts ) {
				int p = port.Port;
				if ( p > N | p < wantedPort ) continue;
				usedPorts.Add ( p );
			}
			if ( usedPorts.Count == 0 ) return wantedPort;
			for ( int i = wantedPort; i < N; i++ ) {
				if ( !usedPorts.Contains ( i ) ) return i;
			}
			return -1;
		}
	}
}