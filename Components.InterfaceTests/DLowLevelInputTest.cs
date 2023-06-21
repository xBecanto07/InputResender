using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading;
using System;
using Xunit;
using System.Runtime.InteropServices;
using System.Data;

namespace Components.InterfaceTests {
	public abstract class DLowLevelInputTest<T> : ComponentTestBase<T> where T : DLowLevelInput {
		protected AutoResetEvent onInputReceived;
		protected List<HInputData> EventList;
		protected readonly KeyCode SimKey;

		public DLowLevelInputTest ( KeyCode simKey ) : base () {
			onInputReceived = new AutoResetEvent ( false );
			EventList = new List<HInputData> ();
			SimKey = simKey;
		}

		[Fact]
		public void SetupSimulateRelease_RaisesCallbackOnce () {
			FetchHLData ( out var HLData, out uint dataCnt, out int dataSize );
			ExecOnHook ( HLData, () => TestObject.SimulateInput ( dataCnt, HLData, dataSize ), true, true );
			EventList.Should ().HaveCount ( (int)dataCnt );
			EventList.Should ().Equal ( HLData ).And.NotBeSameAs ( HLData );
		}

		[Fact]
		public void NegNCodeSkipsProcessing () {
			FetchHLData ( out var HLData, out uint dataCnt, out int dataSize );
			ExecOnHook ( HLData, () => {
				TestObject.SimulateInput ( dataCnt, HLData, dataSize, false );
				if ( !TryForceCallbackSkip ( false ) )
					Assert.Fail ( "This function cannot be tested." );
				TestObject.SimulateInput ( dataCnt, HLData, dataSize, true );
				TestObject.SimulateInput ( dataCnt, HLData, dataSize, false );
			}, false, false );
		}

		[Fact]
		public void ReleaseNonexistingHookThrowsKeyNotFound () {
			Action act = () => TestObject.UnhookHookEx ( 1 );
			act.Should ().Throw<KeyNotFoundException> ();
		}

		protected abstract HInputData[] GenerateKeyboardEvent ( T owner );
		/// <summary>If possible, set wheter callback for next (one or all) raised hook should be skipped or processed (i.e. should next callback have neg. or pos. 'nCode' param.</summary>
		/// <returns>True if setup was succesful, false if this function is not supported.</returns>
		protected abstract bool TryForceCallbackSkip ( bool shouldProcess );

		protected void ExecOnHook ( HInputData[] HLData, Action act, bool shouldRetest, bool shouldReceiveEvent ) {
			EventList.Clear ();
			nint hookID = TestObject.SetHookEx ( 1, SimpleTestCallback, (IntPtr)null, 0 );
			for ( int i = 0; i < HLData.Length; i++ ) HLData[i].UpdateByHook ( TestObject, hookID );
			act ();
			TestObject.UnhookHookEx ( hookID );
			if ( shouldRetest ) act ();
			onInputReceived.WaitOne ( 100 ).Should ().Be ( shouldReceiveEvent );
			if ( !shouldReceiveEvent ) EventList.Should ().HaveCount ( 0 );
		}

		protected virtual IntPtr SimpleTestCallback ( int nCode, IntPtr wParam, IntPtr lParam ) {
			if ( nCode < 0 ) return TestObject.CallNextHook ( 0, nCode, wParam, lParam );
			EventList.Add ( TestObject.ParseHookData ( nCode, wParam, Marshal.ReadInt32 ( lParam ) ) );
			onInputReceived.Set ();
			return 1;
		}

		private void FetchHLData ( out HInputData[] HLData, out uint dataCnt, out int dataSize ) {
			HLData = GenerateKeyboardEvent ( TestObject );
			dataCnt = (uint)HLData.Length;
			dataSize = 0;
			if ( HLData != null && dataCnt > 0 ) {
				dataSize = HLData[0].SizeOf;
				for ( uint i = dataCnt - 1; i >= 1; i-- ) {
					if ( HLData[i].SizeOf != dataSize ) throw new DataMisalignedException ( "Simulating input with different Data size is not supported!" );
				}
			}
		}
	}
}