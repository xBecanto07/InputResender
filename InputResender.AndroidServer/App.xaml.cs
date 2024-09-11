namespace InputResender.AndroidServer {
	public partial class App : Application {
		public App () {
			InitializeComponent ();

			FrontPage frontPage = new ();
			MainPage = new NavigationPage ( frontPage );
		}
	}
}
