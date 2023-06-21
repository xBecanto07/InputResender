using Components.Library;

namespace Components.Interfaces {
	public abstract class DInputParser : ComponentBase<CoreBase> {
		public DInputParser ( CoreBase owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(ProcessInput), typeof(HInputEventDataHolder[])),
				(nameof(ClearMemory), typeof(void)),
				(nameof(MemoryCount), typeof(int))
			};

		public abstract int MemoryCount { get; }

		/// <summary>Combine relevant inputs into groups with newest and most relevant input first followed by 'modifiers'.</summary>
		public abstract HInputEventDataHolder[] ProcessInput ( HInputEventDataHolder inputData );
		public abstract void ClearMemory ();
	}

	public class MInputParser : DInputParser {
		public MInputParser ( CoreBase owner ) : base ( owner ) {
		}

		public override int ComponentVersion => 1;
		public override int MemoryCount => 0;

		public override void ClearMemory () { }
		public override HInputEventDataHolder[] ProcessInput ( HInputEventDataHolder inputData ) => new HInputEventDataHolder[1] { inputData };
	}
}