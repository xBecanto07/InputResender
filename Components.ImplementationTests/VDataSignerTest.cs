using Components.Implementations;
using Components.Interfaces;
using Components.InterfaceTests;

namespace Components.ImplementationTests {
	public class VDataSignerTest : DDataSignerTest {
		public override DDataSigner GenerateTestObject () => new VDataSigner ( OwnerCore );
	}
}