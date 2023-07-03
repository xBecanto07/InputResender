using Components.Library;
using System.Collections.ObjectModel;

namespace Components.Interfaces {
	public abstract class DPacketSender : ComponentBase<CoreBase> {
		public DPacketSender ( CoreBase owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(Connect), typeof(void)),
				(nameof(Disconnect), typeof(void)),
				(nameof(Send), typeof(void)),
				(nameof(Recv), typeof(void)),
				(nameof(ReceiveAsync), typeof(void)),
				(nameof(EPList), typeof(IReadOnlyCollection<IReadOnlyCollection<object>>)),
				(nameof(Connections), typeof(int)),
				(nameof(Errors), typeof(IReadOnlyCollection<(string msg, Exception e)>)),
				(nameof(OwnEP), typeof(object)),
				(nameof(Destroy), typeof(void))
			};

		public abstract IReadOnlyCollection<IReadOnlyCollection<object>> EPList { get; }
		public abstract IReadOnlyCollection<(string msg, Exception e)> Errors { get; }
		public abstract int Connections { get; }
		public abstract object OwnEP ( int TTL, int network );
		public abstract void Connect ( object ep );
		public abstract void Disconnect ( object ep );
		public abstract void Send ( byte[] data );
		public abstract void Recv ( byte[] data );
		public abstract void ReceiveAsync ( Func<byte[], bool> callback );
		public abstract void Destroy ();
	}

	/// <summary>Thread unsafe!</summary>
	public class MPacketSender : DPacketSender {
		List<MPacketSender> ConnList = new List<MPacketSender> ();
		Queue<byte[]> MsgQueue = new Queue<byte[]> ();
		Func<byte[], bool> Callback = null;

		public MPacketSender ( CoreBase owner ) : base ( owner ) { }

		public override int ComponentVersion => 1;
		public override MPacketSender OwnEP ( int TTL, int network ) => this;
		public override int Connections => ConnList.Count;
		public override IReadOnlyCollection<IReadOnlyCollection<MPacketSender>> EPList { get => new []{ this }.AsReadonly2D (); }
		public override IReadOnlyCollection<(string msg, Exception e)> Errors { get => new List<(string msg, Exception e)> ().AsReadOnly (); }

		public override void Connect ( object ep ) => ConnList.Add ( (MPacketSender)ep );
		public override void Disconnect ( object ep ) => ConnList.Remove ( (MPacketSender)ep );
		public override void ReceiveAsync ( Func<byte[], bool> callback ) {
			Callback = callback;
			while ( MsgQueue.Count > 0 ) {
				if ( !Callback ( MsgQueue.Dequeue () ) ) {
					// Caller does no longer want to receive
					Callback = null;
					return;
				}
			}
		}
		public override void Send ( byte[] data ) {
			foreach ( MPacketSender receiver in ConnList )
				receiver.Recv ( data );
		}
		public override void Recv ( byte[] data ) {
			if ( Callback != null ) { if ( !Callback ( data ) ) Callback = null; }
			else if ( MsgQueue.Count < 16 ) MsgQueue.Enqueue ( data );
		}

		public override void Destroy () {
			ConnList.Clear (); MsgQueue.Clear ();
			Callback = null; MsgQueue = null; ConnList = null;
		}
	}
}