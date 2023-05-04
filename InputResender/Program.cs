using InputResender.GUIComponents;

namespace InputResender {
	internal static class Program {
		[STAThread]
		static void Main () {
			ApplicationConfiguration.Initialize ();
			Application.Run ( new MainScreen () );
		}
	}
}