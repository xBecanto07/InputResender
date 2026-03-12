using Components.Library;
using Components.Interfaces;
using System;
using System.Collections.Generic;

namespace InputResender.CLI; 
public class DebugCommand : DCommand<DMainAppCore> {
	override public string Description => "Debugging commands";

	private static List<string> CommandNames = ["debug"];
	private static List<(string, Type)> InterCommands = [("throw", null)];

	public DebugCommand ( DMainAppCore owner, DCommand<DMainAppCore> parent = null )
		: base ( owner, parent?.CallName, CommandNames, InterCommands ) {
	}

	protected override CommandResult ExecIner ( CommandProcessor<DMainAppCore>.CmdContext context ) {
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