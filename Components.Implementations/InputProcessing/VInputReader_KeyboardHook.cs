using Components.Interfaces;
using Components.Library;
using System.Threading;

namespace Components.Implementations {
	public class VInputReader_KeyboardHook : DInputReader {
		private Task inputHandler;
		private string TaskState = "Not started";
		readonly AutoResetEvent waiter;
		bool Continue = true;
		// LowLevelInput should be stored here. Components aren't supposed to be swaped at runtime nor should they be know about each other but 1) they can change and 2) hooks are bound to specific DLowLevelInput variant. As long as this relation is not broken up, creater of hook must be stored.
		Dictionary<DictionaryKey, (DLowLevelInput, HLHookInfo)> HookSet;

		public VInputReader_KeyboardHook ( CoreBase owner ) : base ( owner ) {
			HookSet = new ();
			waiter = new ( false );
		}

		public HLHookInfo GetHook ( DictionaryKey key ) => HookSet[key].Item2;

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
		protected DLowLevelInput LowLevelComponent { get => Owner.Fetch<DLowLevelInput> (); }

		public override ICollection<DictionaryKey> SetupHook ( HHookInfo hookInfo, Func<DictionaryKey, HInputEventDataHolder, bool> mainCB, Action<DictionaryKey, HInputEventDataHolder> delayedCB = null ) {
			var ret = new HashSet<DictionaryKey> ();
			var hooks = LowLevelComponent.SetHookEx ( hookInfo, LocalCallback );

			if (hooks == null) {
				Owner.LogFcn?.Invoke ($"Failed to set hook for {hookInfo}");
				return ret;
			}

			foreach ( var hook in hooks ) {
				HookSet.Add ( hook.Key, (LowLevelComponent, new ( hook, mainCB, delayedCB ) ) );
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
					released += LowLevelComponent.UnhookHookEx ( hookRef.Item2.hook ) ? 1 : 0;
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

		public override string PrintHookInfo ( DictionaryKey key ) {
			if ( !HookSet.TryGetValue ( key, out var hookRef ) ) return null;
			return hookRef.Item1.PrintHookInfo ( key );
		}

		public override uint SimulateInput ( HInputEventDataHolder input, bool allowRecapture ) {
			var LLData = LowLevelComponent.GetLowLevelData ( input );
			return LowLevelComponent.SimulateInput ( 1, new HInputData[1] { LLData }, LLData.SizeOf );
		}

		private bool LocalCallback ( DictionaryKey hookKey, HInputData inputData ) {
			var inputEventDataHolder = LowLevelComponent.GetHighLevelData ( hookKey, this, inputData );
			bool willResend = true;
			if ( HookSet.TryGetValue ( hookKey, out var hookRef ) ) {
				if ( hookRef.Item2.MainCallback != null ) willResend = hookRef.Item2.MainCallback ( hookKey, inputEventDataHolder );
				hookRef.Item2.MessageQueue.Enqueue ( (hookKey, inputEventDataHolder) );
				waiter.Set ();
			}
			return willResend;
		}
		private void CallbackParaTask () {
			TaskState = "Waiting for initialization";
			while ( waiter == null ) Thread.Sleep ( 1 );
			while ( true ) {
				TaskState = "Waiting for signal";
				waiter.WaitOne ();
				if ( !Continue ) break;
				foreach ( var hook in HookSet.Values ) {
					if ( hook.Item2.MessageQueue.Count < 1 ) continue;
					TaskState = "Pushing data";
					var msg = hook.Item2.MessageQueue.Dequeue ();
					if ( hook.Item2.DelayedCB != null ) hook.Item2.DelayedCB ( msg.Item1, msg.Item2 );
				}
			}
			TaskState = "Stopped";
		}

		public override StateInfo Info => new VStateInfo ( this );
		public class VStateInfo : DStateInfo {
			public new VInputReader_KeyboardHook Owner => (VInputReader_KeyboardHook)base.Owner;
			public VStateInfo (VInputReader_KeyboardHook owner) : base (owner) {
				TaskState = owner.TaskState;
			}

			public readonly string TaskState;
			protected override string[] GetHookList () {
				string[] ret = new string[Owner.HookSet.Count];
				int ID = 0;
				foreach ( var hook in Owner.HookSet )
					ret[ID++] = $"{hook.Key} => {hook.Value.Item2.hook}>>({hook.Value.Item2.MainCallback.Method.AsString ()}|{hook.Value.Item2.DelayedCB.Method.AsString ()})[{hook.Value.Item2.MessageQueue.Count}]";
				return ret;
			}
			public override string AllInfo () => $"{base.AllInfo ()}{BR}Task state: {TaskState}";
		}
	}
}