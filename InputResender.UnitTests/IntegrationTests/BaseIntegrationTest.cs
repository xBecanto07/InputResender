using Xunit;
using FluentAssertions;
using Components.Library;
using System.Collections.Concurrent;
using InputResender.CLI;
using System.Collections.Generic;
using InputResender.WindowsGUI.Commands;

namespace InputResender.UnitTests.IntegrationTests;
public class BaseIntegrationTest {
	public static readonly string[] GeneralInitCmds = {
		"loadall", "safemode on", "core new", "core own", "hook manager start"
	};

	private readonly BlockingCollection<string> StdIn, StdOut;
	public ConsoleManager console;
	public CliWrapper cliWrapper;
	public readonly List<CommandResult> CommandResults = new ();

	public BaseIntegrationTest ( params string[] initCmds ) {
		StdIn = new ();
		StdOut = new ();
		console = new ( StdOut.Add, StdIn.Take, Write, null, null );
		cliWrapper = new ( console );

		cliWrapper.CmdProc.SetVar ( CliWrapper.CLI_VAR_NAME, cliWrapper );
		cliWrapper.CmdProc.AddCommand ( new BasicCommands ( console.WriteLine, console.Clear, () => throw new System.NotImplementedException () ) );
		cliWrapper.CmdProc.AddCommand ( new FactoryCommandsLoader () );
		cliWrapper.CmdProc.AddCommand ( new InputCommandsLoader () );
		cliWrapper.CmdProc.AddCommand ( new TopLevelLoader () );

		foreach ( string cmd in initCmds ){
			var res = cliWrapper.ProcessLine ( cmd );
			CommandResults.Add ( res );
		}
	}

	private void Write ( string text ) {
		if ( !StdOut.TryTake ( out string oldLine ) ) oldLine = string.Empty;
		oldLine += text;
		StdOut.Add ( oldLine );
	}

	public CommandResult AssertExec (string cmd, string expRes) {
		var res = cliWrapper.ProcessLine ( cmd );
		res.Should ().NotBeNull ();
		res.Message.Should ().Be ( expRes );
		return res;
	}
}