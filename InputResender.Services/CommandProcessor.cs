using System;
using System.Collections.Generic;

namespace InputResender.Services;
public class CommandProcessor {
	public readonly HashSet<ICommand> SupportedCommands = new ();
	private Action<string> WriteLine;

	public CommandProcessor ( Action<string> writeLine ) {
		WriteLine = writeLine;
	}

	public CommandResult ProcessLine (string line) {
		ArgParser args = new ( line, WriteLine );
		if ( args.ArgC == 0 ) return new CommandResult ( null );

		var cmd = ICommand.Search ( args, SupportedCommands );
		if ( cmd == null ) {
			WriteLine ( $"Command '{args.String ( 0, "Command" )}' not found." );
			return new CommandResult ( null );
		}

		return cmd.Execute ( args );
	}
}