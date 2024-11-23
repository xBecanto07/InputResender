using Components.Library;
using System;

namespace InputResender.CLI; 
public class DebugCommand : ACommand {
	override public string Description => "Debugging commands";
	public DebugCommand ( ACommand parent = null ) : base ( parent?.CallName ) {
		commandNames.Add ( "debug" );

		interCommands.Add ( "throw" );
	}

	protected override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"throw" => "debug throw: Throws an exception",
			_ => null
		}, out var helpRes ) ) return helpRes;
		switch ( context.SubAction ) {
		case "throw": throw new Exception ( "Debug command throw" );
		default: return new CommandResult ( $"Invalid action '{context.SubAction}'." );
		}
	}
}