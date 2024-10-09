using Components.Implementations;
using Components.Interfaces.Commands;
using Components.Library;
using Components.Library.ComponentSystem;
using InputResender.Commands;
using System;
using System.Collections.Generic;

namespace InputResender.CLI; 
public class FactoryCommandsLoader : ACommandLoader {
	protected override string CmdGroupName => "generalCmds";
	protected override IReadOnlyCollection<Func<ACommand>> NewCommands => [
		() => new CoreManagerCommand (),
		() => new ConnectionManagerCommand (),
		() => new ComponentCommandLoader (),
		() => new ContextVarCommands (),
		() => new DebugCommand ()
	];
	protected override IReadOnlyCollection<(string, Func<ACommand, ACommand>)> NewSubCommands => new List<(string, Func<ACommand, ACommand>)> {
		( "core", ( ACommand parent ) => {
			if ( parent is CoreManagerCommand cmdCore )
				RegisterSubCommand ( cmdCore, new CoreCreatorCommand () );
			return null;
		})
	};
}

public class InputCommandsLoader : ACommandLoader {
	protected override string CmdGroupName => "inputCmds";
	protected override IReadOnlyCollection<Func<ACommand>> NewCommands => [
		() => new InputSimulatorCommand (),
		() => new HookManagerCommand ()
	];
}