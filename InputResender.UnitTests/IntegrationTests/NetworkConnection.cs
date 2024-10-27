using Xunit;
using FluentAssertions;
using System.Text.RegularExpressions;
using InputResender.Services.NetClientService.InMemNet;

namespace InputResender.UnitTests.IntegrationTests;
public class NetworkConnection {
	protected readonly BaseIntegrationTest sender, receiver;
	protected static readonly Regex InMemNetReg = new ( ".*(IMN#\\d+:\\d+)" );
	public NetworkConnection () {
		sender = initObj ();
		receiver = initObj ();

		BaseIntegrationTest initObj () {
			BaseIntegrationTest ret = new ( BaseIntegrationTest.GeneralInitCmds );
			ret.cliWrapper.ProcessLine ( "network callback newconn print" );
			ret.cliWrapper.ProcessLine ( "network callback recv print" );
			return ret;
		}
	}

	/// <summary>Cmd 'network hostlist' should return at least one INM endpoint, extract and return it</summary>
	private InMemNetPoint GetEP (BaseIntegrationTest obj) {
		var res = sender.cliWrapper.ProcessLine ( "network hostlist" );
		res.Should ().NotBeNull ();
		res.Message.Should ().NotBeNullOrWhiteSpace ();
		var regexMatch = InMemNetReg.Match ( res.Message );
		regexMatch.Success.Should ().BeTrue ();
		InMemNetPoint.TryParse ( regexMatch.Value, out var ret ).Should ().BeTrue ();
		return ret;
	}

	[Fact]
	public void HappyFlow () {
		var EPA = GetEP ( sender );
		var EPB = GetEP ( receiver );
		EPA.Should ().NotBeNull ();
		EPB.Should ().NotBeNull ();

		//sender.cliWrapper.ProcessLine("")
	}
}