using Components.Library;
using System.Runtime.InteropServices;

namespace Components.Interfaces {
	public abstract class DLowLevelInput : ComponentBase<CoreBase> {
		/// <summary>Callback delegate for for keyboard hooks.<para>Defined by: https://learn.microsoft.com/en-us/previous-versions/windows/desktop/legacy/ms644985(v=vs.85)</para></summary>
		/// <param name="nCode">Processing code: &lt;0 to skip and pass to CallNextHok, 0 to process.</param>
		/// <param name="wParam">Key message type code: obtainable by GetChangeCode(VKChange)</param>
		/// <param name="lParam">Pointer to data. Defined by: https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-kbdllhookstruct?redirectedfrom=MSDN<para>To excess data try: (Windows.Forms.Keys)Marshal.ReadInt32 ( lParam )</para></param>
		/// <returns></returns>
		public delegate IntPtr LowLevelKeyboardProc ( int nCode, IntPtr wParam, IntPtr lParam );

		public DLowLevelInput ( CoreBase owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(SetHookEx), typeof(IntPtr)),
				(nameof(UnhookHookEx), typeof(bool)),
				(nameof(CallNextHook), typeof(IntPtr)),
				(nameof(GetModuleHandleID), typeof(IntPtr)),
				(nameof(ParseHookData), typeof(HInputEventDataHolder)),
				(nameof(SimulateInput), typeof(uint)),
				(nameof(GetLowLevelData), typeof(HInputData)),
				(nameof(GetHighLevelData), typeof(HInputEventDataHolder)),
				(nameof(GetChangeCode), typeof(int)),
				(nameof(GetChangeType), typeof(VKChange)),
				(nameof(HookTypeCode), typeof(int))
			};

		/// <summary></summary>
		/// <param name="idHook">Hook type code, see: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexa</param>
		/// <param name="lpfn">Pointer to callback method. Recommended to use a DLL method (public static)</param>
		/// <param name="hMod">Hook of DLL containing callback. Must be <see langword="null"/> if in same thread, otherwise following is recommended: <code>LoadLibrary(TEXT("c:\\myapp\\sysmsg.dll"))</code></param>
		/// <param name="dwThreadId">Process ID on which the hook should operate</param>
		/// <returns>Hook handle number, ID to this specific hook</returns>
		public abstract IntPtr SetHookEx ( int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId );
		/// <summary>Stop hook specified by its ID of <paramref name="hhk"/></summary>
		public abstract bool UnhookHookEx ( IntPtr hhk );
		/// <summary>Pass processing to another hook in system queue</summary>
		/// <param name="hhk">Ignored parameter</param>
		/// <param name="nCode">Processing code: &lt;0 to skip and pass to CallNextHok, 0 to process.</param>
		/// <param name="wParam">Key message type code: obtainable by GetChangeCode(VKChange)</param>
		/// <param name="lParam">Pointer to data. Defined by: https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-kbdllhookstruct?redirectedfrom=MSDN<para>To excess data try: (Windows.Forms.Keys)Marshal.ReadInt32 ( lParam )</para></param>
		/// <returns></returns>
		public abstract IntPtr CallNextHook ( IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam );
		public abstract IntPtr GetModuleHandleID ( string lpModuleName );
		public abstract HInputData ParseHookData ( int nCode, IntPtr vkChngCode, IntPtr vkCode );
		/// <summary></summary>
		/// <param name="nInputs">Count of inputs in <paramref name="pInputs"/> array</param>
		/// <param name="pInputs">Array of input event orders</param>
		/// <param name="cbSize">Size of single <paramref name="pInputs"/> structure instance</param>
		/// <returns>Number of successfully called input events</returns>
		public abstract uint SimulateInput ( uint nInputs, HInputData[] pInputs, int cbSize );
		public abstract HInputData GetLowLevelData ( HInputEventDataHolder higLevelData );
		public abstract HInputEventDataHolder GetHighLevelData ( DInputReader requester, HInputData highLevelData );
		public abstract int GetChangeCode ( VKChange vkChange );
		public abstract VKChange GetChangeType ( int vkCode );
		public abstract int HookTypeCode { get; }
	}

	public class MLowLevelInput : DLowLevelInput {
		private bool UnhookResult = true;
		private IntPtr SetHookResult = 1;
		private nint CallNextResult = 1;
		private nint moduleHandleResult = 1;
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
		public override uint SimulateInput ( uint nInputs, HInputData[] pInputs, int cbSize ) {
			uint ret = 0;
			for (int i = 0; i < nInputs; i++ ) {
				if ( SimulateSingleInput ( pInputs[i] ) ) ret++;
			}
			return ret;
		}
		private bool SimulateSingleInput (HInputData data ) {
			if ( data == null || data.Data == null ) return false;
			var innerData = data.Data;
			var values = (HInputData_Mock.IInputStruct_Mock)innerData;
			RaiseEvent ( values.HookID, 1, GetChangeCode ( values.KeyChange ), values.VKCode.ToUnmanaged () );
			return true;
		}

		public override nint GetModuleHandleID ( string lpModuleName ) => moduleHandleResult;
		public override HInputData ParseHookData ( int nCode, nint vkChngCode, nint vkCode ) => new HInputData_Mock ( this, nCode, GetChangeType ( (int)vkChngCode ), vkCode );
		/// <inheritdoc />
		public override nint SetHookEx ( int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId ) {
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

		public enum Part { CallNextHookEx, GetModuleHandle, SetHookEx, Unhook }
		public void RaiseEvent ( nint hookID, int nCode, IntPtr wParam, IntPtr lParam ) {
			if (HookList.ContainsKey ( hookID ) )
				HookList[hookID].Invoke ( nCode, wParam, lParam );
		}
		public void SetMockReturn ( Part part, bool validReturn ) {
			switch ( part ) {
			case Part.Unhook: UnhookResult = validReturn; break;
			case Part.SetHookEx: SetHookResult = validReturn ? 1 : (IntPtr)null; break;
			case Part.CallNextHookEx: CallNextResult = validReturn ? 1 : 0; break;
			case Part.GetModuleHandle: moduleHandleResult = validReturn ? 1 : 0; break;
			}
		}

		public override HInputData GetLowLevelData ( HInputEventDataHolder highLevelData ) {
			VKChange pressedState = highLevelData.Pressed >= 1 ? VKChange.KeyDown : VKChange.KeyUp;

			var hooks = highLevelData.HookInfo.HookIDs;
			if ( hooks.Count < 1 ) throw new KeyNotFoundException ( "No hookID was found, that could fit given high-level data!" );

			var innerData = new HInputData_Mock.IInputStruct_Mock ( (int)highLevelData.HookInfo.HookIDs[0], pressedState, highLevelData.InputCode );
			return new HInputData_Mock ( highLevelData.Owner, innerData );
		}

		public override HInputEventDataHolder GetHighLevelData ( DInputReader requester, HInputData highLevelData ) {
			var data = (HInputData_Mock.IInputStruct_Mock)((HInputData_Mock)highLevelData).Data;
			return new HKeyboardEventDataHolder ( requester, 1, (int)Marshal.ReadIntPtr ( data.VKCode ), data.KeyChange == VKChange.KeyDown ? 1 : 0 );

		}
	}
}