using Xunit;
using FluentAssertions;

namespace Components.Library {
	public abstract class CoreBase {
		private readonly Dictionary<string, ComponentBase> Components;
		private static int index = 0;
		public readonly int CoreID;
		public string Name;
		public Action<string> LogFcn = null;

		private static readonly string[] nameList = new string[] { "Alfa", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliett", "Kilo", "Lima", "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey", "Xray", "Yankee", "Zulu" };

		public CoreBase() {
			lock ( nameList ) {
				CoreID = index++;
				Name = nameList[CoreID % nameList.Length];
				if ( CoreID >= nameList.Length ) Name += $"#{CoreID / nameList.Length}";
			}
			Components = new Dictionary<string, ComponentBase> ();
		}

		/// <summary>Register given component under name of Type that it is passed by i.e. using nameof(T)</summary>
		public void Register<T> ( T component ) where T : ComponentBase => Register ( FindCompDefName ( component ), component );
		/// <summary>Register component by name of the underlying type i.e. by specific variant.</summary>
		public void Register ( ComponentBase component ) => Register ( FindCompDefName ( component ), component );
		public void Register ( string key, ComponentBase component ) {
			if ( Components.ContainsKey ( key ) ) Components[key] = component;
			else Components.Add ( key, component );
		}
		private string FindCompDefName ( ComponentBase comp ) => FindCompDefName ( comp.GetType () );
		private string FindCompDefName ( Type t ) {
			while ( t.BaseType != null ) {
				string actName = t.Name;
				if ( t.BaseType.Name == nameof ( ComponentBase ) ) return actName;
				if ( actName.StartsWith ( 'D' ) ) return actName;
				t = t.BaseType;
			}
			return t.Name;
		}

		public void Unregister<T> (T component ) where T : ComponentBase {
			string name = nameof ( T );

			Components.Remove( name );
		}
		/// <summary>Fetch any component that implements givent inteface of <typeparamref name="T"/></summary>
		public T Fetch<T> () where T : ComponentBase<CoreBase> => (T)Fetch ( typeof ( T ) );
		/// <summary>Fetch a component based on it's name (that is name of interface it's implementing). Useful when the interface is inaccessible, than its methods are still accasseble by Fetch(string, Type).</summary>
		public ComponentBase Fetch ( string name ) { if ( Components.TryGetValue ( name, out var comp ) ) return comp; else return null; }
		/// <summary>Fetch any registered component that is implementing given interface or derives from an object of <paramref name="type"/></summary>
		public ComponentBase Fetch (Type type) {
			foreach(var component in Components) {
				if ( component.Value.GetType ().IsAssignableTo ( type ) ) return component.Value;
			}
			return null;
		}

		public bool IsRegistered ( string name ) => Components.ContainsKey ( name );
	}

	public class CoreBaseMock : CoreBase {

	}

	public abstract class CoreTestBase<CoreT> where CoreT : CoreBase {
		protected readonly CoreT TestCore;
 
		public CoreTestBase() {
			TestCore = GenerateTestCore ();
		}

		public void Test_RegisterFetchUnregister_Base<CompT> (CompT component) where CompT : ComponentBase<CoreBase> {
			bool preregistered = TestCore.IsRegistered ( component.GetType ().BaseType.Name );
			if ( preregistered ) TestCore.Unregister ( component );
			else TestCore.Register ( component );
			TestCore.Fetch<CompT> ().Should ().Be ( component );
			TestCore.Fetch ( component.GetType ().BaseType.Name ).Should ().Be ( component );
			if ( preregistered ) TestCore.Register ( component );
			else TestCore.Unregister ( component );
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