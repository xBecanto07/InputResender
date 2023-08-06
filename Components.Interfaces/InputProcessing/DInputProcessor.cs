using Components.Library;
using static Components.Interfaces.InputData;

namespace Components.Interfaces {
	public abstract class DInputProcessor : ComponentBase<CoreBase> {
		public DInputProcessor ( CoreBase owner ) : base ( owner ) {
			CustomModifiers = new Dictionary<KeyCode, Modifier> ();
		}

		public struct KeyCombo {
			public KeyCode Key;
			public Modifier Modifier;
			public KeyCombo ( KeyCode key, Modifier mod = Modifier.None ) { Key = key; Modifier = mod; }
		}
		public struct KeySetup {
			public string Description;
			public KeyCombo keyCombo;
			public KeyCode Key { get => keyCombo.Key; set => keyCombo.Key = value; }
			public Modifier Modifier { get => keyCombo.Modifier; set => keyCombo.Modifier = value; }
			public KeySetup ( string dsc, KeyCode key, Modifier mod = Modifier.None ) { Description = dsc; Key = key; Modifier = mod; }
			public override string ToString () => $"({Description}:'{Key}'+{Modifier})";
		}

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(ProcessInput), typeof(void)),
				(nameof(SetCustomModifier), typeof(void)),
				(nameof(SequentialProcess), typeof(InputData[])),
				(nameof(ReadModifiers), typeof(Modifier)),
			};

		public abstract void ProcessInput ( HInputEventDataHolder[] inputCombination );
		public Action<InputData> Callback;

		private Dictionary<KeyCode, Modifier> CustomModifiers;
		public void SetCustomModifier ( KeyCode key, Modifier customMod ) {
			if ( CustomModifiers.ContainsKey ( key ) ) {
				if ( customMod == Modifier.None ) CustomModifiers.Remove ( key );
				else CustomModifiers[key] = customMod;
			} else if ( customMod != Modifier.None ) CustomModifiers.Add ( key, customMod );
		}
		/// <summary>Can simplify calling of ProcessInput. When any time-dependent results are generated, those can only be captured by original callback (if was assigned beforehand).</summary>
		public InputData[] SequentialProcess ( HInputEventDataHolder[] inputCombination ) {
			List<InputData> ret = new List<InputData> ();
			Action<InputData> origCallback = Callback;
			Callback = ret.Add;
			ProcessInput ( inputCombination );
			Callback = origCallback;
			return ret.ToArray ();
		}

		public Modifier ReadModifiers ( HInputEventDataHolder[] inputCombination) {
			int Cnt = inputCombination == null ? -1 : inputCombination.Length;
			if ( Cnt < 1 ) return Modifier.None;

			Modifier mods = Modifier.None;
			for ( int i = Cnt - 1; i >= 0; i-- ) {
				Modifier nMod;
				KeyCode key = (KeyCode)inputCombination[i].InputCode;
				switch ( key ) {
				case KeyCode.ControlKey: nMod = Modifier.Ctrl; break;
				case KeyCode.ShiftKey: nMod = Modifier.Shift; break;
				case KeyCode.LControlKey: nMod = Modifier.Ctrl; break;
				case KeyCode.RControlKey: nMod = Modifier.Ctrl; break;
				case KeyCode.LShiftKey: nMod = Modifier.Shift; break;
				case KeyCode.RShiftKey: nMod = Modifier.Shift; break;
				case KeyCode.Alt: nMod = Modifier.Alt; break;
				case KeyCode.RMenu: nMod = Modifier.AltGr; break;
				case KeyCode.LWin: nMod = Modifier.WinKey; break;
				case KeyCode.RWin: nMod = Modifier.WinKey; break;
				default:
					if (CustomModifiers.TryGetValue ( key, out Modifier mod ) ) { nMod = mod; break; }
					nMod = Modifier.None;
					break;
				}
				if ( inputCombination[i].Pressed >= 1 ) mods |= nMod;
				else mods &= ~nMod;
			}
			return mods;
		}
	}

	public class MInputProcessor : DInputProcessor {
		public MInputProcessor ( CoreBase owner ) : base ( owner ) { }

		public override int ComponentVersion => 1;

		public override void ProcessInput ( HInputEventDataHolder[] inputCombination ) {
			if ( inputCombination == null || inputCombination.Length == 0 ) return;
			var firstEvent = inputCombination[0];
			Callback?.Invoke ( new InputData ( this ) {
				Cmnd = firstEvent.Pressed >= 1 ? Command.KeyPress : Command.KeyRelease,
				X = firstEvent.Pressed,
				Key = (KeyCode)firstEvent.InputCode,
				DeviceID = firstEvent.HookInfo.DeviceID
			} );
		}
	}

	public class InputData : SerializableDataHolderBase {
		public enum Command { None, KeyPress, KeyRelease, MouseMove, Cancel, Type }
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
		public InputData ( ComponentBase owner, KeyCode key, bool Pressed, Modifier mod = Modifier.None ) : base ( owner ) {
			Cmnd = Pressed ? Command.KeyPress : Command.KeyRelease;
			Key = key;
			Modifiers = mod;
			X = Pressed ? 1 : 0;
			Y = Z = 0;
		}

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