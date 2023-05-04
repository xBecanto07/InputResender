using Components.Interfaces;
using System.Net;

namespace Components.Implementations {
	public class MainAppCore : DMainAppCore {
		public MainAppCore () : base (
			null,
			( core ) => new VInputReader_KeyboardHook ( core ),
			null,
			null,
			null,
			null,
			null ) {
		}

		public override void Initialize () {

		}
		public override void LoadComponents () {

		}
		public override void LoadConfiguration ( string path ) {

		}
		public override void SaveConfiguration ( string path ) {

		}
		public override void RunApp () {

		}
	}
}