using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading;
using System;
using Xunit;

namespace Components.InterfaceTests {
	public abstract class DInputReaderTest : ComponentTestBase<DInputReader> {
		protected List<HInputEventDataHolder> EventList;
		protected AutoResetEvent onInputReceived;

		protected DInputReaderTest () : base () {
			EventList = new List<HInputEventDataHolder> ();
			onInputReceived = new AutoResetEvent ( false );
		}

		[Fact]
		public void SetupSimulateRelease_RaisesCallbackOnce () {
			HInputEventDataHolder inputData = GenerateKeyboardEvent ();
			HHookInfo hookInfo = GenerateHookInfo ();
			EventList.Clear ();
			TestObject.SetupHook ( hookInfo, SimpleTestCallback );
			TestObject.SimulateInput ( inputData, true );
			TestObject.ReleaseHook ( hookInfo );
			TestObject.SimulateInput ( inputData, true );
			onInputReceived.WaitOne ( 100 ).Should ().BeFalse ();
			EventList.Should ().HaveCount ( 1 );
			// No other callback should processed (some key presses during the test could be an issue).
			// Same input event as was simulated should be captured, but by value, not by reference (for hardware input the dataHolder will not be available and so it should be always recreated).
			EventList[0].Should ().Be ( inputData ).And.NotBeSameAs ( inputData );
		}

		[Fact]
		public void ReleaseNonexistingHookThrowsKeyNotFound () {
			Action act = () => TestObject.ReleaseHook ( GenerateHookInfo () );
			act.Should ().Throw<KeyNotFoundException> ();
		}

		protected HInputEventDataHolder GenerateKeyboardEvent ( DInputReader owner = null ) {
			if ( owner == null ) owner = TestObject;
			return new HKeyboardEventDataHolder ( owner, 0, 1, ushort.MaxValue );
		}

		protected bool SimpleTestCallback ( HInputEventDataHolder inpudData ) {
			EventList.Add ( inpudData );
			onInputReceived.Set ();
			return false;
		}

		HHookInfo GenerateHookInfo ( VKChange keyEvent = VKChange.KeyDown, int DeviceID = 1 ) => new HHookInfo ( TestObject, DeviceID, keyEvent );
	}
}
