using Components.Library;
using System;
using System.Collections.Generic;
using static Components.Interfaces.InputData;

namespace Components.Interfaces {
	public abstract class DCommandWorker : ComponentBase<CoreBase> {
		public DCommandWorker ( CoreBase newOwner ) : base ( newOwner ) {
		}

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(RegisterCallback), typeof(void)),
				(nameof(UnregisterCallback), typeof(void)),
				(nameof(Push), typeof(void)),
			};

		public abstract void RegisterCallback ( Action<InputData> callback );
		public abstract void UnregisterCallback ( Action<InputData> callback );
		public abstract void Push ( InputData data );
	}

	public class VCommandWorker : DCommandWorker {
		List<Action<InputData>> Callbacks;
		Queue<InputData> Inputs;
		bool IsProcessing = false;

		public VCommandWorker ( CoreBase newOwner ) : base ( newOwner ) {
			Callbacks = new List<Action<InputData>> ();
			Inputs = new Queue<InputData> ();
		}

		public override int ComponentVersion => 1;

		public override void Push ( InputData data ) {
			lock ( Inputs ) {
				Inputs.Enqueue ( data );
				if ( IsProcessing ) return;
			}
			while ( true ) {
				data = null;
				lock ( Inputs ) {
					if ( Inputs.Count == 0 ) return;
					data = Inputs.Dequeue ();
					IsProcessing = true;
				}
				if ( data != null ) {
					foreach ( var act in Callbacks ) act ( data );
				}
				lock ( Inputs ) {
					IsProcessing = false;
					if ( Inputs.Count == 0 ) return;
				}
			}
		}
		public override void RegisterCallback ( Action<InputData> callback ) {
			if ( !Callbacks.Contains ( callback ) ) Callbacks.Add ( callback );
		}
		public override void UnregisterCallback ( Action<InputData> callback ) => Callbacks.Remove ( callback );
	}
}