using Components.Implementations;
using Components.Library;

namespace Components.ImplementationTests {
	public class MMainAppCoreTest : CoreTestBase<MMainAppCore> {
		public override MMainAppCore GenerateTestCore () => new MMainAppCore ();
	}
}