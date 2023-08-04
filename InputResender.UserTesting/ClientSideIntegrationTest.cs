using Components.Factories;
using Components.Implementations;
using Components.Interfaces;
using Components.Library;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SBld = System.Text.StringBuilder;

namespace InputResender.UserTesting {
	public class ClientSideIntegrationTest : UserTestBase {
		public static ClientState SupportedState = ClientState.Master;
		Queue<byte[]> RecvData;
		string Password = "Blbosti1";

		DMainAppCoreFactory CoreFactory;
		DMainAppCore Core;

		public ClientSideIntegrationTest ( SBld sb ) : base ( sb ) {
			RecvData = new Queue<byte[]> ();
			if ( CoreFactory == null ) CoreFactory = new DMainAppCoreFactory ();
			Core = CoreFactory.CreateVMainAppCore ();
		}
		protected override void Dispose ( bool disposing ) { }

		public IEnumerable<Action> InputProcessingTest () {
			Program.WriteLine ( "Press 'H' key to start the test. Then press combination of Ctrl + Shift + H." );
			yield return () => ReserveChar ( "eh" );
			if ( ShouldCancel () ) yield break;

			Core.InputProcessor.Callback = ProcessedCallback;
			HHookInfo hookInfo = new HHookInfo ( Core.InputReader, 1, VKChange.KeyDown, VKChange.KeyUp );
			var hookIDs = Core.InputReader.SetupHook ( hookInfo, Callback, DelayedCallback );
			foreach ( var ID in hookIDs ) hookInfo.AddHookID ( ID );

			var expMod = InputData.Modifier.Ctrl | InputData.Modifier.Shift;
			int tryCnt = 20;
			while (true) {
				if ( tryCnt < 1 ) { SB.AppendLine ( "Too many wrong attempts!" ); break; }
				while (RecvData.Count < 1) yield return null;

				var msg = RecvData.Dequeue ();
				if ( msg == null ) yield return null;

				var signer = Core.Fetch<DDataSigner> ();
				signer.Key = signer.GenerateIV ( System.Text.Encoding.UTF8.GetBytes ( Password ) );
				var decrypted = signer.Decrypt ( msg );
				var combo = new InputData ( signer );
				combo = (InputData)combo.Deserialize ( decrypted );
				if ( !combo.Pressed ) continue;
				if ( combo.Modifiers != expMod ) {
					Program.WriteLine ( $"({expMod})expected, {combo.Modifiers} active. Waiting for more input ({tryCnt} left)" );
					continue;
				}
				tryCnt--;
				if (combo.Key != KeyCode.H) {
					Program.WriteLine ( $"Expected key '{KeyCode.H}' but received '{combo.Key}'. Waiting for more input ({tryCnt} left)" );
					continue;
				}
				SB.AppendLine ( $"Passed with message: ({combo})" );
				Result.Passed = true;
				yield return null;
				break;
			}

			Core.InputReader.ReleaseHook ( hookInfo );

			RecvData.Clear ();
			RecvData = null;
			yield break;
		}

		private bool Callback ( DictionaryKey key, HInputEventDataHolder inputData ) {
			return true;
		}

		private void DelayedCallback ( DictionaryKey key, HInputEventDataHolder inputData ) {
			if ( inputData == null ) return;
			if ( Result.Passed ) return;

			Program.WriteLine ( $" - Received input {(KeyCode)inputData.InputCode} {(inputData.Pressed >= 1 ? "KeyDown" : "KeyUp")}" );
			Core.DataSigner.Key = Core.DataSigner.GenerateIV ( System.Text.Encoding.UTF8.GetBytes ( Password ) );
			var combo = Core.InputParser.ProcessInput ( inputData );
			Core.InputProcessor.ProcessInput ( combo );
		}
		private void ProcessedCallback (InputData inputData) {
			var msg = Core.DataSigner.Encrypt ( inputData.Serialize () );
			RecvData.Enqueue ( msg );
			Program.SendSignal ();
		}
	}
}