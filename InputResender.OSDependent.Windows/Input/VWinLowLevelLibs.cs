using Components.Interfaces;
using Components.Library;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;
using System.Linq;

namespace InputResender.WindowsGUI {
	public class VWinLowLevelLibs : DLowLevelInput {
		private const int WH_KEYBOARD_LOW_LEVEL = 13;
		private const int WH_MOUSE_LOW_LEVEL = 14;
		private static VWinLowLevelLibs mainInstance;
		private Dictionary<DictionaryKey, int> OwnHooks;
		const int MaxLog = 256;
		public static List<(int nCode, VKChange changeCode, HWInput.KeyboardInput inputData)> EventList = new( MaxLog );

		private static void Log ( int nCode, IntPtr vkChngCode, IntPtr vkCode ) {
			var change = (VKChange)vkChngCode;
			var input = new HWInput.KeyboardInput ( vkCode );
			// if ( EventList.Count > 0 && EventList[0].Item3.Equals ( input ) && EventList[0].Item2 == change ) vkChngCode ^= 0x1;
			if ( EventList.Count >= MaxLog ) EventList.RemoveAt ( MaxLog - 1 );
			EventList.Insert ( 0, (nCode, change, input) );
		}

		public VWinLowLevelLibs ( CoreBase owner ) : base ( owner ) {
			mainInstance = this;
			OwnHooks = new Dictionary<DictionaryKey, int> ();
		}

		public override int ComponentVersion => 1;

		//public override int HookTypeCode => WH_KEYBOARD_LOW_LEVEL; // Could be replaced maybe with something like List<VKChange> SupportedTypes
		private static int GetChangeCode ( VKChange vKChange ) {
			// Source: https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-keydown
			switch ( vKChange ) {
			case VKChange.KeyDown:	return 0x0100;
			case VKChange.KeyUp:	return 0x0101;
			case VKChange.MouseMove: return 0x0200;
			}
			return -1;
		}
		private static VKChange GetChangeType ( int vkCode ) {
			switch ( vkCode ) {
			case 0x0100: return VKChange.KeyDown;
			case 0x0101: return VKChange.KeyUp;
			case 0x0200: return VKChange.MouseMove;
			}
			throw new InvalidCastException ( $"No key change action type corresponds to code {vkCode:X}" );
		}

		public override HInputEventDataHolder GetHighLevelData ( DictionaryKey hookKey, DInputReader requester, HInputData lowLevelData ) {
			HWInput llData = (HWInput)lowLevelData.Data;
			switch (llData.Type) {
			case HWInput.TypeKEY:
				HWInput.KeyboardInput keyInfo = llData.Data.ki;
				return new HKeyboardEventDataHolder ( requester, HookIDDict[hookKey].HookInfo, keyInfo.vkCode, lowLevelData.Pressed );
			case HWInput.TypeMOUSE:
				HWInput.MouseInput mouseInfo = llData.Data.mi;
				return new HMouseEventDataHolder ( requester, HookIDDict[hookKey].HookInfo, mouseInfo.dx, mouseInfo.dy );
			default: throw new ArgumentOutOfRangeException ( nameof ( llData.Type ), llData.Type, "Type of input is not supported!" );
			}
		}
		public override HInputData GetLowLevelData ( HInputEventDataHolder highLevelData ) {
			var inputUnion = new HWInput.InputUnion ();
			uint time = (uint)new TimeSpan ( highLevelData.CreationTime.Ticks ).TotalMilliseconds;

			if (highLevelData is HKeyboardEventDataHolder keyboardInput) {
				var keyboardData = new HWInput.KeyboardInput ();
				keyboardData.time = time;
				keyboardData.vkCode = (ushort)highLevelData.InputCode;
				keyboardData.scanCode = (ushort)highLevelData.InputCode;
				keyboardData.dwFlags = (uint)((highLevelData.Pressed >= 1 ? HWInput.KeyboardInput.CallbackFlags.KeyDown : HWInput.KeyboardInput.CallbackFlags.KeyUp) | HWInput.KeyboardInput.CallbackFlags.ValidCallbackFlags);
				inputUnion.ki = keyboardData;
			} else if (highLevelData is HMouseEventDataHolder mouseInput) {
				var mouseData = new HWInput.MouseInput ();
				mouseData.time = time;
				mouseData.dx = mouseInput.DeltaX;
				mouseData.dy = mouseInput.DeltaY;
				mouseData.mouseData = 0; // Use this to set mouse wheel
				mouseData.dwFlags = 0;
				inputUnion.mi = mouseData;
			} else throw new InvalidCastException ( $"High level data of type {highLevelData.GetType ()} is not supported!" );

			return new WinLLInputData ( highLevelData.Owner, new HWInput ( 1, inputUnion ) );
		}
		public override HInputData ParseHookData ( DictionaryKey hookID, nint vkChngCode, nint vkCode ) {
			if (!OwnHooks.TryGetValue ( hookID, out int hookCode )) throw new KeyNotFoundException ( $"Hook with key {hookID} was not found!" );
			return hookCode switch {
				WH_KEYBOARD_LOW_LEVEL => WinLLInputData.NewKeyboardData ( this, vkChngCode, vkCode ),
				WH_MOUSE_LOW_LEVEL => WinLLInputData.NewMouseData ( this, vkChngCode, vkCode ),
				_ => throw new ArgumentOutOfRangeException ( nameof ( hookCode ), hookCode, "Hook code is not supported!" )
			};
			
		}

		public override nint CallNextHook ( nint hhk, int nCode, nint wParam, nint lParam ) => CallNextHookEx ( hhk, nCode, wParam, lParam );
		public override nint GetModuleHandleID ( string lpModuleName ) => GetModuleHandle ( lpModuleName );

		public override Hook[] SetHookEx ( HHookInfo hookInfo, Func<DictionaryKey, HInputData, bool> callback ) {
			List<int> hookCodes = new ();
			foreach (var vkChange in hookInfo.ChangeMask) {
				int code = vkChange switch {
					VKChange.KeyDown => WH_KEYBOARD_LOW_LEVEL,
					VKChange.KeyUp => WH_KEYBOARD_LOW_LEVEL,
					VKChange.MouseMove => WH_MOUSE_LOW_LEVEL,
					_ => -1
				};
				if ( code > 0 & !hookCodes.Contains ( code ) ) hookCodes.Add ( code );
			}

			List<Hook> ret = new ();

			foreach (int hookCode in hookCodes) {
				var hookKey = HookKeyFactory.NewKey ();
				var hook = new Hook ( this, hookInfo, hookKey, callback, Log );

				if ( hook == null ) {
					System.Text.StringBuilder SB = new System.Text.StringBuilder ();
					SB.AppendLine ( $"Error when creating hook for {hookInfo}!{Environment.NewLine}" );
					PrintErrors ( ( ss ) => SB.AppendLine ( ss ) );
					throw new InvalidOperationException ( SB.ToString () );
				}

				var moduleHandle = GetModuleHandleID ( "user32.dll" );
				//int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
				int threadID = 0;
				var hookID = SetWindowsHookEx ( hookCode, hook.Callback, moduleHandle, (uint)threadID );
				if ( hookID == (IntPtr)null ) {
					ErrorList.Add ( (nameof ( SetHookEx ), new Win32Exception ()) );
					return null;
				}
				HookIDDict.Add ( hookKey, hook );
				OwnHooks.Add ( hookKey, hookCode );
				hook.UpdateHookID ( hookID );
				ret.Add ( hook );
			}
			return ret.ToArray ();
		}
		public override bool UnhookHookEx ( Hook hookID ) {
			if ( !HookIDDict.ContainsPair ( hookID.Key, hookID ) ) throw new KeyNotFoundException ( $"Hook with key {hookID.Key} was not found!" );
			if ( !OwnHooks.ContainsKey ( hookID.Key ) ) throw new KeyNotFoundException ( $"Hook #{hookID.Key} is not owned by this component!" );
			bool ret;
			if (!(ret = UnhookWindowsHookEx ( hookID.HookID ))) ErrorList.Add ( (nameof ( UnhookHookEx ), new Win32Exception ()) );
			HookIDDict.Remove ( hookID.Key );
			OwnHooks.Remove ( hookID.Key );
			return ret;
		}

		public override string PrintHookInfo ( DictionaryKey key ) {
			if (!OwnHooks.TryGetValue ( key, out int hookCode )) return null;
			if (!HookIDDict.TryGetValue ( key, out var hook )) return null;
			return $"WinHook#{key}[{hookCode}]:{hook.HookInfo.DeviceID}<{string.Join ( ", ", hook.HookInfo.ChangeMask )}>";
		}

		public override uint SimulateInput ( uint nInputs, HInputData[] pInputs, int cbSize, bool? shouldProcess = null ) {
			//var inputs = pInputs.Select ( ( input ) => (Input)input.Data ).ToArray ();
			HWInput[] inputs = new HWInput[nInputs];
			for (int i = 0; i < nInputs; i++) {
				inputs[i] = (HWInput)pInputs[i].Data;
				inputs[i].Data.ki.ToSendFlags ();
				inputs[i].Data.ki.ClearValidity ();
			}
			var ret = SendInput ( nInputs, inputs, cbSize );
			if (ret < nInputs) ErrorList.Add ( (nameof ( SimulateInput ), new Win32Exception ()) );
			return ret;
		}
		public override nint GetMessageExtraInfoPtr () => GetMessageExtraInfo ();

		[DllImport ( "User32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		private static extern IntPtr SetWindowsHookEx ( int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId );

		[DllImport ( "User32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		[return: MarshalAs ( UnmanagedType.Bool )]
		private static extern bool UnhookWindowsHookEx ( IntPtr hhk );

		[DllImport ( "User32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		private static extern IntPtr CallNextHookEx ( IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam );

		[DllImport ( "kernel32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		private static extern IntPtr GetModuleHandle ( string lpModuleName );

		[DllImport ( "user32.dll" )]
		private static extern IntPtr GetMessageExtraInfo ();

		[DllImport ( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		private static extern uint SendInput ( uint nInputs, HWInput[] pInputs, int cbSize );


		public override StateInfo Info => new VStateInfo ( this );
		public class VStateInfo : DStateInfo {
			public new VWinLowLevelLibs Owner => (VWinLowLevelLibs)base.Owner;
			public VStateInfo ( VWinLowLevelLibs owner ) : base ( owner) {

			}

			protected override string[] GetHooks () {
				int N = Owner.OwnHooks.Count;
				string[] ret = new string[N];
				int ID = 0;
				foreach ( var hID in Owner.OwnHooks.Keys )
					ret[ID++] = $"{hID} => {HookIDDict[hID]}";
				return ret;
			}
		}
	}




	public class WinLLInputData : HInputData {
		HWInput data;

		public WinLLInputData ( ComponentBase owner, HWInput newData ) : base ( owner ) { data = newData; }
		/// <summary>Create new LLInputData for key press</summary>
		/// <param name="owner">A component that should be set as owner of this dataHolder</param>
		/// <param name="vkCode">Virtual Key Code<para>Must be 0 when dwFlags = KEYEVENTF_UNICODE</para><seealso href="https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes"/></param>
		/// <param name="scanCode">Hardware scan code of the key<para>When dwFlags = KEYEVENTF_UNICODE, it is the Unicode value of given character</para></param>
		/// <param name="dwFlags">Flags of given keystroke.<seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput#members"/></param>
		/// <param name="time">Timestamp of keystroke. When 0, it is set by system.</param>
		/// <param name="dwExtraInfo">Some extra info</param>
		/// <returns></returns>
		public static WinLLInputData NewKeyboardData (ComponentBase owner, ushort vkCode, ushort scanCode, uint dwFlags, uint time, IntPtr dwExtraInfo ) {
			HWInput.KeyboardInput data = new () {
				vkCode = vkCode,
				scanCode = scanCode,
				dwFlags = dwFlags,
				time = time,
				dwExtraInfo = dwExtraInfo
			};
			if ( !data.IsValidated () ) data.dwFlags |= (uint)HWInput.KeyboardInput.SendInputFlags.ValidInputFlags;
			HWInput.InputUnion dataUnion = new HWInput.InputUnion () { ki = data } ;
			return new WinLLInputData ( owner, new HWInput ( HWInput.TypeKEY, dataUnion ) );
		}
		public static WinLLInputData NewKeyboardData ( ComponentBase owner, nint vkChngCode, IntPtr dataPtr ) {
			var data = new HWInput.KeyboardInput ( dataPtr );
			if ( (VKChange)vkChngCode == VKChange.KeyUp )
				data.dwFlags |= (uint)HWInput.KeyboardInput.CallbackFlags.KeyUp;
			HWInput.InputUnion dataUnion = new HWInput.InputUnion () { ki = data };
			var ret = new WinLLInputData ( owner, new HWInput ( HWInput.TypeKEY, dataUnion ) );
			ret.Pressed = (VKChange)vkChngCode;
			return ret;
		}
		public static WinLLInputData NewMouseData (ComponentBase owner, nint vkChngCode, IntPtr dataPtr ) {
			var data = new HWInput.MouseInput ( dataPtr );
			HWInput.InputUnion dataUnion = new HWInput.InputUnion () { mi = data };
			var ret = new WinLLInputData ( owner, new HWInput ( HWInput.TypeMOUSE, dataUnion ) );
			ret.Pressed = (VKChange)vkChngCode;
			return ret;
		}

		public override IInputLLValues Data { get => data; protected set => data = (HWInput)value; }

		public override int SizeOf => data.SizeOf;

		public override int DeviceID { get ; protected set; }
		public override VKChange Pressed { get; protected set; }

		public override DataHolderBase Clone () => new WinLLInputData ( Owner, data );
		public override bool Equals ( object obj ) {
			if ( obj == null ) return false;
			if ( obj.GetType () != GetType () ) return false;
			if ( !base.Equals ( (HInputData)obj ) ) return false;
			var item = (WinLLInputData)obj;
			return data.Equals ( item.data );
		}
		public override int GetHashCode () => data.GetHashCode ();
		public override string ToString () => data.ToString ();
		public override void UpdateByHook ( DLowLevelInput hookObj, DictionaryKey hookID ) { }
	}
}