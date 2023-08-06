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
			ret.Modifiers = ReadModifiers ( inputCombination );
			Callback ( ret );
		}
	}
}
