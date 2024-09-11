using Components.Library;
using InputResender.OSDependent.Windows;
using InputResender.WindowsGUI;
using System;
using System.Collections.Generic;

namespace InputResender.WindowsGUI.Commands;
internal class TopLevelLoader : ACommandLoader {
	protected override string CmdGroupName => "TopLevel";
	protected override IReadOnlyCollection<Func<ACommand>> NewCommands => new Func<ACommand>[] {
		() => new GUICommands (),
		() => new WindowsCommands (),
	};
	protected override IReadOnlyCollection<(string, Func<ACommand, ACommand>)> NewSubCommands => new List<(string, Func<ACommand, ACommand>)> {
		( "hook", ( ACommand parent ) => {
			RegisterSubCommand ( parent, new LowLevelInputCommand ( parent ) );
			return null;
		})
	};
}