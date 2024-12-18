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

namespace InputResender.UnitTests.IntegrationTests;
public class BaseIntegrationTest {
	public static string[] InitCmdsList (params string[] extras) {
		List<string> ret = new () { "loadall", "safemode on", "core new", "core own", "hook manager start" };
		ret.AddRange ( extras );
		return ret.ToArray ();
	}
	public static readonly string[] GeneralInitCmds = InitCmdsList ();

	public static void ConsumeMessages () => Application.DoEvents ();
	public static bool ActiveWait ( int msTime, Func<bool> waiter ) {
		DateTime end = DateTime.Now + TimeSpan.FromMilliseconds ( msTime );
		bool ret = waiter == null;
		while ( DateTime.Now < end ) {
			if (waiter != null ) {
				if ( waiter () ) {
					ret = true;
					break;
				}
			}
			ConsumeMessages ();
			Task.Delay ( 1 ).Wait ();
		}
		ConsumeMessages ();
		return ret;
	}

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
		res.Should ().NotBeNull ().And.BeOfType<CommandResult> ();
		res.Message.Should ().Be ( expRes );
		return res;
	}

	public CommandResult AssertExecByRegex ( string cmd, string regex ) {
		var res = cliWrapper.ProcessLine ( cmd );
		res.Should ().NotBeNull ().And.BeOfType<CommandResult> ();
		res.Message.Should ().MatchRegex ( regex );
		return res;
	}
}