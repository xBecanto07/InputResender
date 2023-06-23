using Components.Interfaces;
using InputResender.GUIComponents;
using System.Net;

namespace Components.Implementations {
	public class MainAppCore : DMainAppCore {
		public MainAppCore () : base (
			null,
			( core ) => new WinLowLevelLibs ( core ),
			( core ) => new VInputReader_KeyboardHook ( core ),
			( core ) => new VInputParser ( core ),
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