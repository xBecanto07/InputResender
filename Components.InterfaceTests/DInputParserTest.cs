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
			TestObject.ProcessInput ( pressEvent ).Should ().Equal ( pressEvent );
			TestObject.ProcessInput ( releaseEvent ).Should ().Contain ( releaseEvent );
			TestObject.ClearMemory ();
			TestObject.MemoryCount.Should ().Be ( 0 );
		}

		[Fact]
		public void InterleavedInput () {
			var events = GenerateInputData ( KeyCode.E, KeyCode.L );
			TestObject.ClearMemory ();
			TestObject.ProcessInput ( events[0] ).Should ().Equal ( events[0] ); // Press E
			TestObject.ProcessInput ( events[2] ).Should ().Contain ( events[2] ); // Press L
			TestObject.ProcessInput ( events[1] ).Should ().Contain ( events[1] ); // Release E
			TestObject.ProcessInput ( events[3] ).Should ().Contain ( events[3] ); // Release L
		}

		protected DataHolder GenerateInputData ( KeyCode keyCode, bool pressed ) {
			return new HKeyboardEventDataHolder ( InputReader, HookInfo, (int)keyCode, pressed ? 1 : 0 );
		}
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
		public override DInputParser GenerateTestObject () => new MInputParser ( Owner );
	}
}