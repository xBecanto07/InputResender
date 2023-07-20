using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace InputResender.UserTestingNetCore {
	public class LL_Keyboard {
		public delegate IntPtr LowLevelKeyboardProc ( int nCode, IntPtr wParam, IntPtr lParam );
		public const int WH_KEYBOARD_LL = 13;
		public const int WM_KEYDOWN = 0x0100;

		public static IntPtr _hookID = IntPtr.Zero;
		public static LowLevelKeyboardProc _hookCallBack = null;

		public static void StartHook ( LowLevelKeyboardProc HookCallback ) {
			Program.MainForm.Invoke ( () => {
				Program.MainForm.ConsoleText.AppendText ( $"{Environment.NewLine}Starting hook!" );
			} );
			_hookCallBack = HookCallback;

			// Low-level setup of hook
			using ( Process curProcess = Process.GetCurrentProcess () )
			using ( ProcessModule curModule = curProcess.MainModule )
				_hookID = SetWindowsHookEx ( WH_KEYBOARD_LL, _hookCallBack, GetModuleHandle ( curModule.ModuleName ), 0 );
		}
		public static void StopHook () { UnhookWindowsHookEx ( _hookID ); }


		[DllImport ( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		public static extern IntPtr SetWindowsHookEx ( int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId );

		[DllImport ( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		[return: MarshalAs ( UnmanagedType.Bool )]
		public static extern bool UnhookWindowsHookEx ( IntPtr hhk );

		// Pass exception to next hook in current hook chain
		[DllImport ( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		public static extern IntPtr CallNextHookEx ( IntPtr hhk, int nCode,
			IntPtr wParam, IntPtr lParam );

		[DllImport ( "kernel32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		public static extern IntPtr GetModuleHandle ( string lpModuleName );

		[Flags]
		public enum InputType {
			Mouse = 0,
			Keyboard = 1,
			Hardware = 2
		}

		[Flags]
		public enum KeyEventF {
			KeyDown = 0x0000,
			ExtendedKey = 0x0001,
			KeyUp = 0x0002,
			Unicode = 0x0004,
			Scancode = 0x0008
		}


		public struct LL_Input {
			public int type;
			public InputUnion u;

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
		}

		[DllImport ( "user32.dll" )]
		protected static extern IntPtr GetMessageExtraInfo ();

		[DllImport ( "user32.dll", SetLastError = true )]
		protected static extern uint SendInput ( uint nInputs, LL_Input[] pInputs, int cbSize );



		/// <summary>Generic class with info about keyboard events.</summary>
		public class KeyEventInfo {
			private int vkCode;
			public int scanCode;
			public LL_Input.KeyHookFlags flags;
			public uint time;
			public int extraInfo;

			public Keys KeyName { get { return (Keys)vkCode; } }
			public int KeyID { get { return vkCode; } }

			public bool IsNewHook ( Stack<KeyEventInfo> hookStack ) {
				if ( hookStack.Count < 1 ) return true;
				KeyEventInfo other = hookStack.Peek ();
				return vkCode != other.vkCode || scanCode != other.scanCode || time != other.time;
			}
			public static KeyEventInfo Load ( IntPtr ptr ) {
				return new KeyEventInfo ( (LL_Input.KeyboardInfo)Marshal.PtrToStructure ( ptr, typeof ( LL_Input.KeyboardInfo ) ) );
			}
			public override string ToString () { return $"{(Keys)vkCode} (c={scanCode} | t={time % 0x1000:X3} | {flags})"; }
			public static explicit operator LL_Input ( KeyEventInfo obj ) {
				return new LL_Input {
					type = (int)InputType.Keyboard,
					u = new LL_Input.InputUnion {
						ki = (LL_Input.KeyboardInput)obj
					}
				};
			}
			public static explicit operator LL_Input.KeyboardInput ( KeyEventInfo obj ) {
				return new LL_Input.KeyboardInput {
					vkCode = (ushort)obj.vkCode,
					scanCode = (ushort)obj.scanCode,
					dwFlags = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
					time = obj.time,
					dwExtraInfo = GetMessageExtraInfo ()
				};
			}
			public KeyEventInfo ( LL_Input.KeyboardInfo obj ) {
				vkCode = (int)obj.vkCode;
				scanCode = (int)obj.scanCode;
				time = obj.time;
				flags = obj.flags;
				extraInfo = obj.dwExtraInfo != IntPtr.Zero ? Marshal.ReadInt32 ( obj.dwExtraInfo ) : 0;
			}
			public void PressKey ( bool Pressed = true ) {
				if ( !Pressed ) flags &= ~LL_Input.KeyHookFlags.KeyUp;
				else flags |= LL_Input.KeyHookFlags.KeyUp;
			}
		}
	}
}
