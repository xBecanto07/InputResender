using Components.Implementations;
using Components.Interfaces.Commands;
using Components.Library;
using Components.Library.ComponentSystem;
using InputResender.Commands;
using System;
using System.Collections.Generic;

namespace InputResender.CLI; 
public class FactoryCommandsLoader : ACommandLoader {
	private static Dictionary<Type, Func<ACommand>> NewCommandList = new () {
		{ typeof(CoreManagerCommand), () => new CoreManagerCommand () },
		{ typeof(ConnectionManagerCommand), () => new ConnectionManagerCommand () },
		{ typeof(ComponentCommandLoader), () => new ComponentCommandLoader () },
		{ typeof(ContextVarCommands), () => new ContextVarCommands () },
		{ typeof(InputCommandsLoader), () => new InputCommandsLoader () },
		{ typeof(SeClavCommandLoader), () => new SeClavCommandLoader () },
		{ typeof(DebugCommand), () => new DebugCommand () },
		{ typeof(PWDCommand), () => new PWDCommand () },
		{ typeof(AutoCmdsCommand), () => new AutoCmdsCommand () },
	};
	private static Dictionary<Type, (string, Func<ACommand, ACommand>)> NewSubCommandList = new () {
		{ typeof (CoreCreatorCommand), ("core", ( ACommand parent ) => {
			if ( parent is CoreManagerCommand cmdCore )
				RegisterSubCommand ( cmdCore, new CoreCreatorCommand ( parent.CallName ) );
			return null;
		} ) },
	};

	public FactoryCommandsLoader () : base ( "generalCmds" ) { }
	protected override IReadOnlyCollection<Func<ACommand>> NewCommands => NewCommandList.Values;

	protected override IReadOnlyCollection<(string, Func<ACommand, ACommand>)> NewSubCommands => NewSubCommandList.Values;
}

public class InputCommandsLoader : ACommandLoader {
	public InputCommandsLoader () : base ( "inputCmds" ) { }
	private static Dictionary<Type, Func<ACommand>> NewCommandList = new () {
		{ typeof(InputSimulatorCommand), () => new InputSimulatorCommand () },
		{ typeof(HookManagerCommand), () => new HookManagerCommand () },
	};
	protected override IReadOnlyCollection<Func<ACommand>> NewCommands => NewCommandList.Values;
}

public class SeClavCommandLoader : ACommandLoader {
	public SeClavCommandLoader () : base ( "seclavCmds" ) { }
	private static Dictionary<Type, Func<ACommand>> NewCommandList = new () {
		{ typeof(SeClavRunnerCommand), () => new SeClavRunnerCommand (Config.LoadFileContent) },
	};
	protected override IReadOnlyCollection<Func<ACommand>> NewCommands => NewCommandList.Values;
}