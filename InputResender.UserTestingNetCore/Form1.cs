using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using static InputResender.UserTestingNetCore.LL_Keyboard;

namespace InputResender.UserTestingNetCore {
	public partial class Form1 : Form {
		static List<(int, IntPtr, IntPtr, KeyEventInfo)> messages = new List<(int, IntPtr, IntPtr, KeyEventInfo)> ();

		public Form1 () {
			InitializeComponent ();
			Debugger.Launch ();
		}

		private void Form1_Load ( object sender, EventArgs e ) {
			//StartHook ( Callback );
		}
		private void DebuggerBtn_Click ( object sender, EventArgs e ) {
			Debugger.Break ();
		}
		private void Form1_FormClosing ( object sender, FormClosingEventArgs e ) {
			if ( _hookID != IntPtr.Zero ) StopHook ();
		}

		private void textBox1_TextChanged ( object sender, EventArgs e ) {
			string text = textBox1.Text;
			if ( text.StartsWith("start") ) {
				text = text.Substring ( "start".Length );
				if ( _hookID != IntPtr.Zero ) StopHook ();
				StartHook ( Callback );
			} else if ( text.StartsWith("end") ) {
				text = text.Substring ( "end".Length );
				if ( _hookID != IntPtr.Zero ) StopHook ();
			}
			int pos;
			while ( (pos = text.IndexOf ( Environment.NewLine )) >= 0 ) {
				ConsoleText.AppendText ( $"{Environment.NewLine}{text.Substring ( 0, pos )}" );
				text = text.Substring ( pos + 1 );
			}
			if (textBox1.Text != text) textBox1.Text = text;
		}



		public static IntPtr Callback ( int nCode, IntPtr vkChngCode, IntPtr lParam ) {
			KeyEventInfo IntInfo = KeyEventInfo.Load ( lParam );
			messages.Add ( (nCode, vkChngCode, lParam, IntInfo) );
			Program.MainForm.ConsoleText.AppendText ( $"{Environment.NewLine}{IntInfo.KeyID}({IntInfo.KeyName}) / {IntInfo.scanCode}" );
			return CallNextHookEx ( _hookID, nCode, vkChngCode, lParam );
		}
	}
}