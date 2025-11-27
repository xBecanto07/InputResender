using Components.Library;
using InputResender.OSDependent.Windows;
using InputResender.WindowsGUI;
using System;
using System.Collections.Generic;

namespace InputResender.WindowsGUI.Commands;
public class TopLevelLoader : ACommandLoader {
	readonly ConsoleManager consoleManager;
	public TopLevelLoader (ConsoleManager console = null) : base ("TopLevel") { consoleManager = console; }

	private static Dictionary<Type, Func<ACommand>> NewCommandList ( TopLevelLoader self ) => new () {
		{ typeof(CLI.FactoryCommandsLoader), ()=> new CLI.FactoryCommandsLoader ()},
		{ typeof(GUICommands), () => new GUICommands ()},
		{ typeof(ComponentVisualizer.ComponentVisualizerCommands), () => new ComponentVisualizer.ComponentVisualizerCommands ()},
		{ typeof ( WindowsCommands ), () => new WindowsCommands ( self.consoleManager )},
	};
	private static Dictionary<Type, (string, Func<ACommand, ACommand>)> NewSubCommandList = new () {
		{ typeof (LowLevelInputCommand), ("hook", ( ACommand parent ) => {
			RegisterSubCommand ( parent, new LowLevelInputCommand ( parent ) );
			return null;
		}) },
	};

	protected override IReadOnlyCollection<Func<ACommand>> NewCommands => NewCommandList ( this ).Values;
	protected override IReadOnlyCollection<(string, Func<ACommand, ACommand>)> NewSubCommands => NewSubCommandList.Values;
}