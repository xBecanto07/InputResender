using Components.Library;

namespace Components.Interfaces {
	public abstract class DInputSimulator : ComponentBase<CoreBase> {
		public bool AllowRecapture { get; set; } = false;

		protected DInputSimulator ( CoreBase newOwner ) : base ( newOwner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(ParseCommand), typeof(HInputEventDataHolder[])),
				(nameof(Simulate), typeof(int)),
				("get_"+nameof(AllowRecapture), typeof(bool)),
				("set_"+nameof(AllowRecapture), typeof(void)),
			};

		public abstract HInputEventDataHolder[] ParseCommand ( InputData data );
		public abstract int Simulate ( params HInputEventDataHolder[] data );
	}

	public abstract class SDInputCommandParser : SubComponentBase<DInputSimulator, InputData.Command> {
		protected SDInputCommandParser ( DInputSimulator newOwner ) : base ( newOwner ) { }
		public abstract HInputEventDataHolder[] Parse ( InputData data );
	}
}