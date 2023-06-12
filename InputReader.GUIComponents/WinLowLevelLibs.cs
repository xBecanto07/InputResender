using Components.Interfaces;
using Components.Library;
using System.Runtime.InteropServices;

namespace InputResender.GUIComponents {
	public partial class WinLowLevelLibs : DLowLevelInput {
		private const int WH_KEYBOARD_LOW_LEVEL = 13;

		/*public static void ProcessHook ( int nCode, IntPtr vkChngCode, IntPtr vkCode ) {
			if ( nCode >= 0 && vkChngCode == WM_KEYDOWN ) {
				int KeyCode = Marshal.ReadInt32 ( vkCode );
				Callback ( (Keys)KeyCode, KeyCode );
				//Console.WriteLine ( (Keys)vkCode );
			}
		}*/

		public WinLowLevelLibs ( CoreBase owner ) : base ( owner ) { }

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
			var inputUnion = new Input.InputUnion ();
			inputUnion.ki = keyboardData;
			return new WinLLInputData ( highLevelData.Owner, new Input ( 1, inputUnion ) );
		}
		public override HInputData ParseHookData ( int nCode, nint vkChngCode, nint vkCode ) => WinLLInputData.NewKeyboardData ( this, (ushort)vkCode, (ushort)(vkCode | vkChngCode), 0, 0, 0 );
			//new HKeyboardEventDataHolder ( caller, 1, Marshal.ReadInt32 ( vkCode ), 1 );
		public override nint CallNextHook ( nint hhk, int nCode, nint wParam, nint lParam ) => CallNextHookEx ( hhk, nCode, wParam, lParam );
		public override nint GetModuleHandleID ( string lpModuleName ) => GetModuleHandle ( lpModuleName );
		public override nint SetHookEx ( int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId ) => SetWindowsHookEx ( idHook, lpfn.Invoke, hMod, dwThreadId );
		public override bool UnhookHookEx ( nint hhk ) => UnhookWindowsHookEx ( hhk );
		public override uint SimulateInput ( uint nInputs, HInputData[] pInputs, int cbSize ) {
			var inputs = pInputs.Select ( ( input ) => (Input)input.Data ).ToArray ();
			return SendInput ( nInputs, inputs, inputs.Length );
		}

		[LibraryImport ( "user32.dll", SetLastError = true )]
		public static partial IntPtr SetWindowsHookEx ( int idHook, Func<int, IntPtr, IntPtr, IntPtr> lpfn, IntPtr hMod, uint dwThreadId );

		[LibraryImport ( "user32.dll", SetLastError = true )]
		[return: MarshalAs ( UnmanagedType.Bool )]
		public static partial bool UnhookWindowsHookEx ( IntPtr hhk );

		[LibraryImport ( "user32.dll", SetLastError = true )]
		public static partial IntPtr CallNextHookEx ( IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam );

		[LibraryImport ( "kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16 )]
		public static partial IntPtr GetModuleHandle ( string lpModuleName );

		[DllImport ( "user32.dll", SetLastError = true )]
		protected static extern uint SendInput ( uint nInputs, Input[] pInputs, int cbSize );
	}



	[Flags]
	public enum KeyEventF {
		KeyDown = 0x0000,
		ExtendedKey = 0x0001,
		KeyUp = 0x0002,
		Unicode = 0x0004,
		Scancode = 0x0008
	}


	public class WinLLInputData : HInputData {
		Input data;

		public WinLLInputData ( ComponentBase owner, Input newData ) : base ( owner ) { data = newData; }
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
	}
	public struct Input : IInputLLValues {
		public int Type;
		public InputUnion Data;

		public const int TypeKEY = 1, TypeMOUSE = 0, TypeHARDWARE = 2;

		public int SizeOf { get {
				int si = sizeof ( int );
				switch ( Type ) {
				case 0: return si + Marshal.SizeOf ( Data.mi );
				case 1: return si + Marshal.SizeOf ( Data.ki );
				case 2: return si + Marshal.SizeOf ( Data.hi );
				}
				return 0;
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
		}
		/// <summary>Obtained info about keyboard event during a hook.</summary>
		[StructLayout ( LayoutKind.Sequential )]
		public class KeyboardInfo {
			public uint vkCode;
			public uint scanCode;
			public KeyHookFlags flags;
			public uint time;
			public IntPtr dwExtraInfo;
		}

		/// <summary>Event info, is part of the 'InputUnion', used when simulating keypress.</summary>
		[StructLayout ( LayoutKind.Sequential )]
		public struct HardwareInput {
			public uint uMsg;
			public ushort wParamL;
			public ushort wParamH;
		}

		public IInputLLValues Clone () => new Input ( Type, Data );
	}
}
