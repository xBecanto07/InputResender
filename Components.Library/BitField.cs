using System.Collections;
using System.Drawing;
using Num = System.UInt64;

namespace Components.Library {
	public struct BitField {
		public const int Width = sizeof ( Num ) * 8;
		Num Data;

		public bool this[int ID] {
			get {
				if ( (ID >= Width) | (ID < 0) ) throw new IndexOutOfRangeException ();
				return (Data & (1UL << ID)) > 0;
			}
			set {
				if ( (ID >= Width) | (ID < 0) ) throw new IndexOutOfRangeException ();
				if ( value ) Data |= 1UL << ID;
				else Data &= ~(1UL << ID);
			}
		}

		public void Reset () => Data = 0;
		public static implicit operator Num ( BitField val ) => val.Data;
		public void Not ( int ID ) => Data ^= 1UL << ID;
		public int Count (bool val ) {
			int ret = 0;
			for (int i = 0; i < Width;i++) {
				ret += ((Data >> i) & 1UL) == (val ? 1UL : 0) ? 1 : 0;
			}
			return ret;
		}
	}
}