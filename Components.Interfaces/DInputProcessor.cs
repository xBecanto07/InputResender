using Components.Library;

namespace Components.Interfaces {
	public abstract class DInputProcessor : ComponentBase<CoreBase> {
		public DInputProcessor ( CoreBase owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(SetupHook), typeof(void)),
				(nameof(ReleaseHook), typeof(void)),
				(nameof(ProcessInput), typeof(void))
			};

		public abstract void SetupHook ( Action callback );
		public abstract void ReleaseHook ();
		public abstract void ProcessInput ( HInputEventDataHolder[] inputCombination );
	}
}