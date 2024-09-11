using Components.Implementations;
using Components.Interfaces;
using Components.Library;
using InputResender.CLI;
using InputResender.WindowsGUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace InputResender.UserTesting {
	public class TapperInputUserTest : UserTestBase {
		DMainAppCoreFactory CoreFactory;
		DMainAppCore Core;
		DTextWriter Writer;
		AutoResetEvent Waiter;
		const string Text = "Hello, World!";

		public TapperInputUserTest ( StringBuilder sb ) : base ( sb ) {
			if ( CoreFactory == null ) CoreFactory = new DMainAppCoreFactory ();
			Core = CoreFactory.CreateVMainAppCore ( DMainAppCore.CompSelect.All & (~DMainAppCore.CompSelect.InputProcessor) );
			new VTapperInput ( Core, new KeyCode[5] { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.Space }, InputData.Modifier.None );
			Writer = new MTextWriter ( Core );
			Waiter = new AutoResetEvent ( false );
		}

		protected override void Dispose ( bool disposing ) {

		}

		public IEnumerable<Action> WritingTest () {
			Program.WriteLine ( $"Press 'T' key to start the test and then type text '{Text}' ... " );
			Program.ClearInput ();
			yield return () => ReserveChar ( "et" );
			if ( ShouldCancel () ) yield break;
			Program.WriteLine ( "--XXX : Shift\t\t--XXX : Shift\r\n-XX-X : H \t\t-X-XX : W \t\r\n--X-- : e \t\tX---- : o \t\r\nXXXX- : l \t\t-X-X- : r \t\r\nXXXX- : l \t\tXXXX- : l \t\r\nX---- : o \t\t---XX : d \t\r\n----X 2 , \t\tXXX-- : Switch\r\nX-X-- : Space\t\tXXXXX : ! " );

			Core.InputProcessor.Callback = ProcessedCallback;
			HHookInfo hookInfo = new HHookInfo ( Core.InputReader, 1, VKChange.KeyDown, VKChange.KeyUp );
			var hookIDs = Core.InputReader.SetupHook ( hookInfo, Callback, DelayedCallback );
			foreach ( var ID in hookIDs ) hookInfo.AddHookID ( ID );

			for (int i = 0; i < 300; i++ ) {
				bool cont = false;
				yield return () => cont = Waiter.WaitOne (1000);
				if ( !cont ) continue;

				i = 0;
				string ss = Writer.Text;
				if ( ss.StartsWith ( Text ) ) break;
				if ( ss.Contains ( "eee" ) ) { SB.AppendLine ( "Test canceled." ); break; }
				if ( ss.Length > 30 ) { SB.AppendLine ( "Too many text." ); break; }
			}

			if (Writer.Text.StartsWith( Text ) ) {
				Result.Passed = true;
				SB.AppendLine ( $"Passed with {Writer.Text}" );
			}

			Core.InputReader.ReleaseHook ( hookInfo );

			yield break;
		}

		private bool Callback ( DictionaryKey key, HInputEventDataHolder inputData ) {
			return false;
		}

		private void DelayedCallback ( DictionaryKey key, HInputEventDataHolder inputData ) {
			var lastEvent = VWinLowLevelLibs.EventList[0];
			//Program.WriteLine ( $"Pressed: {inputData}    (parsed from: {lastEvent.nCode}|{lastEvent.changeCode}|{lastEvent.inputData})" );
			if ( inputData == null ) return;
			if ( Result.Passed ) return;
			var combo = Core.InputParser.ProcessInput ( inputData );
			Core.InputProcessor.ProcessInput ( combo );
		}
		private void ProcessedCallback (InputData command ) {
			if ( !Writer.Type ( command ) ) return;
			Program.WriteLine ( $"Typed: {Writer.Text}" );
			Waiter.Set ();
		}
	}
}