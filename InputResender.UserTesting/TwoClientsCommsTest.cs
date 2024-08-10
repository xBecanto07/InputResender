using Components.Factories;
using Components.Implementations;
using Components.Interfaces;
using Components.Library;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using static Components.Interfaces.DPacketSender;
using static Components.Interfaces.InputData;
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
			Core.InputProcessor.Callback = ProcessedCallback;
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
			Core.ShouldDefaultHookResend = true;
			var hookIDs = Core.InputReader.SetupHook ( hookInfo, Core.DefaultFastHooCallback, Core.DefaultDelayedCallback );
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
			Core.ShouldDefaultHookResend = true;
			var hookIDs = Core.InputReader.SetupHook ( hookInfo, Core.DefaultFastHooCallback, Core.DefaultDelayedCallback );
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
				if ( targEPss.StartsWith ( "ee" ) ) yield break;
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
			Core.PacketSender.OnReceive += ( data, isProcessed ) => {
				var msg = Core.DataSigner.Decrypt ( data );
				InputData combo = (InputData)Combo.Deserialize ( msg );
				if ( combo == null ) return CallbackResult.Skip;
				Program.WriteLine ( $"Received: {combo.Cmnd} | {combo.Key} | {combo.Modifiers}" );
				if ( combo.Cmnd != InputData.Command.KeyPress ) return CallbackResult.Skip;
				if ( combo.Key != key ) return CallbackResult.Skip;
				if ( combo.Modifiers != mods ) return CallbackResult.Skip;
				waiter.Set ();
				return CallbackResult.Stop;
			};
			yield return () => waiter.WaitOne ();
			yield break;
		}

		private void ProcessedCallback (InputData combo ) {
			byte[] msg = Core.DataSigner.Encrypt ( combo.Serialize () );
			if ( UserTestApp.ClientState == ClientState.Slave ) {
				Program.WriteLine ( $"Input: {combo.Cmnd} | {combo.Key} | {combo.Modifiers}" );
				if ( combo.Cmnd != Command.KeyPress ) return;
				if ( combo.Key != KeyCode.C ) return;
				if ( combo.Modifiers != Modifier.Shift ) return;
				Program.WriteLine ( "Sending C+Shift" );
				waiter.Set ();
			}
			Core.PacketSender.Send ( msg );
		}
	}
}