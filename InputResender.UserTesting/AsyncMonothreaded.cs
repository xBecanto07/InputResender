using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InputResender.UserTesting {
	public class AsyncMonothreaded {
		public delegate IEnumerable<Action> MainWorkerDelegate ();

		readonly IEnumerator<Action> CurrentAct;
		readonly Action TaskCallback, Cleanup;
		Thread ActThread;
		bool InProcess = false;
		int TaskID = 0;

		public AsyncMonothreaded ( MainWorkerDelegate mainAct, Action taskFinishCallback, Action finishCleanup ) {
			ActThread = null;
			CurrentAct = mainAct ().GetEnumerator ();
			TaskCallback = taskFinishCallback;
			Cleanup = finishCleanup;
		}

		public void Continue () {
			if ( InProcess ) return;
			if (ActThread != null) {
				if ( ActThread.ThreadState != ThreadState.Stopped ) return;
				//ActThread.Dispose ();
				ActThread = null;
			}

			InProcess = true;
			Thread.MemoryBarrier ();
			bool ended = !CurrentAct.MoveNext ();
			InProcess = false;
			Thread.MemoryBarrier ();
			if ( ended ) {
				Cleanup?.Invoke ();
				return;
			}
			var act = CurrentAct.Current;
			if (act != null) {
				ActThread = new Thread ( () => {
					act ();
					TaskCallback ();
				} );
				ActThread.Name = TaskID.ToString ();
				TaskID++;
				ActThread.Start ();
			}
		}
	}
}