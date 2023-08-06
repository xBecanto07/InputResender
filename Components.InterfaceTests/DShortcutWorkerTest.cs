using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Components.InterfaceTests {
	public abstract class DShortcutWorkerTest : ComponentTestBase<DShortcutWorker> {
		protected int ExecCnt = 0;
		protected const KeyCode DefKey = KeyCode.E;
		protected const InputData.Modifier DefMod = InputData.Modifier.Shift;

		protected DShortcutWorkerTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) { }

		[Fact]
		public void RegisterExecUnregister () {
			ExecCnt = 0;
			InputData inputData = new InputData ( TestObject, DefKey, true, DefMod );
			Action cb = () => ExecCnt++;
			TestObject.Register ( DefKey, DefMod, cb, "Press counter" );
			ExecCnt.Should ().Be ( 0 );
			TestObject.Exec ( inputData ).Should ().BeTrue ();
			ExecCnt.Should ().Be ( 1 );
			TestObject.Unregister ( DefKey, DefMod, cb );
			TestObject.Exec ( inputData ).Should ().BeFalse ();
			ExecCnt.Should ().Be ( 1 );
		}
		[Fact]
		public void NullActionThrows_ArgumentNullException () {
			Action act = () => TestObject.Register ( DefKey, DefMod, null, "Press counter" );
			act.Should ().Throw<ArgumentNullException> ();
		}
		[Fact]
		public void NullDescriptionThrows_ArgumentNullException () {
			Action cb = () => ExecCnt++;
			Action act = () => TestObject.Register ( DefKey, DefMod, cb, null );
			act.Should ().Throw<ArgumentNullException> ();
		}
	}
}
