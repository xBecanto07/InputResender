using Components.Interfaces;
using InputResender.Services.NetClientService;

namespace Components.Implementations;
public class DMainAppCoreFactory {
	public bool PreferMocks = false;

	public VMainAppCore CreateVMainAppCore ( DMainAppCore.CompSelect selector = DMainAppCore.CompSelect.All ) {
		if ( PreferMocks ) return new VMainAppCore (
			( core ) => new MEventVector ( core ),
			( core ) => new MLowLevelInput ( core ),
			( core ) => new MInputReader ( core ),
			( core ) => new MInputMerger ( core ),
			( core ) => new MInputProcessor ( core ),
			( core ) => new MDataSigner ( core ),
			( core ) => MPacketSender.Fetch ( 0, core ),
			( core ) => new VMainAppControls ( core ),
			( core ) => new VShortcutWorker ( core ),
			( core ) => new VCommandWorker ( core ),
			( core ) => new VComponentJoiner ( core ),
			selector
			);
		else return new VMainAppCore (
			( core ) => new MEventVector ( core ),
			( core ) => new MLowLevelInput ( core ),
			( core ) => new VInputReader_KeyboardHook ( core ),
			( core ) => new VInputMerger ( core ),
			( core ) => new VInputProcessor ( core ),
			( core ) => new VDataSigner ( core ),
			( core ) => new VPacketSender ( core ),
			( core ) => new VMainAppControls ( core ),
			( core ) => new VShortcutWorker ( core ),
			( core ) => new VCommandWorker ( core ),
			( core ) => new VComponentJoiner ( core ),
			selector
			);
	}

	public static VMainAppCore CreateDefault ( DMainAppCore.CompSelect selector = DMainAppCore.CompSelect.All, params System.Action<DMainAppCore>[] extras ) {
		var factory = new DMainAppCoreFactory ();
		factory.PreferMocks = false;
		var core = factory.CreateVMainAppCore ( selector );
		foreach ( var extra in extras ) extra ( core );
		return core;
	}

	public static void AddJoiners ( DMainAppCore core ) {
		var compJoiner = core.Fetch<DComponentJoiner> ();
		if ( compJoiner == null ) throw new ArgumentNullException ( nameof ( core ), "Provided core does not have any Joiner component!" );

		DComponentJoiner.TryRegisterJoiner<DInputProcessor, DDataSigner, InputData> ( compJoiner, ( joiner, signer, data ) => {
			// Encrypt InputProcessor callback data
			byte[] bin = data.Serialize ();
			HMessageHolder msg = new ( HMessageHolder.MsgFlags.None, bin );
			return (true, signer.Encrypt ( msg ));
		} );
		DComponentJoiner.TryRegisterJoiner<DDataSigner, DPacketSender, HMessageHolder> ( compJoiner, ( joiner, sender, msg ) => {
			// Send encrypted data
			sender.Send ( msg );
			return (true, null);
		} );
		DComponentJoiner.TryRegisterJoiner<DPacketSender, DDataSigner, HMessageHolder> ( compJoiner, ( joiner, signer, msg ) => {
			// Decrypt received data
			return (true, signer.Decrypt ( msg ));
		} );
		DComponentJoiner.TryRegisterJoiner<DDataSigner, DInputSimulator, HMessageHolder> ( compJoiner, ( joiner, simulator, msg ) => {
			// Simulate input from decrypted data
			byte[] data = msg.InnerMsg;
			InputData input = new ( joiner );
			input.Deserialize ( data, overwrite: true );
			var pressAr = simulator.ParseCommand ( input );
			return (true, simulator.Simulate ( pressAr ));
		} );

		DComponentJoiner.TryRegisterJoiner<DInputReader, DInputMerger, HInputEventDataHolder> ( compJoiner, ( joiner, merger, data ) =>
			(true, merger.ProcessInput ( data )) );
		DComponentJoiner.TryRegisterJoiner<DInputMerger, DInputProcessor, HInputEventDataHolder[]> ( compJoiner, ( joiner, processor, data ) => {
			processor.ProcessInput ( data );
			return (true, null);
		} );
	}
}