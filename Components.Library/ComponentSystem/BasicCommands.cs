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
		commandNames.Add ( "loglevel" );
		commandNames.Add ( "argerrorlvl" );
	}

	override protected CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		// Remember that these are individual commands, so ArgID will be -1 compared to 'norma' subcommand.
		switch ( context.ParentAction ) {
		case "help": return new CommandResult ( context.CmdProc.Help () );
		case "print": Print ( context[0, "Text"] ); return new CommandResult ( context[0] );
		case "info": return new CommandResult ( "InputResender v0.1" );
		case "clear": Clear (); return new CommandResult ( "clear" );
		case "exit": Exit (); return new CommandResult ( "exit" );
		case "safemode": {
			string val = context[0, "Value"];
			switch ( val.ToLower () ) {
			case "on": case "t": case "1": context.CmdProc.SafeMode = true; return new CommandResult ( "Safe mode on." );
			case "off": case "f": case "0": context.CmdProc.SafeMode = false; return new CommandResult ( "Safe mode off." );
			default: return new CommandResult ( $"Unknown value '{val}'." );
			}
		}
		case "loadall": return context.CmdProc.LoadAllCommands ();
		case "argParse": return new CommandResult ( $"Entered {context.Args.ArgC} arguments:{Environment.NewLine}{context.Args.Log ()}" );
		case "argerrorlvl": {
			var lvl = context.Args.EnumC<ArgParser.ErrLvl> ( 1, "Level" );
			context.CmdProc.ArgErrorLevel = lvl;
			return new CommandResult ( $"ArgParser error log level set to {lvl}." );
		}
		case "loglevel": {
			if ( context.CmdProc.Owner == null ) throw new InvalidOperationException ( "Owner core is not set." );
			CoreBase.LogLevel level = context.Args.EnumC<CoreBase.LogLevel> ( 1, "Level" );
			switch ( level ) {
			case CoreBase.LogLevel.None:
				context.CmdProc.Owner.LogFcn = null;
				return new CommandResult ( "Log level set to None." );
			default:
				context.CmdProc.Owner.LogFcn = ( msg ) => Print ( $"! {msg}" );
				return new CommandResult ( $"Log level set to {level} (note that only On/Off is supported at the moment)." );
			}
		}
		default: return new CommandResult ( $"Unknown action '{context.ParentAction}'." );
		}
	}
}