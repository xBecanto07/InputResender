using Components.Interfaces;
using Components.Library;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System;
using static InputResender.GUIComponents.HWInput.KeyboardInput;
using System.Reflection.Emit;

namespace InputResender.GUIComponents; 
public struct HWInput : IInputLLValues {
	public const int Mouse = 0;
	public const int Keyboard = 1;
	public const int Hardware = 2;

	public int Type;
	public InputUnion Data;

	public override string ToString () {
		switch( Type ) {
		case Mouse: return $"Mouse input ({Data.mi})";
		case Keyboard: return $"Keyboard input ({Data.ki})";
		case Hardware: return $"Hardware input ({Data.hi})";
		default: return "Unknown input type";
		}
	}
	public override bool Equals ( [NotNullWhen ( true )] object obj ) {
		if ( obj == null ) return false;
		if ( obj.GetType () != GetType () ) return false;
		var item = (HWInput)obj;
		if ( item.Type != Type ) return false;
		switch( Type ) {
		case Mouse: return item.Data.mi.Equals ( Data.mi );
		case Keyboard: return item.Data.ki.Equals ( Data.ki );
		case Hardware: return item.Data.hi.Equals ( Data.hi );
		default: return false;
		}
	}

	public const int TypeKEY = 1, TypeMOUSE = 0, TypeHARDWARE = 2;

	public int SizeOf { get => Marshal.SizeOf ( typeof ( HWInput ) ); }

	public KeyCode Key { get {
			switch (Type) {
			case TypeKEY: return (KeyCode)Data.ki.vkCode;
			default: return KeyCode.None;
			}
		} }

	public HWInput (int type, InputUnion data) { Type = type; Data = data; }

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

		const uint keyDownID = (uint)(SendInputFlags.KeyDown | SendInputFlags.Scancode);
		const uint keyUpID = (uint)(SendInputFlags.KeyUp | SendInputFlags.Scancode);

		CallbackFlags callbackFlags => (CallbackFlags)dwFlags;
		SendInputFlags sendInputFlags => (SendInputFlags)dwFlags;

		public KeyboardInput (nint ptr ) {
			vkCode = (ushort)Marshal.ReadInt32 ( ptr );
			scanCode = (ushort)Marshal.ReadInt32 ( ptr, 4 );
			dwFlags = (uint)Marshal.ReadInt32 ( ptr, 8 );
			dwFlags |= (uint)CallbackFlags.ValidCallbackFlags;
			time = (uint)Marshal.ReadInt32 ( ptr, 12 );
			dwExtraInfo = Marshal.ReadIntPtr ( ptr, 16 );
		}
		public override string ToString () => $"wVK:{(KeyCode)vkCode}, wScan:{(KeyCode)scanCode}, dwFlags:{dwFlags}{(IsValidated () ? '+' : '?')}, time:{time}, dwEI *{dwExtraInfo}";
		public override bool Equals ( [NotNullWhen ( true )] object obj ) {
			if ( obj == null ) return false;
			if (!(obj is  KeyboardInput )) return false;
			var ki = (KeyboardInput)obj;
			return (ki.vkCode == vkCode) & (ki.scanCode == scanCode);
		}
		/// <summary>Used in keyboard hook callback</summary>
		[Flags] // Copied from: http://pinvoke.net/default.aspx/Structures/KBDLLHOOKSTRUCT.html
		public enum CallbackFlags : uint {
			KeyDown = 0x00,

			EXTENDED = 0x01,
			INJECTED = 0x10,
			ALTDOWN = 0x20,
			KeyUp = 0x80,

			AssignedValidity = 0x0100,
			ValidCallbackFlags = 0x0400 | AssignedValidity
		}
		/// <summary>Used for SendInput</summary>
		[Flags]
		public enum SendInputFlags : uint {
			KeyDown = 0x0000,

			ExtendedKey = 0x0001,
			KeyUp = 0x0002,
			Unicode = 0x0004,
			Scancode = 0x0008,

			AssignedValidity = 0x0100,
			ValidInputFlags = 0x0200 | AssignedValidity
		}
		/// <summary>Used in keyboard hook callback</summary>
		public void ToCallbackFlags () {
			AssertValidity ();
			if ( (dwFlags & (uint)CallbackFlags.ValidCallbackFlags) == 0 ) return;
			SendInputFlags flag = (SendInputFlags)dwFlags;
			CallbackFlags nFlag = 0;
			if ( flag.HasFlag ( SendInputFlags.ExtendedKey ) ) nFlag |= CallbackFlags.EXTENDED;
			if ( flag.HasFlag ( SendInputFlags.KeyUp ) ) nFlag |= CallbackFlags.KeyUp;
			dwFlags = (uint)nFlag;
		}
		/// <summary>Used for SendInput</summary>
		public void ToSendFlags () {
			AssertValidity ();
			if ( (dwFlags & (uint)SendInputFlags.ValidInputFlags) == 0 ) return;
			CallbackFlags flag = (CallbackFlags)dwFlags;
			SendInputFlags nFlag = SendInputFlags.ValidInputFlags;
			if ( flag.HasFlag ( CallbackFlags.EXTENDED ) ) nFlag |= SendInputFlags.ExtendedKey;
			if ( flag.HasFlag ( CallbackFlags.KeyUp ) ) nFlag |= SendInputFlags.KeyUp;
			dwFlags = (uint)nFlag;
		}
		public bool IsValidated () => (dwFlags & (uint)SendInputFlags.AssignedValidity) != 0;
		public void AssertValidity () {
			if ( !IsValidated () ) throw new DataMisalignedException ( "Flags type is not assigned and thus cannot determine format." );
		}
		public void ClearValidity () => dwFlags &= ~((uint)SendInputFlags.ValidInputFlags | (uint)CallbackFlags.ValidCallbackFlags);
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

		/// <summary>Used in mouse hook callback</summary>
		[Flags] // Copied from: http://pinvoke.net/default.aspx/Structures.MSLLHOOKSTRUCT
		public enum CallbackFlags : uint {
			LLMHF_INJECTED = 1,
			LLMHF_LOWER_IL_INJECTED = 2
		}

		public override string ToString () => $"d:{dx}:{dy}, mD:{mouseData}, dwFlags:{dwFlags}, time:{time}, dwEI *{dwExtraInfo}";

		public MouseInput ( nint ptr ) {
			dx = Marshal.ReadInt32 ( ptr );
			dy = Marshal.ReadInt32 ( ptr, 4 );
			dwFlags = (uint)Marshal.ReadInt32 ( ptr, 8 );
			time = (uint)Marshal.ReadInt32 ( ptr, 12 );
			dwExtraInfo = Marshal.ReadIntPtr ( ptr, 16 );
		}
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

	public IInputLLValues Clone () => new HWInput ( Type, Data );
}
