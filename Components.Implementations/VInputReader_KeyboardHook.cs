using Components.Interfaces;
using Components.Library;
using System.Diagnostics;

namespace Components.Implementations {
	public class VInputReader_KeyboardHook : DInputReader {
		List<LLHook> HookSet;

		public VInputReader_KeyboardHook ( CoreBase owner ) : base ( owner ) {
			HookSet = new List<LLHook> ();
		}

		public override int ComponentVersion => 1;
		protected DLowLevelinput LowLevelComponent { get { return Owner.Fetch<DLowLevelinput> (); } }

		public override void ReleaseHook ( HHookInfo hookInfo ) {
			int hookID = FindHook ( hookInfo );
			if ( hookID < 0 ) throw new KeyNotFoundException ( $"Couldn't find a hook ID for Hook Definition: {hookInfo}" );
			HookSet.RemoveAt ( hookID );
			LowLevelComponent.UnhookHookEx ( hookInfo.HookID );
		}

		public override void SetupHook ( HHookInfo hookInfo, Func<HInputEventDataHolder, bool> callback ) {
			var lowLevelComponent = LowLevelComponent;
			using ( Process curProcess = Process.GetCurrentProcess () )
			using ( ProcessModule curModule = curProcess.MainModule )
				foreach ( var changeType in hookInfo.ChangeMask )
					HookSet.Add ( new LLHook ( this, lowLevelComponent, callback, hookInfo, changeType, LowLevelComponent.GetModuleHandleID ( curModule.ModuleName ) ) );
		}

		public override void SimulateInput ( HInputEventDataHolder input, bool allowRecapture ) {
			var LLData = LowLevelComponent.GetLowLevelData ( input );
			LowLevelComponent.SimulateInput ( 1, new HInputData[1] { LLData }, LLData.SizeOf );
		}

		int FindHook (HHookInfo hookInfo) {
			int N = HookSet.Count;
			for ( int i = 0; i < N; i++ ) {
				if (HookSet[i].HookInfo == hookInfo ) return i;
			}
			return -1;
		}

		protected class LLHook {
			public readonly nint HookID;
			public readonly Func<HInputEventDataHolder, bool> Callback;
			public readonly HHookInfo HookInfo;
			public readonly DLowLevelinput LowLevelComponent;
			public readonly VKChange KeyChange;
			public readonly VInputReader_KeyboardHook Caller;

			public LLHook ( VInputReader_KeyboardHook caller, DLowLevelinput lowLevelComponent, Func<HInputEventDataHolder, bool> callback, HHookInfo hookInfo, VKChange keyChange, nint moduleHandle ) {
				LowLevelComponent = lowLevelComponent;
				Callback = callback;
				HookInfo = hookInfo;
				KeyChange = keyChange;
				Caller = caller;
				HookID = LowLevelComponent.SetHookEx ( LowLevelComponent.GetChangeCode ( KeyChange ), LowLevelKeyboardProc, moduleHandle, 0 );
			}

			public IntPtr LowLevelKeyboardProc ( int nCode, IntPtr wParam, IntPtr lParam ) {
				bool Resend = false;
				if ( nCode >= 0 ) {
					var hookEventInfo = LowLevelComponent.ParseHookData ( Caller, nCode, wParam, lParam );
					Resend = Callback ( hookEventInfo );
				}
				return Resend ? LowLevelComponent.CallNextHook ( HookID, nCode, wParam, lParam ) : 1;
			}
		}
	}
}