using Components.Library;
using FluentAssertions;
using System.Windows.Input;

namespace Components.Interfaces {
	public abstract class DInputReader : ComponentBase<CoreBase> {
		public DInputReader ( CoreBase owner ) : base ( owner ) { }

		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(SetupHook), typeof(void)),
				(nameof(ReleaseHook), typeof(void)),
				(nameof(SimulateInput), typeof(void))
			};

		/// <summary>Prepare a hardware hook, that will on recieving an input event call a returned Action, providing input vektor ID and input data</summary>
		public abstract void SetupHook ( HHookInfo hookInfo, Func<HInputEventDataHolder, bool> callback );
		public abstract void ReleaseHook ( HHookInfo hookInfo );
		public abstract void SimulateInput ( HInputEventDataHolder input, bool allowRecapture );
	}

	public class MInputReader : DInputReader {
		Dictionary<HHookInfo, Func<HInputEventDataHolder, bool>> CallbackList;
		public MInputReader ( CoreBase owner ) : base ( owner ) {
			CallbackList = new Dictionary<HHookInfo, Func<HInputEventDataHolder, bool>> ();
		}

		public override int ComponentVersion => 0;

		public override void ReleaseHook ( HHookInfo hookInfo ) {
			CallbackList.Remove ( hookInfo );
		}
		public override void SetupHook ( HHookInfo hookInfo, Func<HInputEventDataHolder, bool> callback ) {
			CallbackList.Add ( hookInfo, callback );
		}
		public override void SimulateInput ( HInputEventDataHolder input, bool allowRecapture ) { }

		public void SimulateEvent ( HInputEventDataHolder eventData ) {
			if ( CallbackList.TryGetValue ( eventData.HookInfo, out var action ) ) action ( eventData );
		}
		public void SimulateKeyPress ( int KeyCode, bool Pressed ) => SimulateEvent ( new HKeyboardEventDataHolder ( this, 0, KeyCode, Pressed ? 1f : 0f ) );
	}
	

	public abstract class HInputEventDataHolder : DataHolderBase {
		public const int PressThreshold = ushort.MaxValue;
		public HHookInfo HookInfo { get; protected set; }
		public int InputCode { get; protected set; }
		public int ValueX { get; protected set; }
		public int ValueY { get; protected set; }
		public int ValueZ { get; protected set; }

		public float Pressed { get {
				const double D = PressThreshold;
				double X = ValueX / D, Y = ValueY / D, Z = ValueZ / D;
				return (float)Math.Sqrt ( X * X + Y * Y + Z * Z );
			} }

		public HInputEventDataHolder ( DInputReader owner, HHookInfo hookInfo ) : base ( owner ) { HookInfo = hookInfo; }

		public override bool Equals ( object obj ) {
			if ( obj == null ) return false;
			if ( obj.GetType () != GetType () ) return false;
			var item = (HInputEventDataHolder)obj;
			return (InputCode == item.InputCode) & (HookInfo == item.HookInfo) & (ValueX == item.ValueX) & (ValueY == item.ValueY) & (ValueZ == item.ValueZ);
		}
		public override int GetHashCode () => (HookInfo, InputCode, ValueX, ValueY, ValueZ).GetHashCode ();
		public override string ToString () => $"{HookInfo.DeviceID}.{InputCode}:{HookInfo.LatestChangeType} [{ValueX.ToShortString ()};{ValueY.ToShortString ()};{ValueZ.ToShortString ()}]";
	}
	public class HKeyboardEventDataHolder : HInputEventDataHolder {
		public HKeyboardEventDataHolder ( DInputReader owner, int deviceID, int keycode, float pressValue ) : base ( owner, new HHookInfo ( owner, deviceID, pressValue > 0.3f ? VKChange.KeyDown : VKChange.KeyUp ) ) {
			InputCode = keycode;
			ValueX = (int)(pressValue * PressThreshold);
		}

		public override DataHolderBase Clone () => new HKeyboardEventDataHolder ( (DInputReader)Owner, HookInfo.DeviceID, InputCode, ValueX / (float)ushort.MaxValue );
	}
}