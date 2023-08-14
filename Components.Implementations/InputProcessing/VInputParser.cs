using Components.Interfaces;
using Components.Library;
using DataHolder = Components.Interfaces.HInputEventDataHolder;

namespace Components.Implementations {
	public class VInputParser : DInputParser {
		protected List<DataHolder> eventList;

		public VInputParser ( CoreBase owner ) : base ( owner ) {
			eventList = new List<DataHolder> ();
		}

		public override int ComponentVersion => 1;
		public override int MemoryCount => eventList.Count;

		public override void ClearMemory () => eventList.Clear ();

		public override DataHolder[] ProcessInput ( DataHolder inputData ) {
			if ( inputData == null ) return new DataHolder[0];
			KeyCode keyCode = (KeyCode)inputData.InputCode;
			List<DataHolder> ret = new List<DataHolder> { (DataHolder)inputData.Clone () };
			for (int i = 0; i < eventList.Count; i++) {
				var data = eventList[i].Clone<DataHolder> ();
				data.SetNewValue ( data.ValueX, data.ValueY, data.ValueZ );
				ret.Add ( data );
			}

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

			if (Owner.LogFcn != null) {
				var SB = new System.Text.StringBuilder ();
				SB.Append ( $" -- {nameof ( ProcessInput )}:" );
				foreach ( var data in ret ) SB.Append ( $"  {(data.Pressed >= 1 ? '↓' : '↑')}{(KeyCode)data.InputCode}" );
				Owner.LogFcn ( SB.ToString () );
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

		public override StateInfo Info => new VStateInfo ( this );
		public class VStateInfo : DStateInfo {
			public new VInputParser Owner => (VInputParser)base.Owner;
			public VStateInfo ( VInputParser owner ) : base ( owner ) { }

			protected override string[] GetBufferedEvents () {
				int N = Owner.eventList.Count;
				string[] ret = new string[N];
				for ( int i = 0; i < N; i++ )
					ret[i] = Owner.eventList[i].ToString ();
				return ret;
			}
		}
	}
}