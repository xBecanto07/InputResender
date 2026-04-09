using Components.Interfaces;
using Components.Library;
using Components.LibraryTests;
using FluentAssertions;
using InputResender.Services;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Components.InterfaceTests;
public abstract class DFileManagerTest : ComponentTestBase<DFileManager> {
	private readonly MockFileService FileService;
	const string content = "Hello, World!";
	const string path = "test.txt";

	public DFileManagerTest ( ITestOutputHelper output ) : base ( output ) {
		FileService = new ();
		TestObject.FileService = FileService;
		FileService.AddMockFile ( path, content );
	}

	public override CoreBase CreateCoreBase () => new CoreBaseMock ();

	[Theory]
	[InlineData ( 16 )]
	[InlineData ( 64 )]
	public void ReadFileBeforeAfterWhitelist ( int stringBase ) {
		string result = null;
		DFileManager.IntegrityException integrityException = null;
		Action readFile = () => result = TestObject.ReadFile ( path );
		integrityException = readFile.Should ().Throw<DFileManager.IntegrityException> ().Which;
		integrityException.Content.Should ().StartWith ( content );
		integrityException.Hash.Should ().NotBeNull ();
		result.Should ().BeNull ();

		switch ( stringBase ) {
		case 16: TestObject.WhitelistHash ( path, HashToHex ( integrityException.Hash ) ); break;
		case 64: TestObject.WhitelistHash ( path, HashTo64 ( integrityException.Hash ) ); break;
		default: throw new ArgumentException ( "Invalid string base" );
		}

		readFile.Should ().NotThrow ();
		result.Should ().Be ( content );
	}

	public void ReadWriteWithHeader () {
		const string password = "password123";
		const string newContent = "New content with header.";
		string result = null;
		PasswordHolder psswd = new ( password );

		Action readWithHeader = () => result = TestObject.ReadFileWithHeader ( path, psswd );
		readWithHeader.Should ().Throw<DFileManager.IntegrityException> ();

		Action writeWithHeader = () => TestObject.WriteFileWithHeader ( path, newContent, psswd );
		writeWithHeader.Should ().NotThrow ();

		readWithHeader.Should ().NotThrow ();
		result.Should ().Be ( newContent );
	}



	private string HashToHex ( byte[] hash ) => BitConverter.ToString ( hash );
	private string HashTo64 ( byte[] hash ) => Convert.ToBase64String ( hash );
}




public class MockFileService : FileAccessService {
	private readonly Dictionary<string, string> MockFiles = [];
	private readonly Dictionary<string, MockStreamWriter> OpenStreams = [];

	public override bool Exists ( string path ) => MockFiles.ContainsKey ( path ) || OpenStreams.ContainsKey ( path );
	public override string ReadAllText ( string path ) {
		if ( MockFiles.TryGetValue ( path, out string val ) ) return val;
		if ( OpenStreams.TryGetValue ( path, out var stream ) )
			throw new InvalidOperationException ( $"File {path} is currently open for writing. Cannot read from it." );
		throw new FileNotFoundException ( $"File {path} not found in mock file service." );
	}
	public override StreamWriter CreateText ( string path ) {
		MockStreamWriter ret = new ();
		ret.OnClose += content => MockFiles[path] = content;
		OpenStreams[path] = ret;
		return ret;
	}

	public void AddMockFile ( string path, string content ) => MockFiles[path] = content;
}

public class MockStreamWriter : StreamWriter {
	private readonly MemoryStream mem;

	public MockStreamWriter () : base ( new MemoryStream () ) {
		mem = (MemoryStream)base.BaseStream;
	}

	public event Action<string> OnClose;

	public override void Close () {
		StreamReader reader = new ( mem );
		string content = reader.ReadToEnd ();
		base.Close ();
		reader.Close ();
		mem.Close ();
		OnClose?.Invoke ( content );
	}
}