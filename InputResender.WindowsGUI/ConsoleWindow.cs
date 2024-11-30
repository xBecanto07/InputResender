using Components.Library;
using InputResender.CLI;
using InputResender.WindowsGUI.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputResender.WindowsGUI; 
public partial class ConsoleWindow : Form {
	public ConsoleWindow () {
		InitializeComponent ();
	}

	public ConsoleManager console;
	public CliWrapper cliWrapper;

	object generalLock = new ();
	ManualResetEvent lineWaiter = null, chWaiter = null;
	string line = null;
	char ch = '\0';

	string lastText = string.Empty;

	private void textBox1_TextChanged ( object sender, EventArgs e ) {
		string text = textBox1.Text.ReplaceLineEndings ( "\n" );
		if ( text == lastText ) return;
		if ( !text.StartsWith(lastText)) {
			lastText = text;
			return;
		}

		// Some text was appended
		// Current issue is that lastText is updated always and not after consuming text. Fix? IDK :(
		// Current solution is simplifying the situation by ommiting consuming text written before the read method was initiated. Futher testing is needed to see if this is limiting or standard console behaviour

		string diff = text[lastText.Length..];
		lock (generalLock) {
			if ( chWaiter != null ) {
				ch = diff[0];
				lastText = text;
				chWaiter.Set ();
			} else if ( diff.EndsWith('\n') ) {
				string[] lines = diff.Split ( '\n' );
				for ( int i = lines.Length - 1; i >= 0; i-- ) {
					if ( string.IsNullOrWhiteSpace ( lines[i] ) ) continue;
					lastText = text;
					if (lineWaiter != null ) {
						line = lines[i];
						lineWaiter.Set ();
					} else {
						cliWrapper.ProcessLine ( lines[i], false );
					}
					break;
				}
			}
		}
	}

	private void ConsoleWindow_Load ( object sender, EventArgs e ) {
		console = new ( WriteLine, ReadLine, Write, Clear, ReadKey, OverwriteLine );
		cliWrapper = InputResender.CLI.Program.StartMain ( [], new TopLevelLoader (), console );
		Task.Delay ( 50 ).Wait ();
		lock ( generalLock ) {
			if (lineWaiter != null) {
				line = "print ASDF";
				lineWaiter.Set ();
			}
		}
	}

	private void SetText (string s) {
		lastText = s.ReplaceLineEndings( "\n" );
		var lines = lastText.Split ( '\n' );
		Invoke ( () => {
			textBox1.Lines = lines;
			textBox1.Select ( textBox1.Text.Length, 0 );
			textBox1.ScrollToCaret ();
		} );
	}

	public void WriteLine ( string s ) => SetText ( lastText + s + '\n' );
	public void Write ( string s ) => SetText ( lastText + s );
	public void Clear () => SetText ( string.Empty );
	public void OverwriteLine ( string s ) {
		var lines = textBox1.Lines;
		if ( lines.Length < 1 ) SetText ( s );
		else {
			lines[^1] = s;
			SetText ( string.Join ( '\n', lines ) );
		}
	}

	private string ReadLine () {
		string ret = null;
		while ( true ) {
			lock ( generalLock ) {
				if ( chWaiter == null && lineWaiter == null ) {
					if ( line != null ) {
						ret = line;
						line = null;
						return ret;
					}
					lineWaiter = new ( false );
					break;
				}
			}
			Task.Delay ( 10 ).Wait ();
		}
		lineWaiter.WaitOne ();

		ret = line;
		line = null;
		lock ( generalLock ) { lineWaiter = null; }
		return ret;
	}

	private char ReadKey () {
		char ret = '\0';
		while ( true ) {
			lock ( generalLock ) {
				if ( chWaiter == null && lineWaiter == null ) {
					if ( line != null ) {
						ret = ch;
						ch = '\0';
						return ret;
					}

					chWaiter = new ( false );
					break;
				}
			}
			Task.Delay ( 10 ).Wait ();
		}
		chWaiter.WaitOne ();

		ret = ch;
		lock ( generalLock ) { chWaiter = null; }
		return ret;
	}
}