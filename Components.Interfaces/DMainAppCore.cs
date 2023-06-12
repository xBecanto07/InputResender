using Components.Interfaces.Networking;
using Components.Library;
using System.Net;

namespace Components.Interfaces
{
    public abstract class DMainAppCore : CoreBase {
		public DEventVector EventVector { get => Fetch<DEventVector> (); }
		public DLowLevelInput LowLevelInput { get => Fetch<DLowLevelInput> (); }
		public DInputReader InputReader { get => Fetch<DInputReader> (); }
		public DInputParser InputParser { get => Fetch<DInputParser> (); }
		public DInputProcessor InputProcessor { get => Fetch<DInputProcessor> (); }
		public DDataSerializer<int> DataSerializer { get => Fetch<DDataSerializer<int>> (); }
		public DDataSigner DataSigner { get => Fetch<DDataSigner> (); }
		public DPacketSender<IPEndPoint> PacketSender { get => Fetch<DPacketSender<IPEndPoint>> (); }

		public DMainAppCore (
			Func<DMainAppCore, DEventVector> CreateEventVector,
			Func<DMainAppCore, DLowLevelInput> CreateLowLevelInput,
			Func<DMainAppCore, DInputReader> CreateInputReader,
			Func<DMainAppCore, DInputParser> CreateInputParser,
			Func<DMainAppCore, DInputProcessor> CreateInputProcessor,
			Func<DMainAppCore, DDataSerializer<int>> CreateDataSerializer,
			Func<DMainAppCore, DDataSigner> CreateDataSigner,
			Func<DMainAppCore, DPacketSender<IPEndPoint>> CreatePacketSender
			) {
			HashSet<string> missingComponents = new HashSet<string> ();
			CreateComponent ( CreateEventVector, nameof ( DEventVector ) );
			CreateComponent ( CreateLowLevelInput, nameof ( DLowLevelInput ) );
			CreateComponent ( CreateInputReader, nameof ( DInputReader ) );
			CreateComponent ( CreateInputParser, nameof ( DInputParser ) );
			CreateComponent ( CreateInputProcessor, nameof ( DInputProcessor ) );
			CreateComponent ( CreateDataSerializer, nameof ( DDataSerializer<int> ) );
			CreateComponent ( CreateDataSigner, nameof ( DDataSigner ) );
			CreateComponent ( CreatePacketSender, nameof ( DPacketSender<IPEndPoint> ) );

			if (missingComponents.Count > 0) {
				var SB = new System.Text.StringBuilder ();
				SB.AppendLine ();
				foreach (var item in missingComponents) SB.AppendLine ( $"  {item}" );
				throw new NullReferenceException ( $"Missing constructors for following components:{SB}" );
			}

			void CreateComponent<T> (Func<DMainAppCore, T> creator, string name) where T : ComponentBase {
				if ( creator == null ) missingComponents.Add ( name );
				else {
					var comp = creator ( this );
					if ( !IsRegistered ( nameof ( T ) ) ) Register ( comp );
				}
			}
		}

		public abstract void Initialize ();
		public abstract void LoadComponents ();
		public abstract void LoadConfiguration ( string path );
		public abstract void SaveConfiguration ( string path );
		public abstract void RunApp ();
	}
}
