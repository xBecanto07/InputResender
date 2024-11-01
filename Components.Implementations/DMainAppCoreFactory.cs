using Components.Interfaces;

namespace Components.Implementations;
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
			( core ) => MPacketSender.Fetch ( 0, core ),
			( core ) => new VMainAppControls ( core ),
			( core ) => new VShortcutWorker ( core ),
			( core ) => new VCommandWorker ( core ),
			selector
			);
		else return new VMainAppCore (
			( core ) => new MEventVector ( core ),
			( core ) => new MLowLevelInput ( core ),
			( core ) => new VInputReader_KeyboardHook ( core ),
			( core ) => new VInputParser ( core ),
			( core ) => new VInputProcessor ( core ),
			( core ) => new VDataSigner ( core ),
			( core ) => new VPacketSender ( core ),
			( core ) => new VMainAppControls ( core ),
			( core ) => new VShortcutWorker ( core ),
			( core ) => new VCommandWorker ( core ),
			selector
			);
	}

	public static VMainAppCore CreateDefault ( params System.Action<DMainAppCore>[] extras ) {
		var factory = new DMainAppCoreFactory ();
		factory.PreferMocks = false;
		var core = factory.CreateVMainAppCore ();
		foreach ( var extra in extras ) extra ( core );
		return core;
	}
}