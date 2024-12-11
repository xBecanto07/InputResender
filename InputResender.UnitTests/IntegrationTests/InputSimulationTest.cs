using Components.Implementations;
using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InputResender.UnitTests.IntegrationTests;
//public class BasicInputSimulationTest : InputSimulationTest {
//	protected override string[] InitCmds => Array.Empty<string> ();
//}

public class WindowsInputSimulationTest : InputSimulationTest {
	protected override string[] InitCmds => ["windows load --force", "windows msgs start"];
}

public abstract class InputSimulationTest : BaseIntegrationTest {
	public static string HookAddRegex ( string inputs ) => @"Hook added \(WinHook#\([+-]?[a-zA-Z0-9\+\*@#$%&\?€£¤]+\)\[13\]:0<" + inputs + ">\\)";
	protected abstract string[] InitCmds { get; }
	private bool HookRet = true; // Please: docs containing what this 'bool' return type means!!!
	List<HInputEventDataHolder> hookCaptures = [];
	AutoResetEvent hookWaiter = null;

	public InputSimulationTest () : base ( GeneralInitCmds ) {
		foreach ( string cmd in InitCmds ) cliWrapper.ProcessLine ( cmd );
		cliWrapper.CmdProc.SetVar ( HookManagerCommand.INPHOOKCBVarName, (object)TestHook );
	}

	[Fact]
	public void SimKeyDown () {
		// In VWinLowLevelLibs (both):
		//	private const int WH_KEYBOARD_LOW_LEVEL = 13;
		//	private const int WH_MOUSE_LOW_LEVEL = 14;
		// Hook info generated as: hookInfo[j++] = core.Fetch<DInputReader> ().PrintHookInfo ( hook );
		AssertExecByRegex ( "hook add Fcn KeyDown", HookAddRegex ( "KeyDown" ) );
		// Manual waiting for windows events will probably be needed herefg
		ConsumeMessages ();
		hookCaptures.Clear ();
		hookWaiter = new ( false );
		AssertExec ( "sim keydown E", "Sent 1 key down events." );
		ActiveWait ( 50, () => hookCaptures.Count > 0 ).Should ().BeTrue ();
		hookCaptures.Should().HaveCount ( 1 );
		AssertKeypress ( hookCaptures[0], KeyCode.E, true );
	}

	[Fact]
	public void SimKeyEvents () {
		AssertExecByRegex ( "hook add Fcn KeyDown KeyUp", HookAddRegex ( "KeyDown, KeyUp" ) );
		ConsumeMessages ();
		hookCaptures.Clear ();
		hookWaiter = new ( false );
		AssertExec ( "sim keydown F", "Sent 1 key down events." );
		AssertExec ( "sim keyup F", "Sent 1 key up events." );
		AssertExec ( "sim keypress G", "Sent 2 keyboard input (keypress) events." );
		ActiveWait ( 50, () => hookCaptures.Count > 3 ).Should ().BeTrue ();
		hookCaptures.Should ().HaveCount ( 4 );
		AssertKeypress ( hookCaptures[0], KeyCode.F, true );
		AssertKeypress ( hookCaptures[1], KeyCode.F, false );
		AssertKeypress ( hookCaptures[2], KeyCode.G, true );
		AssertKeypress ( hookCaptures[3], KeyCode.G, false );
	}

	private static void AssertKeypress ( HInputEventDataHolder e, KeyCode key, bool press ) {
		e.Should ().NotBeNull ()
		.And.BeOfType<HKeyboardEventDataHolder> ();
		var ke = e as HKeyboardEventDataHolder;
		if ( press ) ke.Pressed.Should ().BeGreaterThanOrEqualTo ( 1 );
		else ke.Pressed.Should ().BeLessThan ( 1 );
		e.InputCode.Should ().Be ( (int)key );
	}

	private bool TestHook ( HInputEventDataHolder e ) {
		hookCaptures.Add ( e );
		return false; // False means that input event should be consumed instead of resending to another hook
	}
}