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
		public static T[] Merge<T> (this T[] origAr, params T[][] otherArs) {
			if ( origAr == null ) return null;
			if ( otherArs == null ) return (T[])origAr.Clone ();
			int totSize = origAr.Length;
			int N = otherArs.Length;
			for ( int i = 0; i < N; i++ ) totSize += otherArs[i] == null ? 0 : otherArs[i].Length;
			T[] ret = new T[totSize];
			int pos = 0;
			for ( int i = 0; i < origAr.Length; i++ ) ret[pos++] = origAr[i];
			for (int a = 0; a < N; a++) {
				if ( otherArs[a] == null ) continue;
				int S = otherArs[a].Length;
				for ( int i = 0; i < S; i++ ) ret[pos++] = otherArs[a][i];
			}
			return ret;
		}

		public static long CalcHash ( this byte[] data, int start = 0, int size = -1 ) {
			if ( data == null ) return -1;
			if ( size < 0 ) size = data.Length - start;
			long ret = 0;
			for ( int i = start; i < start + size; i++ ) ret += data[i] ^ i.CalcHash ();
			return ret;
		}

		public static int CalcHash (this int num) {
			uint seed = 0x57981A3D;
			long ret = seed;
			for ( int i = 0; i < 32; i++ ) {
				seed = NextRng ();
				ret += (num & 1) > 0 ? seed : -seed;
				num >>= 1;
			}
			return (int)ret;

			uint NextRng () { return (uint)(seed * 22695477ul + 1); }
		}

		public static IReadOnlyCollection<IReadOnlyCollection<T>> AsReadonly2D<T> ( this T[][] ar) {
			int N = ar.Length;
			IReadOnlyCollection<T>[] ret = new IReadOnlyCollection<T>[N];
			for ( int i = 0; i < N; i++ ) ret[i] = Array.AsReadOnly ( ar[i] );
			return Array.AsReadOnly ( ret );
		}
		public static IReadOnlyCollection<IReadOnlyCollection<T>> AsReadonly2D<T> ( this T[] ar ) => new IReadOnlyCollection<T>[1] { Array.AsReadOnly ( ar ) };

		public static T[][] ToArray2D<T> (this List<List<T>> data) {
			int N = data.Count;
			T[][] ret = new T[N][];
			for ( int i = 0; i < N; i++ ) ret[i] = data[i].ToArray ();
			return ret;
		}
		public static string ToShortCode (this ulong num ) {
			const string chAr = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/*@#$%&?€£¤";
			ulong N = (ulong)chAr.Length;
			string ret = "";
			while (num > 0 ) {
				ret += chAr[(int)(num % N)];
				num /= N;
			}
			return ret == "" ? "0" : ret;
		}
		public static int Crop ( this int x, int min, int max ) => x < min ? min : x > max ? max : x;
	}
}