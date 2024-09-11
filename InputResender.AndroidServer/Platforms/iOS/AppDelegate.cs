using Foundation;

namespace InputResender.AndroidServer {
	[Register ( "AppDelegate" )]
	public class AppDelegate : MauiUIApplicationDelegate {
		protected override MauiApp CreateMauiApp () => MauiProgram.CreateMauiApp ();
	}
}
