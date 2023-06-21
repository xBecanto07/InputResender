using Components.Interfaces;
using Components.Library;
using DataHolder = Components.Interfaces.HInputEventDataHolder;

namespace Components.Implementations {
	public class InputParser : DInputParser {
		protected List<DataHolder> eventList;

		public InputParser ( CoreBase owner ) : base ( owner ) {
			eventList = new List<DataHolder> ();
		}

		public override int ComponentVersion => 1;
		public override int MemoryCount => eventList.Count;

		public override void ClearMemory () => eventList.Clear ();

		public override DataHolder[] ProcessInput ( DataHolder inputData ) {
			if ( inputData == null ) return new DataHolder[0];
			KeyCode keyCode = (KeyCode)inputData.InputCode;
			List<DataHolder> ret = new List<DataHolder> { inputData };
			if ( inputData.Pressed < 1 ) {
				// If key is released
				if (keyCode.IsModifier()) {
					for (int i = 0; i < eventList.Count; i++ ) {
						if ( eventList[i].InputCode != (int)keyCode ) continue;
						eventList.RemoveAt ( i );
						i--;
					}
				} else {
					ret.AddRange ( eventList );
					eventList.Clear ();
				}
			} else {
				ret.AddRange ( eventList );
				// If key is pressed
				if ( keyCode.IsModifier () ) {
					eventList.Add ( inputData );
				} else {
					eventList.Insert ( 0, inputData );
				}
			}
			return ret.ToArray ();
		}
	}
}