using Components.Library;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace Components.Interfaces {
	public class HHookInfo : DataHolderBase {
		/// <summary>Internaly modifiable list of possible input event types.<para>Keep in mind, that hookIDs doesn't need to fit 1to1 with changeMask. That is dependent on low-level specs.</para></summary>
		protected virtual HashSet<VKChange> changeMask {  get; set; }
		protected virtual HashSet<DictionaryKey> hookIDs { get; set; }
		public virtual int DeviceID { get; protected set; }
		/// <summary>Latest change event type (e.g. KeyDown)</summary>
		public virtual VKChange LatestChangeType { get; protected set; }
		/// <summary>Should be probably bound together with LatestChangeType as tuple</summary>
		public virtual int LatestDeviceID { get; protected set; }
		public virtual DLowLevelInput HookLLCallback { get; protected set; }
		public virtual ImmutableList<VKChange> ChangeMask { get => changeMask.ToImmutableList (); }
		/// <summary>List of all assigned hookIDs.<para>Low-Level parser is needed to bind hookID to corresponding ChangeMask(s), since it's dependent on low-level implementation.</para></summary>
		public virtual ImmutableList<DictionaryKey> HookIDs { get => hookIDs.ToImmutableList (); }

		public HHookInfo ( ComponentBase owner, int deviceID, VKChange firstAcceptedChange, params VKChange[] acceptedChanges ) : base ( owner ) {
			DeviceID = deviceID;
			LatestChangeType = firstAcceptedChange;
			changeMask = new HashSet<VKChange> () { firstAcceptedChange };
			hookIDs = new HashSet<DictionaryKey> ();
			for (int i = 0; i < acceptedChanges.Length; i++) changeMask.Add ( acceptedChanges[i] );
		}

		public virtual void AssignEventData (VKChange latechChange) { LatestChangeType = latechChange; }
		public virtual void AssignHookCallback (DLowLevelInput hookCallback) { HookLLCallback = hookCallback; }
		public virtual void AddHookID ( DictionaryKey hookID ) => hookIDs.Add ( hookID );
		public virtual void RemoveHookID ( DictionaryKey hookID ) => hookIDs.Remove ( hookID );

		public override DataHolderBase Clone () {
			var ret = new HHookInfo ( Owner, DeviceID, LatestChangeType );
			foreach ( var hookID in hookIDs ) ret.AddHookID ( hookID );
			return ret;
		}
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

		/// <summary>LHS ∈ RHS, or that RHS isn't missing any data provided by LHS</summary>
		public static bool operator < ( HHookInfo lhs, HHookInfo rhs ) => DoesContain ( lhs, rhs, lhs.hookIDs.ToArray () );
		/// <summary>RHS ∈ LHS, or that LHS isn't missing any data provided by RHS</summary>
		public static bool operator > ( HHookInfo lhs, HHookInfo rhs ) => DoesContain ( rhs, lhs, rhs.hookIDs.ToArray () );
		/// <summary>LHS ∈ RHS, or that RHS isn't missing any data provided by LHS, testing only given HookID instead of all</summary>
		public static bool operator < ( (HHookInfo hookInfo, DictionaryKey hookID) lhs, HHookInfo rhs ) => DoesContain ( lhs.hookInfo, rhs, lhs.hookID );
		/// <summary>LHS ∈ RHS, or that RHS isn't missing any data provided by LHS, testing only given HookID instead of all</summary>
		public static bool operator > ( (HHookInfo hookInfo, DictionaryKey hookID) lhs, HHookInfo rhs ) => DoesContain ( rhs, lhs.hookInfo, lhs.hookID );
		private static bool DoesContain ( HHookInfo smaller, HHookInfo larger, params DictionaryKey[] hookIDs ) {
			bool ret = smaller.DeviceID == larger.DeviceID;
			foreach ( var hookID in hookIDs )
				ret &= larger.hookIDs.Contains ( hookID );
			ret &= larger.changeMask.Contains ( smaller.LatestChangeType );
			return ret;
		}

		public override int GetHashCode () => (DeviceID, LatestChangeType).GetHashCode () ^ ChangeMask.CalcSetHash ();
		public override string ToString () => $"{DeviceID}:{LatestChangeType}:[{ChangeMask.AsString ()}]";
	}
}