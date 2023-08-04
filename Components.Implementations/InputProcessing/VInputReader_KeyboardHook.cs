using Components.Interfaces;
using Components.Library;
using System.Threading;

namespace Components.Implementations {
	public class VInputReader_KeyboardHook : DInputReader {
		private Task inputHandler;
		readonly AutoResetEvent waiter;
		bool Continue = true;
		Dictionary<DictionaryKey, HLHookInfo> HookSet;

		public VInputReader_KeyboardHook ( CoreBase owner ) : base ( owner ) {
			HookSet = new Dictionary<DictionaryKey, HLHookInfo> ();
			waiter = new AutoResetEvent ( false );
		}

		public HLHookInfo GetHook (DictionaryKey key) => HookSet[key];

		public struct HLHookInfo {
			public Hook hook;
			public Func<DictionaryKey, HInputEventDataHolder, bool> MainCallback;
			public Action<DictionaryKey, HInputEventDataHolder> DelayedCB;
			public Queue<(DictionaryKey, HInputEventDataHolder)> MessageQueue;

			public HLHookInfo ( Hook nHook, Func<DictionaryKey, HInputEventDataHolder, bool> mainCallback, Action<DictionaryKey, HInputEventDataHolder> delayedCB ) {
				hook = nHook;
				MainCallback = mainCallback;
				DelayedCB = delayedCB;
				MessageQueue = new Queue<(DictionaryKey, HInputEventDataHolder)> ();
			}
		}

		public override int ComponentVersion => 1;
		protected DLowLevelInput LowLevelComponent { get { return Owner.Fetch<DLowLevelInput> (); } }

		public override ICollection<DictionaryKey> SetupHook ( HHookInfo hookInfo, Func<DictionaryKey, HInputEventDataHolder, bool> mainCB, Action<DictionaryKey, HInputEventDataHolder> delayedCB = null ) {
			var ret = new HashSet<DictionaryKey> ();
			var hooks = LowLevelComponent.SetHookEx ( hookInfo, LocalCallback );

			foreach ( var hook in hooks ) {
				HookSet.Add ( hook.Key, new ( hook, mainCB, delayedCB ) );
				ret.Add ( hook.Key );

				waiter.Reset ();
				Continue = true;
				inputHandler ??= Task.Run ( CallbackParaTask );
			}
			return ret;
		}
		public override int ReleaseHook ( HHookInfo hookInfo ) {
			int released = 0;
			foreach ( var hookID in hookInfo.HookIDs ) {
				if ( !HookSet.TryGetValue ( hookID, out var hookRef ) ) throw new KeyNotFoundException ( $"Couldn't find a hook ID for Hook Definition: {hookInfo}" );
				if ( HookSet.Remove ( hookID ) )
					released += LowLevelComponent.UnhookHookEx ( hookRef.hook ) ? 1 : 0;
			}
			if ( inputHandler != null && HookSet.Count == 0 ) {
				Continue = false;
				waiter.Set ();
				inputHandler.Wait ();
				inputHandler.Dispose ();
				inputHandler = null;
			}
			return released;
		}

		public override uint SimulateInput ( HInputEventDataHolder input, bool allowRecapture ) {
			var LLData = LowLevelComponent.GetLowLevelData ( input );
			return LowLevelComponent.SimulateInput ( 1, new HInputData[1] { LLData }, LLData.SizeOf );
		}

		private bool LocalCallback ( DictionaryKey hookKey, HInputData inputData ) {
			var inputEventDataHolder = LowLevelComponent.GetHighLevelData ( hookKey, this, inputData );
			bool willResend = true;
			if ( HookSet.TryGetValue ( hookKey, out var hookRef ) ) {
				if ( hookRef.MainCallback != null ) willResend = hookRef.MainCallback ( hookKey, inputEventDataHolder );
				hookRef.MessageQueue.Enqueue ( (hookKey, inputEventDataHolder) );
				waiter.Set ();
			}
			return willResend;
		}
		private void CallbackParaTask () {
			while ( waiter == null ) Thread.Sleep ( 1 );
			while ( true ) {
				waiter.WaitOne ();
				if ( !Continue ) break;
				foreach ( var hook in HookSet.Values ) {
					if ( hook.MessageQueue.Count < 1 ) continue;
					var msg = hook.MessageQueue.Dequeue ();
					if ( hook.DelayedCB != null ) hook.DelayedCB ( msg.Item1, msg.Item2 );
				}
			}
		}
	}
}