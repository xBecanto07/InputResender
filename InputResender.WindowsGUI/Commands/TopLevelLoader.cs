using Components.Library;
using InputResender.OSDependent.Windows;
using InputResender.WindowsGUI;
using System;
using System.Collections.Generic;

namespace InputResender.WindowsGUI.Commands;
public class TopLevelLoader : ACommandLoader {
	readonly ConsoleManager consoleManager;
	public TopLevelLoader (ConsoleManager console = null) { consoleManager = console; }

	protected override string CmdGroupName => "TopLevel";
	protected override IReadOnlyCollection<Func<ACommand>> NewCommands => [
		() => new GUICommands (),
		() => new ComponentVisualizer.ComponentVisualizerCommands (),
		() => new WindowsCommands ( consoleManager ),
	];
	protected override IReadOnlyCollection<(string, Func<ACommand, ACommand>)> NewSubCommands => new List<(string, Func<ACommand, ACommand>)> {
		( "hook", ( ACommand parent ) => {
			RegisterSubCommand ( parent, new LowLevelInputCommand ( parent ) );
			return null;
		})
	};
}