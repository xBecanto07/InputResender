using Components.Library;

namespace Components.Interfaces {
	public abstract class DDataSerializer<T> : ComponentBase<CoreBase> {
		public DDataSerializer ( CoreBase owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(Serialize), typeof(byte[])),
				(nameof(Deserialize), typeof(T))
			};

		public abstract byte[] Serialize ( T obj );
		public abstract T Deserialize ( byte[] data );
	}
}