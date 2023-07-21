﻿using Components.Interfaces;
using Components.Library;
using InputResender.GUIComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SBld = System.Text.StringBuilder;
using System.Threading;
using System.Runtime.InteropServices;

namespace InputResender.UserTesting {
	public class ClientSideHookUserTest : UserTestBase {
		public static ClientState SupportedState;

		CoreBase Core;
		DLowLevelInput LLInput;
		nint hookID;
		InputCapture inputCapture;


		public ClientSideHookUserTest ( SBld sb ) : base ( sb ) {
			Core = new CoreBaseMock ();
			LLInput = new VWinLowLevelLibs ( Core );
			hookID = 0;
			inputCapture = new InputCapture ( LLInput, hookID );

		}
		protected override void Dispose ( bool disposing ) {
			if ( hookID != 0 ) LLInput.UnhookHookEx ( hookID );
			Core.Unregister ( LLInput );
		}

		public IEnumerable<Action> HookTest () {
			// Wait for user to be ready
			Program.WriteLine ( "Press 'K' key twice (press and release, once to start hook, second to test the hook) ... " );
			yield return () => ReserveChar ( "ek" );
			if ( ShouldCancel () ) yield break;
			// Since only E or K are accepted, if cancel is not requested (by E), than we should continue

			inputCapture.Clear ();

			inputCapture.StartHook ( Program.WriteLine );
			yield return () => { inputCapture.WaitForKey ( KeyCode.K, VKChange.KeyDown ); };

			inputCapture.ReleaseHook ();
			if ( !inputCapture.LastResult ) yield break;

			Result.Passed = inputCapture.ParseResult ( Program.WriteLine );
			yield break;
		}
	}
}