using Components.Interfaces;
using Components.Library;
using DataHolder = Components.Interfaces.HInputEventDataHolder;

namespace Components.Implementations {
	public class VInputProcessor : DInputProcessor {
		public VInputProcessor ( CoreBase owner ) : base ( owner ) {
		}

		public override int ComponentVersion => 1;

		public override void ProcessInput ( DataHolder[] inputCombination ) {
			int Cnt = inputCombination == null ? -1 : inputCombination.Length;
			if ( Cnt < 1 ) return;

			InputData ret = new InputData ( this ) {
				Cmnd = inputCombination[0].Pressed >= 1 ? InputData.Command.KeyPress : InputData.Command.KeyRelease,
				DeviceID = inputCombination[0].HookInfo.DeviceID,
				Key = (KeyCode)(inputCombination[0].InputCode),
				X = inputCombination[0].ValueX,
				Y = inputCombination[0].ValueY,
				Z = inputCombination[0].ValueZ,
			};
			InputData.Modifier mods = InputData.Modifier.None;
			for (int i = 0; i < Cnt; i++ ) {
				if ( inputCombination[i].Pressed < 1 ) continue;
				switch ((KeyCode)inputCombination[i].InputCode ) {
				case KeyCode.ControlKey: mods |= InputData.Modifier.Ctrl; break;
				case KeyCode.ShiftKey: mods |= InputData.Modifier.Shift; break;
				case KeyCode.LControlKey: mods |= InputData.Modifier.Ctrl; break;
				case KeyCode.RControlKey: mods |= InputData.Modifier.Ctrl; break;
				case KeyCode.LShiftKey: mods |= InputData.Modifier.Shift; break;
				case KeyCode.RShiftKey: mods |= InputData.Modifier.Shift; break;
				case KeyCode.Alt: mods |= InputData.Modifier.Alt; break;
				case KeyCode.RMenu: mods |= InputData.Modifier.AltGr; break;
				case KeyCode.LWin: mods |= InputData.Modifier.WinKey; break;
				case KeyCode.RWin: mods |= InputData.Modifier.WinKey; break;
				}
			}
			ret.Modifiers = mods;
			Callback ( ret );
		}
	}
}
