using Components.Interfaces;
using InputResender.Services;
using System.Collections.Generic;
using Components.Factories;
using RetT = InputResender.Services.ClassCommandResult<Components.Interfaces.DMainAppCore>;
using Components.Implementations;

namespace InputResender.Cmd;
public class CoreCreator : ACommand<RetT> {
	override public string Description => "Creates a new Core instance.";
	override public string Help => "CreateCore";

	public CoreCreator() {
		commandNames.Add ( "CreateCore" );
		commandNames.Add ( "newCore" );

	}

	override protected RetT ExecIner ( ArgParser args, int argID = 1 ) {
		if ( args.ArgC != 1 ) return new RetT ( null, "Invalid argument count." );
		if ( args.String ( 0, "Command" ) != "CreateCore" ) return new RetT ( null, "Invalid command." );

		var Core = DMainAppCoreFactory.CreateDefault ( ( c ) => new VInputSimulator ( c ) );
		return new RetT ( Core, "Core created." );
	}
}
