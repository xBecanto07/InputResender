using System;
using Components.Library;

namespace InputResender.Commands; 
internal class BasicCommands : ACommand<CommandResult> {
	override public string Description => "Basic commands.";
	override public string Help => $"{parentCommandHelp} ({string.Join ( "|", commandNames )})";

	private readonly Action<string> Print;
	private readonly Action Clear;
	private readonly Action Exit;

	public BasicCommands ( Action<string> print, Action clear, Action exit ) : base ( null ) {
		Print = print;
		Clear = clear;
		Exit = exit;

		commandNames.Add ( "help" );
		commandNames.Add ( "print" );
		commandNames.Add ( "info" );
		commandNames.Add ( "clear" );
		commandNames.Add ( "exit" );
	}

	override protected CommandResult ExecIner ( ICommandProcessor context, ArgParser args, int argID = 1 ) {
		argID--;
		string act = args.String ( argID, "Command" );
		if ( act == "help" ) {
			return new CommandResult ( context.Help () );
		} else if ( act == "print" ) {
			Print ( args.String ( argID + 1, "Text" ) );
			return new CommandResult ( args.String ( argID + 1, "Text" ) );
		} else if ( act == "info" ) {
			return new CommandResult ( "InputResender v0.1" );
		} else if ( act == "clear" ) {
			Clear ();
			return new CommandResult ( "clear" );
		} else if ( act == "exit" ) {
			Exit ();
			return new CommandResult ( "exit" );
		} else {
			return new CommandResult ( $"Unknown action '{act}'." );
		}
	}
}
