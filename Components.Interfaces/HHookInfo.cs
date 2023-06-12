using Components.Library;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Components.Interfaces {
	public class HHookInfo : DataHolderBase {
		/// <summary>Internaly modifiable list of possible input event types.<para>Keep in mind, that hookIDs doesn't need to fit 1to1 with changeMask. That is dependent on low-level specs.</para></summary>
		protected virtual HashSet<VKChange> changeMask {  get; set; }
		protected virtual HashSet<nint> hookIDs { get; set; }
		public virtual int DeviceID { get; protected set; }
		/// <summary>Latest change event type (e.g. KeyDown)</summary>
		public virtual VKChange LatestChangeType { get; protected set; }
		public virtual DLowLevelInput HookLLCallback { get; protected set; }
		public virtual ImmutableList<VKChange> ChangeMask { get => changeMask.ToImmutableList (); }
		/// <summary>List of all assigned hookIDs.<para>Low-Level parser is needed to bind hookID to corresponding ChangeMask(s), since it's dependent on low-level implementation.</para></summary>
		public virtual ImmutableList<nint> HookIDs { get => hookIDs.ToImmutableList (); }

		public HHookInfo ( ComponentBase owner, int deviceID, VKChange firstAcceptedChange, params VKChange[] acceptedChanges ) : base ( owner ) {
			DeviceID = deviceID;
			LatestChangeType = firstAcceptedChange;
			changeMask = new HashSet<VKChange> () { firstAcceptedChange };
			hookIDs = new HashSet<nint> ();
			for (int i = 0; i < acceptedChanges.Length; i++) changeMask.Add ( acceptedChanges[i] );
		}

		public virtual void AssignEventData (VKChange latechChange) { LatestChangeType = latechChange; }
		public virtual void AssignHookCallback (DLowLevelInput hookCallback) { HookLLCallback = hookCallback; }
		public virtual void AddHookID ( nint hookID ) => hookIDs.Add ( hookID );
		public virtual void RemoveHookID ( nint hookID ) => hookIDs.Remove ( hookID );

		public override DataHolderBase Clone () => new HHookInfo ( Owner, DeviceID, LatestChangeType );
		public override bool Equals ( object obj ) {
			if ( obj == null ) return false;
			if ( obj.GetType () != GetType () ) return false;
			var item = (HHookInfo)obj;
			int maskN = ChangeMask.Count;
			if ( maskN != item.ChangeMask.Count ) return false;
			if ( DeviceID != item.DeviceID ) return false;
			if ( LatestChangeType != item.LatestChangeType ) return false;
			if ( !ChangeMask.SequenceEqual ( item.ChangeMask ) ) return false;
			return true;
		}
		public override int GetHashCode () => (DeviceID, LatestChangeType).GetHashCode () ^ ChangeMask.CalcSetHash ();
		public override string ToString () => $"{DeviceID}:{LatestChangeType}:[{ChangeMask.AsString ()}]";
	}
}