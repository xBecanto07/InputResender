using Components.Interfaces;
using Components.Library;

namespace Components.Implementations {
	public class VInputSimulator : DInputSimulator {
		public VInputSimulator ( CoreBase newOwner ) : base ( newOwner ) {
		}

		public override int ComponentVersion => 1;

		public override HInputEventDataHolder[] ParseCommand ( InputData data ) => GetParser ( data.Cmnd ).Parse ( data );
		public override int Simulate ( params HInputEventDataHolder[] data ) {
			int ret = 0;
			foreach ( HInputEventDataHolder h in data )
				ret += (short)Owner.Fetch<DInputReader> ().SimulateInput ( h, AllowRecapture );
			return ret;
		}

		private SDInputCommandParser GetParser (InputData.Command cmnd) => (SDInputCommandParser)SubComponentFactory<DInputSimulator, InputData.Command>.Fetch ( this, cmnd );
	}

	public class SInputCommandParser_KeyDown : SDInputCommandParser {
		public SInputCommandParser_KeyDown ( DInputSimulator newOwner ) : base ( newOwner ) { }
		public readonly static SubComponentInfo<DInputSimulator, InputData.Command> SCInfo = new (
			( cmd ) => cmd == InputData.Command.KeyPress ? 1 : 0,
			( cmp, cmd ) => new SInputCommandParser_KeyDown ( cmp )
			);
		public override HInputEventDataHolder[] Parse ( InputData data ) =>
			new[] {
			new HKeyboardEventDataHolder ( data.Owner.Owner.Fetch<DInputReader> (), data.DeviceID, (int)data.Key, data.X, data.X )
			};
	}
	public class SInputCommandParser_KeyUp : SDInputCommandParser {
		public SInputCommandParser_KeyUp ( DInputSimulator newOwner ) : base ( newOwner ) { }
		public readonly static SubComponentInfo<DInputSimulator, InputData.Command> SCInfo = new (
			( cmd ) => cmd == InputData.Command.KeyRelease ? 1 : 0,
			( cmp, cmd ) => new SInputCommandParser_KeyUp ( cmp )
			);
		public override HInputEventDataHolder[] Parse ( InputData data ) =>
			new[] {
			new HKeyboardEventDataHolder ( data.Owner.Owner.Fetch<DInputReader> (), data.DeviceID, (int)data.Key, 0, 0 - data.X )
			};
	}
	public class SInputCommandParser_Type : SDInputCommandParser {
		public SInputCommandParser_Type ( DInputSimulator newOwner ) : base ( newOwner ) { }
		public readonly static SubComponentInfo<DInputSimulator, InputData.Command> SCInfo = new (
			( cmd ) => cmd == InputData.Command.Type ? 1 : 0,
			( cmp, cmd ) => new SInputCommandParser_Type ( cmp )
			);
		public override HInputEventDataHolder[] Parse ( InputData data ) =>
			new[] {
			new HKeyboardEventDataHolder ( data.Owner.Owner.Fetch<DInputReader> (), data.DeviceID, (int)data.Key, data.X, data.X ),
			new HKeyboardEventDataHolder ( data.Owner.Owner.Fetch<DInputReader> (), data.DeviceID, (int)data.Key, 0, 0 - data.X )
			};
	}
}
