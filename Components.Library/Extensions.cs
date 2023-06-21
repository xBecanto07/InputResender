using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SBld = System.Text.StringBuilder;

namespace Components.Library {
	public static class Extensions {
		public const int K = 1000;
		public const int M = K * K;
		public const int G = M * K;
		public static string ToShortString ( this int val ) {
			switch ( val ) {
			case 0: return "0";
			case 1: return "1";
			case int.MaxValue: return "2^32";
			case int.MinValue: return "-2^32";
			default:
				string sgn = val < 0 ? "-" : "";
				val = Math.Abs ( val );
				if ( val > G ) return $"{sgn}{val / G}G";
				if ( val > M ) return $"{sgn}{val / M}M";
				if ( val > K ) return $"{sgn}{val / K}K";
				return val.ToString ();
			}
		}
		public static int CalcSetHash<T> (this ICollection<T> Set) {
			int N = Set.Count;
			int hash = N.GetHashCode ();
			foreach ( T t in Set ) hash ^= t.GetHashCode ();
			return hash;
		}
		public static string AsString<T> (this ICollection<T> Set, string sep = ", ") {
			if (Set == null) return null;
			bool empty = true;
			SBld SB = new SBld ();
			foreach ( T t in Set ) {
				if ( !empty ) SB.Append ( sep );
				SB.Append ( t.ToString () );
			}
			return SB.ToString ();
		}
		public static IntPtr ToUnmanaged (this IntPtr value) {
			var ptr = Marshal.AllocHGlobal ( IntPtr.Size );
			var bAr = BitConverter.GetBytes ( value );
			Marshal.Copy ( bAr, 0, ptr, bAr.Length );
			return ptr;
		}
		public static bool IsModifier (this KeyCode key) => (int)(key & KeyCode.Modifiers) > 1;
		public static bool TryGetValue<T,U> (this Dictionary<T, U> dict, Func<T, bool> predicate, out U val) {
			foreach ( var item in dict ) {
				if ( !predicate ( item.Key ) ) continue;
				val = item.Value;
				return true;
			}
			val = default;
			return false;
		}
	}
}