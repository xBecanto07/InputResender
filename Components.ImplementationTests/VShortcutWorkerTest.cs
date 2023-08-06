using Components.Implementations;
using Components.Interfaces;
using Components.InterfaceTests;
using Components.Library;
using Xunit.Abstractions;

namespace Components.ImplementationTests {
	public class VShortcutWorkerTest : DShortcutWorkerTest {
		public VShortcutWorkerTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) {
		}

		public override CoreBase CreateCoreBase () => new CoreBaseMock ();
		public override DShortcutWorker GenerateTestObject () => new VShortcutWorker ( OwnerCore );
	}
}