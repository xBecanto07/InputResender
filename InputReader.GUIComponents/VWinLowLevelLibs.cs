﻿using Components.Interfaces;
using Components.Library;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;
using System.Linq;

namespace InputResender.GUIComponents {
	public class VWinLowLevelLibs : DLowLevelInput {
		private const int WH_KEYBOARD_LOW_LEVEL = 13;
		private LowLevelKeyboardProc Callback;
		private static VWinLowLevelLibs mainInstance;
		const int MaxLog = 256;
		public static List<(int nCode, VKChange changeCode, Input.KeyboardInput inputData)> EventList = new List<(int, VKChange, Input.KeyboardInput)> ( MaxLog );

		private static void Log ( int nCode, IntPtr vkChngCode, IntPtr vkCode ) {
			var change = (VKChange)vkChngCode;
			var input = new Input.KeyboardInput ( vkCode );
			// if ( EventList.Count > 0 && EventList[0].Item3.Equals ( input ) && EventList[0].Item2 == change ) vkChngCode ^= 0x1;
			if ( EventList.Count >= MaxLog ) EventList.RemoveAt ( MaxLog - 1 );
			EventList.Insert ( 0, (nCode, change, input) );
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

		public override HInputEventDataHolder GetHighLevelData ( DictionaryKey hookKey, DInputReader requester, HInputData lowLevelData ) {
			Input llData = (Input)lowLevelData.Data;
			Input.KeyboardInput keyInfo = llData.Data.ki;
			float newVal = 0, oldVal = 0;
			switch ( lowLevelData.Pressed ) {
			case VKChange.KeyDown: oldVal = 0; newVal = 1; break;
			case VKChange.KeyUp: oldVal = 1; newVal = 0; break;
			}
			var ret = new HKeyboardEventDataHolder ( requester, 0, keyInfo.vkCode, newVal, newVal - oldVal );
			ret.SetNewValue ( ret.Convert ( newVal ), 0, 0 );
			return ret;
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
		public override HInputData ParseHookData ( DictionaryKey hookID, nint vkChngCode, nint vkCode ) => WinLLInputData.NewKeyboardData ( this, vkChngCode, vkCode );

		public override nint CallNextHook ( nint hhk, int nCode, nint wParam, nint lParam ) => CallNextHookEx ( hhk, nCode, wParam, lParam );
		public override nint GetModuleHandleID ( string lpModuleName ) => GetModuleHandle ( lpModuleName );
		public override Hook[] SetHookEx ( HHookInfo hookInfo, Func<DictionaryKey, HInputData, bool> callback ) {
			var hookKey = HookKeyFactory.NewKey ();
			var hook = new Hook ( this, hookInfo, hookKey, callback, Log );

			if ( hook == null ) {
				System.Text.StringBuilder SB = new System.Text.StringBuilder ();
				SB.AppendLine ( $"Error when creating hook for {hookInfo}!{Environment.NewLine}" );
				PrintErrors ( ( ss ) => SB.AppendLine ( ss ) );
				throw new InvalidOperationException ( SB.ToString () );
			}

			var moduleHandle = GetModuleHandleID ( "user32.dll" );
			var hookID = SetWindowsHookEx ( HookTypeCode, hook.Callback, moduleHandle, 0 );
			if ( hookID == (IntPtr)null ) {
				ErrorList.Add ( (nameof ( SetHookEx ), new Win32Exception ()) );
				return null;
			}
			HookIDDict.Add ( hookKey, hookID );
			hook.UpdateHookID ( hookID );
			return new Hook[1] { hook };
		}
		public override bool UnhookHookEx ( Hook hookID ) {
			if ( !HookIDDict.ContainsPair ( hookID.Key, hookID.HookID ) ) throw new KeyNotFoundException ( $"Hook with key {hookID} was not found!" );
			bool ret;
			if (!(ret = UnhookWindowsHookEx ( hookID.HookID ))) ErrorList.Add ( (nameof ( UnhookHookEx ), new Win32Exception ()) );
			HookIDDict.Remove ( hookID.Key );
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
		private static extern uint SendInput ( uint nInputs, Input[] pInputs, int cbSize );




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
		public static WinLLInputData NewKeyboardData ( ComponentBase owner, nint vkChngCode, IntPtr dataPtr ) {
			var data = new Input.KeyboardInput ( dataPtr );
			Input.InputUnion dataUnion = new Input.InputUnion () { ki = data };
			var ret = new WinLLInputData ( owner, new Input ( Input.TypeKEY, dataUnion ) );
			ret.Pressed = (VKChange)vkChngCode;
			return ret;
		}

		public override IInputLLValues Data { get => data; protected set => data = (Input)value; }

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

		public int SizeOf { get => Marshal.SizeOf ( typeof ( Input ) ); }

		public KeyCode Key { get {
				switch (Type) {
				case TypeKEY: return (KeyCode)Data.ki.vkCode;
				default: return KeyCode.None;
				}
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
				return (ki.vkCode == vkCode) & (ki.scanCode == scanCode);
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
