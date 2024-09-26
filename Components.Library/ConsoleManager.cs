using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Components.Library;
public class ConsoleManager {
	public const string EOF = "\x04";
	public const char PsswdStart = '\x0E';
	public const char PsswdEnd = '\x0F';
	private readonly Action<string> RealWrite, RealWriteLine;
	private readonly Func<string> RealReadLine;
	private readonly Func<char> RealReadChar;
	private readonly Action RealClear;
	private readonly Task reader;

	private readonly List<string> InputBuffer = new ();
	private readonly List<string> OutputBuffer = new ();
	private string NewInput = null;
	private readonly AutoResetEvent readerWaiter;
	private bool EndsWithEOL = true;

	public int MaxLineLength { get; set; } = 120;

	public ConsoleManager ( Action<string> realWriteLine, Func<string> realReadLine, Action<string> realWrite, Action realClear = null, Func<char> realReadChar = null ) {
		ArgumentNullException.ThrowIfNull ( realWriteLine, nameof ( realWriteLine ) );
		ArgumentNullException.ThrowIfNull ( realReadLine, nameof ( realReadLine ) );
		ArgumentNullException.ThrowIfNull ( realWrite, nameof ( realWrite ) );

		RealWriteLine = realWriteLine;
		RealWrite = realWrite;
		RealReadLine = realReadLine;
		RealClear = realClear ?? (() => { });
		RealReadChar = realReadChar;
		readerWaiter = new AutoResetEvent ( false );
		reader = new Task ( ReaderTask );
		reader.Start ();
	}

	public void Write ( string text ) { PushOut ( text, false ); RealWrite ( text ); }
	public void WriteLine ( string text ) { 
		int escStart = text.IndexOf ( PsswdStart );
		if ( escStart >= 0 ) {
			int escEnd = text.LastIndexOf ( PsswdEnd );
			if ( escEnd >= 0 ) text = string.Concat ( text.AsSpan ( 0, escStart ), "***", text.AsSpan ( escEnd + 1 ) );
		}
		PushOut ( text, true );
		RealWriteLine ( text );
	}

	private void PushOut (string text, bool endWithEOL) {
		if ( EndsWithEOL ) OutputBuffer.Insert ( 0, text );
		else OutputBuffer[0] += text;
		EndsWithEOL = endWithEOL;
	}

	public void Clear () {
		OutputBuffer.Clear ();
		InputBuffer.Clear ();
		RealClear ();
	}

	private string ReadHidden () {
		if ( RealReadChar == null ) return string.Empty;
		string hidden = string.Empty;
		while ( true ) {
			char c = RealReadChar ();
			if ( c == '#' ) {
				RealWriteLine ( string.Empty );
				return hidden;
			}
			if (c == '$') {
				RealWriteLine ( string.Empty );
				return hidden + '$';
			}
			if ( c == '\x08' ) {
				if ( hidden.Length > 0 ) {
					hidden = hidden[..^1];
					RealWrite ( "\b \b" );
				}
			} else {
				hidden += c;
				RealWrite ( "*" );
			}
		}
	}

	private void ReaderTask () {
		while ( true ) {
			string line = RealReadLine ();
			if ( line == null || line == "exit" ) {
				lock ( readerWaiter ) {
					NewInput = EOF;
					RealWriteLine ( "Stopping reader task" );
					return;
				}
			} else if ( string.IsNullOrEmpty ( line ) ) {
				continue; // Ignore empty lines
			} else if ( line.EndsWith ( '$' ) ) {
				line = line[..^1];
				string hidden = ReadHidden ();
				line += hidden;
				if ( line.EndsWith ( '$' ) ) {
					string end = RealReadLine ();
					if ( end != null ) line = $"{line[..^1]} {end}";
				}
			}

			// Read input and wait for it to be processed
			lock ( readerWaiter ) {
				NewInput = line;
				PushOut ( line, true );
				InputBuffer.Insert ( 0, line );
			}
			readerWaiter.WaitOne ();
		}
	}

	public string LastLine ( int offset = 0 ) => OutputBuffer.Count > offset ? OutputBuffer[offset] : string.Empty;

	public void Reset () {
		lock ( readerWaiter ) {
			NewInput = null;
			InputBuffer.Clear ();
			OutputBuffer.Clear ();
			readerWaiter.Reset ();
		}
	}

	public string ReadLine () {
		lock ( readerWaiter ) {
			if ( NewInput == null ) return null;
			if ( NewInput == EOF ) return EOF;
			string ret = NewInput;
			NewInput = null;
			readerWaiter.Set ();
			return ret;
		}
	}

	public string ReadLineBlocking () {
		while ( true ) {
			string line = ReadLine ();
			if ( line != null ) return line;
			Thread.Sleep ( 1 );
		}
	}

	public void Append ( string line, bool endsWithEOL = true ) {
		string fullLine = LastLine () + line;
		if ( OutputBuffer.Any () ) OutputBuffer[0] = fullLine;
		else OutputBuffer.Add ( fullLine );
		EndsWithEOL = endsWithEOL;
		if (Console.CursorTop > 0) Console.SetCursorPosition ( 0, Console.CursorTop - 1 ); // Replace this with injection
		RealWriteLine ( $"\r{new string ( ' ', Console.WindowWidth - 1 )}\r{fullLine}" );
	}
}