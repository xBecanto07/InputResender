namespace Components.Library {
	public class DropOutStack<T> {
		readonly int N;
		T[] Data;
		int Size;

		public DropOutStack(int maxSize) { N = maxSize; Data = new T[N]; Size = 0; }

		public int Count => Size;

		public void Push ( T item ) { Data[Size] = item; Size = (Size + 1) % N; }
		public T Pop () { if ( Size < 1 ) throw new IndexOutOfRangeException (); Size = (Size - 1) % N; return Data[Size]; }
		public void Clear () { Size = 0; }
		public T SafePop () { if ( Size < 1 ) return default; Size = (Size - 1) % N; return Data[Size]; }
	}
}
