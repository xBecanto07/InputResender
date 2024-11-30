using Components.Interfaces;
using Components.Library;
using System.Threading;

namespace Components.Implementations; 
public class VInputReader_KeyboardHook : DInputReader {
	private readonly DelayedRunner delayedRunner;
	Dictionary<DictionaryKey, (DLowLevelInput, HLHookInfo)> HookSet;

	public VInputReader_KeyboardHook ( CoreBase owner ) : base ( owner ) {
		HookSet = new ();
		delayedRunner = new ( HookSet, Owner );
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

	public override IDictionary<VKChange, DictionaryKey> SetupHook ( HHookInfo hookInfo, Func<DictionaryKey, HInputEventDataHolder, bool> mainCB, Action<DictionaryKey, HInputEventDataHolder> delayedCB = null ) {
		var hooks = LowLevelComponent.SetHookEx ( hookInfo, LocalCallback );
		var ret = new Dictionary<VKChange, DictionaryKey> ();

		if (hooks == null) {
			Owner.LogFcn?.Invoke ($"Failed to set hook for {hookInfo}");
			return ret;
		}

		foreach ( var hook in hooks ) {
			ret.Add ( hook.Key, hook.Value.Key );
			HookSet.TryAdd ( hook.Value.Key, (LowLevelComponent, new ( hook.Value, mainCB, delayedCB ) ) );
		}
		if ( hooks.Any () ) delayedRunner.Start ();
		return ret;
	}
	public override int ReleaseHook ( HHookInfo hookInfo ) {
		int released = 0;
		foreach ( var hookID in hookInfo.HookIDs ) {
			if ( !HookSet.TryGetValue ( hookID, out var hookRef ) ) {
				Owner.LogFcn?.Invoke ( $"Couldn't find a hook ID for Hook Definition: {hookInfo}" );
				continue;
			}
			if ( HookSet.Remove ( hookID ) )
				released += LowLevelComponent.UnhookHookEx ( hookRef.Item2.hook ) ? 1 : 0;
		}
		if ( HookSet.Count == 0 ) delayedRunner.Stop ();
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
			lock (delayedRunner) {
				if ( hookRef.Item2.MainCallback != null ) willResend = hookRef.Item2.MainCallback ( hookKey, inputEventDataHolder );
				Owner.PushDelayedMsg ( $"Received input: {inputEventDataHolder} from {hookKey} with result: {willResend}, taskStatus:{delayedRunner?.TaskState}" );
				hookRef.Item2.MessageQueue.Enqueue ( (hookKey, inputEventDataHolder) );
			}
			delayedRunner.Notify ();
		} else Owner.PushDelayedMsg ( $"No hook found for {hookKey}" );
		return willResend;
	}

	private class DelayedRunner {
		public string TaskState { get; private set; } = "Not started";
		AutoResetEvent waiter, confirmWaiter;
		bool Continue = true;
		readonly IReadOnlyDictionary<DictionaryKey, (DLowLevelInput, HLHookInfo)> HookSet;
		readonly HashSet<(DictionaryKey hookID, HInputEventDataHolder eventData, DLowLevelInput llInput, HLHookInfo hookInfo)> FoundEvents;
		Task runTask;
		public event Action<string> TaskStateChanged;
		private object hookSetLock;
		private readonly CoreBase Owner;

		public DelayedRunner (Dictionary<DictionaryKey, (DLowLevelInput, HLHookInfo)> hookSet, CoreBase owner ) {
			Owner = owner;
			hookSetLock = hookSet;
			HookSet = new System.Collections.ObjectModel.ReadOnlyDictionary<DictionaryKey, (DLowLevelInput, HLHookInfo)> ( hookSet );
			FoundEvents = new ();
		}

		public void Start () {
			lock (this) {
				if (waiter == null) {
					waiter = new ( false );
					confirmWaiter = new ( false );
				} else {
					waiter.Reset ();
					confirmWaiter.Reset ();
				}
				Continue = true;
				runTask ??= Task.Run ( CallbackParaTask );
			}
			confirmWaiter.WaitOne ();
		}

		public void Notify () {
			waiter.Set ();
		}

		public void Stop () {
			lock (this) {
				if ( runTask == null ) return;
				if ( Continue ) {
					Continue = false;
					Interlocked.MemoryBarrierProcessWide ();
					waiter.Set ();
					confirmWaiter.WaitOne ();
				} else if ( runTask != null ) {
					// 'Should' be already closing
					if ( !runTask.Wait ( 500 ) ) throw new System.Threading.SynchronizationLockException ( "Possible deadlock" );
				} else return; // Already closed
				waiter.Dispose ();
				waiter = null;
				confirmWaiter.Dispose ();
				confirmWaiter = null;
				runTask.Dispose ();
				runTask = null;
			}
		}

		private void SetTaskState ( string newState ) {
			TaskState = newState;
			TaskStateChanged?.Invoke ( newState );
		}

		private void CallbackParaTask () {
			SetTaskState ( "Waiting for initialization");
			while ( waiter == null || confirmWaiter == null ) Thread.Sleep ( 1 );
			confirmWaiter.Set ();
			while ( true ) {
				SetTaskState ( "Waiting for signal");
				waiter.WaitOne ( 20 );
				if ( !Continue ) break;

				lock (this) {
					SetTaskState ( "Pushing data" );
					foreach ( var (key, hook) in HookSet ) {
						while ( hook.Item2.MessageQueue.TryDequeue ( out var val ) )
							FoundEvents.Add ( (val.Item1, val.Item2, hook.Item1, hook.Item2) );
					}
					SetTaskState ( "Stopped pushing data" );
				}
				if ( !FoundEvents.Any () ) continue;
				Owner.PushDelayedMsg ( $"Found {FoundEvents.Count} events" );

				SetTaskState ( "Processing events" );
				foreach (var e in FoundEvents) {
					Owner.PushDelayedMsg ( $"Processing event: {e.eventData}" );
					try {
						e.hookInfo.DelayedCB?.Invoke ( e.hookID, e.eventData );
					} catch (Exception ex) {
						Owner.PushDelayedError ( "Error during delayed processing of input event!", ex );
					}
				}
				SetTaskState ( "Finished processing events" );
				FoundEvents.Clear ();
			}
			SetTaskState ( "Stopped");
			confirmWaiter.Set ();
		}
	}

	public override StateInfo Info => new VStateInfo ( this );
	public class VStateInfo : DStateInfo {
		public new VInputReader_KeyboardHook Owner => (VInputReader_KeyboardHook)base.Owner;
		public VStateInfo (VInputReader_KeyboardHook owner) : base (owner) {
			TaskState = owner.delayedRunner.TaskState;
		}

		public readonly string TaskState;
		protected override string[] GetHookList () {
			string[] ret = new string[Owner.HookSet.Count];
			int ID = 0;
			foreach ( var hook in Owner.HookSet ) {
				var HLInfo = hook.Value.Item2;
				ret[ID++] = $"{hook.Key} => {HLInfo.hook}>>({HLInfo.MainCallback.Method.DeclaringType?.Name}.{HLInfo.MainCallback.Method.Name}|{HLInfo.DelayedCB.Method.DeclaringType?.Name}.{HLInfo.DelayedCB.Method.Name})[{HLInfo.MessageQueue.Count}]";
			}
			return ret;
		}
		public override string AllInfo () => $"{base.AllInfo ()}{BR}Task state: {TaskState}";
	}
}