﻿using Components.Library;

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
		/// <summary></summary>
		/// <returns>Returns number of successfully simulated events</returns>
		public abstract uint SimulateInput ( HInputEventDataHolder input, bool allowRecapture );
		public HInputEventDataHolder SimulateKeyInput ( HHookInfo hookInfo, VKChange action, KeyCode key ) {
			HInputEventDataHolder ret = new HKeyboardEventDataHolder ( this, hookInfo, (int)key, action );
			SimulateInput ( ret, true );
			return ret;
		}

		public abstract class DStateInfo : StateInfo {
			protected DStateInfo ( DInputReader owner ) : base ( owner ) {
				HookList = GetHookList ();
			}
			public readonly string[] HookList;
			protected abstract string[] GetHookList ();
			public override string AllInfo () => $"{base.AllInfo ()}{BR}Hooks:{BR}{string.Join ( BR, HookList )}";
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
			public override string ToString () => $"{Key} => {MainCB.Method.AsString ()}...{DelayedCB.Method.AsString ()}";
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
				info.MainCB ( info.Key, data );
				if ( info.DelayedCB != null ) info.DelayedCB ( info.Key, data );
				return 1;
			}
			return 0;
		}

		public uint SimulateKeyPress ( HHookInfo hookInfo, KeyCode keyCode, VKChange Pressed ) => SimulateInput ( new HKeyboardEventDataHolder ( this, hookInfo, (int)keyCode, Pressed), false );

		public override StateInfo Info => new VStateInfo ( this );
		public class VStateInfo : DStateInfo {
			public VStateInfo (MInputReader owner) : base (owner) { }

			protected override string[] GetHookList () {
				var InputReader = (MInputReader)Owner;
				string[] ret = new string[InputReader.CallbackList.Count];
				int ID = 0;
				foreach ( var cb in InputReader.CallbackList )
					ret[ID++] = $"{cb.Key} => {cb.Value}";
				return ret;
			}
		}
	}
	


	public class HKeyboardEventDataHolder : HInputEventDataHolder {
		public HKeyboardEventDataHolder ( DInputReader owner, HHookInfo hookInfo, int keycode, VKChange change ) : this ( owner, hookInfo, keycode, change == VKChange.KeyDown ? 1 : 0, change == VKChange.KeyDown ? 1 : -1 ) { }
		public HKeyboardEventDataHolder ( DInputReader owner, int deviceID, int keycode, float pressValue, float delta ) : this ( owner, new HHookInfo ( owner, deviceID, pressValue > 1 ? VKChange.KeyDown : VKChange.KeyUp ), keycode, pressValue, delta ) { }
		public HKeyboardEventDataHolder ( DInputReader owner, HHookInfo hookInfo, int keycode, float pressValue, float delta ) : base ( owner, hookInfo ) {
			InputCode = keycode;
			ValueX = Convert ( pressValue );
			DeltaX = Convert ( delta );
			ValueY = ValueZ = DeltaY = DeltaZ = 0;
		}

		public override DataHolderBase Clone () => new HKeyboardEventDataHolder ( (DInputReader)Owner, HookInfo, InputCode, ValueX / (float)PressThreshold, DeltaX / (float)PressThreshold );
	}
}