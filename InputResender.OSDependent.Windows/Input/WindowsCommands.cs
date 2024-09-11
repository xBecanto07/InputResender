using Components.Interfaces;
using Components.Library;
using InputResender.Commands;
using InputResender.WindowsGUI;
using System;
using System.Collections.Generic;

namespace InputResender.OSDependent.Windows; 
public class WindowsCommandsLoader : ACommandLoader {
	protected override string CmdGroupName => "Windows";
	protected override IReadOnlyCollection<Func<ACommand>> NewCommands => new Func<ACommand>[] {
		() => new WindowsCommands (),
	};
}

public class WindowsCommands : ACommand {
	override public string Description => "Offers access to Windows specific functionalities";

	public WindowsCommands () : base ( null ) {
		commandNames.Add ( "windows" );

		interCommands.Add ( "load" );
	}

	protected override CommandResult ExecIner ( CommandProcessor context, ArgParser args, int argID = 1 ) {
		switch ( args.String ( argID, "Action" ) ) {
		case "load":
			var actCore = context.GetVar<CoreBase> ( CoreManagerCommand.ActiveCoreVarName );
			if ( actCore.IsRegistered<DLowLevelInput> () ) {
				var llInput = actCore.Fetch<DLowLevelInput> ();
				if (llInput.GetType() == typeof(VWinLowLevelLibs)) {
					return new CommandResult ( "Windows dependencies already loaded." );
				}
				if (args.String(argID + 1, null) == "force") {
					do {
						actCore.Unregister ( actCore.Fetch<DLowLevelInput> () );
					} while ( actCore.IsRegistered<DLowLevelInput> () );
				} else return new CommandResult ( $"LowLevelInput is already registered with variant {llInput.GetType().Name}. To override, use 'force' hint." );
			}
			new VWinLowLevelLibs ( actCore );
			return new CommandResult ( "Windows dependencies loaded." );
		default: return new CommandResult ( "Invalid action." );
		}
	}
}