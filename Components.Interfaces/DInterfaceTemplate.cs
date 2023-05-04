using Components.Library;

namespace Components.Interfaces {
	public abstract class DInterfaceTemplate : ComponentBase<CoreBase> {
		public DInterfaceTemplate ( CoreBase owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(SomeMethod), typeof(void))
			};

		public abstract void SomeMethod ( int param );
	}
}