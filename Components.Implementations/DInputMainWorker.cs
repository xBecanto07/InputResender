using Components.Interfaces;
using Components.Library;

namespace Components.Implementations {
	public abstract class DInputMainWorker : ComponentBase<MainAppCore> {
		public DInputMainWorker ( MainAppCore owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () {
			var ret = new List<(string opCode, Type opType)> () {
				(nameof(Start), typeof(void)),
				(nameof(Stop), typeof(void)),
				(nameof(ProcessInput), typeof(bool))
			};
			return ret;
		}

		public abstract void Start ();
		public abstract void Stop ();
		public abstract bool ProcessInput ( HInputEventDataHolder inputData );
		public abstract void ExecuteAction ();
	}

	public class VInputMainWorker : DInputMainWorker {
		readonly HHookInfo HookInfo;

		public VInputMainWorker ( MainAppCore owner ) : base ( owner ) {
			HookInfo = new HHookInfo ( this, 0, VKChange.KeyDown, VKChange.KeyUp );
		}

		public override int ComponentVersion => 1;

		public override void Start () {
			Owner.InputReader.SetupHook ( HookInfo, ProcessInput );
			Owner.InputProcessor.SetupHook ( ExecuteAction );
		}
		public override void Stop () {
			Owner.InputReader.ReleaseHook ( HookInfo );
			Owner.InputProcessor.ReleaseHook ();
		}
		public override bool ProcessInput (HInputEventDataHolder inputData) {
			var inputCombination = Owner.InputParser.ProcessInput ( inputData );
			Owner.InputProcessor.ProcessInput ( inputCombination );
			return false;
		}
		public override void ExecuteAction () {

		}
	}
}