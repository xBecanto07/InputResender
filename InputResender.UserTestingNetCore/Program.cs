using System;
using System.Windows.Forms;

namespace InputResender.UserTestingNetCore {
	internal static class Program {
		public static Form1 MainForm;

		[STAThread]
		static void Main () {
			// To customize application configuration such as set high DPI settings or default font,
			// see https://aka.ms/applicationconfiguration.
			ApplicationConfiguration.Initialize ();
			MainForm = new Form1 ();
			Application.Run ( MainForm );
		}
	}
}