using Components.Library;

namespace Components.Interfaces {
	public abstract class DEventVector : ComponentBase<CoreBase> {
		public DEventVector ( CoreBase owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(SetupCallback), typeof(void)),
				(nameof(ReleaseCallback), typeof(void)),
				(nameof(RaiseEvent), typeof(void))
			};

		public abstract void SetupCallback ( string eventName, Action callback );
		public abstract void ReleaseCallback ( string eventName );
		public abstract void RaiseEvent ( string eventName );

		public abstract class DStateInfo : StateInfo {
			public DStateInfo ( DEventVector owner ) : base ( owner ) {
				Callbacks = PrintCallbacks ();
			}
			public readonly string[] Callbacks;
			protected abstract string[] PrintCallbacks ();
			public override string AllInfo () => $"{base.AllInfo ()}{BR}Callbacks:{BR}{string.Join ( BR, Callbacks )}";
		}
	}

	public class MEventVector : DEventVector {
		Dictionary<string, Action> events;

		public MEventVector ( CoreBase owner ) : base ( owner ) { events = new Dictionary<string, Action> (); }
		public override int ComponentVersion => 1;

		public override void RaiseEvent ( string eventName ) { if ( events.TryGetValue ( eventName, out var fcn ) ) fcn (); }
		public override void ReleaseCallback ( string eventName ) => events.Remove ( eventName );
		public override void SetupCallback ( string eventName, Action callback ) => events.Add ( eventName, callback );

		public override StateInfo Info => new VStateInfo ( this );
		public class VStateInfo : DStateInfo {
			public VStateInfo ( MEventVector owner ) : base ( owner ) { }

			protected override string[] PrintCallbacks () {
				var targ = (MEventVector)Owner;
				int N = targ.events.Count;
				string[] ret = new string[N];
				int ID = 0;
				foreach ( var e in targ.events )
					ret[ID++] = $"{e.Key} => {e.Value.Method.AsString}";
				return ret;
			}
		}
	}
}