using System;
using System.Collections.Generic;
using Components.Interfaces;
using Components.Library;
using InputResender.Commands;
using SeClav;

namespace Components.Implementations; 
public class ScriptedInputProcessorCommand : ACommand {
	public override string Description => "Command to access scripted-based input processor.";

	private static List<string> CommandNames = ["SIP"];
	private static List<(string, Type)> InterCommands = [
		("status", null)
		, ("force", null)
		, ("assign", null)
		];

	public ScriptedInputProcessorCommand ( string parentDsc = null )
		: base (parentDsc, CommandNames, InterCommands ) {
	}

	protected override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"status" => CallName + " status: Get the current status of the Scripted Input Processor.",
			"force" => CallName + " force: Force to use SIP as input processor.",
			"assign" => CallName + " assign <ScriptName>: Assign a compiled script to the SIP.\n\tScriptName: Name of the script to assign, compiled with 'seclav parse <ScriptFile>'.",
			_ => null
		}, out var helpRes ) ) return helpRes;

		//var SCLcmder = context.CmdProc.GetCommandInstance<Interfaces.Commands.SeClavRunnerCommand> ();
		switch ( context.SubAction ) {
		case "status": {
			DMainAppCore core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
			if ( core == null ) return new CommandResult ( "No active core found." );
			var sip = core.Fetch<DInputProcessor> ();
			if ( sip == null || sip is not VScriptedInputProcessor scriptedSIP )
				return new CommandResult ( "Input Processor is not a SIP." );
			if ( scriptedSIP.Script == null )
				return new CommandResult ( "SIP running, no skript assigned." );
			return new CommandResult ( $"SIP running, assigned {(scriptedSIP.Script.IsUsingModule ( Components.Implementations.VScriptedInputProcessor.SCL_Module.ModuleName ) ? "integratable" : "non-integratable")} script '{scriptedSIP.Script.ScriptName}'." );
		}
		case "force": {
			DMainAppCore core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
			if ( core == null ) return new CommandResult ( "No active core found." );
			var sip = core.Fetch<DInputProcessor> ();
			if ( sip != null && sip is VScriptedInputProcessor )
				return new CommandResult ( "Input Processor is already SIP." );

			core.Unregister ( sip );
			var newSIP = new VScriptedInputProcessor ( core );
			return new CommandResult ( "SIP assigned as Input Processor." );
		}
		case "assign": {
			string scriptName = context.Args.String ( context.ArgID + 1, "ScriptName" );
			if ( string.IsNullOrEmpty ( scriptName ) )
				return new CommandResult ( "ScriptName cannot be empty." );

			DMainAppCore core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
			if ( core == null ) return new CommandResult ( "No active core found." );
			var sip = core.Fetch<DInputProcessor> ();
			if ( sip == null || sip is not VScriptedInputProcessor scriptedSIP )
				return new CommandResult ( "Input Processor is not a SIP." );

			var sclCmd = context.CmdProc.GetCommandInstance<Interfaces.Commands.SeClavRunnerCommand> ();
			if ( sclCmd == null )
				return new CommandResult ( "SeClavRunnerCommand not found in Command Processor." );
			var SCLcmder = sclCmd as Interfaces.Commands.SeClavRunnerCommand;

			var parsedScript = SCLcmder.TryGetParsedScript ( scriptName );
			if ( parsedScript == null )
				return new CommandResult ( $"No parsed script found with name '{scriptName}'. Please parse it first using 'seclav parse <ScriptFile>'." );

			scriptedSIP.AssignScript ( parsedScript );
			return new CommandResult ( $"Script '{scriptName}' assigned to SIP." );
		}
		default: return new CommandResult ( $"Unknown sub-action '{context.SubAction}'." );
		}
	}
}
