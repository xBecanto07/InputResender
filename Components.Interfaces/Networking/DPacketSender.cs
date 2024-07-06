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
				("get_"+nameof(EPList), typeof(IReadOnlyCollection<IReadOnlyCollection<object>>)),
				("get_"+nameof(Connections), typeof(int)),
				("get_"+nameof(Errors), typeof(IReadOnlyCollection<(string msg, Exception e)>)),
				(nameof(OwnEP), typeof(object)),
				(nameof(Destroy), typeof(void)),
				(nameof(IsEPConnected), typeof(bool)),
				(nameof(IsPacketSenderConnected), typeof(bool))
			};

		public abstract IReadOnlyCollection<IReadOnlyCollection<object>> EPList { get; }
		public abstract IReadOnlyCollection<(string msg, Exception e)> Errors { get; }
		public abstract int Connections { get; }
		public abstract object OwnEP ( int TTL, int network );
		public abstract void Connect ( object ep );
		public abstract void Disconnect ( object ep );
		public abstract void Send ( byte[] data );
		/// <summary>Direct receive</summary>
		public abstract void Recv ( byte[] data );
		public abstract void ReceiveAsync ( Func<byte[], bool> callback );
		public abstract void Destroy ();
		public abstract bool IsEPConnected ( object ep );
		/// <summary>Returns true if at least one connection to any of target EPs is active</summary>
		public bool IsPacketSenderConnected (DPacketSender packetSender) {
			foreach ( var eps in packetSender.EPList )
				foreach ( var ep in eps )
					if ( IsEPConnected ( ep ) ) return true;
			return false;
		}

		public abstract class DStateInfo : StateInfo {
			protected DStateInfo ( DPacketSender owner ) : base ( owner ) {
				Connections = GetConnections ();
				Buffers = GetBuffers ();
				EPList = new string[owner.EPList.Count];
				int ID = 0;
				var SB = new System.Text.StringBuilder ();
				foreach (var eps in owner.EPList ) {
					SB.Clear ();
					int N = eps.Count;
					int sID = 0;

					foreach ( var ep in eps.Reverse () ) {
						for ( int i = 0; i < sID; i++ ) SB.Append ( "|-\t" );
						SB.AppendLine ( ep.ToString () );
					}

					EPList[ID++] = SB.ToString ();
				}

				ID = 0;
				Errors = new string[owner.Errors.Count];
				foreach ( var e in owner.Errors )
					Errors[ID++] = $"{e.msg} ({e.e.Message})";
			}
			protected abstract string[] GetConnections ();
			protected abstract string[] GetBuffers ();
			public readonly string[] Buffers;
			public readonly string[] Connections;
			public readonly string[] EPList;
			public readonly string[] Errors;
			public override string AllInfo () => $"{base.AllInfo ()}{BR}EP List:{BR}{string.Join ( BR, EPList )}{BR}Connections:{BR}{string.Join ( BR, Connections )}{BR}Buffer:{BR}{string.Join ( BR, Buffers )}{BR}Errors:{BR}{string.Join ( BR, Errors )}";
		}
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
		public override bool IsEPConnected ( object ep ) => ConnList.Contains ( ep );

		public override void Connect ( object ep ) {
			if ( ep is not MPacketSender mpSender ) throw new InvalidCastException ( $"Unexpected object type: {ep.GetType ().Name}." );
			if ( mpSender == this ) throw new InvalidOperationException ( "Cannot connect to self" );
			if ( ConnList.Contains ( mpSender ) ) throw new InvalidOperationException ( $"Already connected to {mpSender.Name}" );
			if ( mpSender.ConnList.Contains ( this ) ) throw new InvalidOperationException ( $"{mpSender.Name} is already connected to this" );

			ConnList.Add ( mpSender );
			mpSender.ConnList.Add ( this );
		}
		public override void Disconnect ( object ep ) {
			if ( ep is not MPacketSender mpSender ) throw new InvalidCastException ( $"Unexpected object type: {ep.GetType ().Name}." );
			if ( !ConnList.Remove ( mpSender ) ) throw new InvalidOperationException ( $"No active connection to {ep}" );
			if ( !mpSender.ConnList.Remove ( this ) ) throw new InvalidOperationException ( $"{mpSender.Name} is not connected to this" );
		}
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

		public override StateInfo Info => new VStateInfo ( this );
		public class VStateInfo : DStateInfo {
			public new MPacketSender Owner => (MPacketSender)base.Owner;
			public VStateInfo ( MPacketSender owner ) : base ( owner ) {
				LocalCallback = Owner.Callback.Method.AsString ();
			}

			public string LocalCallback;

			protected override string[] GetConnections () {
				int N = Owner.ConnList.Count;
				string[] ret = new string[N];
				for ( int i = 0; i < N; i++ )
					ret[i] = $"MPacketSender {{{Owner.ConnList[i].Name}}}";
				return ret;
			}
			protected override string[] GetBuffers () {
				int N = Owner.MsgQueue.Count;
				string[] ret = new string[N];
				int ID = 0;
				foreach ( var msg in Owner.MsgQueue )
					ret[ID++] = msg.ToHex ();
				return ret;
			}
			public override string AllInfo () => $"{base.AllInfo ()}{BR}Callback:{BR}{LocalCallback}";
		}
	}
}