using Components.Library;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Components.Interfaces {
	public class MLowLevelInput : DLowLevelInput {
		private bool UnhookResult = true;
		private IntPtr SetHookResult = 1;
		private nint CallNextResult = 1;
		private nint moduleHandleResult = 1;
		private bool ProcessCallbacks = true;
		Dictionary<nint, LowLevelKeyboardProc> HookList;
		int LastID = 1;

		public MLowLevelInput ( CoreBase owner ) : base ( owner ) {
			HookList = new Dictionary<nint, LowLevelKeyboardProc> ();
		}

		public override int ComponentVersion => 1;
		public override int HookTypeCode => 1;

		/// <inheritdoc />
		public override nint CallNextHook ( nint hhk, int nCode, nint wParam, nint lParam ) => CallNextResult;
		public override int GetChangeCode ( VKChange vkChange ) {
			// Source: https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-keydown
			switch ( vkChange ) {
			case VKChange.KeyDown: return 0x0100;
			case VKChange.KeyUp: return 0x0101;
			}
			return -1;
		}
		public override VKChange GetChangeType ( int vkCode ) {
			switch (vkCode ) {
			case 0x0100: return VKChange.KeyDown;
			case 0x0101: return VKChange.KeyUp;
			}
			throw new InvalidCastException ( $"No key change action type corresponds to code {vkCode:X}" );
		}
		public override uint SimulateInput ( uint nInputs, HInputData[] pInputs, int cbSize, bool? shouldProcess = null ) {
			uint ret = 0;
			for (int i = 0; i < nInputs; i++ ) {
				// If ProcessCallbacks is false, than "inner mechanics" will forbid processing.
				//   only otherwise wish of method caller can be pleased.
				int nCode = (ProcessCallbacks ? shouldProcess ?? ProcessCallbacks : false) ? 0 : -1;
				if ( SimulateSingleInput ( pInputs[i], nCode ) ) ret++;
			}
			return ret;
		}
		private bool SimulateSingleInput ( HInputData data, int nCode ) {
			if ( data == null || data.Data == null ) return false;
			var innerData = data.Data;
			var values = (HInputData_Mock.IInputStruct_Mock)innerData;
			RaiseEvent ( values.HookID, nCode, GetChangeCode ( values.KeyChange ), values.VKCode.ToUnmanaged () );
			return true;
		}

		public override nint GetModuleHandleID ( string lpModuleName ) => moduleHandleResult;
		public override HInputData ParseHookData ( int nCode, nint vkChngCode, nint vkCode ) => new HInputData_Mock ( this, nCode, GetChangeType ( (int)vkChngCode ), vkCode );
		/// <inheritdoc />
		public override nint SetHookEx ( LowLevelKeyboardProc lpfn ) {
			if ( SetHookResult < 0 ) return SetHookResult;
			HookList.Add ( LastID, lpfn );
			return LastID++;
		}
		/// <inheritdoc />
		public override bool UnhookHookEx ( nint hhk ) {
			if ( !HookList.ContainsKey ( hhk ) ) throw new KeyNotFoundException ( $"Hook with ID #{hhk} was not found!" );
			if ( !HookList.Remove ( hhk ) ) throw new InvalidOperationException ( $"Unknown issue during unhooking #{hhk}" );
			return UnhookResult;
		}

		public enum Part { CallNextHookEx, GetModuleHandle, SetHookEx, Unhook, NCode }
		public void RaiseEvent ( nint hookID, int nCode, IntPtr wParam, IntPtr lParam ) {
			if ( HookList.ContainsKey ( hookID ) ) {
				int nnCode = nCode < 0 ? -1 : 1;
				nnCode *= (int)hookID;
				HookList[hookID].Invoke ( nnCode, wParam, lParam );
			}
		}
		public void SetMockReturn ( Part part, bool validReturn ) {
			switch ( part ) {
			case Part.Unhook: UnhookResult = validReturn; break;
			case Part.SetHookEx: SetHookResult = validReturn ? 1 : (IntPtr)null; break;
			case Part.CallNextHookEx: CallNextResult = validReturn ? 1 : 0; break;
			case Part.GetModuleHandle: moduleHandleResult = validReturn ? 1 : 0; break;
			case Part.NCode: ProcessCallbacks = validReturn; break;
			}
		}

		public override HInputData GetLowLevelData ( HInputEventDataHolder highLevelData ) {
			VKChange pressedState = highLevelData.Pressed >= 1 ? VKChange.KeyDown : VKChange.KeyUp;

			var hooks = highLevelData.HookInfo.HookIDs;
			if ( hooks.Count < 1 ) throw new KeyNotFoundException ( "No hookID was found, and so cannot find any context for given high-level data!" );

			var innerData = new HInputData_Mock.IInputStruct_Mock ( (int)hooks[0], pressedState, highLevelData.InputCode );
			return new HInputData_Mock ( highLevelData.Owner, innerData );
		}

		public override HInputEventDataHolder GetHighLevelData ( DInputReader requester, HInputData highLevelData ) {
			var data = (HInputData_Mock.IInputStruct_Mock)((HInputData_Mock)highLevelData).Data;
			return new HKeyboardEventDataHolder ( requester, 1, (int)Marshal.ReadIntPtr ( data.VKCode ), data.KeyChange == VKChange.KeyDown ? 1 : 0 );
		}

		public override nint GetMessageExtraInfoPtr () => nint.Zero;
	}
}