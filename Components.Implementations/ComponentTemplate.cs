﻿using Components.Library;
using Components.Interfaces;
using Xunit.Abstractions;

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

	public class InterfaceTemplateTest : ComponentTestBase<ComponentTemplate> {
		public InterfaceTemplateTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) { }
		public override CoreBase CreateCoreBase () => new CoreBaseMock ();
		public override ComponentTemplate GenerateTestObject () => new ComponentTemplate ( OwnerCore );
	}

	public class InterfaceTemplateMock : ComponentTemplate {
		public InterfaceTemplateMock ( CoreBase owner ) : base ( owner ) { }


	}
}