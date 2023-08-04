using Components.Library;
using static Components.Interfaces.InputData;

namespace Components.Interfaces {
	public abstract class DTextWriter : ComponentBase<CoreBase> {
		protected DTextWriter ( CoreBase newOwner ) : base ( newOwner ) { }

		protected override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(Text), typeof(string)),
				(nameof(Type), typeof(bool)),
				(nameof(Undo), typeof(void)),
				(nameof(Redo), typeof(void)),
				(nameof(Clear), typeof(void)),
			};

		public abstract string Text { get; }
		public abstract bool Type ( InputData input );
		public abstract void Undo ();
		public abstract void Redo ();
		public abstract void Clear ();
	}
}
