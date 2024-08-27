using Components.Factories;
using Components.Library;
using InputResender.Commands;
using InputResender.GUIComponents;
using System;
using System.Collections.Generic;

namespace InputResender; 
internal class TopLevelLoader : ACommandLoader {
	protected override string CmdGroupName => "TopLevel";
	protected override IReadOnlyCollection<Func<ACommand>> NewCommands => new Func<ACommand>[] {
		() => new FactoryCommandsLoader (),
		() => new GUICommands (),
	};
	protected override IReadOnlyCollection<(string, Func<ACommand, ACommand>)> NewSubCommands => new List<(string, Func<ACommand, ACommand>)> {
		( "hook", ( ACommand parent ) => {
			RegisterSubCommand ( parent, new LowLevelInputCommand ( parent ) );
			return null;
		})
	};
}