using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Components.InterfaceTests {
	public abstract class DInputProcessorTest : ComponentTestBase<DInputProcessor> {
		DInputParser InputParser;

		public override CoreBase CreateCoreBase () {
			var ret = new CoreBaseMock ();
			InputParser = new MInputParser ( ret );
			return ret;
		}

		[Fact]
		public void EmptyInputToEmptyOutput () {
			TestObject.ProcessInput ( null ).Should ().Be ( InputData.Empty ( TestObject ) );
		}
	}

	public class MInputProcessorTest : DInputProcessorTest {
		public override MInputProcessor GenerateTestObject () => new MInputProcessor ( OwnerCore );


	}

	public class InputDataTest : SerializableDataHolderTestBase<InputData> {
		public InputDataTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) { }

		public override InputData GenerateTestObject ( int variant ) {
			if ( variant < 0 ) return InputData.Empty ( OwnerComp );
			else return new InputData ( OwnerComp ) {
				Cmnd = variant % 2 == 1 ? InputData.Command.KeyRelease : InputData.Command.KeyPress,
				Key = (KeyCode)(variant / 2),
				DeviceID = 1,
				Modifiers = (InputData.Modifier)(variant / 8),
				X = 1 - variant % 2
			};
		}

		public override System.Collections.Generic.List<InputData> GetTestData () {
			var ret = new System.Collections.Generic.List<InputData> ();
			for ( int i = -2; i < 12; i++ ) ret.Add ( GenerateTestObject ( i ) );
			return ret;
		}
	}
}