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
public class InputTransformationTest : BaseSystemTest {
	public InputTransformationTest ( ITestOutputHelper output ) : base ( output, "load sclModules", "load joiners" ) {
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

	[Fact]
	public void ShowInputStatusInfoInline () {
		Test ( [
			@"seclav parse SIPtest.scl --inline=""
			@using BasicModule
			@using ScriptedInputProcessor

			@in SIP_Status_t sip_status
			PRINT_SIP_STATUS sip_status""",
			"SIP force",
			"SIP assign SIPtest.scl",
			"auto run setupPipes",
			"hook add Pipeline Keydown KeyUp",
			"sim keydown R",
			], [
				"ScriptedInputProcessor Status:"
				, "InputCombination Count: 1"
			]
			, TestSensitivity.None
			, TestTimeout.Short );
	}

	[Fact]
	public void InputPassthrough () {
		Test ( [
			@"seclav parse SIPtest.scl --inline=""
			@using BasicModule
			@using ScriptedInputProcessor

			@in SIP_Status_t sip_status
			PRINT_SIP_STATUS sip_status
			String keyName = GET_SIP_KEY_NAME sip_status 0
			Int pressState = GET_SIP_KEY_STATUS sip_status keyName
			FIRE_KEY sip_status keyName pressState
			""",
			"SIP force",
			"SIP assign SIPtest.scl",
			"auto run setupPipes",
			"hook add Print Keydown KeyUp",
			"sim keydown R",
			], [
				"Requested simulating input of 0.82 (R)[65K;0;0] Δ[65K;0;0]",
				"Processing event: 0.82 (R)[65K;0;0] Δ[65K;0;0]",
				"ScriptedInputProcessor Status:",
				"Firing key 'R' - Pressed: True",
				"Encountered Input Event: hook catched R (1) : 0:[KeyDown, KeyUp]",
			]
			, TestSensitivity.None
			, TestTimeout.Short );
	}
}