using Components.Implementations;
using Components.Interfaces.Commands;
using Components.Library;
using InputResender.Commands;
using System;
using System.Collections.Generic;

namespace Components.Factories; 
public class FactoryCommandsLoader : ACommandLoader {
	protected override string CmdGroupName => "compfactory";
	protected override IReadOnlyCollection<Func<ACommand>> NewCommands => new Func<ACommand>[] {
		() => new CoreManagerCommand (),
		() => new ConnectionManagerCommand (),
		() => new ComponentCommandLoader ()
	};
	protected override IReadOnlyCollection<(string, Func<ACommand, ACommand>)> NewSubCommands => new List<(string, Func<ACommand, ACommand>)> {
		( "core", ( ACommand parent ) => {
			if ( parent is CoreManagerCommand cmdCore )
				RegisterSubCommand ( cmdCore, new CoreCreatorCommand () );
			return null;
		})
	};

	/*public override string Description => "Loads commands controling the component factory";
	public override string Help => $"{parentCommandHelp} {commandNames.First ()}";

	public FactoryCommandsLoader ( string parentHelp = null ) : base ( parentHelp ) => commandNames.Add ( CommandLoader.BaseLoadCmdName + "-factory" );

	protected override CommandResult ExecIner ( ICommandProcessor context, ArgParser args, int argID ) {
		context.AddCommand ( new ComponentCommandLoader () );
		context.AddCommand ( new NetworkManagerCommand () );
		context.AddCommand ( new CommandLoader () );
		return new CommandResult ( "Factory commands loaded." );
	}*/
}