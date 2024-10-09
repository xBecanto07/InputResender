using Components.Library;
using System.ComponentModel;
using System.Net;
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
				(nameof(SetHookEx), typeof(Hook[])),
				(nameof(UnhookHookEx), typeof(bool)),
				(nameof(CallNextHook), typeof(IntPtr)),
				(nameof(ParseHookData), typeof(HInputEventDataHolder)),
				(nameof(SimulateInput), typeof(uint)),
				(nameof(GetLowLevelData), typeof(HInputData)),
				(nameof(GetHighLevelData), typeof(HInputEventDataHolder)),
				(nameof(ErrorList), typeof(Win32Exception)),
				(nameof(PrintErrors), typeof(void)),
				(nameof(GetMessageExtraInfoPtr), typeof(nint)),
				(nameof(PrintHookInfo), typeof(string)),
			};

		protected static Dictionary<DictionaryKey, Hook> HookIDDict = new ();
		protected static DictionaryKeyFactory HookKeyFactory = new DictionaryKeyFactory ();

		/// <summary></summary>
		/// <param name="idHook">Hook type code, see: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexa</param>
		/// <param name="lpfn">Pointer to callback method. Recommended to use a DLL method (public static)</param>
		/// <param name="hMod">Hook of DLL containing callback. Must be <see langword="null"/> if in same thread, otherwise following is recommended: <code>LoadLibrary(TEXT("c:\\myapp\\sysmsg.dll"))</code></param>
		/// <param name="dwThreadId">Process ID on which the hook should operate</param>
		/// <returns>Hook handle number, ID to this specific hook</returns>
		public abstract IDictionary<VKChange, Hook> SetHookEx ( HHookInfo hookInfo, Func<DictionaryKey, HInputData, bool> lpfn );
		/// <summary>Stop hook specified by its ID of <paramref name="hhk"/></summary>
		public abstract bool UnhookHookEx ( Hook hookID );
		/// <summary>Pass processing to another hook in system queue</summary>
		/// <param name="hhk">Ignored parameter</param>
		/// <param name="nCode">Processing code: &lt;0 to skip and pass to CallNextHok, 0 to process.</param>
		/// <param name="wParam">Key message type code: obtainable by GetChangeCode(VKChange)</param>
		/// <param name="lParam">Pointer to data. Defined by: https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-kbdllhookstruct?redirectedfrom=MSDN<para>To excess data try: (Windows.Forms.Keys)Marshal.ReadInt32 ( lParam )</para></param>
		/// <returns></returns>
		public abstract IntPtr CallNextHook ( IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam );
		public abstract string PrintHookInfo ( DictionaryKey key );
		public abstract HInputData ParseHookData ( DictionaryKey hookID, IntPtr vkChngCode, IntPtr vkCode );
		/// <summary></summary>
		/// <param name="nInputs">Count of inputs in <paramref name="pInputs"/> array</param>
		/// <param name="pInputs">Array of input event orders</param>
		/// <param name="cbSize">Size of single <paramref name="pInputs"/> structure instance</param>
		/// <returns>Number of successfully called input events</returns>
		public abstract uint SimulateInput ( uint nInputs, HInputData[] pInputs, int cbSize, bool? shouldProcess = null );
		public abstract HInputData GetLowLevelData ( HInputEventDataHolder higLevelData );
		public abstract HInputEventDataHolder GetHighLevelData ( DictionaryKey hookKey, DInputReader requester, HInputData highLevelData );
		public List<(string, Exception)> ErrorList { get; } = new ();
		public void PrintErrors (Action<string> outAct) {
			foreach ( var error in ErrorList ) {
				if (error.Item2 is Win32Exception w32e)
					outAct ( $"Error during {error.Item1}: {w32e.ErrorCode}" );
				else outAct ( $"Error during {error.Item1}: {error.Item2.Message}" );
				outAct ( error.ToString () );
			}
			ErrorList.Clear ();
		}
		public abstract nint GetMessageExtraInfoPtr ();

		public abstract class DStateInfo : StateInfo {
			protected DStateInfo ( DLowLevelInput owner ) : base ( owner ) {
				List<string> errors = new List<string> ();
				Hooks = GetHooks ();
				owner.PrintErrors ( errors.Add );
				Errors = errors.ToArray ();
			}
			public readonly string[] Errors, Hooks;
			protected abstract string[] GetHooks ();
			public override string AllInfo () => $"{base.AllInfo ()}{BR}Hooks:{BR}{string.Join ( BR, Hooks )}{BR}Errors:{BR}{string.Join ( BR, Errors )}";
		}
	}


	public class Hook : DataHolderBase<DLowLevelInput> {
		public readonly DictionaryKey Key;
		public nint HookID { get; private set; }
		public Func<DictionaryKey, HInputData, bool> HLCallback;
		public bool EnforcePassthrough = false;
		public Action<int, IntPtr, IntPtr> Log;
		public int MsgID = 0;
		public readonly DLowLevelInput.LowLevelKeyboardProc Callback;
		public readonly HHookInfo HookInfo;
		private readonly GCHandle _gcHandler;

		private DLowLevelInput LLInput { get => Owner; }

		public Hook ( DLowLevelInput owner, HHookInfo hookInfo, DictionaryKey key, Func<DictionaryKey, HInputData, bool> callback, Action<int, IntPtr, IntPtr> log = null ) : base ( owner ) {
			Key = key;
			HookInfo = (HHookInfo)hookInfo.Clone ();
			HLCallback = callback;
			Log = log;
			Callback = LLCallback;
			_gcHandler = GCHandle.Alloc ( Callback );
		}
		~Hook () {
			_gcHandler.Free ();
		}

		public void UpdateHookID ( nint hookID ) => HookID = hookID;

		private nint LLCallback ( int nCode, IntPtr wParam, IntPtr lParam ) {
			MsgID++;
			Log?.Invoke ( nCode, wParam, lParam );
			if ( (int)wParam == 0x0101 ) {
				;
			}
			bool resend = false;
			if ( (nCode >= 0) | (EnforcePassthrough) ) {
				var res = LLInput.ParseHookData ( Key, wParam, lParam );
				resend = HLCallback == null ? true : HLCallback ( Key, res );
			}
			return resend ? LLInput.CallNextHook ( HookID, nCode, wParam, lParam ) : 1;
		}

		public override DataHolderBase<DLowLevelInput> Clone () {
			var ret = new Hook ( LLInput, HookInfo, Key, HLCallback );
			ret.HookID = HookID;
			ret.HLCallback = HLCallback;
			ret.Log = Log;
			return ret;
		}
		public override bool Equals ( object obj ) => GetHashCode () == obj.GetHashCode ();
		public override int GetHashCode () => Key.GetHashCode () ^ HookID.GetHashCode () ^ HLCallback.GetHashCode () ^ Owner.GetHashCode ();
		public override string ToString () => $"Hook #{Key} of {HookID:X}";
		public string ToDetailedString () => $"Hook #{Key} of {HookID:X} >> {HLCallback.Method.AsString ()}{(Log != null ? " Logged" : "")}{(EnforcePassthrough ? " Must process" : "")}";
	}
}