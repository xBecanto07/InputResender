using Components.Implementations;
using Components.Interfaces;
using Components.Interfaces.Commands;
using Components.Library;
using Components.Library.ComponentSystem;
using InputResender.CLI;
using InputResender.Commands;
using System;
using System.Collections.Generic;
using System.Reflection;
using InputResender.WindowsGUI.Commands;
using InputResender.WindowsGUI;
using InputResender.OSDependent.Windows;

namespace InputResender.UnitTests; 
internal class GlobalCommandList {
	public readonly Dictionary<Type, List<string>> CommandList = new () {
		{ typeof (ConnectionManagerCommand), ["conns", "conns list", "conns send", "conns close", "conns callback"] },
		{ typeof (CoreCreatorCommand), ["core new", "core create"] },
		{ typeof ( HookManagerCommand ), ["hook", "hook manager", "hook manager start", "hook manager status", "hook list", "hook add", "hook remove"] },

		{ typeof ( InputSimulatorCommand ), ["sim", "sim mousemove", "sim keydown", "sim keyup", "sim keypress"] },
		{ typeof (PasswordManagerCommand), ["password", "pw", "password add", "pw add", "password print", "pw print"] },
		{ typeof (TargetManagerCommand), ["target", "tEP", "target set", "tEP set"] },
		{ typeof (HookCallbackManagerCommand), ["hookcb", "hookcb list", "hookcb set", "hookcb active"] },
		{ typeof ( NetworkManagerCommand ), ["network", "network hostlist", "network conn", "network callback", "network info"] },
		{ typeof (ListHostsNetworkCommand), ["network hostlist"] },
		{ typeof (NetworkConnsManagerCommand), ["network conn", "network conn list", "network conn send"] },
		{ typeof (NetworkCallbacks), ["network callback", "network callback list", "network callback recv", "network callback newconn"] },
		{ typeof (EndPointInfoCommand), ["network info"] },

		{ typeof ( BasicCommands ), ["safemode", "loadall", "argParse", "argerrorlvl", "loglevel"] },
		{ typeof (ContextVarCommands), ["context", "context set", "context get", "context reset", "context add"] },
		{ typeof ( CoreManagerCommand ), ["core", "core act", "core typeof"] },

		{ typeof (DebugCommand), ["debug"] },
		{ typeof (GUICommands), ["gui", "gui start", "gui stop"] },
		{ typeof (ComponentVisualizer.ComponentVisualizerCommands), ["visualizer", "visualizer start", "visualizer stop", "visualizer update", "visualizer status"] },
		{ typeof (WindowsCommands), ["windows", "windows load"] },
		{ typeof (LowLevelInputCommand), ["hook inpll", "hook inpll list"] }
	};

	public readonly Dictionary<Type, List<Type>> Loaders = new () {
		{typeof(TopLevelLoader), [typeof(GUICommands), typeof(ComponentVisualizer.ComponentVisualizerCommands), typeof(WindowsCommands), typeof (LowLevelInputCommand)] },
		{typeof(FactoryCommandsLoader), [typeof(CoreManagerCommand), typeof(ConnectionManagerCommand), typeof(ComponentCommandLoader), typeof(ContextVarCommands), typeof(DebugCommand), typeof(CoreCreatorCommand)] },
		{typeof(ComponentCommandLoader), [typeof(NetworkManagerCommand), typeof(PasswordManagerCommand), typeof(TargetManagerCommand), typeof(HookCallbackManagerCommand)] },
		{typeof(InputCommandsLoader), [typeof(InputSimulatorCommand), typeof(HookManagerCommand)] },
		{typeof(NetworkManagerCommand), [typeof(ListHostsNetworkCommand), typeof(NetworkConnsManagerCommand), typeof(NetworkCallbacks), typeof(EndPointInfoCommand)] }
	};

	public readonly List<Type> AllCommandTypes, AllLoaders;

	public GlobalCommandList () {
		AllCommandTypes = new ();
		AllLoaders = new ();

		foreach ( Assembly asm in AppDomain.CurrentDomain.GetAssemblies () ) {
			foreach ( Type type in asm.GetTypes () ) {
				if ( type.Namespace != null && type.Namespace.EndsWith ( "Tests" ) ) continue;
				if ( type.IsSubclassOf ( typeof ( ACommand ) ) && !type.IsAbstract ) {
					if ( type.Name.EndsWith ( "Loader" ) ) AllLoaders.Add ( type );
					else AllCommandTypes.Add ( type );
				}
			}
		}
	}
}
