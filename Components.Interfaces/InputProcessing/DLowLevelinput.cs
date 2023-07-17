using Components.Library;
using System.ComponentModel;

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
				(nameof(HookTypeCode), typeof(int)),
				(nameof(ErrorList), typeof(Win32Exception)),
				(nameof(PrintErrors), typeof(void)),
				(nameof(GetMessageExtraInfoPtr), typeof(nint))
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
		public abstract uint SimulateInput ( uint nInputs, HInputData[] pInputs, int cbSize, bool? shouldProcess = null );
		public abstract HInputData GetLowLevelData ( HInputEventDataHolder higLevelData );
		public abstract HInputEventDataHolder GetHighLevelData ( DInputReader requester, HInputData highLevelData );
		public abstract int GetChangeCode ( VKChange vkChange );
		public abstract VKChange GetChangeType ( int vkCode );
		public abstract int HookTypeCode { get; }
		public List<(string, Win32Exception)> ErrorList { get; } = new List<(string, Win32Exception)> ();
		public void PrintErrors (Action<string> outAct) {
			foreach ( var error in ErrorList ) {
				outAct ( $"Error during {error.Item1}: {error.Item2.ErrorCode}" );
				outAct ( error.ToString () );
			}
			ErrorList.Clear ();
		}
		public abstract nint GetMessageExtraInfoPtr ();
	}
}