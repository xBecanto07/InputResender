using Components.Interfaces;
using Components.Library;
using DataHolder = Components.Interfaces.HInputEventDataHolder;
using ModE = Components.Interfaces.InputData.Modifier;

namespace Components.Implementations {
	public class VTapperInput : DInputProcessor {
		enum StateType { Normal, Shift, Switch }
		readonly (KeyCode key, ModE mod)[][] mapping = new (KeyCode, ModE)[5][] {
			new (KeyCode, ModE)[32] {
				(KeyCode.None, ModE.None), (KeyCode.O, ModE.None), (KeyCode.I, ModE.None), (KeyCode.F, ModE.None),
				(KeyCode.E, ModE.None), (KeyCode.Space, ModE.None), (KeyCode.S, ModE.None), (KeyCode.NoName, ModE.None),
				(KeyCode.A, ModE.None), (KeyCode.B, ModE.None), (KeyCode.R, ModE.None), (KeyCode.Y, ModE.None),
				(KeyCode.N, ModE.None), (KeyCode.Down, ModE.None), (KeyCode.K, ModE.None), (KeyCode.L, ModE.None),
				(KeyCode.T, ModE.None), (KeyCode.M, ModE.None), (KeyCode.P, ModE.None), (KeyCode.V, ModE.None),
				(KeyCode.U, ModE.None), (KeyCode.Q, ModE.None), (KeyCode.H, ModE.None), (KeyCode.G, ModE.None),
				(KeyCode.D, ModE.None), (KeyCode.X, ModE.None), (KeyCode.W, ModE.None), (KeyCode.Z, ModE.None),
				(KeyCode.ShiftKey, ModE.None), (KeyCode.LineFeed, ModE.None), (KeyCode.J, ModE.None), (KeyCode.C, ModE.None)
			},
			new (KeyCode, ModE)[32] {
				(KeyCode.None, ModE.None), (KeyCode.LineFeed, ModE.None), (KeyCode.Tab, ModE.None), (KeyCode.OemCloseBrackets, ModE.None),
				(KeyCode.Space, ModE.None), (KeyCode.None, ModE.None), (KeyCode.OemOpenBrackets, ModE.None), (KeyCode.NoName, ModE.None),
				(KeyCode.Back, ModE.None), (KeyCode.OemSemicolon, ModE.Shift), (KeyCode.D0, ModE.Shift), (KeyCode.OemBackslash, ModE.None),
				(KeyCode.D9, ModE.Shift), (KeyCode.None, ModE.None), (KeyCode.OemMinus, ModE.None), (KeyCode.OemMinus, ModE.Shift),
				(KeyCode.Oemcomma, ModE.None), (KeyCode.OemPeriod, ModE.None), (KeyCode.OemCloseBrackets, ModE.Shift), (KeyCode.OemQuotes, ModE.Shift),
				(KeyCode.OemOpenBrackets, ModE.Shift), (KeyCode.OemQuestion, ModE.Shift), (KeyCode.OemQuotes, ModE.None), (KeyCode.Oemtilde, ModE.Shift),
				(KeyCode.PrintScreen, ModE.None), (KeyCode.OemPeriod, ModE.Shift), (KeyCode.VolumeMute, ModE.None), (KeyCode.Oemcomma, ModE.Shift),
				(KeyCode.ShiftKey, ModE.None), (KeyCode.None, ModE.None), (KeyCode.OemMinus, ModE.Shift), (KeyCode.OemQuestion, ModE.None)
			},
			new (KeyCode, ModE)[32] {
				(KeyCode.None, ModE.None), (KeyCode.None, ModE.None), (KeyCode.Sleep, ModE.None), (KeyCode.End, ModE.None),
				(KeyCode.Help, ModE.None), (KeyCode.None, ModE.None), (KeyCode.Home, ModE.None), (KeyCode.None, ModE.None),
				(KeyCode.Back, ModE.Ctrl), (KeyCode.A, ModE.Ctrl), (KeyCode.PageDown, ModE.None), (KeyCode.Right, ModE.None),
				(KeyCode.PageUp, ModE.None), (KeyCode.None, ModE.None), (KeyCode.Down, ModE.None), (KeyCode.Help, ModE.None),
				(KeyCode.Delete, ModE.None), (KeyCode.V, ModE.Ctrl), (KeyCode.C, ModE.Ctrl), (KeyCode.LWin, ModE.None),
				(KeyCode.X, ModE.Ctrl), (KeyCode.None, ModE.None), (KeyCode.None, ModE.None), (KeyCode.Escape, ModE.None),
				(KeyCode.Apps, ModE.None), (KeyCode.Left, ModE.None), (KeyCode.None, ModE.None), (KeyCode.Y, ModE.Ctrl),
				(KeyCode.None, ModE.None), (KeyCode.None, ModE.None), (KeyCode.Z, ModE.Ctrl), (KeyCode.Up, ModE.None)
			},
			new (KeyCode, ModE)[32] {
				(KeyCode.None, ModE.None), (KeyCode.O, ModE.Shift), (KeyCode.I, ModE.Shift), (KeyCode.F, ModE.Shift),
				(KeyCode.E, ModE.Shift), (KeyCode.Space, ModE.None), (KeyCode.S, ModE.Shift), (KeyCode.None, ModE.None),
				(KeyCode.A, ModE.Shift), (KeyCode.B, ModE.Shift), (KeyCode.R, ModE.Shift), (KeyCode.Y, ModE.Shift),
				(KeyCode.N, ModE.Shift), (KeyCode.Down, ModE.None), (KeyCode.K, ModE.Shift), (KeyCode.L, ModE.Shift),
				(KeyCode.T, ModE.Shift), (KeyCode.M, ModE.Shift), (KeyCode.P, ModE.Shift), (KeyCode.V, ModE.Shift),
				(KeyCode.U, ModE.Shift), (KeyCode.Q, ModE.Shift), (KeyCode.H, ModE.Shift), (KeyCode.G, ModE.Shift),
				(KeyCode.D, ModE.Shift), (KeyCode.X, ModE.Shift), (KeyCode.W, ModE.Shift), (KeyCode.Z, ModE.Shift),
				(KeyCode.ShiftKey, ModE.None), (KeyCode.LineFeed, ModE.None), (KeyCode.J, ModE.Shift), (KeyCode.C, ModE.Shift)
			},
			new (KeyCode, ModE)[32] {
				(KeyCode.None, ModE.None), (KeyCode.Oemplus, ModE.Shift), (KeyCode.D1, ModE.None), (KeyCode.Down, ModE.None),
				(KeyCode.D2, ModE.None), (KeyCode.Right, ModE.None), (KeyCode.D3, ModE.None), (KeyCode.D5, ModE.Shift),
				(KeyCode.D4, ModE.None), (KeyCode.Left, ModE.None), (KeyCode.D5, ModE.None), (KeyCode.D7, ModE.Shift),
				(KeyCode.D6, ModE.None), (KeyCode.D4, ModE.Shift), (KeyCode.D7, ModE.None), (KeyCode.D8, ModE.Shift),
				(KeyCode.D8, ModE.None), (KeyCode.Up, ModE.None), (KeyCode.D9, ModE.None), (KeyCode.D3, ModE.Shift),
				(KeyCode.D0, ModE.None), (KeyCode.Space, ModE.None), (KeyCode.D2, ModE.Shift), (KeyCode.NoName, ModE.None),
				(KeyCode.D6, ModE.Shift), (KeyCode.OemSemicolon, ModE.Shift), (KeyCode.OemPipe, ModE.Shift), (KeyCode.D5, ModE.Shift),
				(KeyCode.Space, ModE.None), (KeyCode.D8, ModE.Shift), (KeyCode.D4, ModE.Shift), (KeyCode.D1, ModE.Shift)
			}
		};
		readonly KeySetup[] Setup;
		readonly HashSet<KeyCode> TriggerKeys;
		readonly ModE TriggerMod;
		int Presses = 0;
		BitField LastTapCombo = new BitField ();
		StateType State = StateType.Normal;
		InputData LastRet = null;
		DateTime LastPress;
		public int WaitTime = 350;
		public Action<string> Log = null;

		BitField fingers;
		bool WaitForRelease;

		// Should allow only single modifier for all keys. Dsc is also useless. So constructor should rather be (CoreBase, KeyCode[], Modifier = None)
		public VTapperInput ( CoreBase owner, KeySetup[] setup ) : base ( owner ) {
			Setup = setup;
			if ( Setup.Length < 5 ) throw new DataMisalignedException ( $"TapStrap simulator processor needs 5 keys defined!" );
			TriggerMod = setup[0].Modifier;
			TriggerKeys = new HashSet<KeyCode> ( 5 );
			for ( int i = 0; i < 5; i++ ) {
				if ( TriggerKeys.Contains ( setup[i].Key ) ) throw new DataMisalignedException ( $"Key {setup[i].Key} cannot be used to represent two different fingers!" );
				TriggerKeys.Add ( setup[i].Key );
			}
		}

		public override int ComponentVersion => 1;

		public string PrintState () => $"{Convert.ToString ( fingers.Value, 2 )}x{Presses}:{State}:{(WaitForRelease ? "Pressed" : "Released")}";

		public struct KeyState {
			public Dictionary<KeyCode, bool> Keys;
			public ModE ActMods;
			public bool KeyReleased;
		}

		int GetID (KeyCode key) {
			for (int i = 0; i < 5; i++)
				if (key == Setup[i].Key) return i;
			return -1;
		}

		bool ParseInput ( DataHolder[] inputCombo, out BitField OrigVal, out BitField NewVal, out BitField Pressed, out BitField Released ) {
			OrigVal = new BitField ();
			NewVal = new BitField ();
			Pressed = new BitField ();
			Released = new BitField ();
			bool IsAnyReleased = false;
			Owner.LogFcn?.Invoke ( $"SETUP = {Setup[0].Key} {Setup[1].Key} {Setup[2].Key} {Setup[3].Key} {Setup[4].Key} ({TriggerMod})" );

			for ( int i = inputCombo.Length - 1; i >= 0; i-- ) {
				KeyCode key = (KeyCode)inputCombo[i].InputCode;
				int ID = GetID ( key );
				Owner.LogFcn?.Invoke ( $"  @i{i}: [{key}] (#{ID}) = ( {inputCombo[i].ValueX} | Δ{inputCombo[i].DeltaX} )" );
				if ( ID < 0 ) continue;
				OrigVal[ID] = inputCombo[i].ValueX - inputCombo[i].DeltaX >= 1;
				NewVal[ID] = inputCombo[i].ValueX >= 1;

				if ( inputCombo[i].DeltaX < 0 ) { // finger is being released
					Pressed[ID] = OrigVal[ID];
					Released[ID] = NewVal[ID];
					IsAnyReleased = true;
				} else {
					Released[ID] = OrigVal[ID];
					Pressed[ID] = NewVal[ID];
				}
			}
			return IsAnyReleased;
		}

		public override void ProcessInput ( DataHolder[] input ) {
			int Cnt = input == null ? -1 : input.Length;
			if ( Cnt < 1 ) return;
			var mods = ReadModifiers ( input );
			if ( (mods & TriggerMod) < TriggerMod ) return;

			var anyReleased = ParseInput ( input, out var origVal, out var newVal, out var pressed, out var released );

			string LF = Environment.NewLine;
			Owner.LogFcn?.Invoke ( $"Current state: {State}{LF}Presses: {Presses}{LF}Waiting for release: {WaitForRelease}{LF}{(anyReleased ? "Some key is released" : "No key was released")}{LF}Finger state:{LF}\t\tOrigVal : {origVal.ToString ().PadLeft ( 5, '0' )}{LF}\tNewValue: {newVal.ToString ().PadLeft ( 5, '0' )}{LF}\tPressed : {pressed.ToString ().PadLeft ( 5, '0' )}{LF}\tReleased: {released.ToString ().PadLeft ( 5, '0' )}" );

			if ( WaitForRelease ) {
				if ( released.Value == 0 ) WaitForRelease = false;
				Owner.LogFcn?.Invoke ( $"Waiting for release ({(released.Value == 0 ? "Is Released" : "Still waiting")})" );
				return;
			} else if ( anyReleased ) {
				if ( released.Value != 0 ) WaitForRelease = true;
				else Owner.LogFcn?.Invoke ( $"Key pressed AND released, not waiting." );
				DateTime time = DateTime.Now;
				double msDiff = (time - LastPress).TotalMilliseconds;
				if ( State == StateType.Normal && Presses < 2 && msDiff < WaitTime && LastTapCombo == pressed ) {
					Presses++;
					var ret = LastRet.Clone<InputData> ();
					ret.Cmnd = InputData.Command.Cancel;
					Callback ( ret );
					Owner.LogFcn?.Invoke ( $"Pressed {pressed.ToString ().PadLeft ( 5, '0' )}  {Presses}x times in {msDiff}ms after last press" );
				} else {
					LastTapCombo = pressed;
					Presses = 0;
				}
				LastPress = time;

				if (State == StateType.Switch && pressed.Value == 0x1FUL) {
					msDiff = 0;
				}
				LastRet = new InputData ( this );
				int mapID = 0;
				switch ( State ) {
				case StateType.Normal:
					switch ( mapping[0][LastTapCombo].key ) {
					case KeyCode.ShiftKey: State = StateType.Shift; LastTapCombo.Reset (); return;
					case KeyCode.NoName: State = StateType.Switch; LastTapCombo.Reset (); return;
					}
					mapID = Presses;
					break;
				case StateType.Shift:
					switch ( mapping[0][LastTapCombo].key ) {
					case KeyCode.ShiftKey: State = StateType.Normal; LastTapCombo.Reset (); return;
					case KeyCode.NoName: State = StateType.Switch; LastTapCombo.Reset (); return;
					default: State = StateType.Normal ; break;
					}
					mapID = 3;
					break;
				case StateType.Switch:
					switch ( mapping[0][LastTapCombo].key ) {
					case KeyCode.ShiftKey: State = StateType.Switch; LastTapCombo.Reset (); return;
					case KeyCode.NoName: State = StateType.Normal; LastTapCombo.Reset (); return;
					}
					mapID = 4;
					break;
				}
				var key = mapping[mapID][LastTapCombo];
				LastRet.Key = key.key;
				LastRet.Modifiers = key.mod;
				LastRet.X = 1;
				LastRet.Y = LastRet.Z = 0;
				LastRet.Cmnd = InputData.Command.Type;
				LastRet.DeviceID = input[0].HookInfo.DeviceID;
				Callback?.Invoke ( LastRet.Clone<InputData> () );
				if ( key.key == KeyCode.Back & key.mod == ModE.Ctrl ) {
					Callback?.Invoke ( LastRet.Clone<InputData> () );
					Callback?.Invoke ( LastRet.Clone<InputData> () );
					Callback?.Invoke ( LastRet.Clone<InputData> () );
				}
				return;
			} else {
				Owner.LogFcn?.Invoke ( $"No key released, skipping." );
				return;
			}
		}
	}
}