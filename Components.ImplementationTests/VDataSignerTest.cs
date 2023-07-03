using Components.Implementations;
using Components.Interfaces;
using Components.InterfaceTests;
using Xunit.Abstractions;

namespace Components.ImplementationTests {
	public class VDataSignerTest : DDataSignerTest {
		public VDataSignerTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) { }
		public override DDataSigner GenerateTestObject () => new VDataSigner ( OwnerCore );
	}
}