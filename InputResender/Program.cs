using Components.Factories;
using InputResender.GUIComponents;

namespace InputResender {
	internal static class Program {
		[STAThread]
		static void Main () {
			ApplicationConfiguration.Initialize ();

			DMainAppCoreFactory coreFactory = new DMainAppCoreFactory ();
			coreFactory.PreferMocks = false;
			var core = coreFactory.CreateVMainAppCore ();

			Application.Run ( new MainScreen ( core ) );
		}
	}
}