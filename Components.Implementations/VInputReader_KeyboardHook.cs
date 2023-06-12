using Components.Interfaces;
using Components.Library;
using System;
using System.Diagnostics;

namespace Components.Implementations {
	public class VInputReader_KeyboardHook : DInputReader {
		Dictionary<nint, LLHook> HookSet;

		public VInputReader_KeyboardHook ( CoreBase owner ) : base ( owner ) {
			HookSet = new Dictionary<nint, LLHook> ();
		}

		public override int ComponentVersion => 1;
		protected DLowLevelInput LowLevelComponent { get { return Owner.Fetch<DLowLevelInput> (); } }

		public override int ReleaseHook ( HHookInfo hookInfo ) {
			int released = 0;
			foreach( nint hookID in hookInfo.HookIDs ) {
				if ( !HookSet.ContainsKey ( hookID ) ) throw new KeyNotFoundException ( $"Couldn't find a hook ID for Hook Definition: {hookInfo}" );
				if ( HookSet.Remove ( hookID ) )
					released += LowLevelComponent.UnhookHookEx ( hookID ) ? 1 : 0;
			}
			return released;
		}

		public override ICollection<nint> SetupHook ( HHookInfo hookInfo, Func<HInputEventDataHolder, bool> callback ) {
			var lowLevelComponent = LowLevelComponent;
			var ret = new HashSet<nint> ();
			using ( Process curProcess = Process.GetCurrentProcess () )
			using ( ProcessModule curModule = curProcess.MainModule )
				foreach ( var changeType in hookInfo.ChangeMask ) {
					nint moduleHandle = LowLevelComponent.GetModuleHandleID ( curModule.ModuleName );
					var newHook = new LLHook ( this, lowLevelComponent, callback, hookInfo, changeType );
					newHook.RegisterLL ( moduleHandle );
					HookSet.Add(newHook.HookID, newHook );
					ret.Add ( newHook.HookID );
				}
			return ret;
		}

		public override uint SimulateInput ( HInputEventDataHolder input, bool allowRecapture ) {
			var LLData = LowLevelComponent.GetLowLevelData ( input );
			return LowLevelComponent.SimulateInput ( 1, new HInputData[1] { LLData }, LLData.SizeOf );
		}

		protected class LLHook {
			public nint HookID { get; private set; }
			public readonly Func<HInputEventDataHolder, bool> Callback;
			public readonly HHookInfo HookInfo;
			public readonly DLowLevelInput LowLevelComponent;
			public readonly VKChange KeyChange;
			public readonly VInputReader_KeyboardHook Caller;

			public LLHook ( VInputReader_KeyboardHook caller, DLowLevelInput lowLevelComponent, Func<HInputEventDataHolder, bool> callback, HHookInfo hookInfo, VKChange keyChange ) {
				LowLevelComponent = lowLevelComponent;
				Callback = callback;
				HookInfo = hookInfo;
				KeyChange = keyChange;
				Caller = caller;
				HookID = 0;
			}

			public nint RegisterLL ( nint moduleHandle ) {
				if ( HookID != 0 ) throw new AccessViolationException ( $"Hook was already registered as {HookID}!" );
				int changeCode = LowLevelComponent.GetChangeCode ( KeyChange );
				return HookID = LowLevelComponent.SetHookEx ( changeCode, LowLevelKeyboardProc, moduleHandle, 0 );
			}

			public IntPtr LowLevelKeyboardProc ( int nCode, IntPtr wParam, IntPtr lParam ) {
				bool Resend = false;
				if ( nCode >= 0 ) {
					HInputData hookEventInfo = LowLevelComponent.ParseHookData ( nCode, wParam, lParam );
					Resend = Callback ( LowLevelComponent.GetHighLevelData ( Caller, hookEventInfo ) );
				}
				return Resend ? LowLevelComponent.CallNextHook ( HookID, nCode, wParam, lParam ) : 1;
			}
		}
	}
}