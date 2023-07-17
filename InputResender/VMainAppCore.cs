using Components.Interfaces;
using InputResender.GUIComponents;
using System.Net;

namespace Components.Implementations {
	public class VMainAppCore : DMainAppCore {
		public VMainAppCore ( CompSelect componentSelector = CompSelect.All ) : base (
			( core ) => new MEventVector ( core ),
			( core ) => new VWinLowLevelLibs ( core ),
			( core ) => new VInputReader_KeyboardHook ( core ),
			( core ) => new VInputParser ( core ),
			( core ) => new VInputProcessor ( core ),
			( core ) => new VDataSigner ( core ),
			( core ) => new VPacketSender ( core ),
			componentSelector ) {
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