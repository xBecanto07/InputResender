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
		Queue<HInputEventDataHolder> InputQueue;
		Queue<byte[]> RecvData;
		AutoResetEvent InputSignaler;
		string Password = "Blbosti1";
		Task ProcessingTask;

		DMainAppCoreFactory CoreFactory;
		CoreBase Core;

		public ClientSideIntegrationTest ( SBld sb ) : base ( sb ) {
			InputSignaler = new AutoResetEvent ( false );
			InputQueue = new Queue<HInputEventDataHolder> ();
			RecvData = new Queue<byte[]> ();
			if ( CoreFactory == null ) CoreFactory = new DMainAppCoreFactory ();
			Core = CoreFactory.CreateVMainAppCore ();
			ProcessingTask = Task.Run ( InputProcesser );
		}
		protected override void Dispose ( bool disposing ) {
		}

		public IEnumerable<Action> InputProcessingTest () {
			Program.WriteLine ( "Press 'H' key to start the test. Then press combination of Ctrl + Shift + H." );
			yield return () => ReserveChar ( "eh" );
			if ( ShouldCancel () ) yield break;

			var LLInput = Core.Fetch<DInputReader> ();
			HHookInfo hookInfo = new HHookInfo ( LLInput, 1, VKChange.KeyDown, VKChange.KeyUp );
			var hookIDs = LLInput.SetupHook ( hookInfo, Callback, null );
			foreach ( var ID in hookIDs ) hookInfo.AddHookID ( ID );

			var expMod = InputData.Modifier.Ctrl | InputData.Modifier.Shift;
			int tryCnt = 20;
			while (true) {
				if ( tryCnt < 1 ) { Result.Msg = "Too many wrong attempts!"; break; }
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
				Result.Msg = $"Passed with message: ({combo})";
				Result.Passed = true;
				break;
			}

			LLInput.ReleaseHook ( hookInfo );

			InputQueue.Clear ();
			InputQueue = null;
			InputSignaler.Set ();
			RecvData.Clear ();
			RecvData = null;
			yield return ProcessingTask.Wait;
			ProcessingTask.Dispose ();
			ProcessingTask = null;
			InputSignaler.Dispose ();
			InputSignaler = null;
			yield break;
		}

		public bool Callback ( DictionaryKey key, HInputEventDataHolder inputData ) {
			if ( InputQueue == null ) return true;
			InputQueue.Enqueue ( inputData );
			InputSignaler.Set ();
			return true;
		}

		public void InputProcesser () {
			while (true) {
				InputSignaler.WaitOne ();
				if ( InputQueue == null ) return;
				if ( InputQueue.Count == 0 ) continue;
				var inputData = InputQueue.Dequeue ();
				if ( inputData == null ) continue;

				Program.WriteLine ( $" - Received input {(KeyCode)inputData.InputCode} {(inputData.Pressed>=1?"KeyDown":"KeyUp")}" );
				var inputCombo = Core.Fetch<DInputParser> ().ProcessInput ( inputData );
				var inputCommand = Core.Fetch<DInputProcessor> ().ProcessInput ( inputCombo );
				var signer = Core.Fetch<DDataSigner> ();
				signer.Key = signer.GenerateIV ( System.Text.Encoding.UTF8.GetBytes ( Password ) );
				var IV = signer.GenerateIV ();
				var code = signer.Encrypt ( inputCommand.Serialize (), IV );
				RecvData.Enqueue ( code );
				Program.SendSignal ();
			}
		}
	}
}