using Xunit;
using FluentAssertions;
using Components.Library;
using System.Collections.Concurrent;
using InputResender.CLI;
using System.Collections.Generic;
using InputResender.WindowsGUI.Commands;
using System.Windows.Forms;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Components.Implementations;
using InputResender.UnitTests.IntegrationTests;

namespace InputResender.UnitTests.SystemTests;
public class InputTransformation : BaseSystemTest {
	public InputTransformation ( ITestOutputHelper output ) : base ( output, "load sclModules", "load joiners" ) {
	}

	[Fact]
	public void ShowInputStatusInfo () {
		Test ( [
			"seclav parse SIPtest.scl",
			"SIP force",
			"SIP assign SIPtest.scl",
			"auto run setupPipes",
			"hook add Pipeline Keydown KeyUp",
			"sim keydown R",
			], [
				"ScriptedInputProcessor Status:"
				, "InputCombination Count: 1" // It would be nice if multiple results could be merged into groups, where results inside of group would be order sensitive, individual groups wouldn't have to be. This would also allow to skip lines, group would be like Line1, Line 5. Order enforecd, whatever between is skipped. This could expand possibilities of the 'exclusive' sensitivity.
			]
			, TestSensitivity.None
			, TestTimeout.Short );
	}
}