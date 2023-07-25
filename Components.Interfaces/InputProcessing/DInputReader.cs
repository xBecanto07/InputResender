using Components.Library;

namespace Components.Interfaces {
	public abstract class DInputReader : ComponentBase<CoreBase> {
		public DInputReader ( CoreBase owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(SetupHook), typeof(void)),
				(nameof(ReleaseHook), typeof(void)),
				(nameof(SimulateInput), typeof(uint)),
				(nameof(SimulateInput), typeof(HInputEventDataHolder))
			};

		/// <summary>Prepare a hardware hook, that will on recieving an input event call a returned Action, providing input vektor ID and input data</summary>
		/// <param name="hookInfo">Use given info to initialize hook(s). Fills back info about created hooks (e.g. hookIDs).</param>
		public abstract ICollection<DictionaryKey> SetupHook ( HHookInfo hookInfo, Func<DictionaryKey, HInputEventDataHolder, bool> mainCB, Action<DictionaryKey, HInputEventDataHolder> delayedCB );
		public abstract int ReleaseHook ( HHookInfo hookInfo );
		public abstract uint SimulateInput ( HInputEventDataHolder input, bool allowRecapture );
		public HInputEventDataHolder SimulateKeyInput ( HHookInfo hookInfo, VKChange action, KeyCode key ) {
			HInputEventDataHolder ret = new HKeyboardEventDataHolder ( this, hookInfo, (int)key, action == VKChange.KeyDown ? 1 : 0 );
			SimulateInput ( ret, true );
			return ret;
		}
	}

	public class MInputReader : DInputReader {
		Dictionary<HHookInfo, LocHookInfo> CallbackList;
		DictionaryKeyFactory keyFactory;

		struct LocHookInfo {
			public DictionaryKey Key;
			public Func<DictionaryKey, HInputEventDataHolder, bool> MainCB;
			public Action<DictionaryKey, HInputEventDataHolder> DelayedCB;

			public LocHookInfo ( DictionaryKey key, Func<DictionaryKey, HInputEventDataHolder, bool> mainCB, Action<DictionaryKey, HInputEventDataHolder> delayedCB ) { Key = key; MainCB = mainCB; DelayedCB = delayedCB; }
		}

		public MInputReader ( CoreBase owner ) : base ( owner ) {
			keyFactory = new DictionaryKeyFactory ();
			CallbackList = new Dictionary<HHookInfo, LocHookInfo> ();
		}

		public override int ComponentVersion => 1;

		public override int ReleaseHook ( HHookInfo hookInfo ) { return CallbackList.Remove ( hookInfo ) ? 1 : 0; }
		public override ICollection<DictionaryKey> SetupHook ( HHookInfo hookInfo, Func<DictionaryKey, HInputEventDataHolder, bool> mainCB, Action<DictionaryKey, HInputEventDataHolder> delayedCB = null ) {
			var key = keyFactory.NewKey ();
			CallbackList.Add ( hookInfo, new ( key, mainCB, delayedCB ) );
			return new DictionaryKey[]{ key };
		}
		public override uint SimulateInput ( HInputEventDataHolder input, bool allowRecapture ) {
			if ( CallbackList.TryGetValue ( (item) => input.HookInfo < item, out var info ) ) {
				var data = (HInputEventDataHolder)input.Clone ();
				var ret = info.MainCB ( info.Key, data ) ? 0u : 1u;
				if ( info.DelayedCB != null ) info.DelayedCB ( info.Key, data );
				return ret;
			}
			return 0;
		}

		public uint SimulateKeyPress ( KeyCode keyCode, bool Pressed, int DeviceID = 1 ) => SimulateInput ( new HKeyboardEventDataHolder ( this, DeviceID, (int)keyCode, Pressed ? 1f : 0f ), false );
	}
	


	public class HKeyboardEventDataHolder : HInputEventDataHolder {
		public HKeyboardEventDataHolder ( DInputReader owner, int deviceID, int keycode, float pressValue ) : this ( owner, new HHookInfo ( owner, deviceID, pressValue > 0.3f ? VKChange.KeyDown : VKChange.KeyUp ), keycode, pressValue ) {
			InputCode = keycode;
			ValueX = (int)(pressValue * PressThreshold);
			ValueY = ValueZ = 0;
		}
		public HKeyboardEventDataHolder ( DInputReader owner, HHookInfo hookInfo, int keycode, float pressValue ) : base ( owner, hookInfo ) {
			InputCode = keycode;
			ValueX = (int)(pressValue * PressThreshold);
			ValueY = ValueZ = 0;
		}

		public override DataHolderBase Clone () => new HKeyboardEventDataHolder ( (DInputReader)Owner, HookInfo.DeviceID, InputCode, ValueX / (float)ushort.MaxValue );
	}
}