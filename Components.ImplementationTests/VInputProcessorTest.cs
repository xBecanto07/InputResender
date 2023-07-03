using Components.Implementations;
using Components.InterfaceTests;
using Components.Interfaces;
using Components.Library;
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;

namespace Components.ImplementationTests {
	public class VInputProcessorTest : DInputProcessorTest {
		public VInputProcessorTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) { }
		public override VInputProcessor GenerateTestObject () => new VInputProcessor ( OwnerCore );
	}
}