using Components.Library;
using System.Net;

namespace Components.Interfaces {
	public abstract class DMainAppCore : CoreBase {
		public DEventVector EventVector { get => Fetch<DEventVector> (); }
		public DInputReader InputReader { get => Fetch<DInputReader> (); }
		public DInputParser InputParser { get => Fetch<DInputParser> (); }
		public DInputProcessor InputProcessor { get => Fetch<DInputProcessor> (); }
		public DDataSerializer<int> DataSerializer { get => Fetch<DDataSerializer<int>> (); }
		public DDataSigner DataSigner { get => Fetch<DDataSigner> (); }
		public DPacketSender<IPEndPoint> PacketSender { get => Fetch<DPacketSender<IPEndPoint>> (); }

		public DMainAppCore (
			Func<DMainAppCore, DEventVector> CreateEventVector,
			Func<DMainAppCore, DInputReader> CreateInputReader,
			Func<DMainAppCore, DInputParser> CreateInputParser,
			Func<DMainAppCore, DInputProcessor> CreateInputProcessor,
			Func<DMainAppCore, DDataSerializer<int>> CreateDataSerializer,
			Func<DMainAppCore, DDataSigner> CreateDataSigner,
			Func<DMainAppCore, DPacketSender<IPEndPoint>> CreatePacketSender

			) {
			CreateComponent ( CreateEventVector );
			CreateComponent ( CreateInputReader );
			CreateComponent ( CreateInputParser );
			CreateComponent ( CreateInputProcessor );
			CreateComponent ( CreateDataSerializer );
			CreateComponent ( CreateDataSigner );
			CreateComponent ( CreatePacketSender );

			void CreateComponent<T> (Func<DMainAppCore, T> creator) where T : ComponentBase {
				var comp = creator ( this );
				if ( !IsRegistered ( nameof ( T ) ) ) Register ( comp );
			}
		}

		public abstract void Initialize ();
		public abstract void LoadComponents ();
		public abstract void LoadConfiguration ( string path );
		public abstract void SaveConfiguration ( string path );
		public abstract void RunApp ();
	}
}
