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
			ret.AddRange ( eventList );

			if ( inputData.Pressed < 1 ) {
				// If key is released
				if (keyCode.IsModifier()) {
					RemoveEvents ( keyCode );
				} else {
					RemoveEvents ( keyCode );
				}
			} else {
				// If key is pressed
				if ( keyCode.IsModifier () ) {
					eventList.Add ( inputData );
				} else {
					eventList.Insert ( 0, inputData );
				}
			}
			return ret.ToArray ();
		}

		private void RemoveEvents (KeyCode keyCode) {
			for ( int i = 0; i < eventList.Count; i++ ) {
				if ( eventList[i].InputCode != (int)keyCode ) continue;
				eventList.RemoveAt ( i );
				i--;
			}
		}
	}
}