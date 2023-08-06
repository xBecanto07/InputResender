using Xunit;
using FluentAssertions;
using System;

namespace Components.Library {
	public abstract class CoreBase {
		private readonly DictionaryKeyFactory KeyFactory;
		private readonly Dictionary<DictionaryKey, ComponentInfo> Components;
		private static int index = 0;
		public readonly int CoreID;
		public string Name;
		public Action<string> LogFcn = null;

		public class ComponentInfo {
			public readonly ComponentBase Component;
			public readonly DictionaryKey GlobalID;
			public Type[] AcceptedTypes, TypeTree;
			public string Name;
			public string VariantName;
			public DictionaryKey GroupID;
			public int Priority;

			public ComponentInfo (ComponentBase comp, DictionaryKey globID) { Component = comp; GlobalID = globID; }
		}

		public class ComponentGroup {
			public readonly CoreBase Core;
			IEnumerable<ComponentInfo> comps;

			public ComponentGroup(CoreBase core, Func<ComponentInfo, bool> selector ) {
				Core = core;
				comps = Core.Components.Values.Where ( selector );
			}
			public ComponentGroup Reduce ( Func<ComponentInfo, bool> selector ) { comps = comps.Where ( selector ); return this; }
			public ComponentBase[] GetComponents () => comps.Select ( ( val ) => val.Component ).ToArray ();
			public ComponentInfo[] GetInfoGroup () => comps.ToArray ();
			public ComponentInfo GetInfo () {
				int mostPrio = int.MinValue;
				ComponentInfo ret = null;
				foreach (var c in comps) {
					if ( c.Priority > mostPrio ) { mostPrio = c.Priority; ret = c; }
				}
				return ret;
			}
			public ComponentBase Get () => GetInfo ()?.Component;
			public bool Contains ( ComponentBase obj ) => comps.Any ( ( val ) => val.Component == obj );

			public static Func<ComponentInfo, bool> ByType<T> () where T : ComponentBase => ByType ( typeof ( T ) );
			public static Func<ComponentInfo, bool> ByType (Type t) => ( comp ) => comp.TypeTree.Contains ( t );
			public static Func<ComponentInfo, bool> ByName ( string name ) => ( comp ) => comp.Name == name;
			public static Func<ComponentInfo, bool> BySubGroupID ( DictionaryKey subGroupID ) => ( comp ) => comp.GroupID == subGroupID;
			public static Func<ComponentInfo, bool> ByVariantName ( string variantName ) => ( comp ) => comp.VariantName == variantName;
			public static Func<ComponentInfo, bool> ByAcceptedType ( Type acceptedType ) => ( comp ) => comp.AcceptedTypes.Contains ( acceptedType );
		}

		private static readonly string[] nameList = new string[] { "Alfa", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliett", "Kilo", "Lima", "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey", "Xray", "Yankee", "Zulu" };

		public CoreBase() {
			lock ( nameList ) {
				CoreID = index++;
				Name = nameList[CoreID % nameList.Length];
				if ( CoreID >= nameList.Length ) Name += $"#{CoreID / nameList.Length}";
			}
			KeyFactory = new DictionaryKeyFactory ();
			Components = new Dictionary<DictionaryKey, ComponentInfo> ();
		}

		public void Register<T> ( T component, string name = null, int priority = 0 ) where T : ComponentBase {
			DictionaryKey key = DictionaryKey.Empty;
			Register (component, ref key, name, priority );
		}
		public DictionaryKey Register<T> ( T component, ref DictionaryKey perTypeID, string name = null, int priority = 0 ) where T : ComponentBase {
			if ( IsRegistered ( component ) ) throw new ArgumentException ( "Given component instance is already registered!" );
			DictionaryKey retKey = KeyFactory.NewKey ();
			ComponentInfo compInfo = new ComponentInfo ( component, retKey );
			compInfo.AcceptedTypes = component.AcceptedTypes;
			compInfo.TypeTree = FindCompDefName ( component.GetType () );
			compInfo.Name = name ?? compInfo.TypeTree[0].Name;
			compInfo.VariantName = component.VariantName ?? compInfo.TypeTree[^1].Name;
			compInfo.Priority = priority;
			if ( perTypeID.Valid ) compInfo.GroupID = perTypeID;
			else {
				var group = new ComponentGroup (this, ( comp ) => comp.TypeTree[0] == compInfo.TypeTree[0] ).GetInfoGroup ();
				int ID = 1;
				foreach (var comp in group) {
					int compID = comp.GroupID.GetHashCode ();
					if ( compID >= ID ) ID = compID + 1;
				}
				perTypeID = new DictionaryKey ( ID );
				compInfo.GroupID = perTypeID;
			}
			Components.Add ( retKey, compInfo );
			return retKey;
		}
		public void Register (ComponentInfo compInfo) {
			if ( Components.ContainsKey ( compInfo.GlobalID ) ) throw new ArgumentException ( $"There is another component already registered under key {compInfo.GlobalID}" );
			Components.Add ( compInfo.GlobalID, compInfo );
		}

		public ComponentBase this[DictionaryKey ID] => Components[ID].Component;
		public ComponentInfo this[ComponentBase comp] { get {
				foreach (var compInfo in  Components) {
					if ( compInfo.Value.Component == comp )
						return compInfo.Value;
				}
				return null;
			} }

		public void SelectNewPriority ( ComponentInfo[] group, ComponentBase newPrioComp, int newPrio = 15) {
			int N = group.Length;
			for (int i = 0; i < N; i++ ) {
				var CI = group[i];
				if ( Components.TryGetValue ( CI.GlobalID, out var comp ) ) {
					if ( comp != CI ) throw new KeyNotFoundException ( $"Component {CI.Name}#{CI.GlobalID} is not the one, registered in this core under same ID!" );
				} else throw new KeyNotFoundException ( $"Component {CI.Name}#{CI.GlobalID} is not a member of this core!" );

				CI.Priority = CI.Component == newPrioComp ? newPrio : 0;
			}
		}

		private Type[] FindCompDefName ( Type t ) {
			List<Type> ret = new List<Type> ();
			while ( t.BaseType != null ) {
				ret.Insert ( 0, t );
				string actName = t.Name;
				if ( t.BaseType == typeof ( ComponentBase ) ) break;
				if ( actName.StartsWith ( 'D' ) ) break;
				t = t.BaseType;
			}
			return ret.ToArray ();
		}

		public void Unregister<T> (T component ) where T : ComponentBase {
			var info = this[component];
			//if ( info == null ) throw new KeyNotFoundException ( "Given component is not registered!" );
			if ( info == null ) return;
			Components.Remove ( info.GlobalID );
		}

		public T Fetch<T> ( DictionaryKey subGroupID = default, string variantName = null, Type acceptedType = null ) where T : ComponentBase => Fetch ( typeof ( T ), subGroupID, variantName, acceptedType ) as T;
		public ComponentBase Fetch ( Type t, DictionaryKey subGroupID = default, string variantName = null, Type acceptedType = null ) {
			var ret = new ComponentGroup ( this, ComponentGroup.ByType ( t ) );
			if ( subGroupID.Valid ) ret.Reduce ( ComponentGroup.BySubGroupID ( subGroupID ) );
			if ( variantName != null ) ret.Reduce ( ComponentGroup.ByVariantName ( variantName ) );
			if ( acceptedType != null ) ret.Reduce ( ComponentGroup.ByAcceptedType ( acceptedType ) );
			return ret.Get ();
		}
		public ComponentBase Fetch (string name, DictionaryKey subGroupID = default, string variantName = null, Type acceptedType = null ) {
			var ret = new ComponentGroup ( this, ComponentGroup.ByName ( name ) );
			if ( subGroupID.Valid ) ret.Reduce ( ComponentGroup.BySubGroupID ( subGroupID ) );
			if ( variantName != null ) ret.Reduce ( ComponentGroup.ByVariantName ( variantName ) );
			if ( acceptedType != null ) ret.Reduce ( ComponentGroup.ByAcceptedType ( acceptedType ) );
			return ret.Get ();
		}


		public bool IsRegistered ( ComponentBase comp ) {
			foreach ( var val in Components ) if ( val.Value.Component == comp ) return true;
			return false;
		}
		public bool IsRegistered ( string name ) {
			foreach ( var val in Components ) if ( val.Value.Name == name ) return true;
			return false;
		}
		public bool IsRegistered<T> () {
			Type t = typeof ( T );
			foreach ( var val in Components ) if ( val.Value.TypeTree.Contains ( t ) ) return true;
			return false;
		}
	}

	public class CoreBaseMock : CoreBase {

	}

	public abstract class CoreTestBase<CoreT> where CoreT : CoreBase {
		protected readonly CoreT TestCore;
 
		public CoreTestBase() {
			TestCore = GenerateTestCore ();
		}

		public void Test_RegisterFetchUnregister_Base<CompT> (CompT component) where CompT : ComponentBase<CoreBase> {
			bool preregistered = TestCore.IsRegistered ( component );
			if (preregistered) {
				var compInfo = TestRegistered ( component );
				TestCore.Unregister ( component );
				TestUnregistered ( component );
				TestCore.Register ( compInfo );
				TestRegistered ( component ).Should ().Be ( compInfo );
				TestCore[compInfo.GlobalID].Should ().Be ( component );
			} else {
				TestUnregistered ( component );
				DictionaryKey subGroupID = new DictionaryKey ();
				var origInfo = TestCore.Register ( component, ref subGroupID );
				TestRegistered ( component ).Should ().Be ( origInfo );
				TestCore.Unregister ( component );
				TestUnregistered ( component );
			}
		}

		private CoreBase.ComponentInfo TestRegistered<CompT> ( CompT component ) where CompT : ComponentBase<CoreBase> {
			new CoreBase.ComponentGroup ( TestCore, CoreBase.ComponentGroup.ByType<CompT> () ).Contains ( component ).Should ().BeTrue ();
			var compInfo = TestCore[component];
			TestCore[compInfo.GlobalID].Should ().Be ( component );
			return compInfo;
		}
		private void TestUnregistered<CompT> (CompT component) where CompT : ComponentBase<CoreBase> {
			new CoreBase.ComponentGroup ( TestCore, CoreBase.ComponentGroup.ByType<CompT> () ).Contains ( component ).Should ().BeFalse ();
			TestCore[component].Should ().BeNull ();
		}

		public void Test_Availability_Base ( params ComponentBase[] components) {
			foreach (var component in components) {
				TestCore.Fetch ( component.GetType () ).Should ().Be ( component );
			}
		}

		[Fact]
		public void Test_RegisterFetchUnregister_MockComponent () {
			Test_RegisterFetchUnregister_Base ( new ComponentMock ( TestCore ) );
		}

		public abstract CoreT GenerateTestCore ();
	}
}