using Components.Library;

namespace Components.Interfaces {
	public abstract class DDataSigner : ComponentBase<CoreBase> {
		public DDataSigner ( CoreBase owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(Key), typeof(void)),
				(nameof(Encrypt), typeof(void)),
				(nameof(Decrypt), typeof(void))
			};

		public abstract byte[] Key { get; protected set; }
		public abstract byte[] Encrypt ( byte[] data );
		public abstract byte[] Decrypt ( byte[] data );
	}
}