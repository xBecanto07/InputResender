using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Components.Interfaces;
using Components.Library;

namespace Components.Implementations {
	public class VDataSigner : DDataSigner {
		const int HashSize = MD5.HashSizeInBytes;
		const int keySize = MD5.HashSizeInBytes; // 128 / 8

		public VDataSigner ( CoreBase owner ) : base ( owner ) {
		}

		private byte[] key;
		public override int KeySize { get => keySize; }
		public override byte[] Key { get => key; set => key = value.Length == KeySize ? value : MD5.HashData ( value ); }
		public override int ComponentVersion => 1;
		public override int ChecksumSize => HashSize * 2 + KeySize;

		public override byte[] Decrypt ( byte[] data, byte[] IV = null ) {
			// Data size should be checked in integrity test
			if ( !TestIntegrity ( data ) ) return null;
			if ( IV == null ) IV = data.SubArray ( ChecksumSize - KeySize, KeySize );
			if ( !TestPsswd ( data, IV ) ) return null;

			using ( Aes aes = Aes.Create () ) {
				aes.Key = Key;
				aes.IV = IV;
				ReadOnlySpan<byte> dataSpan = new ReadOnlySpan<byte> ( data, ChecksumSize, data.Length - ChecksumSize );
				return aes.DecryptCbc ( dataSpan, IV );
			}
		}
		public override byte[] Encrypt ( byte[] data, byte[] IV ) {
			using ( Aes aes = Aes.Create () ) {
				aes.Key = Key;
				aes.IV = IV;

				List<byte> ret = new List<byte> ();
				ret.AddRange ( MD5.HashData ( Key.Merge ( IV ) ) );
				ret.AddRange ( IV );
				ret.AddRange ( aes.EncryptCbc ( data, IV ) );
				byte[] hash = MD5.HashData ( ret.ToArray () );
				ret.InsertRange ( 0, hash );
				return ret.ToArray ();
			}
		}
		public override bool TestIntegrity ( byte[] data ) {
			using MD5 md5 = MD5.Create ();
			byte[] hash = md5.ComputeHash ( data, HashSize, data.Length - HashSize );
			return CheckHash ( hash, data, 0 );
		}
		public override bool TestPsswd ( byte[] data, byte[] IV ) {
			byte[] hash = MD5.HashData ( Key.Merge ( IV ) );
			return CheckHash ( hash, data, HashSize );
		}

		private bool CheckHash ( byte[] calcHash, byte[] data, int start) {
			int correct = 0;
			for ( int i = start, j = 0; j < HashSize; i++, j++ ) correct += data[i] == calcHash[j] ? 1 : 0;
			return correct == HashSize;
		}

		public override byte[] GenerateIV ( byte[] data = null ) {
			if ( data == null ) {
				using Aes aes = Aes.Create ();
				aes.KeySize = KeySize * 8;
				aes.GenerateIV ();
				return aes.IV;
			} else return MD5.HashData ( data );
		}
	}
}