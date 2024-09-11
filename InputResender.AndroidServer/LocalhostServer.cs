using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Components.Library;

namespace InputResender.AndroidServer; 
internal class LocalhostServer {
	Task listener;
	UdpClient udpClient;
	IPEndPoint localEP;
	readonly FrontPage Owner;

	public LocalhostServer ( FrontPage owner ) {
		Owner = owner;
		listener = new Task ( Worker );
		listener.Start ();
	}

	private void Worker () {
		while (true) {
			for ( int attempt = 0; udpClient == null && attempt <= 10; attempt++ ) {
				try {
					localEP = new IPEndPoint ( IPAddress.Loopback, 40500 + 10 * attempt );
					udpClient = new UdpClient ( localEP );
					Owner.console.WriteLine ( $"Bound to port {40500 + 10 * attempt} on address {localEP.Address}" );
					break;
				} catch ( Exception e ) {
					Owner.console.WriteLine ( $"Failed to bind to port {40500 + 10 * attempt}: {e.Message}" );
					if ( attempt == 10 ) {
						Owner.console.WriteLine ( "Failed to bind to any port. Exiting." );
						return;
					}
				}
			}

			byte[] data;
			IPEndPoint remoteEP = new ( IPAddress.Loopback, 0 );
			try {
				data = udpClient.Receive ( ref remoteEP );
			} catch ( Exception e ) {
				Owner.console.WriteLine ( $"Failed to receive data: {e.Message}" );
				continue;
			}

			if (data == null) { Owner.console.WriteLine ( "Received null data" ); continue; }

			try {
				byte[] ACK = [0x06];
				udpClient.Send ( ACK, ACK.Length, remoteEP );
			} catch ( Exception e ) {
				Owner.console.WriteLine ( $"Failed to send ACK: {e.Message}" );
			}

			if (data.Length == 1 && data[0] == 0x05) {
				Owner.console.WriteLine ( $"New connection from {remoteEP}" );
				continue;
			}

			string line = System.Text.Encoding.UTF8.GetString ( data );
			string ret = Owner.ExecLine ( line );

			byte[] response = System.Text.Encoding.UTF8.GetBytes ( ret );
			try {
				udpClient.Send ( response, response.Length, remoteEP );
			} catch ( Exception e ) {
				Owner.console.WriteLine ( $"Failed to send response: {e.Message}" );
			}
		}
	}
}