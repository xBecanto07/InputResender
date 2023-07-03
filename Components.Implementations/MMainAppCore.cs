using Components.Interfaces;
using System.Net;

namespace Components.Implementations {
	public class MMainAppCore : DMainAppCore {
		public MMainAppCore () : base (
			( core ) => new MEventVector ( core ),
			( core ) => new MLowLevelInput ( core ),
			( core ) => new MInputReader ( core ),
			( core ) => new MInputParser ( core ),
			( core ) => new MInputProcessor ( core ),
			( core ) => new MDataSigner ( core ),
			( core ) => new MPacketSender ( core ) ) {
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