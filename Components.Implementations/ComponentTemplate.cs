using Components.Library;
using Components.Interfaces;

namespace Components.Interfaces {
	public class ComponentTemplate : DInterfaceTemplate {
		public override int ComponentVersion => 1;

		public ComponentTemplate ( CoreBase owner ) : base ( owner ) { }

		public override void SomeMethod ( int param ) { }
	}

	public class InterfaceTemplateTest : ComponentTestBase<ComponentTemplate> {
		public InterfaceTemplateTest () : base () { }
		public override CoreBase CreateCoreBase () => new CoreBaseMock ();
		public override ComponentTemplate GenerateTestObject () => new ComponentTemplate ( OwnerCore );
	}

	public class InterfaceTemplateMock : ComponentTemplate {
		public InterfaceTemplateMock ( CoreBase owner ) : base ( owner ) { }


	}
}