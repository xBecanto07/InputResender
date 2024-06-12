using System;
using System.Collections.Generic;
using System.Threading;

namespace InputResender.Services.NetClientService {
	internal sealed class Connector<EPT> where EPT : class, INetPoint {

		private class Requester {
			public readonly ANetDevice<EPT> Device;
			public readonly ANetDeviceLL<EPT> DeviceLL;
			public readonly EPT Target;
			private readonly ManualResetEvent Signal;
			public readonly NetworkConnection.MessageSender Sender;
			public NetworkConnection.NetworkInfo Connection { get; private set; }

			public Requester ( ANetDevice<EPT> dev, ANetDeviceLL<EPT> devLL, EPT targ, NetworkConnection.MessageSender sender ) {
				Device = dev;
				DeviceLL = devLL;
				Target = targ;
				Sender = sender;
				Signal = new ManualResetEvent ( false );
			}
			public NetMessagePacket CreateConnRequestSignal () => NetMessagePacket.CreateSignal ( INetDevice.SignalMsgType.Connect, Device.EP, Target );

			public void Notify ( NetworkConnection.NetworkInfo conn ) {
				Connection = conn;
				Interlocked.MemoryBarrierProcessWide ();
				Signal.Set ();
			}
			public NetworkConnection.NetworkInfo Wait ( CancellationToken? ct ) {
				int res;
				if ( ct == null ) res = Signal.WaitOne () ? 0 : 1;
				else res = WaitHandle.WaitAny ( new WaitHandle[] { Signal, ct.Value.WaitHandle } );

				if ( res == 1 ) throw new OperationCanceledException ();
				return Connection;
			}
		}

		private static Dictionary<INetPoint, Requester> ActiveAttempts = new ();

		public static NetworkConnection.NetworkInfo Connect ( ANetDevice<EPT> dev, ANetDeviceLL<EPT> devLL, EPT targ, NetworkConnection.MessageSender sender, CancellationToken? ct ) {
			if ( dev == null ) throw new ArgumentNullException ( nameof ( dev ) );
			if ( targ == null ) throw new ArgumentNullException ( nameof ( targ ) );
			if ( dev.EP == null ) throw new InvalidOperationException ( "Device not bound" );
			if ( dev.EP != devLL.LocalEP ) throw new InvalidOperationException ( "Device and LL device are not bound to same EP" );

			Requester req = new Requester ( dev, devLL, targ, sender );
			lock ( ActiveAttempts ) {
				if ( ActiveAttempts.ContainsKey ( targ ) ) throw new InvalidOperationException ( "Already connecting" );
				ActiveAttempts.Add ( targ, req );
			}

			var packet = req.CreateConnRequestSignal ();

			devLL.OnReceive += OnReceive;
			devLL.Send ( packet.Data, packet.TargetEP as EPT );
			return req.Wait ( ct );
		}

		private static bool OnReceive ( NetMessagePacket msg ) {
			Requester req;
			if ( msg == null ) return false;
			if ( msg.SignalType != INetDevice.SignalMsgType.Connect ) return false;

			lock ( ActiveAttempts ) {
				if ( !ActiveAttempts.TryGetValue ( msg.SourceEP, out req ) ) return false;
				ActiveAttempts.Remove ( msg.SourceEP );
			}

			// Create connection
			var conn = NetworkConnection.Create ( req.Device, msg.SourceEP, req.Sender );
			req.Notify ( conn );
			return true;
			//req.Device.OnClosed += ( dev, ep ) => conn.Close ();
		}
	}
}