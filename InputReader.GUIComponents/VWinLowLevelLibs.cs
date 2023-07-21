using Components.Interfaces;
using Components.Library;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace InputResender.GUIComponents {
	public class VWinLowLevelLibs : DLowLevelInput {
		private const int WH_KEYBOARD_LOW_LEVEL = 13;
		private LowLevelKeyboardProc Callback;
		private static VWinLowLevelLibs mainInstance;
		public static List<(int, VKChange, KeyCode, Input.KeyboardInput)> EventList = new List<(int, VKChange, KeyCode, Input.KeyboardInput)> ();
		public static LowLevelKeyboardProc internalCallback = ProcessHook;

		private static nint ProcessHook ( int nCode, IntPtr vkChngCode, IntPtr vkCode ) {
			EventList.Add ( ( nCode, (VKChange)vkChngCode, (KeyCode)vkCode, new Input.KeyboardInput ( vkCode )) );
			//if ( nCode >= 0 ) Callback ( (Keys)KeyCode, KeyCode );
			if ( nCode >= 0 ) {
				return mainInstance.Callback ( nCode, vkChngCode, vkCode );
			} else {
				return mainInstance.CallNextHook ( 0, nCode, vkChngCode, vkCode );
			}
		}

		public VWinLowLevelLibs ( CoreBase owner ) : base ( owner ) { mainInstance = this; }

		public override int ComponentVersion => 1;
		public override int HookTypeCode => WH_KEYBOARD_LOW_LEVEL;
		public override int GetChangeCode ( VKChange vKChange ) {
			// Source: https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-keydown
			switch ( vKChange ) {
			case VKChange.KeyDown:	return 0x0100;
			case VKChange.KeyUp:	return 0x0101;
			}
			return -1;
		}
		public override VKChange GetChangeType ( int vkCode ) {
			switch ( vkCode ) {
			case 0x0100: return VKChange.KeyDown;
			case 0x0101: return VKChange.KeyUp;
			}
			throw new InvalidCastException ( $"No key change action type corresponds to code {vkCode:X}" );
		}

		public override HInputEventDataHolder GetHighLevelData ( DInputReader requester, HInputData lowLevelData ) {
			Input llData = (Input)lowLevelData.Data;
			Input.KeyboardInput keyInfo = llData.Data.ki;
			return new HKeyboardEventDataHolder ( requester, 0, keyInfo.vkCode, 1 );
		}
		public override HInputData GetLowLevelData ( HInputEventDataHolder highLevelData ) {
			var keyboardData = new Input.KeyboardInput ();
			keyboardData.time = (uint)new TimeSpan ( highLevelData.CreationTime.Ticks ).TotalMilliseconds;
			keyboardData.vkCode = (ushort)highLevelData.InputCode;
			keyboardData.scanCode = (ushort)highLevelData.InputCode;
			keyboardData.dwFlags = highLevelData.Pressed >= 1 ? 0u : 0x8u;
			var inputUnion = new Input.InputUnion ();
			inputUnion.ki = keyboardData;
			return new WinLLInputData ( highLevelData.Owner, new Input ( 1, inputUnion ) );
		}
		public override HInputData ParseHookData ( int nCode, nint vkChngCode, nint vkCode ) => WinLLInputData.NewKeyboardData ( this, (ushort)vkCode, (ushort)(vkCode | vkChngCode), 0, 0, 0 );

		public override nint CallNextHook ( nint hhk, int nCode, nint wParam, nint lParam ) => CallNextHookEx ( hhk, nCode, wParam, lParam );
		public override nint GetModuleHandleID ( string lpModuleName ) => GetModuleHandle ( lpModuleName );
		public override nint SetHookEx ( int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId ) {
			Callback = lpfn;
			var ret = SetWindowsHookEx ( idHook, internalCallback, hMod, dwThreadId );
			if ( ret == (IntPtr)null ) ErrorList.Add ( (nameof ( SetHookEx ), new Win32Exception ()) );
			return ret;
		}
		public override bool UnhookHookEx ( nint hhk ) {
			bool ret;
			if (!(ret = UnhookWindowsHookEx ( hhk ))) ErrorList.Add ( (nameof ( UnhookHookEx ), new Win32Exception ()) );
			return ret;
		}
		public override uint SimulateInput ( uint nInputs, HInputData[] pInputs, int cbSize, bool? shouldProcess = null ) {
			var inputs = pInputs.Select ( ( input ) => (Input)input.Data ).ToArray ();
			var ret = SendInput ( nInputs, inputs, cbSize );
			if (ret < nInputs) ErrorList.Add ( (nameof ( SimulateInput ), new Win32Exception ()) );
			return ret;
		}
		public override nint GetMessageExtraInfoPtr () => GetMessageExtraInfo ();

		[DllImport ( "User32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		protected static extern IntPtr SetWindowsHookEx ( int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId );

		[DllImport ( "User32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		[return: MarshalAs ( UnmanagedType.Bool )]
		protected static extern bool UnhookWindowsHookEx ( IntPtr hhk );

		[DllImport ( "User32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		protected static extern IntPtr CallNextHookEx ( IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam );

		[DllImport ( "kernel32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		protected static extern IntPtr GetModuleHandle ( string lpModuleName );

		[DllImport ( "user32.dll" )]
		private static extern IntPtr GetMessageExtraInfo ();

		[DllImport ( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		protected static extern uint SendInput ( uint nInputs, Input[] pInputs, int cbSize );
	}



	[Flags]
	public enum KeyEventF : uint {
		KeyDown = 0x0000,
		ExtendedKey = 0x0001,
		KeyUp = 0x0002,
		Unicode = 0x0004,
		Scancode = 0x0008
	}


	public class WinLLInputData : HInputData {
		Input data;

		public WinLLInputData ( ComponentBase owner, Input newData ) : base ( owner ) { data = newData; }
		/// <summary>Create new LLInputData for key press</summary>
		/// <param name="owner">A component that should be set as owner of this dataHolder</param>
		/// <param name="vkCode">Virtual Key Code<para>Must be 0 when dwFlags = KEYEVENTF_UNICODE</para><seealso href="https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes"/></param>
		/// <param name="scanCode">Hardware scan code of the key<para>When dwFlags = KEYEVENTF_UNICODE, it is the Unicode value of given character</para></param>
		/// <param name="dwFlags">Flags of given keystroke.<seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput#members"/></param>
		/// <param name="time">Timestamp of keystroke. When 0, it is set by system.</param>
		/// <param name="dwExtraInfo">Some extra info</param>
		/// <returns></returns>
		public static WinLLInputData NewKeyboardData (ComponentBase owner, ushort vkCode, ushort scanCode, uint dwFlags, uint time, IntPtr dwExtraInfo ) {
			Input.KeyboardInput data = new () {
				vkCode = vkCode,
				scanCode = scanCode,
				dwFlags = dwFlags,
				time = time,
				dwExtraInfo = dwExtraInfo
			};
			Input.InputUnion dataUnion = new Input.InputUnion () { ki = data } ;
			return new WinLLInputData ( owner, new Input ( Input.TypeKEY, dataUnion ) );
		}

		public override IInputLLValues Data { get => data; protected set => data = (Input)value; }

		public override int SizeOf => data.SizeOf;

		public override DataHolderBase Clone () => new WinLLInputData ( Owner, data );
		public override bool Equals ( object obj ) {
			if ( obj == null ) return false;
			if ( obj.GetType () != GetType () ) return false;
			var item = (WinLLInputData)obj;
			return data.Equals ( item.data );
		}
		public override int GetHashCode () => data.GetHashCode ();
		public override string ToString () => data.ToString ();
		public override void UpdateByHook ( DLowLevelInput hookObj, nint hookID ) { }
	}
	public struct Input : IInputLLValues {
		public int Type;
		public InputUnion Data;

		public override string ToString () {
			switch( Type ) {
			case 0: return $"Mouse input ({Data.mi})";
			case 1: return $"Keyboard input ({Data.ki})";
			case 2: return $"Hardware input ({Data.hi})";
			default: return "Unknown input type";
			}
		}
		public override bool Equals ( [NotNullWhen ( true )] object obj ) {
			if ( obj == null ) return false;
			if ( obj.GetType () != GetType () ) return false;
			var item = (Input)obj;
			if ( item.Type != Type ) return false;
			switch( Type ) {
			case 0: return item.Data.mi.Equals ( Data.mi );
			case 1: return item.Data.ki.Equals ( Data.ki );
			case 2: return item.Data.hi.Equals ( Data.hi );
			default: return false;
			}
		}

		public const int TypeKEY = 1, TypeMOUSE = 0, TypeHARDWARE = 2;

		public int SizeOf { get {
				return Marshal.SizeOf ( typeof ( Input ) );/*
				int si = sizeof ( int );
				switch ( Type ) {
				case 0: return si + Marshal.SizeOf ( Data.mi );
				case 1: return si + Marshal.SizeOf ( Data.ki );
				case 2: return si + Marshal.SizeOf ( Data.hi );
				}
				return 0;*/
			} }

		public Input (int type, InputUnion data) { Type = type; Data = data; }

		[StructLayout ( LayoutKind.Explicit )]
		public struct InputUnion {
			[FieldOffset ( 0 )] public MouseInput mi;
			[FieldOffset ( 0 )] public KeyboardInput ki;
			[FieldOffset ( 0 )] public HardwareInput hi;
		}

		// Input things copied from https://www.codeproject.com/Articles/5264831/How-to-Send-Inputs-using-Csharp
		/// <summary>Event info, is part of the 'InputUnion', used when simulating keypress.</summary>
		[StructLayout ( LayoutKind.Sequential )]
		public struct KeyboardInput {
			public ushort vkCode;
			public ushort scanCode;
			public uint dwFlags;
			public uint time;
			public IntPtr dwExtraInfo;

			const uint keyDownID = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode);
			const uint keyUpID = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode);

			public KeyboardInput (nint ptr ) {
				vkCode = (ushort)Marshal.ReadInt32 ( ptr );
				scanCode = (ushort)Marshal.ReadInt32 ( ptr, 4 );
				dwFlags = (uint)Marshal.ReadInt32 ( ptr, 8 );
				time = (uint)Marshal.ReadInt32 ( ptr, 12 );
				dwExtraInfo = Marshal.ReadIntPtr ( ptr, 16 );
			}
			public override string ToString () => $"wVK:{(KeyCode)vkCode}, wScan:{(KeyCode)scanCode}, dwFlags:{dwFlags}, time:{time}, dwEI *{dwExtraInfo}";
			public override bool Equals ( [NotNullWhen ( true )] object obj ) {
				if ( obj == null ) return false;
				if (!(obj is  KeyboardInput )) return false;
				var ki = (KeyboardInput)obj;
				return (ki.vkCode == vkCode) & (ki.scanCode == scanCode) & (ki.dwFlags == dwFlags);
			}
		}

		[Flags] // Copied from: http://pinvoke.net/default.aspx/Structures/KBDLLHOOKSTRUCT.html
		public enum KeyHookFlags : uint {
			EXTENDED = 0x01,
			INJECTED = 0x10,
			ALTDOWN = 0x20,
			KeyUp = 0x80,
		}

		[StructLayout ( LayoutKind.Sequential )]
		/// <summary>Event info, is part of the 'InputUnion', used when simulating keypress.</summary>
		public struct MouseInput {
			public int dx;
			public int dy;
			public uint mouseData;
			public uint dwFlags;
			public uint time;
			public IntPtr dwExtraInfo;

			public override string ToString () => $"d:{dx}:{dy}, mD:{mouseData}, dwFlags:{dwFlags}, time:{time}, dwEI *{dwExtraInfo}";
		}
		/// <summary>Obtained info about keyboard event during a hook.</summary>
		[StructLayout ( LayoutKind.Sequential )]
		public class KeyboardInfo {
			public uint vkCode;
			public uint scanCode;
			public uint flags;
			public uint time;
			public IntPtr dwExtraInfo;

			public override string ToString () => $"wVK:{(KeyCode)vkCode}, wScan:{(KeyCode)scanCode}, dwFlags:{flags}, time:{time}, dwEI *{dwExtraInfo}";
		}

		/// <summary>Event info, is part of the 'InputUnion', used when simulating keypress.</summary>
		[StructLayout ( LayoutKind.Sequential )]
		public struct HardwareInput {
			public uint uMsg;
			public ushort wParamL;
			public ushort wParamH;

			public override string ToString () => $"msg:{uMsg}, L:{wParamL}, H:{wParamH}";
		}

		public IInputLLValues Clone () => new Input ( Type, Data );
	}
}
