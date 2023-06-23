using Components.Implementations;
using Components.InterfaceTests;
using Components.Interfaces;
using Components.Library;
using Xunit;
using FluentAssertions;

namespace Components.ImplementationTests {
	public class VInputProcessorTest : DInputProcessorTest {
		public override VInputProcessor GenerateTestObject () => new VInputProcessor ( OwnerCore );


	}
}