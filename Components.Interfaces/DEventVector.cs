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
	}
}