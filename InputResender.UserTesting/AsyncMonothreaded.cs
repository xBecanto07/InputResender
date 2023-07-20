using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InputResender.UserTesting {
	public class AsyncMonothreaded {
		public delegate IEnumerable<Action> MainWorkerDelegate ();

		readonly IEnumerator<Action> CurrentAct;
		readonly Action TaskCallback;
		Task ActTask;

		public AsyncMonothreaded ( MainWorkerDelegate mainAct, Action taskFinishCallback ) {
			ActTask = null;
			CurrentAct = mainAct ().GetEnumerator ();
			TaskCallback = taskFinishCallback;
			Continue ();
		}

		public void Continue () {
			if (ActTask != null) {
				if ( !ActTask.IsCompleted ) return;
				ActTask.Dispose ();
				ActTask = null;
			}

			if ( !CurrentAct.MoveNext () ) return;
			var act = CurrentAct.Current;
			if (act != null) {
				ActTask = Task.Run ( () => {
					act ();
					TaskCallback ();
				} );
			}
		}
	}
}