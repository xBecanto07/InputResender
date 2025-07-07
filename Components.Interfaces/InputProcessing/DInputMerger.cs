using Components.Library;

namespace Components.Interfaces {
	public abstract class DInputMerger : ComponentBase<CoreBase> {
		public DInputMerger ( CoreBase owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(ProcessInput), typeof(HInputEventDataHolder[])),
				(nameof(ClearMemory), typeof(void)),
				(nameof(MemoryCount), typeof(int))
			};

		public abstract int MemoryCount { get; }

		/// <summary>Combine relevant inputs into groups with newest and most relevant input first followed by 'modifiers'.</summary>
		public abstract HInputEventDataHolder[] ProcessInput ( HInputEventDataHolder inputData );
		public abstract void ClearMemory ();

		public abstract class DStateInfo : StateInfo {
			protected DStateInfo ( DInputMerger owner ) : base ( owner ) {
				BufferedEvents = GetBufferedEvents ();
			}
			public readonly string[] BufferedEvents;
			protected abstract string[] GetBufferedEvents ();
			public override string AllInfo () => $"{base.AllInfo ()}{BR}Buffer:{BR}{string.Join ( BR, BufferedEvents )}";
		}
	}

	public class MInputMerger : DInputMerger {
		public MInputMerger ( CoreBase owner ) : base ( owner ) {
		}

		public override int ComponentVersion => 1;
		public override int MemoryCount => 0;

		public override void ClearMemory () { }
		public override HInputEventDataHolder[] ProcessInput ( HInputEventDataHolder inputData ) => new HInputEventDataHolder[1] { inputData };

		public override StateInfo Info => throw new NotImplementedException ();
		public class VStateInfo : DStateInfo {
			public VStateInfo ( MInputMerger owner ) : base ( owner ) { }

			protected override string[] GetBufferedEvents () => new string[0];
		}
	}
}