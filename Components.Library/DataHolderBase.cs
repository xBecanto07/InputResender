namespace Components.Library {
	public abstract class DataHolderBase {
		public readonly DateTime CreationTime;
		public readonly ComponentBase Owner;

		public DataHolderBase ( ComponentBase owner ) {
			Owner = owner;
			CreationTime = DateTime.Now;
		}
		public virtual bool FullEqCheck { get; protected set; } = true;
		public virtual bool IsSerializable { get; } = false;
		public abstract DataHolderBase Clone ();
		public T Clone<T> () where T : DataHolderBase => (T)Clone ();
		public abstract override bool Equals ( object obj );
		public abstract override int GetHashCode ();
		public abstract override string ToString ();
	}

	public abstract class SerializableDataHolderBase : DataHolderBase {
		public override bool IsSerializable => true;
		public SerializableDataHolderBase ( ComponentBase owner ) : base ( owner ) { }
		public abstract byte[] Serialize ();
		public abstract SerializableDataHolderBase Deserialize ( byte[] Data );
	}
}