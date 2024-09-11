using Components.Library;
using Components.Interfaces;

namespace Components.Interfaces {
	public class ComponentTemplate : DInterfaceTemplate {
		public override int ComponentVersion => 1;

		public ComponentTemplate ( CoreBase owner ) : base ( owner ) { }

		public override void SomeMethod ( int param ) { }

		public override StateInfo Info => new MStateInfo ( this );
		public class MStateInfo : StateInfo {
			public MStateInfo (ComponentTemplate template ) : base ( template ) { }
		}
	}

	public class InterfaceTemplateMock : ComponentTemplate {
		public InterfaceTemplateMock ( CoreBase owner ) : base ( owner ) { }


	}
}