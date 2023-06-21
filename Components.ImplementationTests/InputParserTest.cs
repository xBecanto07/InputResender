using Components.Implementations;
using Components.InterfaceTests;
using Components.Interfaces;
using Components.Library;
using Xunit;
using FluentAssertions;

namespace Components.ImplementationTests {
	public class InputParserTest : DInputParserTest {
		public override DInputParser GenerateTestObject () => new InputParser ( Owner );
	}
}