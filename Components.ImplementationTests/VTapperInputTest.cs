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

		public override DInputProcessor GenerateTestObject () => new VTapperInput ( OwnerCore, new KeyCode[5] { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.Space }, InputData.Modifier.None );

		[Fact]
		public void WriteHelloWorld () {

		}
	}
}