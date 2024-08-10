using System;

namespace Components.Library; 
public class BasicCommands : ACommand<CommandResult> {
	override public string Description => "Basic commands.";
	override public string Help => "help - Show this help." + Environment.NewLine +
		"info - Show info." + Environment.NewLine +
		"print <Text> - Print text." + Environment.NewLine +
		"clear - Clear screen." + Environment.NewLine +
		"exit - Exit program." + Environment.NewLine +
		"safemode <on|off> - Enable or disable safe mode." + Environment.NewLine +
		"loadall - Load all commands." + Environment.NewLine;

	private readonly Action<string> Print;
	private readonly Action Clear;
	private readonly Action Exit;

	public BasicCommands ( Action<string> print, Action clear, Action exit ) : base ( null ) {
		Print = print;
		Clear = clear;
		Exit = exit;

		commandNames.Add ( "safemode" );
		commandNames.Add ( "help" );
		commandNames.Add ( "print" );
		commandNames.Add ( "info" );
		commandNames.Add ( "clear" );
		commandNames.Add ( "exit" );
		commandNames.Add ( "loadall" );
		commandNames.Add ( "argParse" );
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
		} else if ( act == "safemode" ) {
			string val = args.String ( argID + 1, "Value" );
			switch ( val.ToLower () ) {
			case "on": case "t": case "1": context.SafeMode = true; return new CommandResult ( "Safe mode on." );
			case "off": case "f": case "0": context.SafeMode = false; return new CommandResult ( "Safe mode off." );
			default: return new CommandResult ( $"Unknown value '{val}'." );
			}
		} else if ( act == "loadall" ) {
			return context.LoadAllCommands ();
		} else if ( act == "argParse" ) {
			// Debug only command, don't add to help
			return new CommandResult ( $"Entered {args.ArgC} arguments:{Environment.NewLine}{args.Log ()}" );
		} else {
			return new CommandResult ( $"Unknown action '{act}'." );
		}
	}
}