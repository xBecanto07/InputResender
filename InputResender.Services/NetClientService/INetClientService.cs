using System.Net;
using System.Net.Sockets;
using SBld = System.Text.StringBuilder;
using ClientType = InputResender.Services.INetClientService.ClientType;

namespace InputResender.Services {
	public abstract class INetClientService : IDisposable {
		private static List<INetClientService> RegisteredClients = new List<INetClientService> ();
		public readonly IPEndPoint EP;
		public Task<Result> ActTask { get; private set; }
		protected Task<Result> ActInternTask;
		private Queue<byte[]> PacketBuffer;
		public int Available { get => PacketBuffer.Count; }
		public abstract ClientType ServiceType { get; }
		public readonly ManualResetEvent ReceiveWaiter;
		protected bool ShouldStop;

		public INetClientService ( IPEndPoint ep ) {
			EP = ep;
			PacketBuffer = new Queue<byte[]> ();
			ReceiveWaiter = new ManualResetEvent ( false );
			RegisteredClients.Add ( this );
		}

		public Task<byte[]> RecvAsync () => Task.Run ( WaitForRecv );
		public byte[] WaitForRecv () {
			do {
				lock ( PacketBuffer ) {
					if ( PacketBuffer.Count < 1 ) continue;
					if ( ShouldStop ) return null;
					ReceiveWaiter.Reset ();
					return PacketBuffer.Dequeue ();
				}
			} while ( ReceiveWaiter.WaitOne () );
			return null;
		}
		/// <summary>Send data to a given EP</summary>
		/// <param name="data">Binary data to be sent</param>
		/// <param name="ep">Target IP End point, where the data should be sent to. When null, own EP from 'this' will be used.</param>
		/// <param name="attemptDirect">When true, direct hand over of the message will be tried. That is if 'this' instance have a reference to the receiver object, the data will be than transmited by some methods callings without envolment of sockets or others.</param>
		public void Send ( byte[] data, IPEndPoint ep = null, bool attemptDirect = true ) {
			if ( ep == null ) ep = EP;
			if ( attemptDirect ) {
				foreach ( var client in RegisteredClients ) {
					if ( ep != client.EP ) continue;
					lock ( client.PacketBuffer ) {
						client.PacketBuffer.Enqueue ( (byte[])data.Clone () );
					}
					client.ReceiveWaiter.Set ();
					return;
				}
			}
			InnerSend ( data, ep );
		}
		public void Dispose () {
			Stop ();
			InnerDispose ( );
			RegisteredClients.Remove ( this );
			PacketBuffer.Clear ();
			PacketBuffer = null;
			ReceiveWaiter.Dispose ();
		}

		protected abstract Task<Result> InnerRecv ( ManualResetEvent prepared );
		public void Start () {
			var prepared = new ManualResetEvent ( false );
			InnerStart ( prepared );
			ActTask = RecvTask ( prepared );
			prepared.WaitOne ();
		}
		public void Stop () {
			InnerStop ();
			ActTask?.Wait ();
		}
		public abstract void InnerStart ( ManualResetEvent prepared );
		public abstract void InnerStop ();
		protected abstract void InnerDispose ();
		protected abstract void InnerSend ( byte[] data, IPEndPoint EP );

		public override string ToString () => $"NetClient ({ServiceType}) {((ActTask == null || ActTask.IsCompleted) ? "waiting" : "listening")} on {EP} with {Available} packets.";

		private Task<Result> RecvTask ( ManualResetEvent prepared ) {
			return Task.Run ( () => {
				if ( ActInternTask == null ) {
					ActInternTask = InnerRecv ( prepared );
				}
				var ret = ActInternTask.Result;
				ActInternTask = null;
				ActTask = RecvTask ( null );
				switch ( ret.ResultType ) {
				case Result.Type.Received:
				case Result.Type.Direct:
					lock ( PacketBuffer ) {
						PacketBuffer.Enqueue ( ret.Data );
						ReceiveWaiter.Set ();
					}
					break;
				case Result.Type.Interrupted:
					break;
				case Result.Type.Closed:
					break;
				default: break;
				}
				return ret;
			} );
		}

		public struct Result {
			public static int MsgCnt = 0;
			public int MsgID;
			public enum Type { Received, Interrupted, Closed, Direct, Error }
			public Type ResultType;
			public byte[] Data;
			public Result ( byte[] data, bool isDirect = false ) { Data = data; ResultType = isDirect ? Type.Direct : Type.Received; MsgID = MsgCnt++; }
			public Result (Type errType) { Data = null; ResultType = errType; MsgID = MsgCnt++; }
		}

		public enum ClientType { Unknown, UDP }
		public static INetClientService Create ( ClientType type, IPEndPoint EP ) {
			switch ( type ) {
			case ClientType.UDP: return new UDPClientService ( EP );
			default: return null;
			}
		}
	}

	public class UDPClientService : INetClientService {
		public override ClientType ServiceType => ClientType.UDP;
		protected UdpClient UdpClient;
		CancellationTokenSource cts;

		public UDPClientService ( IPEndPoint ep ) : base ( ep ) {
			UdpClient = new UdpClient ( ep );
			cts = new CancellationTokenSource ();
		}

		public override void InnerStart ( ManualResetEvent prepared ) {
			if ( ActInternTask != null && !ActInternTask.IsCompleted ) return;
			cts.TryReset ();
			ActInternTask = InnerRecv ( prepared );
		}
		public override void InnerStop () {
			if ( ActInternTask == null || ActInternTask.IsCompleted ) return;
			cts.Cancel ();
			ActInternTask?.Wait ();
			ActInternTask?.Dispose ();
			cts.TryReset ();
		}
		protected override void InnerDispose () {
			Stop ();
			UdpClient.Dispose ();
			cts.Dispose ();
			UdpClient = null;
			ActInternTask = null;
		}
		protected override Task<Result> InnerRecv ( ManualResetEvent prepared ) {
			if ( ActInternTask != null && !ActInternTask.IsCompleted ) return ActInternTask;
			return Task.Run ( () => {
				try {
					prepared?.Set ();
					var recvResult = UdpClient.ReceiveAsync ( cts.Token ).GetAwaiter ().GetResult ();
					
					if ( recvResult.Buffer != null ) {
						return new Result ( recvResult.Buffer );
					}
				} catch ( Exception e ) {
					if ( cts.IsCancellationRequested ) return new Result ( Result.Type.Closed );
					else if ( e.Message.Contains ( "WSACancelBlockingCall" ) ) return new Result ( Result.Type.Interrupted );
					else return new Result ( Result.Type.Error );
				}
				return new Result ( Result.Type.Error );
			} );
		}

		protected override void InnerSend ( byte[] data, IPEndPoint EP ) {
			UdpClient.Send ( data, data.Length, EP );
		}
	}

	public class TCPClientService : INetClientService {
		public override ClientType ServiceType => ClientType.Unknown;
		public TCPClientService ( IPEndPoint ep ) : base ( ep ) {
		}

		public override void InnerStart ( ManualResetEvent prepared ) => throw new NotImplementedException ();
		public override void InnerStop () => throw new NotImplementedException ();
		protected override void InnerDispose () => throw new NotImplementedException ();
		protected override Task<Result> InnerRecv (ManualResetEvent prepared ) => throw new NotImplementedException ();
		protected override void InnerSend ( byte[] data, IPEndPoint EP ) => throw new NotImplementedException ();
	}

	public class NetClientList : IDisposable {
		private readonly List<INetClientService> ClientList;
		private readonly List<Task<byte[]>> RecvList;
		public int Count { get => ClientList.Count; }
		public bool Active { get; private set; }
		private AutoResetEvent Signal;
		private INetClientService.Result DirectMessage;

		public NetClientList () {
			Active = false;
			Signal = new AutoResetEvent ( false );
			ClientList = new List<INetClientService> ();
			RecvList = new List<Task<byte[]>> ();
		}

		public INetClientService Add ( IPEndPoint ep, ClientType type = ClientType.Unknown ) {
			if ( type == ClientType.Unknown ) type = ClientType.UDP;
			var client = INetClientService.Create ( type, ep );
			if ( client == null ) throw new NotSupportedException ( $"Client type {type} seems to not be supported by the service factory!" );
			ClientList.Add ( client );
			RecvList.Add ( null );
			if ( Active ) client.Start ();
			Direct ( new INetClientService.Result () { ResultType = INetClientService.Result.Type.Interrupted } );
			return client;
		}
		public bool Remove ( IPEndPoint ep, ClientType type = ClientType.Unknown ) {
			if ( type == ClientType.Unknown ) type = ClientType.UDP;
			int N = ClientList.Count;
			for ( int i = 0; i < N; i++ ) {
				if ( ClientList[i].EP != ep ) continue;
				if ( ClientList[i].ServiceType != type ) continue;
				ClientList[i].Dispose ();
				ClientList.RemoveAt ( i );
				RecvList[i].Dispose ();
				RecvList.RemoveAt ( i );
				Direct ( new INetClientService.Result () { ResultType = INetClientService.Result.Type.Interrupted } );
				return true;
			}
			return false;
		}
		public (Task<byte[]> task, INetClientService client) WaitAny () {
			int N = ClientList.Count;
			for (int i = 0; i < N; i++ ) {
				if ( RecvList[i] == null ) RecvList[i] = ClientList[i].RecvAsync ();
			}



			List<Task<byte[]>> tasks = new( RecvList ) {
				Task.Run ( () => {
					while ( true ) {
						Signal.WaitOne ();
						if ( DirectMessage.ResultType == INetClientService.Result.Type.Direct )
							return DirectMessage.Data;
					}
				} )
			};
			int ID = Task.WaitAny ( tasks.ToArray () );
			if ( ID == N ) return (tasks[ID], null);
			RecvList[ID] = null;
			return (tasks[ID], ClientList[ID]);
		}
		public void Interrupt () { foreach ( var client in ClientList ) client.Stop (); Active = false; }
		public void Start () { foreach ( var client in ClientList ) client.Start (); Active = true; }
		public void Direct ( byte[] data ) => Direct ( new INetClientService.Result ( data, true ) );
		public void Direct ( INetClientService.Result data) {
			DirectMessage = data;
			Thread.MemoryBarrier ();
			Signal.Set ();
		}
		public void Dispose () {
			Active = false;
			foreach ( var client in ClientList ) client.Dispose ();
			foreach ( var task in RecvList ) task.Dispose ();
			ClientList.Clear ();
			RecvList.Clear ();
		}
		public override string ToString () {
			SBld SB = new SBld ();
			SB.AppendLine ( $"NetClient List [{Count}]: {(Active ? "Active" : "Stopped")}" );
			for ( int i = 0; i < ClientList.Count; i++ )
				SB.AppendLine ( $"  {i} : {ClientList[i]}" );
			return SB.ToString ();
		}
		public void Foreach (Action<INetClientService> act) => ClientList.ForEach ( act );
	}
}