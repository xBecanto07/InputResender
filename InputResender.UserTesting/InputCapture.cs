using Components.Interfaces;
using Components.Library;
using InputResender.GUIComponents;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace InputResender.UserTesting {
	public class InputCapture {
		public AutoResetEvent waiter;
		public List<InputInfo> messages;
		public InputInfo LastMessage { get; private set; }
		public bool LastResult { get; private set; }
		public bool ResendEx = false;
		readonly DLowLevelInput LLInput;
		Hook hookID;

		public InputCapture ( DLowLevelInput llInput ) {
			waiter = new AutoResetEvent ( false );
			messages = new List<InputInfo> ();
			LLInput = llInput;
		}

		public struct InputInfo {
			public int nCode;
			public DictionaryKey HookKey;
			public HInputData InputData;

			public VKChange changeType { get => InputData.Pressed; }
			public KeyCode keyCode { get => InputData.Key;
			}
			public InputInfo ( DictionaryKey key, HInputData data ) { nCode = 0; HookKey = key; InputData = data; }

			public InputInfo ( int nnCode ) { nCode = nnCode; HookKey = new DictionaryKey ( 0 ); InputData = null; }
		}

		public bool ParseResult (Action<string> log) {
			int N = LastMessage.nCode;
			if ( N < 0 ) log ( "No messages found! (might be issue with test itself)" );
			if ( N > 0 ) log ( $"Received {N} keyboard events, but none fits expected result!" );
			else log ( "Test passed" );
			return N == 0;
		}
		public bool Accept ( InputInfo info ) { LastMessage = info; return LastResult = true; }
		public bool Reject ( InputInfo info ) { LastMessage = info; return LastResult = false; }
		public bool Callback ( DictionaryKey hookKey, HInputData inputData ) {
			messages.Add ( new ( hookKey, inputData ) );
			Thread.MemoryBarrier ();
			waiter.Set ();
			return true;
		}
		public void Clear () {
			waiter.Reset ();
			messages.Clear ();
			LastMessage = new InputInfo ( -3 );
			LastResult = false;
		}

		public bool WaitForKey ( KeyCode key, VKChange change ) {
			bool recv;
			InputInfo message = new InputInfo ( -2 );
			waiter.Reset ();
			for ( int i = 0; i < 10; i++ ) {
				recv = waiter.WaitOne ( 1000 );
				if ( recv ) {
					message = TestOnKey ( key, change );
					if ( message.nCode > 0 ) {
						Program.WriteLine ( $"Expected '{key}'({change}) key closest of {message.nCode} messages is '{message.keyCode}'({message.changeType}). Remaining {10 - i} attempts!" );
					} else if ( message.nCode == 0 ) return Accept ( message );
				}
			}
			return Reject ( message );
		}
		public InputInfo TestOnKey ( KeyCode key, VKChange actType ) {
			if ( messages.Count == 0 ) return new ( -1 );
			var ret = messages[0];
			foreach ( var message in messages ) {
				if ( message.keyCode != key ) continue;
				if ( message.changeType != actType ) continue;
				ret = message;
				ret.nCode = 0;
				return ret;
			}
			ret.nCode = messages.Count;
			return ret;
		}

		public void StartHook (Action<string> errLog) {
			var moduleHandle = LLInput.GetModuleHandleID ( "user32.dll" );
			hookID = LLInput.SetHookEx ( Callback );
			if ( hookID == null ) {
				LLInput.PrintErrors ( ( ss ) => errLog ( ss ) );
				throw new ApplicationException ( $"Error while setting up a hook!" );
			}
		}
		public void ReleaseHook () {
			if ( hookID == null ) return;
			LLInput.UnhookHookEx ( hookID );
			hookID = null;
		}
	}
}