using Components.Implementations;
using Components.InterfaceTests;
using Components.Interfaces;
using Components.Library;
using Xunit;
using FluentAssertions;

namespace Components.ImplementationTests {
	public class VInputReader_WinKeyHookTest : DInputReaderTest {
		MLowLevelInput LowLevelInput;

		public VInputReader_WinKeyHookTest () : base () { }

		public override CoreBase CreateCoreBase () {
			var ret = new CoreBaseMock ();
			LowLevelInput = new MLowLevelInput ( ret );
			return ret;
		}
		public override DInputReader GenerateTestObject () => new VInputReader_KeyboardHook ( OwnerCore );
	}

	public class MInputReaderTest : DInputReaderTest {
		MLowLevelInput LowLevelInput;

		public MInputReaderTest () : base () { }

		public override CoreBase CreateCoreBase () {
			var ret = new CoreBaseMock ();
			LowLevelInput = new MLowLevelInput ( ret );
			return ret;
		}
		public override DInputReader GenerateTestObject () => new MInputReader ( OwnerCore );

		[Fact]
		public void SimKeyPressPasses () {
			HInputEventDataHolder inputData = GenerateKeyboardEvent ();
			ExecOnHook ( inputData, () => ((MInputReader)TestObject).SimulateKeyPress ( KeyCode.E, true, inputData.HookInfo.DeviceID )
			, true, true );
			EventList.Should ().HaveCount ( 1 );
		}
	}
}