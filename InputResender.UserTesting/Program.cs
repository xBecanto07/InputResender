using System.Windows.Forms;
using System;
using System.Collections.Generic;

namespace InputResender.UserTesting {
	public static class Program {
		public static Form1 MainForm;
		public static List<string> Inputs = new List<string> ();
		public static bool Initialized = false;

		/// <summary></summary>
		[STAThread]
		static void Main () {
			// To customize application configuration such as set high DPI settings or default font,
			// see https://aka.ms/applicationconfiguration.
			ApplicationConfiguration.Initialize ();
			MainForm = new Form1 ();
			Application.Run ( MainForm );
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