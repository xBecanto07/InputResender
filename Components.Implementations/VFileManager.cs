using Components.Interfaces;
using Components.Library;
using InputResender.Services;
using System.Security.Cryptography;

namespace Components.Implementations;
public class VFileManager : DFileManager {
	public override int ComponentVersion => 1;

	public const int HashSizeHex = SHA3_256.HashSizeInBytes * 2;
	//public const int HashSizeBase64_1 = SHA3_256.HashSizeInBytes * 4 / 3;
	public const int HashSizeBase64 = SHA3_256.HashSizeInBytes * 4 / 3 + (SHA3_256.HashSizeInBytes * 4) % 3;

	/*
	 * Sure, this approach leaves a lot to be desired, but something better than nothing.
	 * While it does not prevent actual hacker attack,
	 *   it will provide some level of protection against most common errors
	 *   and increases difficulty of file tampering.
	 *     Sidenote: why on Earth is it tAmper?? Wiktionary: "From Middle French temprer, Doublet of temper, from Latin temperare". I mean, ok. But where did the 'a' come from? 🤔
	 * If we assume that the input hash cannot be altered by attacker,
	 *   we probably can also assume that modifying the file until the hash matches is not an option.
	 * The 'white-list' hash therefore must be altered by attacker.
	 *   To prevent this we'll use a 'file with header', where encrypted hash is stored in the header.
	 *   User provides password, calculated hash is encrypted and compared with the header hash.
	 *   This header relies solely on the password. If compromised by any common attack,
	 *     attacker can easily store own hash of tampered file, encrypted with that password.
	 * Other possible way of attack is to modify the stored value of the hash in memory.
	 *   Encrypting the hash could help but storing a key would just shift the problem.
	 *   While possible, this attack is more difficult and requires some level of dedication (and realtime access to the system).
	 * I believe that this approach should be sufficient.
	 * Good programmers who are skilled in encryption and security are more than welcome to implement better component variant. 😉
	 */

	private readonly Dictionary<string, byte[]> hashes = [];
	private readonly byte[] iv;
	private readonly int ivN;

	public VFileManager ( CoreBase owner ) : base ( owner ) {
		using ( Aes aes = Aes.Create() ) {
			aes.GenerateIV ();
			iv = aes.IV;
			ivN = iv.Length;

			if ( !aes.ValidKeySize ( SHA3_256.HashSizeInBits ) )
				throw new InvalidOperationException ( $"AES does not support key size of {SHA3_256.HashSizeInBits} bits, which is required for hashing. This should never happen." );
		}
	}

	public override void WhitelistHash ( string filePath, string hash ) {
		if ( hashes.ContainsKey ( filePath ) )
			throw new InvalidOperationException ( $"File {filePath} is already whitelisted." );
		if ( !FileService.Exists ( filePath ) )
			throw new FileNotFoundException ( $"File {filePath} not found." );
		ArgumentNullException.ThrowIfNull ( hash );

		byte[] hashBytes;
		switch ( hash.Length ) {
		case HashSizeHex:
			hashBytes = new byte[SHA3_256.HashSizeInBytes];
			for ( int i = 0, j = 0; i < hash.Length; i += 2, j++ )
				hashBytes[j] = Convert.ToByte ( hash.Substring ( i, 2 ), 16 );
			break;
		case HashSizeBase64:
			hashBytes = Convert.FromBase64String ( hash );
			break;
		default:
			throw new ArgumentException ( $"Hash string has invalid length. Expected {HashSizeHex} for hex or {HashSizeBase64} for base64, but found {hash.Length}", nameof ( hash ) );
		}

		StoreHash ( filePath, hashBytes );
	}

	public override string ReadFile ( string path ) {
		if ( !FileService.Exists ( path ) )
			throw new FileNotFoundException ( $"File {path} not found." );

		string content = FileService.ReadAllText ( path );
		byte[] hash = SHA3_256.HashData ( System.Text.Encoding.UTF8.GetBytes ( content ) );

		byte[] expected = ReadHash ( path );
		if ( !hash.SequenceEqual ( expected ) )
			throw new IntegrityException ( $"File {path} integrity check failed. Hash does not match the expected value.", hash, content );

		return content;
	}

	public override byte[] ReadBinary ( string path ) {
		if ( !FileService.Exists ( path ) )
			throw new FileNotFoundException ( $"File {path} not found." );

		byte[] content = FileService.ReadAllBytes ( path );
		byte[] hash = SHA3_256.HashData ( content );

		byte[] expected = ReadHash ( path );
		if ( !hash.SequenceEqual ( expected ) )
			throw new IntegrityException ( $"File {path} integrity check failed. Hash does not match the expected value.", hash, System.Text.Encoding.UTF8.GetString ( content ) );

		return content;
	}

	public override string ReadFileWithHeader ( string path, PasswordHolder password ) {
		ArgumentNullException.ThrowIfNull ( password );
		if ( !FileService.Exists ( path ) )
			throw new FileNotFoundException ( $"File {path} not found." );

		string content = FileService.ReadAllText ( path );
		int firstLF = content.IndexOf ( '\n' );
		int lastLF = content.LastIndexOf ( '\n' );
		if ( firstLF == -1 || lastLF == -1 || firstLF == lastLF || firstLF != HashSizeBase64 + 1 )
			throw new IntegrityException ( $"File {path} does not contain a valid header.", null, content );

		string header = content[..firstLF].Trim();
		content = content[firstLF..lastLF].Trim();

		byte[] encryptedHash = Convert.FromBase64String ( header );
		byte[] expectedHash = CalcFileHeader ( content, password );
		if ( !encryptedHash.SequenceEqual ( expectedHash ) )
			throw new IntegrityException ( $"File {path} integrity check failed. Hash does not match the expected value.", expectedHash, content );

		return content;
	}

	public override void WriteFileWithHeader ( string path, string content, PasswordHolder password ) {
		ArgumentNullException.ThrowIfNull ( password );
		ArgumentException.ThrowIfNullOrWhiteSpace ( path );
		content = content.Trim ();

		byte[] encryptedHash = CalcFileHeader ( content, password );
		var file = FileService.CreateText (  path );
		file.WriteLine ( Convert.ToBase64String ( encryptedHash ) );
		file.WriteLine ( content );
		file.Close ();
	}


	private byte[] CalcFileHeader ( string content, PasswordHolder password ) {
		byte[] hash = SHA3_256.HashData ( System.Text.Encoding.UTF8.GetBytes ( content ) );
		return password.Mask ( hash );
		/*using ( Aes aes = Aes.Create () ) {
			aes.IV = new byte[ivN];
			Array.Clear (  iv, 0, ivN );
			password.Assign ( aes );
			return aes.EncryptCbc ( hash, aes.IV );
		}*/
	}

	private void StoreHash( string path, byte[] hash ) {
		hashes[path] = ConvertHash ( hash );
	}
	private byte[] ReadHash( string path ) {
		if ( !hashes.TryGetValue ( path, out var hash ) )
			return [];
		return ConvertHash ( hash );
	}
	private byte[] ConvertHash ( byte[] hash ) {
		byte[] res = new byte[hash.Length];
		for (int i = 0; i < hash.Length; i++ ) {
			res[i] = (byte)(hash[i] ^ iv[i % ivN]);
		}
		return res;
	}
}