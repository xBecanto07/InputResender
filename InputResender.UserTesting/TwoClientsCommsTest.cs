using Components.Factories;
using Components.Implementations;
using Components.Interfaces;
using Components.Library;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SBld = System.Text.StringBuilder;

namespace InputResender.UserTesting {
	internal class TwoClientsCommsTest : UserTestBase {
		Queue<byte[]> RecvData;
		string Password = "Blbosti1";
		DMainAppCoreFactory CoreFactory;
		DMainAppCore Core;
		IPEndPoint TargEP;
		InputData Combo;
		AutoResetEvent waiter;

		public TwoClientsCommsTest ( SBld sb ) : base ( sb ) {
			if ( CoreFactory == null ) CoreFactory = new DMainAppCoreFactory ();
			Core = CoreFactory.CreateVMainAppCore ();
			Core.DataSigner.Key = Core.DataSigner.GenerateIV ( System.Text.Encoding.UTF8.GetBytes ( Password ) );
			Combo = new InputData ( Core.DataSigner );
			RecvData = new Queue<byte[]> ();
			waiter = new AutoResetEvent ( false );
		}

		protected override void Dispose ( bool disposing ) { }

		public IEnumerable<Action> Sender () {
			if ( ShouldCancel ( ClientState.Master ) ) yield break;
			foreach ( var a in RequestIPEP () ) yield return a;

			Program.WriteLine ( "Start test by double pressing 'S'. Then to finish press 'Shift+C' on second machine." );
			yield return () => ReserveChar ( "es" );
			if ( ShouldCancel () ) yield break;

			HHookInfo hookInfo = new HHookInfo ( Core.InputReader, 1, VKChange.KeyDown );
			var hookIDs = Core.InputReader.SetupHook ( hookInfo, Callback, DelayedCallback );
			foreach ( var ID in hookIDs ) hookInfo.AddHookID ( ID );

			foreach ( var a in WaitForMessage ( KeyCode.C, InputData.Modifier.Shift ) ) yield return a;

			Core.InputReader.ReleaseHook ( hookInfo );
			Result.Passed = true;
			yield break;
		}

		public IEnumerable<Action> Listener () {
			if ( ShouldCancel ( ClientState.Slave ) ) yield break;
			foreach ( var a in RequestIPEP () ) yield return a;

			Program.WriteLine ( "Test will start, when you press 'S' on second machine. Then press 'Shift+C'" );
			foreach ( var a in WaitForMessage ( KeyCode.S, InputData.Modifier.None ) ) yield return a;

			HHookInfo hookInfo = new HHookInfo ( Core.InputReader, 1, VKChange.KeyDown );
			var hookIDs = Core.InputReader.SetupHook ( hookInfo, Callback, DelayedCallback );
			foreach ( var ID in hookIDs ) hookInfo.AddHookID ( ID );

			yield return () => waiter.WaitOne ();

			Result.Passed = true;

			yield break;
		}

		private IEnumerable<Action> RequestIPEP () {
			Program.WriteLine ( $"Enter target IP End Point ... (This EP seems to be: {Core.PacketSender.OwnEP ( 1, 0 )})" );
			while ( true ) {
				string targEPss = "";
				yield return () => { targEPss = Program.ReadLine (); };
				if ( IPEndPoint.TryParse ( targEPss, out TargEP ) ) {
					Core.PacketSender.Connect ( TargEP );
					break;
				}
				Program.WriteLine ( $"Cannot parse '{targEPss}' to a valid IP End point!" );
			}
			yield break;
		}
		private IEnumerable<Action> WaitForMessage (KeyCode key, InputData.Modifier mods) {
			Program.WriteLine ( $"Waiting for {InputData.Command.KeyPress} | {key} | {mods}" );
			AutoResetEvent waiter = new AutoResetEvent ( false );
			Core.PacketSender.ReceiveAsync ( ( data ) => {
				var msg = Core.DataSigner.Decrypt ( data );
				InputData combo = (InputData)Combo.Deserialize ( msg );
				if ( combo == null ) return true;
				Program.WriteLine ( $"Received: {combo.Cmnd} | {combo.Key} | {combo.Modifiers}" );
				if ( combo.Cmnd != InputData.Command.KeyPress ) return true;
				if ( combo.Key != key ) return true;
				if ( combo.Modifiers != mods ) return true;
				waiter.Set ();
				return false;
			} );
			yield return () => waiter.WaitOne ();
			yield break;
		}

		private bool Callback ( DictionaryKey key, HInputEventDataHolder inputData ) {
			return true;
		}
		private void DelayedCallback ( DictionaryKey key, HInputEventDataHolder inputData ) {
			byte[] msg;
			if (UserTestApp.ClientState == ClientState.Slave ) {
				msg = Core.EncryptInput ( inputData, out InputData combo );
				Program.WriteLine ( $"Input: {combo.Cmnd} | {combo.Key} | {combo.Modifiers}" );
				if ( combo.Cmnd != InputData.Command.KeyPress ) return;
				if ( combo.Key != KeyCode.C ) return;
				if ( combo.Modifiers != InputData.Modifier.Shift ) return;
				Program.WriteLine ( "Sending C+Shift" );
				waiter.Set ();
			} else {
				msg = Core.EncryptInput ( inputData );
			}
			Core.PacketSender.Send ( msg );
		}
	}
}