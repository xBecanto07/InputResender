using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading;
using System;
using Xunit;

namespace Components.InterfaceTests {
	public class MLowLevelInputTest : ComponentTestBase<MLowLevelInput> {
		protected AutoResetEvent onInputReceived;
		protected List<HInputData> EventList;
		protected const int HookID = 1;

		protected MLowLevelInputTest () : base () {
			onInputReceived = new AutoResetEvent ( false );
			EventList = new List<HInputData> ();
		}

		public override CoreBase CreateCoreBase () => new CoreBaseMock ();
		public override MLowLevelInput GenerateTestObject () => new MLowLevelInput ( Owner );

		[Fact]
		public void SetupSimulateRelease_RaisesCallbackOnce () {
			var HLData = GenerateKeyboardEvent ();

			EventList.Clear ();
			TestObject.SetHookEx ( 1, SimpleTestCallback, (IntPtr)null, 0 );
			TestObject.SimulateInput ( 1, new HInputData[1] { HLData }, HLData.SizeOf );
			TestObject.UnhookHookEx ( HookID );
			TestObject.SimulateInput ( 1, new HInputData[1] { HLData }, HLData.SizeOf );
			onInputReceived.WaitOne ( 100 ).Should ().BeFalse ();
			EventList.Should ().HaveCount ( 1 );
			EventList[0].Should ().Be ( HLData ).And.NotBeSameAs ( HLData );
		}

		[Fact]
		public void ReleaseNonexistingHookThrowsKeyNotFound () {
			Action act = () => TestObject.UnhookHookEx ( 1 );
			act.Should ().Throw<KeyNotFoundException> ();
		}

		protected HInputData GenerateKeyboardEvent ( MLowLevelInput owner = null ) {
			if ( owner == null ) owner = TestObject;
			return new HInputData_Mock (TestObject, new HInputData_Mock.IInputStruct_Mock (HookID, VKChange.KeyDown, (nint)KeyCode.F));
		}

		protected IntPtr SimpleTestCallback ( int nCode, IntPtr wParam, IntPtr lParam ) {
			if (nCode < 0) return TestObject.CallNextHook (0, nCode, wParam, lParam );
			//EventList.Add(TestObject.ParseHookData(TestObject, ))
			return 0;
		}

		HHookInfo GenerateHookInfo ( VKChange keyEvent = VKChange.KeyDown, int DeviceID = 1 ) => new HHookInfo ( TestObject, DeviceID, keyEvent );
	}
}