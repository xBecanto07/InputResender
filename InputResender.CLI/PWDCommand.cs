using Components.Library;
using System;
using System.Collections.Generic;

namespace InputResender.CLI;
public class PWDCommand : ACommand {
	public override string Description => "Command for Working Directory management";
	private static List<string> CommandNames = ["pwd"];
	private static List<(string, Type)> InterCommands = [
		("set", null)
		];

	public PWDCommand ( string parentDsc = null )
		: base ( parentDsc, CommandNames, InterCommands ) { }

	protected override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if ( context.Args.ArgC < context.ArgID + 1 )
			return new CommandResult ( "HomePath=" + Config.HomePath );

		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"set" => CallName + " set <Path>: Set the working directory\n\tPath: The new working directory path",
			_ => CallName + ": Show the current working directory"
		}, out var helpRes ) ) return helpRes;
		switch ( context.SubAction ) {
		case "set": {
			string path = context.Args.String ( context.ArgID + 1, "Path" );
			if ( string.IsNullOrEmpty ( path ) )
				return new CommandResult ( "Path cannot be empty." );
			try {
				Config.HomePath = path;
				return new CommandResult ( $"Working directory set to '{Config.HomePath}'." );
			} catch ( Exception ex ) {
				return new CommandResult ( $"Failed to set working directory to '{path}': {ex.Message}" );
			}
		}
		default:
			return new CommandResult ( $"Unknown subcommand '{context.SubAction}'." );
		}
	}
}