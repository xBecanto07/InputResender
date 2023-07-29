using System.Windows.Forms;
using System;
using System.Collections.Generic;
using Xunit;
using System.Threading.Tasks;

namespace InputResender.UserTesting {
	public class Program {
		public static Form1 MainForm;
		public static List<string> Inputs = new List<string> ();
		public static bool Initialized = false;
		static bool FirstRun = true;
		public static Task MainThread = null;

		[STAThread]
		public static void Main () {
			if ( MainThread == null ) MainThread = Task.Run ( () => {
				if ( FirstRun ) ApplicationConfiguration.Initialize ();
				FirstRun = false;
				MainForm = new Form1 ();
				Application.Run ( MainForm );
				MainForm.Dispose ();
			} );
			else MainForm.Clear ();
		}

		public static void WriteLine ( string line = "" ) => MainForm.WriteLine ( line );
		public static void Write ( string s ) => MainForm.Write ( s );
		public static void Backspace ( int N = 1 ) => MainForm.Backspace ( N );
		public static void DeleteLine () => MainForm.DeleteLine ();
		public static void ClearInput () => MainForm.ClearInput ();
		public static string ReadLine ( bool blocking = true ) => MainForm.ReadLine ( blocking );
		public static char Read ( bool blocking = true ) => MainForm.Read ( blocking );
		public static void UpdateText () => MainForm.UpdateText ();
		public static void WaitTime ( int ms ) {
			MainForm.timer1.Interval = ms;
			MainForm.timer1.Start ();
		}

		public static void SendSignal ( bool? state = null ) {
			if ( !Initialized ) return;
			MainForm.Invoke ( () => {
				MainForm.ActiveTask.Checked = state ?? !MainForm.ActiveTask.Checked;
			} );
		}
	}
}