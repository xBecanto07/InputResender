using Components.Library;
using System.Runtime.InteropServices;

namespace Components.Interfaces {
	public abstract class HInputEventDataHolder : DataHolderBase {
		public const int PressThreshold = ushort.MaxValue;
		public HHookInfo HookInfo { get; protected set; }
		public int InputCode { get; protected set; }
		public int ValueX { get; protected set; }
		public int ValueY { get; protected set; }
		public int ValueZ { get; protected set; }

		public float Pressed {
			get {
				const double D = PressThreshold;
				double X = ValueX / D, Y = ValueY / D, Z = ValueZ / D;
				return (float)Math.Sqrt ( X * X + Y * Y + Z * Z );
			}
		}

		public HInputEventDataHolder ( DInputReader owner, HHookInfo hookInfo ) : base ( owner ) { HookInfo = hookInfo; }

		public void AddHookIDs (ICollection<nint> hookIDs) {
			foreach (nint hookID in hookIDs) HookInfo.AddHookID ( hookID );
		}

		public override bool Equals ( object obj ) {
			if ( obj == null ) return false;
			if ( obj.GetType () != GetType () ) return false;
			var item = (HInputEventDataHolder)obj;
			bool fullEqCheck = FullEqCheck & item.FullEqCheck;
			bool ret = (InputCode.Equals ( item.InputCode )) &
				(ValueX.Equals ( item.ValueX )) &
				(ValueY.Equals ( item.ValueY )) &
				(ValueZ.Equals ( item.ValueZ ));
			return ret & (HookInfo.Equals ( item.HookInfo ));
		}
		public override int GetHashCode () => (HookInfo, InputCode, ValueX, ValueY, ValueZ).GetHashCode ();
		public override string ToString () => $"{HookInfo.DeviceID}.{InputCode}:{HookInfo.LatestChangeType} [{ValueX.ToShortString ()};{ValueY.ToShortString ()};{ValueZ.ToShortString ()}]";
	}


	public abstract class HInputData : DataHolderBase {
		protected HInputData ( ComponentBase owner ) : base ( owner ) {
		}

		public abstract IInputLLValues Data { get; protected set; }
		public abstract int SizeOf { get; }
		public abstract void UpdateByHook (DLowLevelInput hookObj, nint hookID);
	}

	public interface IInputLLValues {
		IInputLLValues Clone ();
		int SizeOf { get; }
	}


	public class HInputData_Mock : HInputData {
		IInputStruct_Mock data;

		public HInputData_Mock ( ComponentBase owner, IInputStruct_Mock values ) : base ( owner ) => data = values;
		public HInputData_Mock ( ComponentBase owner, int hookID, VKChange keyChange, IntPtr vkCode ) : base ( owner ) => data = new IInputStruct_Mock ( hookID, keyChange, vkCode );

		public override IInputLLValues Data {
			get => data;
			protected set => data = (IInputStruct_Mock)value;
		}

		public override int SizeOf => data.SizeOf;

		public override DataHolderBase Clone () => new HInputData_Mock ( Owner, (IInputStruct_Mock)data.Clone () );
		public override bool Equals ( object obj ) {
			if ( obj == null ) return false;
			if ( obj.GetType () != GetType () ) return false;
			var item = (HInputData_Mock)obj;
			return data.Equals ( item.data );
		}
		public override int GetHashCode () => data.HookID.GetHashCode ();
		public override string ToString () => $"#{data.HookID}:{data.KeyChange}@{data.VKCode}";
		public override void UpdateByHook ( DLowLevelInput hookObj, nint hookID ) {
			data.HookID = (int)hookID;
		}

		public struct IInputStruct_Mock : IInputLLValues {
			public int HookID;
			public VKChange KeyChange;
			public IntPtr VKCode;

			public IInputStruct_Mock ( int hookID, VKChange keyChange, IntPtr vkCode ) { HookID = hookID; KeyChange = keyChange; VKCode = vkCode; }

			public int SizeOf => Marshal.SizeOf ( this );

			public IInputLLValues Clone () => new IInputStruct_Mock ( HookID, KeyChange, VKCode );
		}
	}
}