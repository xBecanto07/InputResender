using Components.Library;
using System.Linq;

namespace InputResender.WindowsGUI;
public class LowLevelInputCommand : ACommand {
	override public string Description => "Low level hook (Win) access.";
	public LowLevelInputCommand ( ACommand parent ) : base ( parent?.CallName ) {
		commandNames.Add ( "inpll" );
	}

	override protected CommandResult ExecIner ( CommandProcessor context, ArgParser args, int argID ) {
		VWinLowLevelLibs LLInput = context.Owner.Fetch<VWinLowLevelLibs> ();
		if ( LLInput == null ) return new CommandResult ( "Low level input library not available." );

		switch ( args.String ( argID + 1, "Action" ) ) {
		default: return new CommandResult ( $"Invalid action '{args.String ( argID + 1, "Action" )}'." );
		case "list":
			switch ( args.String ( argID + 2, "Selector" ) ) {
			default: return new CommandResult ( $"Invalid selector '{args.String ( argID + 2, "Selector" )}'." );
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