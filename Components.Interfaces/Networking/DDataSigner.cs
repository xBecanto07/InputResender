using Components.Library;

namespace Components.Interfaces
{
    public abstract class DDataSigner : ComponentBase<CoreBase> {
        public DDataSigner ( CoreBase owner ) : base ( owner ) { }

        protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
                (nameof(KeySize), typeof(int)),
				(nameof(ChecksumSize), typeof(int)),
				(nameof(Key), typeof(byte[])),
				(nameof(Key), typeof(void)),
				(nameof(Encrypt), typeof(byte[])),
                (nameof(Decrypt), typeof(byte[])),
				(nameof(TestPsswd), typeof(bool)),
				(nameof(TestIntegrity), typeof(bool)),
				(nameof(GenerateIV), typeof(byte[]))
			};

        public abstract int KeySize { get; }
        public abstract int ChecksumSize { get; }
        public abstract byte[] Key { get; set; }
        public abstract byte[] Encrypt ( byte[] data, byte[] IV );
        public abstract byte[] Decrypt ( byte[] data, byte[] IV );
        public abstract bool TestPsswd ( byte[] data, byte[] IV );
        public abstract bool TestIntegrity ( byte[] data );
        /// <summary>Passing <see langword="null"/> generates IV by random. Generated IV should also be usable as a key.</summary>
        public abstract byte[] GenerateIV ( byte[] data = null );
	}

	public class MDataSigner : DDataSigner {
		public MDataSigner ( CoreBase owner ) : base ( owner ) { }

		public override int KeySize { get => sizeof ( int ); }
		public override byte[] Key { get; set; }
		public override int ComponentVersion => 1;
		public override int ChecksumSize { get => sizeof ( ushort ) * 2; }

		public override byte[] GenerateIV ( byte[] data = null ) {
			if ( data == null ) return BitConverter.GetBytes ( Random.Shared.Next () );
			if ( data.Length == KeySize ) return (byte[])data.Clone ();
			int ret = data.Length.CalcHash ();
			for ( int i = 0; i < data.Length; i++ ) ret ^= ((int)data[i]).CalcHash ();
			return BitConverter.GetBytes ( ret );
		}
		public override byte[] Decrypt ( byte[] data, byte[] IV ) {
            // Data size should be checked in integrity test
            if ( !TestIntegrity ( data ) ) return null;
            if ( !TestPsswd ( data, IV ) ) return null;
			byte[] ret = new byte[data.Length - ChecksumSize];
            CryptFunc ( data, IV, ( val, i ) => ret[i] = val, ChecksumSize );
            return ret;

		}
		public override byte[] Encrypt ( byte[] data, byte[] IV ) {
            if ( data == null ) return null;
            byte[] ret = new byte[data.Length + ChecksumSize];
			long hash = 0;
			CryptFunc ( data, IV, ( val, i ) => { ret[i + ChecksumSize] = val; hash += val; } );
			PushNumber ( ret, (ushort)Key.Merge ( IV ).CalcHash (), 2 );
            PushNumber ( ret, (ushort)ret.CalcHash ( 2 ), 0 );
			return ret;
        }
		public override bool TestPsswd ( byte[] data, byte[] IV ) {
			ushort calcHash = (ushort)Key.Merge ( IV ).CalcHash ();
			ushort locHash = (ushort)ParseNum ( data, ChecksumSize / 2, ChecksumSize / 2 );
            return calcHash == locHash;
		}
		public override bool TestIntegrity ( byte[] data ) {
            if ( data == null ) return false;
            int N = data.Length;
            int ChckN = ChecksumSize / 2;
            if (N < ChckN + 1 ) return false;
            return (ushort)ParseNum ( data, 0, ChckN ) - (ushort)data.CalcHash ( ChckN ) == 0;
        }

        protected void CryptFunc ( byte[] data, byte[] IV, Action<byte, int> act, int dataStart = 0 ) {
            if ( dataStart < 0 ) dataStart = ChecksumSize;
			int pssN = Key.Length;
            for ( int i = dataStart, j = 0; i < data.Length; i++, j++ ) act ( (byte)(data[i] ^ Key[j % pssN] ^ IV[j % pssN]), j );
		}

		public static long ParseNum ( byte[] data, int start, int size = -1 ) {
			if ( size < 0 ) size = data.Length - start;
			long ret = 0;
			for ( int i = start; i < start + size; i++ ) ret += data[i] << (i - start) * 8;
			return ret;
		}

		private void PushNumber (byte[] data, ushort val, int Pos) {
			byte[] hashByte = BitConverter.GetBytes ( val );
			for ( int i = 0; i < hashByte.Length; i++ ) data[i + Pos] = hashByte[i];
		}
	}
}