using Components.Library;
using ModE = Components.Interfaces.InputData.Modifier;

namespace Components.Interfaces {
	public abstract class DShortcutWorker : ComponentBase<CoreBase> {
		protected struct ShortcutInfo {
			public string Description;
			public Action Action;
			public KeyCode Key;
			public ModE Modifier;
			public override string ToString () => $"{Modifier}+{Key}:{Description}";
		}

		public DShortcutWorker ( CoreBase newOwner ) : base ( newOwner ) {
		}

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(Exec), typeof(bool)),
				(nameof(Register), typeof(void)),
				(nameof(Unregister), typeof(void))
			};

		public abstract bool Exec ( InputData inputData );
		public abstract void Register ( KeyCode key, ModE mod, Action callback, string description );
		public abstract void Unregister ( KeyCode key, ModE mod, Action callback );
	}
}
