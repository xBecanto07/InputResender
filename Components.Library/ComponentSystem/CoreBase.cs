using System;
using System.Collections.ObjectModel;

namespace Components.Library {
	public abstract class CoreBase {
		private readonly DictionaryKeyFactory KeyFactory;
		private readonly Dictionary<DictionaryKey, ComponentInfo> Components;
		internal ReadOnlyDictionary<DictionaryKey, ComponentInfo> RegisteredComponents => Components.AsReadOnly ();
		private static int index = 0;
		public readonly int CoreID;
		public string Name;
		public Action<string> LogFcn = null;
		private readonly List<string> DelayedMessages = new ();
		public enum LogLevel { None, Error, Warning, Info, Debug, All }
		public event Action<string> OnError, OnMessage;

		public class ComponentInfo {
			public readonly ComponentBase Component;
			public readonly DictionaryKey GlobalID;
			public Type[] AcceptedTypes, TypeTree;
			public string Name;
			public string VariantName;
			public DictionaryKey GroupID;
			public int Priority;

			public ComponentInfo ( ComponentBase comp, DictionaryKey globID ) { Component = comp; GlobalID = globID; }
			public override string ToString () => $"{Component}({GlobalID}:{Name}:{GroupID}:{Priority})";
		}

		public class ComponentGroup {
			public readonly CoreBase Core;
			IEnumerable<ComponentInfo> comps;

			public ComponentGroup ( CoreBase core, params Func<ComponentInfo, bool>[] selectors ) {
				Core = core;
				comps = Core.Components.Values;
				foreach ( var selector in selectors ) Reduce ( selector );
			}
			public ComponentGroup Reduce ( Func<ComponentInfo, bool> selector ) {
				if ( selector != null ) { //comps = comps.Where ( selector );
					List<ComponentInfo> newComps = [];
					foreach ( var comp in comps )
						if ( selector ( comp ) )
							newComps.Add ( comp );
					comps = newComps;
				}
				return this;
			}
			public ComponentBase[] GetComponents () => comps.Select ( ( val ) => val.Component ).ToArray ();
			public ComponentInfo[] GetInfoGroup () => comps.ToArray ();
			public ComponentInfo GetInfo () {
				int mostPrio = int.MinValue;
				ComponentInfo ret = null;
				foreach ( var c in comps ) {
					if ( c.Priority > mostPrio ) { mostPrio = c.Priority; ret = c; }
				}
				return ret;
			}
			public ComponentBase Get () => GetInfo ()?.Component;
			public bool Contains ( ComponentBase obj ) => comps.Any ( ( val ) => val.Component == obj );

			public static Func<ComponentInfo, bool> ByType<T> () where T : ComponentBase => ByType ( typeof ( T ) );
			public static Func<ComponentInfo, bool> ByType ( Type t ) => t == null ? null : ( comp ) => comp.TypeTree.Contains ( t );
			public static Func<ComponentInfo, bool> ByName ( string name ) => name == null ? null : ( comp ) => comp.Name == name;
			public static Func<ComponentInfo, bool> BySubGroupID ( DictionaryKey subGroupID ) => subGroupID == default ? null : ( comp ) => comp.GroupID == subGroupID;
			public static Func<ComponentInfo, bool> ByVariantName ( string variantName ) => variantName == null ? null : ( comp ) => comp.VariantName == variantName;
			public static Func<ComponentInfo, bool> ByAcceptedType ( Type acceptedType ) => acceptedType == null ? null :  ( comp ) => comp.AcceptedTypes.Contains ( acceptedType );
		}

		private static readonly string[] nameList = new string[] { "Alfa", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliett", "Kilo", "Lima", "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey", "Xray", "Yankee", "Zulu" };

		public CoreBase () {
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
			Register ( component, ref key, name, priority );
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
				var group = new ComponentGroup ( this, ( comp ) => comp.TypeTree[0] == compInfo.TypeTree[0] ).GetInfoGroup ();
				int ID = 1;
				foreach ( var comp in group ) {
					int compID = comp.GroupID.GetHashCode ();
					if ( compID >= ID ) ID = compID + 1;
				}
				perTypeID = new DictionaryKey ( ID );
				compInfo.GroupID = perTypeID;
			}
			Components.Add ( retKey, compInfo );
			return retKey;
		}
		public void Register ( ComponentInfo compInfo ) {
			if ( Components.ContainsKey ( compInfo.GlobalID ) ) throw new ArgumentException ( $"There is another component already registered under key {compInfo.GlobalID}" );
			Components.Add ( compInfo.GlobalID, compInfo );
		}

		public ComponentBase this[DictionaryKey ID] => Components[ID].Component;
		public ComponentInfo this[ComponentBase comp] {
			get {
				foreach ( var compInfo in Components ) {
					if ( compInfo.Value.Component == comp )
						return compInfo.Value;
				}
				return null;
			}
		}

		public void SelectNewPriority ( ComponentInfo[] group, ComponentBase newPrioComp, int newPrio = 15 ) {
			int N = group.Length;
			for ( int i = 0; i < N; i++ ) {
				var CI = group[i];
				if ( Components.TryGetValue ( CI.GlobalID, out var comp ) ) {
					if ( comp != CI ) throw new KeyNotFoundException ( $"Component {CI.Name}#{CI.GlobalID} is not the one, registered in this core under same ID!" );
				} else throw new KeyNotFoundException ( $"Component {CI.Name}#{CI.GlobalID} is not a member of this core!" );

				CI.Priority = CI.Component == newPrioComp ? newPrio : 0;
			}
		}

		public static Type[] FindCompDefName ( Type t ) {
			if ( t == null ) return null;
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

		public void Unregister ( ComponentBase component ) {
			var info = this[component];
			//if ( info == null ) throw new KeyNotFoundException ( "Given component is not registered!" );
			if ( info == null ) return;
			Components.Remove ( info.GlobalID );
		}

		public T Fetch<T> ( DictionaryKey subGroupID = default, string variantName = null, Type acceptedType = null ) where T : ComponentBase => Fetch ( typeof ( T ), null, subGroupID, variantName, acceptedType ) as T;
		public ComponentBase Fetch (
			Type t = null
			, string name = null
			, DictionaryKey subGroupID = default
			, string variantName = null
			, Type acceptedType = null )
			=> new ComponentGroup ( this
			, ComponentGroup.ByType ( t )
			, ComponentGroup.ByName ( name )
			, ComponentGroup.BySubGroupID ( subGroupID )
			, ComponentGroup.ByVariantName ( variantName )
			, ComponentGroup.ByAcceptedType ( acceptedType )
			).Get ();


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

		public void PushDelayedMsg ( string msg ) {
			lock ( DelayedMessages ) DelayedMessages.Add ( msg );
			OnMessage?.Invoke ( msg );
		}
		public void PushDelayedError ( string msg, Exception ex ) {
			msg = $"{msg}: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
			lock ( DelayedMessages ) DelayedMessages.Add ( msg );
			OnError?.Invoke ( msg );
		}
		public void FlushDelayedMsgs ( Action<string> printer = null ) {
			var cmdProc = Fetch<CommandProcessor> ();
			if ( cmdProc == null ) return;

			lock ( DelayedMessages ) {
				if ( !DelayedMessages.Any () ) return;
				var PrintFcn = printer ?? LogFcn;
				if ( PrintFcn == null ) {
					PrintFcn = ( msg ) => cmdProc.ProcessLine ( $"print \"{msg}\"" );
				}

				foreach ( var msg in DelayedMessages ) PrintFcn.Invoke ( msg );
				DelayedMessages.Clear ();
			}
		}
	}

	public class ComponentSelector {
		public DictionaryKey? ID;
		public string VariantName;
		public Type ComponentType;

		public ComponentSelector ( CoreBase core = null, DictionaryKey? id = null, string variantName = null, Type componentType = null ) {
			ID = id;
			VariantName = variantName;
			ComponentType = componentType;

			AssertComponent ( core );
		}

		void AssertComponent ( CoreBase core ) {
			if ( ComponentType != null ) {
				Type t = ComponentType;
				while ( t.BaseType != null ) {
					if ( t.BaseType == typeof ( ComponentBase ) ) return;
					t = t.BaseType;
				}
				throw new Exception ( $"Only components can be joined, but {t.Name} was provided!" );
			} else {
				if ( core == null ) throw new ArgumentNullException ( nameof ( core ), "Core must be provided to assert component selector validity!" );
				if ( ID != null ) {
					if ( core[ID.Value] == null )
						throw new Exception ( $"No component with ID {ID.Value} was found!" );
				} else if ( VariantName != null ) {
					if ( core.Fetch ( variantName: VariantName ) == null )
						throw new Exception ( $"No component variant with name {VariantName} was found!" );
				}
			}
		}

		public ComponentBase Fetch (CoreBase core) {
			if ( ID != null ) return core[ID.Value];
			if ( ComponentType != null ) return core.Fetch ( t: ComponentType );
			if ( VariantName != null ) return core.Fetch ( variantName: VariantName );
			return null;
		}
		public T Fetch<T> ( CoreBase core ) where T : ComponentBase => Fetch ( core ) as T;

		public override string ToString () {
			List<string> dsc = new ();
			if ( ID != null ) dsc.Add ( $"ID: {ID}" );
			if ( ComponentType != null ) dsc.Add ( $"Type: {ComponentType.Name}" );
			if ( VariantName != null ) dsc.Add ( $"Variant: {VariantName}" );
			return $"<{string.Join ( ", ", dsc )}>";
		}
	}

	public class CoreBaseMock : CoreBase {

	}
}