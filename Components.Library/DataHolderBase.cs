namespace Components.Library {
	public abstract class DataHolderBase {
		public readonly DateTime CreationTime;
		public readonly ComponentBase Owner;

		public DataHolderBase(ComponentBase owner) {
			Owner = owner;
			CreationTime = DateTime.Now;
		}
		public virtual bool FullEqCheck { get; protected set; } = true;
		public abstract DataHolderBase Clone();
		public abstract override bool Equals ( object obj );
		public abstract override int GetHashCode ();
		public abstract override string ToString ();
	}
}