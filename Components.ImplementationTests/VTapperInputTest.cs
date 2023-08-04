using Components.Implementations;
using Components.InterfaceTests;
using Components.Interfaces;
using Components.Library;
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;

namespace Components.ImplementationTests {
	public class VTapperInputTest : DInputProcessorTest {
		public VTapperInputTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) {
		}

		public override DInputProcessor GenerateTestObject () => new VTapperInput ( OwnerCore, new DInputProcessor.KeySetup[5] { new ( "Little", KeyCode.A ), new ( "Ring", KeyCode.S ), new ( "Middle", KeyCode.D ), new ( "Index", KeyCode.F ), new ( "Thumb", KeyCode.Space ) } );

		[Fact]
		public void WriteHelloWorld () {

		}
	}
}