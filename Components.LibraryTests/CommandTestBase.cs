using FluentAssertions;
using Components.Library;
using System;
using InputResender.Commands;

namespace Components.LibraryTests; 
public class CommandTestBase {
	protected CommandProcessor CmdProc;
	System.Text.StringBuilder SB = new ();

	public CommandTestBase (ACommand testedCmd) {
		CmdProc = new ( (s) => SB.AppendLine ( s ) );
		CmdProc.AddCommand ( testedCmd );
	}

	public CommandResult AssertCorrectMsg ( string line, string expected ) {
		var res = CmdProc.ProcessLine ( line );
		try {
			res.Should ().NotBeNull ().And.NotBeOfType<ErrorCommandResult> ();
		} catch {
			throw new Exception ( $"Expected valid response, but got error: {((ErrorCommandResult)res).Message}" );
		}
		res.Message.Should ().Be ( expected );
		return res;
	}

	public ErrorCommandResult AssertError ( string line, string expected ) {
		var res = CmdProc.ProcessLine ( line );
		res.Should ().NotBeNull ().And.BeOfType<ErrorCommandResult> ();
		res.Message.Should ().Be ( expected );
		return (ErrorCommandResult) res;
	}

	public void AssertThrow<ExT> (string line, string errMsg) where ExT : Exception {
		CommandResult res = null;
		try {
			res = CmdProc.ProcessLine ( line );
		} catch (ExT e) {
			e.Message.Should ().Be ( errMsg );
			return;
		}
		// If exception was not thrown, it might be processed and returned as ErrorCOmmandResult
		res.Should ().NotBeNull ().And
			.BeOfType<ErrorCommandResult> ().Which
			.Message.Should ().Be ( errMsg );
	}

	protected void AssertMissingCore ( string cmd ) => AssertThrow<ArgumentException> ( cmd, $"Variable '{CoreManagerCommand.ActiveCoreVarName}' not found." );
}