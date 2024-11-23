using Components.Library;
using Components.LibraryTests;
using FluentAssertions;
using InputResender.CLI;
using InputResender.UnitTests.IntegrationTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using SBld = System.Text.StringBuilder;
using Outputter = Xunit.Abstractions.ITestOutputHelper;

[assembly: TestFramework ( "InputResender.UnitTests.ParallelXunitRunner", "InputResender.UnitTests" )]
namespace InputResender.UnitTests;
public class GlobalCommandTest : BaseIntegrationTest {
	static readonly GlobalCommandList CommandList = new GlobalCommandList ();
	public Outputter Output;
	/*
	HELP EXAMPLE:
	if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => $"sim mousemove [-x <Xaxis>] [-y <Yaxis>]: Simulate mouse move event\n\t-x <Xaxis> = 0: X axis movement\n\t-y <Yaxis> = 0: Y axis movement", out var helpRes ) ) return helpRes;

	if ( TryPrintHelp ( context.Args, context.ArgID + 1, () =context.SubAction switch {
		"act" => "Prints the name of the active core.",
		"typeof" => "Prints the type of the component of thactive core.",
		_ => null
	}, out var helpRes ) ) return new ( null, helpRes.Message );

	sim (mousemove|keydown|keyup|keypress)
	  - Can simulate user hardware input
		EndPoint: IP end point or InMemNet point" to contain "tEP set ".
		-x <Xaxis> = 0: X axis movement
		-x <Xaxis>: X axis movement
		-x: X axis movement
		--XAxis <Xaxis> = 0: X axis movement
	  - Can simulate user hardware input

	.*?[a-zA-Z]+? .*?(( \([a-zA-Z]+?(\|[a-zA-Z]+?)*\))|( <\S+?>))+?.*?((
	\s+ - [a-zA-Z]+.*)|(
	\s+[a-zA-Z]+: .*)|(
	\s+((-[a-z])|(--[A-Z][a-zA-Z]+))( <[a-zA-Z]+>)?( = \S+)?: \S.*))+
		*/
	const string ascii = "[a-zA-Z_-]+";
	const string asciiSpace = $"{ascii}( {ascii})*";
	const string arg1 = $" \\({ascii}(\\|{ascii})*\\)";
	const string arg2 = $" <\\S+?>";
	const string sw = $"((-[a-z])|(--[A-Z][a-zA-Z]+)|(<{ascii}>))";
	const string line1 = $"{callname}.*?(({arg1})|({arg2}))*"; // Not restrictive enough. Since this is getting realllly large, custom regex processor is getting more and more tempting.
	const string line2 = $"\\n\\s+- {asciiSpace}.*";
	const string line3 = $"\\n\\s+{asciiSpace}: .*";
	const string line5 = $"\\n\\s+[+>] .*";
	const string line4 = $"\\n\\s+{sw}({arg2})*( = \\S+)?: \\S.*";
	public static readonly Regex helpRegex = new ( $"^{line1}((: [\\S ]*)|({line2})|({line3})|({line5})|({line4}))+$", RegexOptions.ExplicitCapture );

	const string callname = $"Usage:( ({ascii})(\\[\\.(\\|({ascii}))\\])?)+";
	public static readonly Regex callnameRegex = new ( callname );

	public GlobalCommandTest ( Outputter output ) : base ( GeneralInitCmds ) {
		Output = output;
	}

	[Fact]
	public void AllCommandsAreTested () {
		List<string> missing = new ();
		foreach ( var cmdCls in CommandList.AllCommandTypes )
			if ( !CommandList.CommandList.ContainsKey ( cmdCls ) ) missing.Add ( $" - {cmdCls}" );
		if ( missing.Any () ) PrintProblematicTests ( "are not in the list of tested commands" );

		foreach ( var cmdInfo in CommandList.CommandList )
			if ( !cmdInfo.Value.Any () ) missing.Add ( $" - {cmdInfo.Key}" );
		if ( missing.Any () ) PrintProblematicTests ( "do not have any tested command lines" );

		void PrintProblematicTests ( string errMsg ) {
			string missingInfo = string.Join ( '\n', missing );
			string allTested = string.Join ( '\n', CommandList.CommandList.Keys.Select ( T => $" - {T}" ) );
			Assert.Fail ( $"The following commands {errMsg}:\n{missingInfo}\nCurrently tested commands:\n{allTested}" );
		}
	}

	[Fact]
	public void AllCommandsCanBeLoaded () {
		List<Type> loaded = [typeof ( FactoryCommandsLoader ), typeof ( InputCommandsLoader ), typeof ( BasicCommands )];
		List<Type> waiting = CommandList.AllCommandTypes.Select ( t => t ).ToList ();
		waiting.AddRange ( CommandList.AllLoaders );
		foreach ( var T in loaded ) waiting.Remove ( T );

		while ( waiting.Any () ) {
			bool changed = false;
			foreach ( var loader in CommandList.Loaders ) {
				if ( !loaded.Contains ( loader.Key ) ) continue;
				foreach ( var cmdT in loader.Value ) {
					if ( loaded.Contains ( cmdT ) ) continue;
					if ( !waiting.Contains ( cmdT ) ) continue;
					changed = true;
					loaded.Add ( cmdT );
					waiting.Remove ( cmdT );

				}
			}
			if ( !changed ) Assert.Fail ( "Following commands can never be loaded:" + string.Join ( '\n', waiting.Select ( T => $" - {T}" ) ) );
		}
	}

	static void AssertSubcommandHelp (string helpMsg) {
		string[] lines = helpMsg.Replace ( "\r\n", "\n" ).Split ( '\n' );
		SBld SB = null;

		for ( int i = 0; i < lines.Length; i++ ) {
			if ( SB != null ) {
				if ( lines[i].StartsWith ( " > " ) )
					SB.AppendLine ( lines[i][3..] );
				else {
					AssertHelpMsg ( null, SB.ToString () );
					SB.Clear ();
					SB = null;
				}
			}
			if ( SB == null ) {
				if ( !lines[i].StartsWith ( " +" ) ) continue;
				lines[i].Should ().StartWith ( " + Usage: ", "no line other than start of sub-command should start with '+' sign" );
				SB = new ( lines[i][3..] );
			}
		}
		if ( SB != null ) AssertHelpMsg ( null, SB.ToString () );
	}

	static void AssertHelpMsg ( string command, string helpMsg ) {
		helpMsg.Should ().NotBeNullOrWhiteSpace ().And
			.MatchAnyRegex ( [helpRegex] );
		if ( !string.IsNullOrWhiteSpace ( command ) )
			helpMsg.Should ().MatchRegex ( callnameRegex, command.Split ( ' ' ) );
		AssertSubcommandHelp ( helpMsg );
	}

	[Theory]
	[MemberData ( nameof ( CommandIterator ) )]
	public void HelpAvailable_P ( string command ) {
		string helpMsg = null;
		command += ' ';
		foreach ( string hs in ACommand.HelpSwitches ) {
			string cmd = command + hs;
			if ( helpMsg == null ) Output?.WriteLine ( $" > {cmd}" );
			var res = cliWrapper.ProcessLine ( cmd );
			res.Should ().NotBeNull ().And.NotBeOfType<ErrorCommandResult> ();

			if ( helpMsg == null ) {
				AssertHelpMsg ( command, res.Message );

				Output?.WriteLine ( res.Message );
				helpMsg = res.Message;
			} else res.Message.Should ().Be ( helpMsg );
		}
	}

	public static IEnumerable<object[]> CommandIterator () {
		foreach ( var comm in CommandList.CommandList ) {
			foreach ( var cmd in comm.Value ) {
				yield return [cmd];
			}
		}
	}
}