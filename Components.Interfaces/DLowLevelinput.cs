using Components.Library;
using System.Runtime.InteropServices;

namespace Components.Interfaces {
	public abstract class DLowLevelinput : ComponentBase<CoreBase> {
		/// <summary>Callback delegate for for keyboard hooks.<para>Defined by: https://learn.microsoft.com/en-us/previous-versions/windows/desktop/legacy/ms644985(v=vs.85)</para></summary>
		/// <param name="nCode">Processing code: &lt;0 to skip and pass to CallNextHok, 0 to process.</param>
		/// <param name="wParam">Key message type code: obtainable by GetChangeCode(VKChange)</param>
		/// <param name="lParam">Pointer to data. Defined by: https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-kbdllhookstruct?redirectedfrom=MSDN<para>To excess data try: (Windows.Forms.Keys)Marshal.ReadInt32 ( lParam )</para></param>
		/// <returns></returns>
		public delegate IntPtr LowLevelKeyboardProc ( int nCode, IntPtr wParam, IntPtr lParam );

		public DLowLevelinput ( CoreBase owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(SetHookEx), typeof(IntPtr)),
				(nameof(UnhookHookEx), typeof(bool)),
				(nameof(CallNextHook), typeof(IntPtr)),
				(nameof(GetModuleHandleID), typeof(IntPtr)),
				(nameof(ParseHookData), typeof(HInputEventDataHolder)),
				(nameof(GetChangeCode), typeof(int)),
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
		public abstract HInputEventDataHolder ParseHookData ( DInputReader caller, int nCode, IntPtr vkChngCode, IntPtr vkCode );
		/// <summary></summary>
		/// <param name="nInputs">Count of inputs in <paramref name="pInputs"/> array</param>
		/// <param name="pInputs">Array of input event orders</param>
		/// <param name="cbSize">Size of single <paramref name="pInputs"/> structure instance</param>
		/// <returns>Number of successfully called input events</returns>
		public abstract uint SimulateInput ( uint nInputs, HInputData[] pInputs, int cbSize );
		public abstract HInputData GetLowLevelData ( HInputEventDataHolder higLevelData );
		public abstract int GetChangeCode ( VKChange vKChange );
		public abstract int HookTypeCode { get; }
	}

	public class MLowLevelInput : DLowLevelinput {
		private bool UnhookResult = true;
		private IntPtr SetHookResult = 1;
		private nint CallNextResult = 1;
		private nint moduleHandleResult = 1;
		Dictionary<nint, LowLevelKeyboardProc> HookList;
		int LastID = 0;

		public MLowLevelInput ( CoreBase owner ) : base ( owner ) {
			HookList = new Dictionary<nint, LowLevelKeyboardProc> ();
		}

		public override int ComponentVersion => 1;
		public override int HookTypeCode => 1;

		/// <inheritdoc />
		public override nint CallNextHook ( nint hhk, int nCode, nint wParam, nint lParam ) => CallNextResult;
		public override int GetChangeCode ( VKChange vKChange ) {
			// Source: https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-keydown
			switch ( vKChange ) {
			case VKChange.KeyDown: return 0x0100;
			case VKChange.KeyUp: return 0x0101;
			}
			return -1;
		}
		public override uint SimulateInput ( uint nInputs, HInputData[] pInputs, int cbSize ) {
			uint ret = 0;
			for (int i = 0; i < nInputs; i++ ) {
				var data = pInputs[i];
				var values = (HInputData_Mock.IInputStruct_Mock)data.Data;
				RaiseEvent ( values.HookID, 0, GetChangeCode(values.KeyChange), values.VKCode.ToUnmanaged () );
				ret++;
			}
			return ret;
		}
		public override nint GetModuleHandleID ( string lpModuleName ) => moduleHandleResult;
		public override HInputEventDataHolder ParseHookData ( DInputReader caller, int nCode, nint vkChngCode, nint vkCode ) => new HKeyboardEventDataHolder ( caller, 1, Marshal.ReadInt32 ( vkCode ), 1 );
		/// <inheritdoc />
		public override nint SetHookEx ( int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId ) {
			if ( SetHookResult < 0 ) return SetHookResult;
			HookList.Add ( LastID, lpfn );
			return LastID++;
		}
		/// <inheritdoc />
		public override bool UnhookHookEx ( nint hhk ) {
			HookList.Remove ( hhk );
			return UnhookResult;
		}

		public enum Part { CallNextHookEx, GetModuleHandle, SetHookEx, Unhook }
		public void RaiseEvent (nint hookID, int nCode, IntPtr wParam, IntPtr lParam ) => HookList[hookID].Invoke ( nCode, wParam, lParam );
		public void SetMockReturn ( Part part, bool validReturn ) {
			switch ( part ) {
			case Part.Unhook: UnhookResult = validReturn; break;
			case Part.SetHookEx: SetHookResult = validReturn ? 1 : (IntPtr)null; break;
			case Part.CallNextHookEx: CallNextResult = validReturn ? 1 : 0; break;
			case Part.GetModuleHandle: moduleHandleResult = validReturn ? 1 : 0; break;
			}
		}

		public override HInputData GetLowLevelData ( HInputEventDataHolder highLevelData ) {
			return new HInputData_Mock ( highLevelData.Owner, new HInputData_Mock.IInputStruct_Mock ( (int)highLevelData.HookInfo.HookID, highLevelData.Pressed >= 1 ? VKChange.KeyDown : VKChange.KeyUp, highLevelData.InputCode ) );
		}
	}

	public class HHookInfo : DataHolderBase {
		public virtual int DeviceID { get; protected set; }
		public virtual HashSet<VKChange> ChangeMask { get; protected set; }
		public virtual VKChange LatestChangeType { get; protected set; }
		public virtual DLowLevelinput HookLLCallback { get; protected set; }
		public virtual nint HookID { get; protected set; }

		public HHookInfo ( ComponentBase owner, int deviceID, VKChange firstAcceptedChange, params VKChange[] acceptedChanges ) : base ( owner ) {
			DeviceID = deviceID;
			LatestChangeType = firstAcceptedChange;
			ChangeMask = new HashSet<VKChange> () { firstAcceptedChange };
			for (int i = 0; i < acceptedChanges.Length; i++) ChangeMask.Add ( acceptedChanges[i] );
		}

		public virtual void AssignEventData (VKChange latechChange) { LatestChangeType = latechChange; }
		public virtual void AssignHook (nint hookID, DLowLevelinput hookCallback) { HookLLCallback = hookCallback; }

		public override DataHolderBase Clone () => new HHookInfo ( Owner, DeviceID, LatestChangeType );
		public override bool Equals ( object obj ) {
			if ( obj == null ) return false;
			if ( obj.GetType () != GetType () ) return false;
			var item = (HHookInfo)obj;
			int maskN = ChangeMask.Count;
			if ( maskN != item.ChangeMask.Count ) return false;
			if ( DeviceID != item.DeviceID ) return false;
			if ( LatestChangeType == item.LatestChangeType ) return false;
			if ( !ChangeMask.SetEquals ( item.ChangeMask ) ) return false;
			return true;
		}
		public override int GetHashCode () => (DeviceID, LatestChangeType).GetHashCode () ^ ChangeMask.CalcSetHash ();
		public override string ToString () => $"{DeviceID}:{LatestChangeType}:[{ChangeMask.AsString ()}]";
	}
}