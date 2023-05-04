using Components.Library;
using System.Runtime.InteropServices;

namespace Components.Interfaces {
	public abstract class HInputData : DataHolderBase {
		protected HInputData ( ComponentBase owner ) : base ( owner ) {
		}

		public abstract IInputLLValues Data { get; protected set; }
		public abstract int SizeOf { get; }
	}

	public interface IInputLLValues {
		IInputLLValues Clone ();
		int SizeOf { get; }
	}


	public class HInputData_Mock : HInputData {
		IInputStruct_Mock data;

		public HInputData_Mock ( ComponentBase owner, IInputStruct_Mock values ) : base ( owner ) {
			data = values;
		}

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
		public override string ToString () => $"{data.HookID}";


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