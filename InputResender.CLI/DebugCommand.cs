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
		switch ( context.SubAction ) {
		case "throw": throw new Exception ( "Debug command throw" );
		default: return new CommandResult ( $"Invalid action '{context.SubAction}'." );
		}
	}
}