using Components.Implementations;
using Components.Interfaces;
using InputResender.GUIComponents;

namespace Components.Factories {
	public class DMainAppCoreFactory {
		public bool PreferMocks = false;

		public VMainAppCore CreateVMainAppCore ( DMainAppCore.CompSelect selector = DMainAppCore.CompSelect.All ) {
			if ( PreferMocks ) return new VMainAppCore (
				( core ) => new MEventVector ( core ),
				( core ) => new MLowLevelInput ( core ),
				( core ) => new MInputReader ( core ),
				( core ) => new MInputParser ( core ),
				( core ) => new MInputProcessor ( core ),
				( core ) => new MDataSigner ( core ),
				( core ) => new MPacketSender ( core ),
				selector
				);
			else return new VMainAppCore (
				( core ) => new MEventVector ( core ),
				( core ) => new VWinLowLevelLibs ( core ),
				( core ) => new VInputReader_KeyboardHook ( core ),
				( core ) => new VInputParser ( core ),
				( core ) => new VInputProcessor ( core ),
				( core ) => new VDataSigner ( core ),
				( core ) => new VPacketSender ( core ),
				selector
				);
		}
	}
}