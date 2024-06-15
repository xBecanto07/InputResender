using InputResender.Services;
using FluentAssertions;
using Xunit;
using System.Collections.Generic;
using InputResender.Services.NetClientService.InMemNet;
using InputResender.Services.NetClientService;
using System.Threading;

namespace InputResender.ServiceTests {
	public class NetworkConnectionHappyFlowTest {
		public static IEnumerable<object[]> GetNetPoints () {
			yield return new object[] { InMemNetPoint.NextAvailable (), InMemNetPoint.NextAvailable () };

			yield return INetPoint.NextAvailable<IPNetPoint> ( 2 );
		}

		[Fact]
		public void CreateConnectionFromInMemNetPoint ()
			=> CreateConnectionFromNetPoints<InMemNetPoint> ();

		[Fact]
		public void CreateConnectionFromIPNetPoint ()
			=> CreateConnectionFromNetPoints<IPNetPoint> ();

		private static void CreateConnectionFromNetPoints<T> () where T : INetPoint {
			var EPs = INetPoint.NextAvailable<T> ( 2 );
			// Create both devices
			EPs.Should ().NotBeNull ().And.HaveCount ( 2 );
			EPs[0].Should ().NotBeNull ();
			EPs[1].Should ().NotBeNull ();
			EPs[1].Should ().NotBeSameAs ( EPs[0] ).And.NotBe ( EPs[0] );
			EPs[0].DscName = "Sender_A";
			EPs[1].DscName = "Receiver_B";

			var devA = NetworkDeviceFactory.CreateDevice ( EPs[0] );
			devA.DeviceName = EPs[0].DscName;
			var devB = NetworkDeviceFactory.CreateDevice ( EPs[1] );
			devB.DeviceName = EPs[1].DscName;
			devA.Should ().NotBeNull ();
			devB.Should ().NotBeNull ();

			byte[] msgA = new byte[16];
			byte[] msgB = new byte[16];
			for ( int i = 0; i < 16; i++ ) {
				msgA[i] = (byte)(i + 1);
				msgB[i] = (byte)(16 - i);
			}

			CancellationTokenSource CTS = new CancellationTokenSource ();
			NetworkConnection connBA = null;

			// Allow devB to accept connections
			devB.AcceptAsync ( ( conn ) => connBA = conn, CTS.Token );

			// Request connection from devA to devB
			var connAB = devA.Connect ( EPs[1], null ); // Pass null callback since we will do synchronous receive

			devA.ActiveConnections.Should ().ContainKey ( EPs[1] ).And.HaveCount ( 1 );
			devA.ActiveConnections[EPs[1]].Should ().BeSameAs ( connAB );
			devB.ActiveConnections.Should ().ContainKey ( EPs[0] ).And.HaveCount ( 1 );

			// Send test message from devA to devB
			/* - NetworkConnection.Send (byte[])
			   - InMemDevice:INetDevice.Send (NetMessagePacket, INetPoint)
			   - NetworkConnection.AcceptMessage (NetMessagePacket) */
			connAB.Send ( msgA ).Should ().BeTrue ();
			var msgFromA = connBA.Receive (); // With non-memory devices some delay is expected here, maybe add timeout to the Receive method?
			msgFromA.Should ().NotBeNull ();
			msgFromA.Data.Should ().BeEquivalentTo ( msgA ).And.NotBeSameAs ( msgA );
			msgFromA.Error.Should ().Be ( INetDevice.NetworkError.None );
			msgFromA.SourceEP.Should ().Be ( EPs[0] );
			msgFromA.TargetEP.Should ().Be ( EPs[1] );
			msgFromA.SignalType.Should ().Be ( INetDevice.SignalMsgType.None );
			msgFromA.IsFor ( EPs[1] ).Should ().BeTrue ();
			msgFromA.IsFor ( EPs[0] ).Should ().BeFalse ();
			msgFromA.IsFrom ( EPs[0] ).Should ().BeTrue ();
			msgFromA.IsFrom ( EPs[1] ).Should ().BeFalse ();

			// Send a different test message from devB to devA
			connBA.Send ( msgB );
			var msgFromB = connAB.Receive ();
			msgFromB.Data.Should ().BeEquivalentTo ( msgB ).And.NotBeSameAs ( msgB );
			msgFromB.Error.Should ().Be ( INetDevice.NetworkError.None );
			msgFromB.SourceEP.Should ().Be ( EPs[1] );
			msgFromB.TargetEP.Should ().Be ( EPs[0] );
			msgFromB.SignalType.Should ().Be ( INetDevice.SignalMsgType.None );
			msgFromB.IsFor ( EPs[0] ).Should ().BeTrue ();
			msgFromB.IsFor ( EPs[1] ).Should ().BeFalse ();
			msgFromB.IsFrom ( EPs[1] ).Should ().BeTrue ();
			msgFromB.IsFrom ( EPs[0] ).Should ().BeFalse ();

			// Close the connection
			NetworkCloseWatcher closeWatcherAB = new ( connAB );
			NetworkCloseWatcher closeWatcherBA = new ( connBA );
			connAB.Close ();
			closeWatcherAB.Assert ();
			closeWatcherBA.Assert ();
			connAB.LocalDevice.Should ().BeNull ();
			connAB.TargetEP.Should ().BeNull ();
			connBA.LocalDevice.Should ().BeNull ();
			connBA.TargetEP.Should ().BeNull ();
			devA.ActiveConnections.Should ().BeEmpty ();
			devB.ActiveConnections.Should ().BeEmpty ();

			// Close the devices
			devA.Close ();
			devB.Close ();
			devA.EP.Should ().BeNull ();
			devB.EP.Should ().BeNull ();
		}
	}

	internal class NetworkCloseWatcher {
		public int ClosedCount { get; private set; } = 0;
		readonly INetDevice Device;
		readonly INetPoint Point;

		public NetworkCloseWatcher ( NetworkConnection conn ) {
			conn.OnClosed += OnClosed;
			Device = conn.LocalDevice;
			Point = conn.TargetEP;
		}

		private void OnClosed ( INetDevice dev, INetPoint ep ) {
			dev.Should ().BeSameAs ( Device );
			ep.Should ().BeSameAs ( Point );
			ClosedCount++;
		}

		public void Assert () {
			ClosedCount.Should ().Be ( 1 );
		}
	}
}