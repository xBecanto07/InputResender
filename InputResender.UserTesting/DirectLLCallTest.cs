using Components.Library;
using InputResender.GUIComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SBld = System.Text.StringBuilder;
using System.Threading;
using System.Runtime.InteropServices;

namespace InputResender.UserTesting {
	public class DirectLLCallTest : UserTestBase {
		protected delegate IntPtr LowLevelKeyboardProc ( int nCode, IntPtr wParam, IntPtr lParam );
		protected const int WH_KEYBOARD_LL = 13;
		protected const int WM_KEYDOWN = 0x0100;

		protected static IntPtr _hookID = IntPtr.Zero;
		private static LowLevelKeyboardProc _hookCallBack = null;

		public static AutoResetEvent waiter = new AutoResetEvent ( false );
		public static List<(int nCode, VKChange changeType, KeyCode keyCode, Input.KeyboardInput inputData)> messages = new List<(int nCode, VKChange changeType, KeyCode keyCode, Input.KeyboardInput inputData)> ();

		public DirectLLCallTest ( SBld sb ) : base ( sb ) {
			StartHook ( Callback );
		}

		public IEnumerable<Action> MainTest () {
			Program.WriteLine ( "Press k ..." );
			yield return () => {
				var ret = waiter.WaitOne ( 10000 );
				if ( ret ) {
					SB.AppendLine ( $"Correct press detecting after {messages.Count} messages" );
					Result.Passed = true;
				}
			};
			yield break;
		}

		protected override void Dispose ( bool disposing ) {
			if ( _hookID != IntPtr.Zero ) StopHook ();
		}


		private static IntPtr Callback (int nCode, IntPtr vkChngCode, IntPtr lParam) {
			var info = new Input.KeyboardInput ( lParam );
			messages.Add( (nCode, (VKChange)vkChngCode, (KeyCode)Marshal.ReadInt32 ( lParam ), info) );
			if (nCode >= 0) {
				if ( info.vkCode == (int)KeyCode.K ) waiter.Set ();
			}
			return CallNextHookEx ( _hookID, nCode, vkChngCode, lParam );
		}


		protected static void StartHook ( LowLevelKeyboardProc HookCallback ) {
				_hookCallBack = HookCallback;

				// Low-level setup of hook
				using ( Process curProcess = Process.GetCurrentProcess () )
				using ( ProcessModule curModule = curProcess.MainModule )
					_hookID = SetWindowsHookEx ( WH_KEYBOARD_LL, _hookCallBack, GetModuleHandle ( curModule.ModuleName ), 0 );
		}
		public static void StopHook () {
			UnhookWindowsHookEx ( _hookID );
		}


		[DllImport ( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		protected static extern IntPtr SetWindowsHookEx ( int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId );

		[DllImport ( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		[return: MarshalAs ( UnmanagedType.Bool )]
		protected static extern bool UnhookWindowsHookEx ( IntPtr hhk );

		// Pass exception to next hook in current hook chain
		[DllImport ( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		protected static extern IntPtr CallNextHookEx ( IntPtr hhk, int nCode,
			IntPtr wParam, IntPtr lParam );

		[DllImport ( "kernel32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		protected static extern IntPtr GetModuleHandle ( string lpModuleName );

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


		public struct NewInput {
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
	}
}
