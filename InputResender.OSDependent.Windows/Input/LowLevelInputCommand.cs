using Components.Library;
using System.Linq;

namespace InputResender.WindowsGUI;
public class LowLevelInputCommand : ACommand {
	override public string Description => "Low level hook (Win) access.";
	override public string Help => $"Usage: {parentCommandHelp} inpll list <Selector> - {Description}\n\tSelector: (events|hooks|errors)";
	protected override bool PrintHelpOnEmpty => true;

	public LowLevelInputCommand ( ACommand parent ) : base ( parent?.CallName ) {
		commandNames.Add ( "inpll" );

		interCommands.Add ( "list" );
	}

	override protected CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if (TryPrintHelp (context.Args, context.ArgID+ 1, () => $"{context[-2]} inpll list <Selector>\n\tSelector: (events|hooks|errors)", out var helpRes ) ) return helpRes;

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