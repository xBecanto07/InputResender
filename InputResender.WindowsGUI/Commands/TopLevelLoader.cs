using Components.Library;
using InputResender.OSDependent.Windows;
using InputResender.WindowsGUI;
using System;
using System.Collections.Generic;
using Components.Interfaces;

namespace InputResender.WindowsGUI.Commands;
public class TopLevelLoader : ACommandLoader<DMainAppCore> {
	readonly ConsoleManager consoleManager;
	public TopLevelLoader ( DMainAppCore owner, ConsoleManager console = null)
		: base ( owner, "TopLevel") { consoleManager = console; }

	private static Dictionary<Type, Func<DMainAppCore, DCommand<DMainAppCore>>> NewCommandList ( TopLevelLoader self ) => new () {
		{ typeof(CLI.FactoryCommandsLoader), ( core ) => new CLI.FactoryCommandsLoader ( core )},
		{ typeof(GUICommands), ( core ) => new GUICommands ( core )},
		{ typeof(ComponentVisualizer.ComponentVisualizerCommands), ( core ) => new ComponentVisualizer.ComponentVisualizerCommands ( core )},
		{ typeof ( WindowsCommands ), ( core ) => new WindowsCommands ( core, self.consoleManager )},
	};
	private static Dictionary<Type, (string, Func<DCommand<DMainAppCore>, DCommand<DMainAppCore>>)> NewSubCommandList = new () {
		{ typeof (LowLevelInputCommand), ("hook", ( DCommand<DMainAppCore> parent ) => {
			RegisterSubCommand ( parent, new LowLevelInputCommand ( parent ) );
			return null;
		}) },
	};

	protected override IReadOnlyCollection<Func<DMainAppCore, DCommand<DMainAppCore>>> NewCommands => NewCommandList ( this ).Values;
	protected override IReadOnlyCollection<(string, Func<DCommand<DMainAppCore>, DCommand<DMainAppCore>>)> NewSubCommands => NewSubCommandList.Values;
}