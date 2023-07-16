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
		public abstract ICollection<nint> SetupHook ( HHookInfo hookInfo, Func<HInputEventDataHolder, bool> callback );
		public abstract int ReleaseHook ( HHookInfo hookInfo );
		public abstract uint SimulateInput ( HInputEventDataHolder input, bool allowRecapture );
		public HInputEventDataHolder SimulateKeyInput ( HHookInfo hookInfo, VKChange action, KeyCode key ) {
			HInputEventDataHolder ret = new HKeyboardEventDataHolder ( this, hookInfo, (int)key, action == VKChange.KeyDown ? 1 : 0 );
			SimulateInput ( ret, true );
			return ret;
		}
	}

	public class MInputReader : DInputReader {
		Dictionary<HHookInfo, Func<HInputEventDataHolder, bool>> CallbackList;
		public MInputReader ( CoreBase owner ) : base ( owner ) {
			CallbackList = new Dictionary<HHookInfo, Func<HInputEventDataHolder, bool>> ();
		}

		public override int ComponentVersion => 1;

		public override int ReleaseHook ( HHookInfo hookInfo ) { return CallbackList.Remove ( hookInfo ) ? 1 : 0; }
		public override ICollection<nint> SetupHook ( HHookInfo hookInfo, Func<HInputEventDataHolder, bool> callback ) { CallbackList.Add ( hookInfo, callback ); return new nint[]{ 1 }; }
		public override uint SimulateInput ( HInputEventDataHolder input, bool allowRecapture ) {
			if ( CallbackList.TryGetValue ( (item) => input.HookInfo < item, out var fnc ) ) {
				return fnc ( (HInputEventDataHolder)input.Clone () ) ? 0u : 1u;
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