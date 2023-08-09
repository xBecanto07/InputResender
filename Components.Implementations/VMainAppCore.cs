using Components.Interfaces;

namespace Components.Implementations {
	public class VMainAppCore : DMainAppCore {
		public VMainAppCore ( Func<DMainAppCore, DEventVector> CreateEventVector,
			Func<DMainAppCore, DLowLevelInput> CreateLowLevelInput,
			Func<DMainAppCore, DInputReader> CreateInputReader,
			Func<DMainAppCore, DInputParser> CreateInputParser,
			Func<DMainAppCore, DInputProcessor> CreateInputProcessor,
			Func<DMainAppCore, DDataSigner> CreateDataSigner,
			Func<DMainAppCore, DPacketSender> CreatePacketSender,
			Func<DMainAppCore, DMainAppControls> CreateMainAppControls,
			Func<DMainAppCore, DShortcutWorker> CreateShortcutWorker,
			Func<DMainAppCore, DCommandWorker> CreateCommandWorker,
			CompSelect componentMask = CompSelect.All ) : base ( CreateEventVector, CreateLowLevelInput, CreateInputReader, CreateInputParser, CreateInputProcessor, CreateDataSigner, CreatePacketSender, CreateMainAppControls, CreateShortcutWorker, CreateCommandWorker, componentMask ) {
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