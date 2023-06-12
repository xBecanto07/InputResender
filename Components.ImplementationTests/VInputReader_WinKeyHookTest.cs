﻿using Components.Implementations;
using Components.InterfaceTests;
using Components.Interfaces;
using Components.Library;
using Xunit;

namespace Components.ImplementationTests {
	public class VInputReader_WinKeyHookTest : DInputReaderTest {
		MLowLevelInput LowLevelInput;

		public VInputReader_WinKeyHookTest () : base () { }

		public override CoreBase CreateCoreBase () {
			var ret = new CoreBaseMock ();
			LowLevelInput = new MLowLevelInput ( ret );
			return ret;
		}
		public override DInputReader GenerateTestObject () => new VInputReader_KeyboardHook ( Owner );
	}
}