using Components.Interfaces;
using Components.Library;

using CallbackHolder = Components.Interfaces.HCallbackHolder<Components.Interfaces.DHookManager.HookCallback>;
using DeviceID = System.Int32;

namespace Components.Implementations;
public class VHookManager : DHookManager {
	readonly Dictionary<DeviceID, HookGroup> ActiveHooks = new ();
	readonly Dictionary<(DeviceID, CBType), HashSet<CallbackHolder>> Callbacks = new ();
	readonly Dictionary<DictionaryKey, HHookInfo> OwnedHooks = new ();

	public VHookManager ( CoreBase owner ) : base ( owner ) {
	}

	public override CallbackHolder AddCallback ( CBType cbType, DeviceID device = -1 ) {
		var key = (device, cbType);
		var ret = new CallbackHolder ( this, ( holder ) => Callbacks[key].Remove ( holder ) ); // holder should be same as ret
		if ( !Callbacks.ContainsKey ( key ) ) Callbacks.Add ( key, new HashSet<CallbackHolder> () );
		Callbacks[key].Add ( ret );
		return ret;
	}

	/// <summary>Return value can be ignored. It is returned only to let caller know if hook was successfully added and to provide info if caller needs that. Otherwise, all important data are stored inside of this component</summary>
	public override IReadOnlyCollection<DictionaryKey> AddHook ( DeviceID device, params VKChange[] vKChanges ) {
		HashSet<DictionaryKey> ret = new ();
		List<VKChange> WinLLHook = new (); // keyboard and mouse can be combined here
		foreach ( var vKChange in vKChanges ) {
			switch ( vKChange ) {
			case VKChange.KeyDown: WinLLHook.Add ( vKChange ); break;
			case VKChange.KeyUp: WinLLHook.Add ( vKChange ); break;
			case VKChange.MouseMove: WinLLHook.Add ( vKChange ); break;
			default:
				throw new ArgumentOutOfRangeException ( nameof ( vKChanges ), vKChange, null );
			}
		}

		if ( !WinLLHook.Any () ) return ret;

		var inputReader = Owner.Fetch<VInputReader_KeyboardHook> ();
		if ( inputReader == null ) throw new InvalidOperationException ( "VInputReader_KeyboardHook not found!" );
		if ( !ActiveHooks.ContainsKey ( device ) ) ActiveHooks.Add ( device, new HookGroup () );

		var first = WinLLHook[0];
		WinLLHook.RemoveAt ( 0 );
		HHookInfo keyHookInfo = new ( this, device, first, WinLLHook.ToArray () );
		var setup = inputReader.SetupHook ( keyHookInfo, FastCB, DelayedCB );
		foreach ( var newHook in ActiveHooks[device].Add ( setup ) )
			ret.Add ( newHook );

		/*if ( WinLLHook.Any () ) {
			var first = WinLLHook[0];
			WinLLHook.RemoveAt ( 0 );
			HHookInfo keyHookInfo = new ( this, device, first, WinLLHook.ToArray () );
			var keyHooks = Owner.Fetch<VInputReader_KeyboardHook> ().SetupHook ( keyHookInfo, FastCB, DelayedCB );
			if ( !ActiveHooks.ContainsKey ( device ) ) ActiveHooks.Add ( device, new HookGroup () );
			ActiveHooks[device].Add ( WinLLHook, keyHooks );
			ret.UnionWith ( keyHooks );
		}*/
		return ret;
	}

	public override HHookInfo GetHook ( DeviceID device, VKChange vKChange ) {
		if ( !ActiveHooks.ContainsKey ( device ) ) return null;
		var dev = ActiveHooks[device];
		//foreach ( var hook in dev.HookList ) if ( hook.Item2 == vKChange ) return hook;
		return null;
	}

	public override void ClearHooks ( DeviceID device = 0 ) {
		if ( ActiveHooks.ContainsKey ( device ) ) {
			ActiveHooks[device].Clear ();
			ActiveHooks.Remove ( device );
		}
		List<CallbackHolder> cbToRemove = new ();
		foreach ( var cb in Callbacks ) if ( cb.Key.Item1 == device ) cbToRemove.AddRange ( cb.Value );
		foreach ( var cb in cbToRemove ) cb.Unregister ();

	}

	public override Dictionary<DeviceID, DictionaryKey> ListHooks () {
		Dictionary<DeviceID, DictionaryKey> ret = new ();
		foreach ( var deviceHooks in ActiveHooks )
			foreach ( var hook in deviceHooks.Value.AllUniqueHooks )
				ret.Add ( deviceHooks.Key, hook );
		// Shouldn't this maybe return Tuple<VKChange, hookID>?
		return ret;
	}

	public override void RemoveHook ( DeviceID device, params VKChange[] vKChanges ) => throw new NotImplementedException ();

	private bool FastCB ( DictionaryKey key, HInputEventDataHolder e ) {
		if ( !Callbacks.TryGetValue ( (e.HookInfo.DeviceID, CBType.Fast), out var cbs ) ) return true; // no callback of this type, pass to another callback
		foreach ( var cb in cbs ) if ( !cb.callback ( e ) ) return false;
		return true;
	}
	private void DelayedCB ( DictionaryKey key, HInputEventDataHolder e ) {
		if ( !Callbacks.TryGetValue ( (e.HookInfo.DeviceID, CBType.Delayed), out var cbs ) ) return;
		foreach ( var cb in cbs ) if ( !cb.callback ( e ) ) return;
	}

	/// <summary>Since hooks don't map 1:1 to 'hook actions' (e.g. keydown), this class serves as abstraction of such mapping, i.e. presents as Dictionary&lt;VKChange, DictionaryKey&gt; ??Is this still valid after '2 HHookInfo should hold VKChange=>HookD mapping'??</summary>
	protected class HookGroup {
		// This might better be both-way dictionary, but atm the mapping DictKey->VKChange seems to not be needed.
		private readonly Dictionary<VKChange, DictionaryKey> hookDict = new ();
		private readonly HashSet<DictionaryKey> allHooks = new ();

		public HashSet<DictionaryKey> Add ( IDictionary<VKChange, DictionaryKey> setup ) {
			HashSet<DictionaryKey> newHooks = new ();
			foreach ( var (key, value) in setup )
				if ( !hookDict.TryAdd ( key, value ) )
					throw new InvalidOperationException ( $"Hook for {key} already exists" );
				else if ( allHooks.Add ( value ) ) newHooks.Add ( value );
			return newHooks;
		}

		public HashSet<DictionaryKey> this[VKChange vkChange] => throw new NotImplementedException ();

		public IReadOnlyCollection<DictionaryKey> AllUniqueHooks => allHooks.ToList ().AsReadOnly ();

		public void Clear () {
			hookDict.Clear ();
		}
	}
}