﻿using Components.Library;
using System.Net;

namespace Components.Interfaces;
public abstract class DMainAppCore : CoreBase {
	[Flags]
	public enum CompSelect { None = 0, EventVector = 1, LLInput = 2, InputReader = 4, InputParser = 8, InputProcessor = 16, DataSigner = 32, PacketSender = 64, MainAppControls = 128, ShortcutWorker = 256, CommandWorker = 512, All = 0xFFFF }

	public DEventVector EventVector { get => Fetch<DEventVector> (); }
	public DLowLevelInput LowLevelInput { get => Fetch<DLowLevelInput> (); }
	public DInputReader InputReader { get => Fetch<DInputReader> (); }
	public DInputParser InputParser { get => Fetch<DInputParser> (); }
	public DInputProcessor InputProcessor { get => Fetch<DInputProcessor> (); }
	public DDataSigner DataSigner { get => Fetch<DDataSigner> (); }
	public DPacketSender PacketSender { get => Fetch<DPacketSender> (); }
	public DMainAppControls MainAppControls { get => Fetch<DMainAppControls> (); }
	public DShortcutWorker ShortcutWorker { get => Fetch<DShortcutWorker> (); }
	public DCommandWorker CommandWorker { get => Fetch<DCommandWorker> (); }

	public DMainAppCore (
		Func<DMainAppCore, DEventVector> CreateEventVector,
		Func<DMainAppCore, DLowLevelInput> CreateLowLevelInput,
		Func<DMainAppCore, DInputReader> CreateInputReader,
		Func<DMainAppCore, DInputParser> CreateInputParser,
		Func<DMainAppCore, DInputProcessor> CreateInputProcessor,
		Func<DMainAppCore, DDataSigner> CreateDataSigner,
		Func<DMainAppCore, DPacketSender> CreatePacketSender,
		Func<DMainAppCore, DMainAppControls> CreateMainAppControls,
		Func<DMainAppCore, DShortcutWorker> CreateShortcutWorker,
		Func<DMainAppCore, DCommandWorker> CreateCommandWorker,
		CompSelect componentMask = CompSelect.All
		) {

		int compID = 1;
		HashSet<string> missingComponents = new HashSet<string> ();
		CreateComponent ( CreateEventVector, nameof ( DEventVector ) );
		CreateComponent ( CreateLowLevelInput, nameof ( DLowLevelInput ) );
		CreateComponent ( CreateInputReader, nameof ( DInputReader ) );
		CreateComponent ( CreateInputParser, nameof ( DInputParser ) );
		CreateComponent ( CreateInputProcessor, nameof ( DInputProcessor ) );
		CreateComponent ( CreateDataSigner, nameof ( DDataSigner ) );
		CreateComponent ( CreatePacketSender, nameof ( DPacketSender ) );
		CreateComponent ( CreateMainAppControls, nameof ( DMainAppControls ) );
		CreateComponent ( CreateShortcutWorker, nameof ( DShortcutWorker ) );
		CreateComponent ( CreateCommandWorker, nameof ( DCommandWorker ) );

		if ( missingComponents.Count > 0 ) {
			var SB = new System.Text.StringBuilder ();
			SB.AppendLine ();
			foreach ( var item in missingComponents ) SB.AppendLine ( $"  {item}" );
			throw new NullReferenceException ( $"Missing constructors for following components:{SB}" );
		}

		void CreateComponent<T> ( Func<DMainAppCore, T> creator, string name ) where T : ComponentBase {
			int locCompID = compID;
			compID <<= 1;
			if ( ((int)componentMask & locCompID) == 0 ) return;

			if ( creator == null ) missingComponents.Add ( name );
			else {
				var comp = creator ( this );
				if ( comp == null ) return; // Component was intented to not be created, otherwise the creator itself should throw exception
				if ( !IsRegistered<T> () ) Register ( comp );
			}
		}
	}

	public abstract void Initialize ();
	public abstract void LoadComponents ();
	public abstract void LoadConfiguration ( string path );
	public abstract void SaveConfiguration ( string path );
	public abstract void RunApp ();

	public bool ShouldDefaultHookResend;
	public bool DefaultFastHooCallback ( DictionaryKey key, HInputEventDataHolder inputData ) => ShouldDefaultHookResend;
	public void DefaultDelayedCallback ( DictionaryKey key, HInputEventDataHolder inputData ) {
		var combo = InputParser.ProcessInput ( inputData );
		InputProcessor.ProcessInput ( combo );
	}

	public static MMainAppCore CreateMock ( CompSelect selector = CompSelect.All ) => new MMainAppCore (
			( core ) => new MEventVector ( core ),
			( core ) => new MLowLevelInput ( core ),
			( core ) => new MInputReader ( core ),
			( core ) => new MInputParser ( core ),
			( core ) => new MInputProcessor ( core ),
			( core ) => new MDataSigner ( core ),
			( core ) => MPacketSender.Fetch ( 0, core ),
			( core ) => new VMainAppControls ( core ),
			( core ) => null,
			( core ) => new VCommandWorker ( core ),
			selector
			);
}


public class MMainAppCore : DMainAppCore {
	public MMainAppCore ( Func<DMainAppCore, DEventVector> CreateEventVector,
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

	public override void Initialize () {}
	public override void LoadComponents () {}
	public override void LoadConfiguration ( string path ) {}
	public override void SaveConfiguration ( string path ) {}
	public override void RunApp () {}
}