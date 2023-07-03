using Components.Implementations;
using Components.Interfaces;
using Components.Library;
using Xunit;

namespace InputResender.UnitTests {
	public class DMainAppCoreTest : CoreTestBase<DMainAppCore> {
		public override DMainAppCore GenerateTestCore () => new VMainAppCore ();

		[Fact]
		public void Test_RegisterFetchUnregister () {
			Test_RegisterFetchUnregister_Base ( TestCore.EventVector );
			Test_RegisterFetchUnregister_Base ( TestCore.InputReader );
			Test_RegisterFetchUnregister_Base ( TestCore.InputParser );
			Test_RegisterFetchUnregister_Base ( TestCore.InputProcessor );
			Test_RegisterFetchUnregister_Base ( TestCore.DataSigner );
			Test_RegisterFetchUnregister_Base ( TestCore.PacketSender );
		}

		[Fact]
		public void Test_Availability () {
			Test_Availability_Base ( TestCore.EventVector );
			Test_Availability_Base ( TestCore.InputReader );
			Test_Availability_Base ( TestCore.InputParser );
			Test_Availability_Base ( TestCore.InputProcessor );
			Test_Availability_Base ( TestCore.DataSigner );
			Test_Availability_Base ( TestCore.PacketSender );
		}
	}
}
