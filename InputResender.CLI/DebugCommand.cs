using Components.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputResender.CLI; 
public class DebugCommand : ACommand {
	override public string Description => "Debugging commands";
	public DebugCommand ( ACommand parent = null ) : base ( parent?.CallName ) {
		commandNames.Add ( "debug" );

		interCommands.Add ( "throw" );
	}

	protected override CommandResult ExecIner ( CommandProcessor context, ArgParser args, int argID ) {
		switch ( args.String ( argID, "Action" ) ) {
		case "throw": throw new Exception ( "Debug command throw" );
		default: return new CommandResult ( $"Invalid action '{args.String ( argID, "Action" )}'." );
		}
	}
}