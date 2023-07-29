using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using SBld = System.Text.StringBuilder;

namespace InputResender.UserTesting {
	public partial class Form1 : Form {
		const int MaxLines = 40;
		List<string> lines;
		AutoResetEvent inputWaiter;
		Queue<string> inputLines;
		string newText = "";

		public Form1 () {
			lines = new List<string> ();
			inputLines = new Queue<string> ();
			inputWaiter = new AutoResetEvent ( false );
			InitializeComponent ();
		}


		public void WriteLine ( string s = "" ) {
			lines.Insert ( 0, s );
			if ( lines.Count > MaxLines ) lines.RemoveAt ( MaxLines );
			UpdateText ();
		}
		public void Write ( string s ) {
			if ( lines.Count > 0 ) lines[0] += s;
			else lines.Add ( s );
			UpdateText ();
		}
		public void Backspace ( int N = 1 ) { lines[0] = lines[0].Substring ( 0, lines[0].Length - N ); UpdateText (); }
		public void DeleteLine () { lines.RemoveAt ( 0 ); UpdateText (); }
		public void ClearInput () { inputLines.Clear (); newText = ""; Invoke ( () => ConsoleIN.Text = "" ); }
		public string ReadLine ( bool blocking = true ) {
			if ( !blocking ) return inputLines.Count > 0 ? inputLines.Dequeue () : null;
			while ( inputLines.Count == 0 ) inputWaiter.WaitOne ();
			return inputLines.Dequeue ();
		}
		public char Read ( bool blocking = true ) {
			if ( blocking ) while ( ConsoleIN.Text.Length == 0 ) inputWaiter.WaitOne ();
			else if ( ConsoleIN.Text.Length == 0 ) return '\0';
			string text = ConsoleIN.Text;
			char c = text[0];
			Invoke ( () => ConsoleIN.Text = text[1..] );
			return c;
		}

		public void UpdateText () {
			if ( !Program.Initialized ) return;
			SBld SB = new SBld ();
			for ( int i = lines.Count - 1; i >= 0; i-- ) {
				SB.AppendLine ( lines[i] );
			}

			Invoke ( () => ConsoleOUT.Text = SB.ToString () );
		}
		public void Clear () {
			Invoke ( () => {
				lines.Clear ();
				inputWaiter.Reset ();
				inputLines.Clear ();
				newText = "";
				ConsoleOUT.Text = "";
				ConsoleIN.Text = "";
				UserTestApp.Continue ();
			} );


		}

		private void ConsoleOK_Click ( object sender, System.EventArgs e ) {
			ConsoleIN.Text += "\n"; // Should fire TextChanged event, if not, add manual activation
		}

		private void ConsoleIN_TextChanged ( object sender, System.EventArgs e ) {
			string text = ConsoleIN.Text;
			int pos;
			while ( (pos = text.IndexOf ( Environment.NewLine )) >= 0 ) {
				inputLines.Enqueue ( text.Substring ( 0, pos ) );
				text = text.Substring ( pos + 1 );
			}
			inputWaiter.Set (); // signal any change of input (for Console.Read)
			UserTestApp.Continue ();
		}

		private void Form1_Load ( object sender, System.EventArgs e ) {
			UserTestApp.Continue ();
		}

		private void timer1_Tick ( object sender, System.EventArgs e ) {
			UserTestApp.Continue ();
		}

		private void ActiveTask_CheckedChanged ( object sender, System.EventArgs e ) {
			UserTestApp.Continue ();
		}

		private void Form1_Activated ( object sender, System.EventArgs e ) {
			ConsoleIN.Text = "";
			Program.Initialized = true;
			Thread.MemoryBarrier ();
			UserTestApp.TaskSignaler.Set ();
		}

		private void Awakener_Tick ( object sender, EventArgs e ) {
			UpdateText ();
			UserTestApp.Continue ();
		}
	}
}