using Components.Library;
using static Components.Interfaces.InputData;

namespace Components.Interfaces {
	public abstract class DInputProcessor : ComponentBase<CoreBase> {
		public DInputProcessor ( CoreBase owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(ProcessInput), typeof(void))
			};

		public abstract InputData ProcessInput ( HInputEventDataHolder[] inputCombination );
	}

	public class MInputProcessor : DInputProcessor {
		public MInputProcessor ( CoreBase owner ) : base ( owner ) { }

		public override int ComponentVersion => 1;

		public override InputData ProcessInput ( HInputEventDataHolder[] inputCombination ) {
			if ( inputCombination == null || inputCombination.Length == 0 )
				return Empty ( this );
			var firstEvent = inputCombination[0];
			return new InputData ( this ) {
				Cmnd = firstEvent.Pressed >= 1 ? Command.KeyPress : Command.KeyRelease,
				X = firstEvent.Pressed,
				Key = (KeyCode)firstEvent.InputCode,
				DeviceID = firstEvent.HookInfo.DeviceID
			};
		}
	}

	public class InputData : SerializableDataHolderBase {
		public enum Command { None, KeyPress, KeyRelease, MouseMove }
		[Flags]
		public enum Modifier {
			None = 0, Shift = 1, Ctrl = 2, Alt = 4, AltGr = 8, WinKey = 16,
			CustMod1 = 32, CustMod2 = 64, CustMod3 = 128, CustMod4 = 256,
			CustMod5 = 512, CustMod6 = 1024, CustMod7 = 2048, CustMod8 = 4096,
			CustMod9 = 1 << 13, CustMod10 = 1 << 14, CustMod11 = 1 << 15, CustMod12 = 1 << 16,
			CustMod13 = 1 << 17, CustMod14 = 1 << 18, CustMod15 = 1 << 19, CustMod16 = 1 << 20,
			CustMod17 = 1 << 21, CustMod18 = 1 << 22, CustMod19 = 1 << 23, CustMod20 = 1 << 24,
			CustMod21 = 1 << 25, CustMod22 = 1 << 26, CustMod23 = 1 << 27, CustMod24 = 1 << 28,
			CustMod25 = 1 << 29, CustMod26 = 1 << 30, CustMod27 = 1 << 31
		}

		public Command Cmnd = Command.None;
		public KeyCode Key = KeyCode.None;
		public int DeviceID = 0;
		public Modifier Modifiers = Modifier.None;
		public float X = 0, Y = 0, Z = 0;
		public bool Pressed { get { return (X >= 1) | (Y >= 1) | (Z >= 1); } }

		public static InputData Empty (ComponentBase owner) {
			return new InputData ( owner ) {
				Cmnd = Command.None,
				Key = KeyCode.None,
				DeviceID = 0, Modifiers = 0,
				X = 0, Y = 0, Z = 0
			};
		}
		public InputData ( ComponentBase owner ) : base ( owner ) { }

		public override DataHolderBase Clone () => new InputData (Owner) { Cmnd = Cmnd, Key = Key, DeviceID = DeviceID, Modifiers = Modifiers, X = X, Y = Y, Z = Z };
		public override bool Equals ( object obj ) {
			if ( obj == null ) return false;
			if ( obj.GetType () != GetType () ) return false;
			var item = (InputData)obj;
			bool ret = (Cmnd.Equals ( item.Cmnd )) &
				(Key.Equals ( item.Key )) &
				(DeviceID.Equals ( item.DeviceID )) &
				(Modifiers.Equals ( item.Modifiers )) &
				(X.Equals ( item.X )) &
				(Y.Equals ( item.Y )) &
				(Z.Equals ( item.Z ));
			return ret;
		}
		public override int GetHashCode () => (Cmnd, Key, DeviceID, Modifiers, X, Y, Z).GetHashCode ();
		public override string ToString () => $"({Cmnd}@{Key}({DeviceID})[{Modifiers}]:{{{X}, {Y}, {Z}}})";
		public override SerializableDataHolderBase Deserialize ( byte[] Data ) {
			return new InputData ( Owner ) {
				Cmnd = (Command)BitConverter.ToUInt32 ( Data, 0 ),
				Key = (KeyCode)BitConverter.ToUInt32 ( Data, 4 ),
				DeviceID = BitConverter.ToInt32 ( Data, 8 ),
				Modifiers = (Modifier)BitConverter.ToUInt32 ( Data, 12 ),
				X = BitConverter.ToSingle ( Data, 16 ),
				Y = BitConverter.ToSingle ( Data, 20 ),
				Z = BitConverter.ToSingle ( Data, 24 )
			};
		}
		public override byte[] Serialize () {
			List<byte> ret = new List<byte> ();
			ret.AddRange ( BitConverter.GetBytes ( (uint)Cmnd ) );
			ret.AddRange ( BitConverter.GetBytes ( (uint)Key ) );
			ret.AddRange ( BitConverter.GetBytes ( DeviceID ) );
			ret.AddRange ( BitConverter.GetBytes ( (uint)Modifiers ) );
			ret.AddRange ( BitConverter.GetBytes ( X ) );
			ret.AddRange ( BitConverter.GetBytes ( Y ) );
			ret.AddRange ( BitConverter.GetBytes ( Z ) );
			return ret.ToArray ();
		}
	}
}