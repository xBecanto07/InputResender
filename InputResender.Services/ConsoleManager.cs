using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InputResender.Services;
public class ConsoleManager {
	public const string EOF = "\x04";
	private readonly Action<string> RealWrite, RealWriteLine;
	private readonly Func<string> RealReadLine;
	private readonly Task reader;

	private readonly List<string> InputBuffer = new ();
	private readonly List<string> OutputBuffer = new ();
	private string NewInput = null;
	private readonly AutoResetEvent readerWaiter;

	public ConsoleManager ( Action<string> realWriteLine = null, Func<string> realReadLine = null, Action<string> realWrite = null ) {
		RealWriteLine = realWriteLine ?? Console.WriteLine;
		RealWrite = realWrite ?? Console.Write;
		RealReadLine = realReadLine ?? Console.ReadLine;
		readerWaiter = new AutoResetEvent ( false );
		reader = new Task ( ReaderTask );
		reader.Start ();
	}

	public void Write ( string text ) { OutputBuffer.Insert ( 0, text ); RealWrite ( text ); }
	public void WriteLine ( string text ) { OutputBuffer.Insert ( 0, text ); RealWriteLine ( text ); }

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
			} else {
				// Read input and wait for it to be processed
				lock ( readerWaiter ) {
					NewInput = line;
					OutputBuffer.Insert ( 0, line );
					InputBuffer.Insert ( 0, line );
				}
				readerWaiter.WaitOne ();
			}
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

	public void Append ( string line ) {
		string fullLine = LastLine () + line;
		if ( OutputBuffer.Any () ) OutputBuffer[0] = fullLine;
		else OutputBuffer.Add ( fullLine );
		if (Console.CursorTop > 0) Console.SetCursorPosition ( 0, Console.CursorTop - 1 ); // Replace this with injection
		RealWriteLine ( $"\r{new string ( ' ', Console.WindowWidth - 1 )}\r{fullLine}" );
	}
}