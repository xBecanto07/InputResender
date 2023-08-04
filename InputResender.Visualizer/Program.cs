using System;
using System.Windows.Forms;

namespace InputResender.Visualizer {
	internal static class Program {
		public static Visualizer MainForm;

		[STAThread]
		static void Main () {
			ApplicationConfiguration.Initialize ();
			MainForm = new Visualizer ();
			Application.Run ( MainForm );
		}
	}
}