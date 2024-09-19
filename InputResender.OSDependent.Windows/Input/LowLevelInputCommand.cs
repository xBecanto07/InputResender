using Components.Library;
using System.Linq;

namespace InputResender.WindowsGUI;
public class LowLevelInputCommand : ACommand {
	override public string Description => "Low level hook (Win) access.";
	public LowLevelInputCommand ( ACommand parent ) : base ( parent?.CallName ) {
		commandNames.Add ( "inpll" );
	}

	override protected CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		VWinLowLevelLibs LLInput = context.CmdProc.Owner.Fetch<VWinLowLevelLibs> ();
		if ( LLInput == null ) return new CommandResult ( "Low level input library not available." );

		switch ( context.SubAction ) {
		default: return new CommandResult ( $"Invalid action '{context.SubAction}'." );
		case "list":
			switch ( context[1, "Selector"] ) {
			default: return new CommandResult ( $"Invalid selector '{context[1]}'." );
			case "events":
				string retEv = string.Join ( ", ", VWinLowLevelLibs.EventList.Select ( e => $"{e.nCode}|{e.changeCode} = {e.inputData}" ) );
				VWinLowLevelLibs.EventList.Clear ();
				return new CommandResult ( retEv );
			case "hooks":
				return new CommandResult ( string.Join ( ", ", (LLInput.Info as VWinLowLevelLibs.VStateInfo)?.Hooks ) );
			case "errors":
				string retEr = string.Join ( ", ", LLInput.ErrorList );
				LLInput.ErrorList.Clear ();
				return new CommandResult ( retEr );
			}
		}
	}
}