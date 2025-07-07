using Components.Interfaces;
using Components.InterfaceTests;
using Components.Library;
using Components.LibraryTests;
using FluentAssertions;
using GUIComponentTests;
using InputResender.WindowsGUI;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace InputResender.GUIComponentTests; 
public class WinLLInputStatusExtraTest : CInputLLParserTest<WinLLInputStatusExtra> {
	DateTime? firstInputTime = null;

	public WinLLInputStatusExtraTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) { }
	protected override CInputLLParser.InputEventParser GetParser () => new WinLLInputStatusExtra.WinLLInputStatusParser ();

	protected override void AssertLoadedData ( WinLLInputStatusExtra info ) {
		HWInputTest.Assert ( info.inputData, extraInfo: info.StatusPtr );
		info.ShouldProcess.Should().BeTrue ();
		info.TimeOfRegistration.Should ().BeInRange (
			HWInput.TimeConvert ( firstInputTime.Value ),
			HWInput.TimeConvert ( DateTime.Now ) );
		info.UID.Should ().BeGreaterThanOrEqualTo ( 42 );
		Marshal.ReadInt32 ( info.StatusPtr ).Should ().Be ( WinLLInputStatusExtra.MARK );
	}

	protected override nint GenerateInputMemory () {
		firstInputTime ??= DateTime.Now;
		return HWInputTest.GenerateInputMemory ( null );
	}
}