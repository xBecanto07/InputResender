using Components.Interfaces;
using Components.Library;
using InputResender.GUIComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TaskRes = System.ValueTuple<bool, string>;
using SBld = System.Text.StringBuilder;
using System.Threading;
using System.Runtime.InteropServices;

namespace InputResender.UserTesting {
	public class ClientSideHookUserTest : UserTestBase {
		public static ClientState SupportedState;

		CoreBase Core;
		DLowLevelInput LLInput;
		nint hookID;

		AutoResetEvent waiter;
		List<(int nCode, VKChange changeType, KeyCode keyCode, Input.KeyboardInput inputData)> messages;


		public ClientSideHookUserTest ( SBld sb ) : base ( sb ) {
			Core = new CoreBaseMock ();
			LLInput = new VWinLowLevelLibs ( Core );
			hookID = 0;
			waiter = new AutoResetEvent ( false );
			messages = new List<(int, VKChange, KeyCode, Input.KeyboardInput)> ();

		}
		protected override void Dispose ( bool disposing ) {
			if ( hookID != 0 ) LLInput.UnhookHookEx ( hookID );
			Core.Unregister ( LLInput );
		}

		public bool TestSimulateKeypress () {
			var inputData = WinLLInputData.NewKeyboardData ( LLInput, (ushort)KeyCode.F, (ushort)KeyCode.F, 0, 123456, LLInput.GetMessageExtraInfoPtr () );
			var sent = LLInput.SimulateInput ( 1, new[] { inputData }, inputData.SizeOf, true );
			LLInput.PrintErrors ( Program.WriteLine );
			return sent == 1;
		}

		public bool HookTest () {
			var EventList = VWinLowLevelLibs.EventList;
			Program.WriteLine ( "Press 'K' key twice (press and release, once to start hook, second to test the hook) ... " );
			while ( true ) {
				switch ( Program.Read () ) {
				case 'e': SB.AppendLine ( "Canceling the test." ); return false;
				case 'k': break;
				default: continue;
				}
				messages.Clear ();
				SetupHook ();
				var message = TestOnKey ( KeyCode.K, VKChange.KeyDown );
				ReleaseHook ();
				if ( message.nCode < 0 ) {
					Program.WriteLine ( "No messages found! (might be issue with test itself)" );
					return false;
				}
				if ( message.nCode > 0 ) {
					Program.WriteLine ( $"Received {message.nCode} keyboard events, but none fits expected result!" );
					return false;
				}
				return true;
			}
			//return ( true );
		}




		private (int nCode, VKChange changeType, KeyCode keyCode, Input.KeyboardInput inputData) TestOnKey ( KeyCode key, VKChange actType ) {
			if ( !waiter.WaitOne ( 10000 ) )
				return (-1, default, default, default);
			foreach ( var message in messages ) {
				if ( message.keyCode != key ) continue;
				if ( message.changeType != actType ) continue;
				var ret = message;
				ret.nCode = 0;
				return ret;
			}
			return (messages.Count, default, default, default);
		}

		public nint Callback ( int nCode, nint wParam, nint lParam ) {
			messages.Add ( (nCode, LLInput.GetChangeType ( (int)wParam ), (KeyCode)Marshal.ReadInt32 ( lParam ), new Input.KeyboardInput ( lParam )) );
			Thread.MemoryBarrier ();
			waiter.Set ();
			return LLInput.CallNextHook ( 0, nCode, wParam, lParam ); // return 1;
		}

		private void SetupHook () {
			using ( Process curProcess = Process.GetCurrentProcess () )
			using ( ProcessModule curModule = curProcess.MainModule ) {
				var moduleHandle = LLInput.GetModuleHandleID ( curModule.ModuleName );
				hookID = LLInput.SetHookEx ( LLInput.HookTypeCode, Callback, moduleHandle, 0 );
				if ( hookID == 0 ) {
					LLInput.PrintErrors ( ( ss ) => SB.AppendLine ( ss ) );
					throw new ApplicationException ( $"Error while setting up a hook!" );
				}
			}
		}
		private void ReleaseHook () {
			LLInput.UnhookHookEx ( hookID );
			hookID = 0;
		}
	}
}