using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace InputResender.WindowsGUI; 
public class WinLLInputStatusExtra : CInputLLParser.InputEventInfo {
	public const int MARK = StatusData.MARK;
	[StructLayout ( LayoutKind.Sequential )]
	private class StatusData {
		public const int MARK = 0x5ad40fb7;
		public readonly int StructMark;
		public readonly nint OrigExtraInfo;
		public readonly uint TimeOfRegistration;
		public readonly uint UID;
		public readonly nint structPtr;
		private int Holders;
		private int locked;

		public StatusData (uint uid, nint origData) {
			StructMark = MARK;
			OrigExtraInfo = origData;
			TimeOfRegistration = (uint)(System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond);
			UID = uid; // This should be set by the caller
			Holders = 1; // Initial holder count

			//GCHandle handle = GCHandle.Alloc ( this, GCHandleType.Pinned );
			//structPtr = handle.AddrOfPinnedObject ();
			structPtr = Marshal.AllocHGlobal ( Marshal.SizeOf<StatusData> () );
			Marshal.StructureToPtr ( this, structPtr, false ); // Save the structure to unmanaged memory
		}
		private StatusData () { }

		public static StatusData TryLoad ( nint ptr ) => TryLoad ( ptr, out var _ );
		public static StatusData TryLoad ( nint ptr, out System.Exception ex ) {
			ex = null;
			if ( ptr == nint.Zero ) { ex = new System.ArgumentNullException ( nameof ( ptr ) ); return null; }
			try {
				int mark = Marshal.ReadInt32 ( ptr );
				if ( mark != MARK ) {
					ex = new System.InvalidOperationException ( $"Invalid struct mark {mark:X} at {ptr:X}, expected {MARK:X}" );
					return null;
				}
			} catch ( System.Exception e ) { ex = e; return null; }
			try {
				var ret = Marshal.PtrToStructure<StatusData> ( ptr );
				ret.Holders++;
				Marshal.StructureToPtr ( ret, ptr, false ); // Update the structure in memory
				return ret;
			}
			catch ( System.Exception e ) { ex = e; return null; }
		}

		private void Lock() {
			while ( Interlocked.CompareExchange ( ref locked, 1, 0 ) != 0 )
				continue;
			Interlocked.MemoryBarrierProcessWide ();
		}
		public void Unlock() {
			Interlocked.MemoryBarrierProcessWide ();
			locked = 0;
		}

		public void Free () {
			Lock ();
			Holders--;
			if ( Holders <= 0 ) {
				Unlock ();
				Marshal.FreeHGlobal ( structPtr );
			} else {
				Unlock ();
			}
		}
	}




	public readonly HWInput inputData;
	readonly StatusData statusData;
	//readonly GCHandle handle;

	public uint TimeOfRegistration => statusData.TimeOfRegistration;
	public uint UID => statusData.UID;
	public nint StatusPtr => statusData.structPtr;

	private WinLLInputStatusExtra(HWInput data, StatusData status, bool consume, bool process) {
		inputData = data;
		statusData = status;
		//handle = statusData.Handle;
		ShouldProcess = process;
		CanConsume = consume;
	}

	public override void Dispose () {
		statusData.Free ();
	}

	protected override bool Equals ( CInputLLParser.InputEventInfo other ) {
		if ( other == null ) return false;
		if ( other.GetType () != GetType () ) return false;
		var item = (WinLLInputStatusExtra)other;
		bool ret = inputData.Equals ( item.inputData );
		ret &= (statusData.UID == item.statusData.UID);
		ret &= (statusData.OrigExtraInfo == item.statusData.OrigExtraInfo);
		ret &= (statusData.structPtr == item.statusData.structPtr);
		ret &= (statusData.TimeOfRegistration == item.statusData.TimeOfRegistration);
		return ret;
	}

	protected override CInputLLParser.InputEventInfoAssertor CreateAssertor () => new WinLLInputStatusExtraAssertor ();

	public class WinLLInputStatusParser : CInputLLParser.InputEventParser {
		Dictionary<nint, WinLLInputStatusExtra> Extras = [];
		uint uidProvider = 42;

		public override WinLLInputStatusExtra Parse ( int nCode, nint wParam, nint lParam ) {
			HWInput data;
			try { data = new ( wParam, lParam ); } catch ( System.Exception _ ) { return null; }
			lock ( Extras ) {
				if ( Extras.TryGetValue ( data.ExtraInfo, out var extra ) )
					return extra;
			}
			var status = StatusData.TryLoad ( data.ExtraInfo );
			if ( status == null ) {
				lock ( Extras ) {
					status = new ( uidProvider++, data.ExtraInfo );
				}
				data.UpdateExtraInfo ( lParam, status.structPtr );
			}
			WinLLInputStatusExtra ret = new ( data, status, true, nCode >= 0 );
			lock ( Extras ) { Extras.Add ( data.ExtraInfo, ret ); }
			return ret;
		}

		public override void Dispose () {}
	}

	public class WinLLInputStatusExtraAssertor : CInputLLParser.InputEventInfoAssertor {
		public uint? ExpTimeOfRegistration;
		public uint? ExpUID;
		public nint? ExpStatusPtr;
		public nint? ExpOrigExtraInfo;
		// General input
		public uint? ExpDWFlags;
		public uint? ExpTime;
		public IntPtr? ExpDWExtraInfo;
		// Keyboard input
		public ushort? ExpVkCode;
		public ushort? ExpScanCode;
		// Mouse input
		public int? ExpDx;
		public int? ExpDy;
		public uint? ExpMouseData;

		public override void Assert ( CInputLLParser.InputEventInfo item ) {
			item.Should ().NotBeNull ();
			var info = item.Should ().BeOfType<WinLLInputStatusExtra> ().Subject;
			if ( ExpTimeOfRegistration.HasValue ) info.TimeOfRegistration.Should ().Be ( ExpTimeOfRegistration.Value );
			if ( ExpUID.HasValue ) info.UID.Should ().Be ( ExpUID.Value );
			if ( ExpStatusPtr.HasValue ) info.StatusPtr.Should ().Be ( ExpStatusPtr.Value );
			if ( ExpOrigExtraInfo.HasValue ) info.statusData.OrigExtraInfo.Should ().Be ( ExpOrigExtraInfo.Value );

			if ( ExpDWFlags.HasValue ) info.inputData.DWFlags.Should ().Be ( ExpDWFlags.Value );
			if ( ExpTime.HasValue ) info.inputData.Time.Should ().Be ( ExpTime.Value );
			if ( ExpDWExtraInfo.HasValue ) info.inputData.ExtraInfo.Should ().Be ( ExpDWExtraInfo.Value );
			if ( ExpVkCode.HasValue ) {
				info.inputData.Type.Should ().Be ( HWInput.TypeKEY );
				info.inputData.Data.ki.vkCode.Should ().Be ( ExpVkCode.Value );
			}
			if ( ExpScanCode.HasValue ) {
				info.inputData.Type.Should ().Be ( HWInput.TypeKEY );
				info.inputData.Data.ki.scanCode.Should ().Be ( ExpScanCode.Value );
			}
			if ( ExpDx.HasValue ) {
				info.inputData.Type.Should ().Be ( HWInput.TypeMOUSE );
				info.inputData.Data.mi.dx.Should ().Be ( ExpDx.Value );
			}
			if ( ExpDy.HasValue ) {
				info.inputData.Type.Should ().Be ( HWInput.TypeMOUSE );
				info.inputData.Data.mi.dy.Should ().Be ( ExpDy.Value );
			}
			if ( ExpMouseData.HasValue ) {
				info.inputData.Type.Should ().Be ( HWInput.TypeMOUSE );
				info.inputData.Data.mi.mouseData.Should ().Be ( ExpMouseData.Value );
			}
		}

		protected override void FillInner ( CInputLLParser.InputEventInfo info ) {
			if ( info == null ) return;
			var item = info.Should ().BeOfType<WinLLInputStatusExtra> ().Subject;
			ExpTimeOfRegistration = item.TimeOfRegistration;
			ExpUID = item.UID;
			ExpStatusPtr = item.StatusPtr;
			ExpOrigExtraInfo = item.statusData.OrigExtraInfo;
			ExpDWFlags = item.inputData.DWFlags;
			ExpTime = item.inputData.Time;
			ExpDWExtraInfo = item.inputData.ExtraInfo;
			if ( item.inputData.Type == HWInput.TypeKEY ) {
				ExpVkCode = item.inputData.Data.ki.vkCode;
				ExpScanCode = item.inputData.Data.ki.scanCode;
			} else if ( item.inputData.Type == HWInput.TypeMOUSE ) {
				ExpDx = item.inputData.Data.mi.dx;
				ExpDy = item.inputData.Data.mi.dy;
				ExpMouseData = item.inputData.Data.mi.mouseData;
			}
		}
	}
}