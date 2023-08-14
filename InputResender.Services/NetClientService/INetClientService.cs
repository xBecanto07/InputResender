using System.Net;
using System.Net.Sockets;
using SBld = System.Text.StringBuilder;
using ClientType = InputResender.Services.INetClientService.ClientType;
using System.Diagnostics;

namespace InputResender.Services {
	public abstract class INetClientService : IDisposable {
		private static List<INetClientService> RegisteredClients = new List<INetClientService> ();
		public readonly IPEndPoint EP;
		private Queue<Result> PacketBuffer;
		public int Available { get => PacketBuffer.Count; }
		public abstract ClientType ServiceType { get; }
		public readonly AutoResetEvent ReceiveWaiter;
		CancellationTokenSource cts;
		TaskService TaskCreator;
		public Action<string> LogFcn;

		public INetClientService ( IPEndPoint ep ) {
			EP = ep;
			PacketBuffer = new Queue<Result> ();
			ReceiveWaiter = new AutoResetEvent ( false );
			RegisteredClients.Add ( this );
			TaskCreator = new TaskService ( TaskCreatorAct );
			TaskCreator.Start ();
			cts = new CancellationTokenSource ();
		}
		public void Dispose () {
			Stop ();
			InnerDispose ();
			TaskCreator.Stop ();
			TaskCreator.Dispose ();
			RegisteredClients.Remove ( this );
			PacketBuffer.Clear ();
			PacketBuffer = null;
			ReceiveWaiter.Dispose ();
			cts.Dispose ();
		}

		public Task<Result> RecvAsync () => Task.Run ( WaitForRecv );
		public Result WaitForRecv () {
			do {
				lock ( PacketBuffer ) {
					if ( PacketBuffer.Count < 1 ) continue;
					if ( cts.IsCancellationRequested ) return new Result ( Result.Type.Interrupted );
					ReceiveWaiter.Reset ();
					return PacketBuffer.Dequeue ();
				}
			} while ( ReceiveWaiter.WaitOne () );
			return new Result ( Result.Type.Error ); ;
		}
		/// <summary>Send data to a given EP</summary>
		/// <param name="data">Binary data to be sent</param>
		/// <param name="ep">Target IP End point, where the data should be sent to. When null, own EP from 'this' will be used.</param>
		/// <param name="attemptDirect">When true, direct hand over of the message will be tried. That is if 'this' instance have a reference to the receiver object, the data will be than transmited by some methods callings without envolment of sockets or others.</param>
		public void Send ( byte[] data, IPEndPoint ep = null, bool attemptDirect = true ) {
			if ( ep == null ) ep = EP;
			LogFcn?.Invoke ( $"Sending data[{data.Length}] to {ep} (direct={(attemptDirect ? "allowed" : "forbidden")}){Environment.NewLine}\t{ToString ()}" );
			if ( attemptDirect ) {
				foreach ( var client in RegisteredClients ) {
					if ( !ep.Equals ( client.EP ) ) continue;
					lock ( client.PacketBuffer ) {
						client.PacketBuffer.Enqueue ( new Result ( (byte[])data.Clone () ) );
					}
					LogFcn?.Invoke ( $"Direct assigning data into ({client})'s queue as item #{client.PacketBuffer.Count}" );
					client.ReceiveWaiter.Set ();
					return;
				}
			}
			InnerSend ( data, ep );
		}

		public void Start () {
			if ( PacketBuffer == null ) throw new ObjectDisposedException ( nameof ( INetClientService ) );
			TaskCreator.Signal ( false );
		}
		public void Stop () {
			cts.Cancel ();
		}
		protected abstract Result InnerRecv ( CancellationToken ct );
		protected abstract void InnerDispose ();
		protected abstract void InnerSend ( byte[] data, IPEndPoint EP );

		public override string ToString () => $"NetClient ({ServiceType}) on {EP} with {Available} packets.";

		private void TaskCreatorAct (TaskService context, ManualResetEvent initWaiter ) {
			context.State = "Starting";
			while (true) {
				initWaiter.Set ();
				context.State = "Waiting for signal";
				context.WaitSignal ( false );
				if (context.ShouldStop) {
					context.State = "Stopping after CTS stop request";
					return;
				}

				context.State = "Waiting for receive";
				Result data = InnerRecv ( cts.Token );
				if ( cts.IsCancellationRequested ) break;
				PushResult ( data );

				context.Signal ( true );
			}
		}
		private void PushResult ( Result data ) {
			switch ( data.ResultType ) {
			case Result.Type.Received:
			case Result.Type.Direct:
				lock ( PacketBuffer ) {
					data.PushTrace ( $"Received data ({data.ResultType})" );
					PacketBuffer.Enqueue ( data );
					ReceiveWaiter.Set ();
				}
				break;
			case Result.Type.Interrupted:
				break;
			case Result.Type.Closed:
				break;
			default: break;
			}
		}

		public struct Result {
			public static int MsgCnt = 0;
			public int MsgID;
			public enum Type { Received, Interrupted, Closed, Direct, Error }
			public Type ResultType;
			public List<(string, StackTrace)> Origin;
			public byte[] Data;
			public Result ( byte[] data, bool isDirect = false ) { Data = data; ResultType = isDirect ? Type.Direct : Type.Received; MsgID = MsgCnt++; Origin = new List<(string, StackTrace)> (); PushTrace ( "Creating valid result" ); }
			public Result ( Type errType ) { Data = null; ResultType = errType; MsgID = MsgCnt++; Origin = new List<(string, StackTrace)> (); PushTrace ( $"Creating error ({errType}) result" ); }
			public void PushTrace ( string context ) => Origin?.Add ( (context, new StackTrace ()) );
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

		public UDPClientService ( IPEndPoint ep ) : base ( ep ) {
			UdpClient = new UdpClient ( ep );
		}

		protected override void InnerDispose () {
			Stop ();
			UdpClient.Dispose ();
			UdpClient = null;
		}
		protected override Result InnerRecv ( CancellationToken ct ) {
			try {
				var recvResult = UdpClient.ReceiveAsync ( ct ).GetAwaiter ().GetResult ();

				if ( recvResult.Buffer != null ) {
					return new Result ( recvResult.Buffer );
				} else if ( ct.IsCancellationRequested ) return new Result ( Result.Type.Closed );
			} catch ( Exception e ) {
				if ( ct.IsCancellationRequested ) return new Result ( Result.Type.Closed );
				else if ( e.Message.Contains ( "WSACancelBlockingCall" ) ) return new Result ( Result.Type.Interrupted );
				else return new Result ( Result.Type.Error );
			}
			return new Result ( Result.Type.Error );
		}

		protected override void InnerSend ( byte[] data, IPEndPoint EP ) {
			LogFcn?.Invoke ( $"Sending data[{data.Length}] over socket to {EP}" );
			UdpClient.Send ( data, data.Length, EP );
		}
	}

	public class TCPClientService : INetClientService {
		public override ClientType ServiceType => ClientType.Unknown;
		public TCPClientService ( IPEndPoint ep ) : base ( ep ) {
		}

		protected override void InnerDispose () => throw new NotImplementedException ();
		protected override Result InnerRecv ( CancellationToken ct ) => throw new NotImplementedException ();
		protected override void InnerSend ( byte[] data, IPEndPoint EP ) => throw new NotImplementedException ();
	}

	public class NetClientList : IDisposable {
		private readonly List<INetClientService> ClientList;
		private readonly List<Task<INetClientService.Result>> RecvList;
		public int Count { get => ClientList.Count; }
		public bool Active { get; private set; }
		private AutoResetEvent Signal;
		private INetClientService.Result DirectMessage;

		public NetClientList () {
			Active = false;
			Signal = new AutoResetEvent ( false );
			ClientList = new List<INetClientService> ();
			RecvList = new List<Task<INetClientService.Result>> ();
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
		public (Task<INetClientService.Result> task, INetClientService client) WaitAny () {
			int N = ClientList.Count;
			for (int i = 0; i < N; i++ ) {
				if ( RecvList[i] == null ) RecvList[i] = ClientList[i].RecvAsync ();
			}



			List<Task<INetClientService.Result>> tasks = new( RecvList ) {
				Task.Run ( () => {
					while ( true ) {
						Signal.WaitOne ();
						if ( DirectMessage.ResultType == INetClientService.Result.Type.Direct )
							return DirectMessage;
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