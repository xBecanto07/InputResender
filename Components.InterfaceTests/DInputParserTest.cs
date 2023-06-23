using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading;
using System;
using Xunit;
using DataHolder = Components.Interfaces.HInputEventDataHolder;

namespace Components.InterfaceTests {
	public abstract class DInputParserTest : ComponentTestBase<DInputParser> {
		protected DInputReader InputReader;
		protected HHookInfo HookInfo;

		public override CoreBase CreateCoreBase () {
			var ret = new CoreBaseMock ();
			InputReader = new MInputReader ( ret );
			HookInfo = new HHookInfo ( InputReader, 1, VKChange.KeyDown, VKChange.KeyUp );
			return ret;
		}

		[Fact]
		public void PressRelease () {
			var pressEvent = GenerateInputData ( KeyCode.E, true );
			var releaseEvent = GenerateInputData ( KeyCode.E, false );
			TestObject.ClearMemory ();
			TestObject.MemoryCount.Should ().Be ( 0 );
			Process ( pressEvent, true, pressEvent );
			Process ( releaseEvent, false, releaseEvent );
			TestObject.ClearMemory ();
			TestObject.MemoryCount.Should ().Be ( 0 );
		}

		[Fact]
		public void InterleavedInput () {
			var events = GenerateInputData ( KeyCode.E, KeyCode.L );
			TestObject.ClearMemory ();
			Process ( events[0], true, events[0] ); // Press E
			Process ( events[2], false, events[2] ); // Press L
			Process ( events[1], false, events[1] ); // Release E
			Process ( events[3], false, events[3] ); // Release L
		}

		protected void Process ( DataHolder newInput, bool Equal, params DataHolder[] currentState) {
			if ( Equal ) TestObject.ProcessInput ( newInput ).Should ().Equal ( currentState );
			else TestObject.ProcessInput ( newInput ).Should ().Contain ( currentState );
		}
		protected DataHolder GenerateInputData ( KeyCode keyCode, bool pressed ) {
			return new HKeyboardEventDataHolder ( InputReader, HookInfo, (int)keyCode, pressed ? 1 : 0 );
		}
		/// <summary>Generate holders, always for given KeyCode in pair of (press/release)</summary>
		protected DataHolder[] GenerateInputData ( params KeyCode[] keyCode ) {
			var ret = new DataHolder[keyCode.Length * 2];
			for (int i = 0; i < ret.Length; i+=2) {
				ret[i] = GenerateInputData ( keyCode[i / 2], true );
				ret[i+1] = GenerateInputData ( keyCode[i / 2], false );
			}
			return ret;
		}
	}

	public class MInputParserTest : DInputParserTest {
		public override DInputParser GenerateTestObject () => new MInputParser ( OwnerCore );
	}
}