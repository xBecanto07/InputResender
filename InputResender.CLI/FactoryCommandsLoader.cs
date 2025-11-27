using Components.Implementations;
using Components.Interfaces.Commands;
using Components.Library;
using Components.Library.ComponentSystem;
using InputResender.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

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
		{ typeof(LoaderCommand), () => new LoaderCommand () },
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
		{ typeof(ScriptedInputProcessorCommand), () => new ScriptedInputProcessorCommand () },
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



public class LoaderCommand : ACommand {
	public override string Description => "Loads various components, commands, data or configurations.";

	private static List<string> CommandNames = ["load"];
	private static List<(string, Type)> InterCommands = [
		("sclModules", null)
		, ("joiners", null)
		];
	public LoaderCommand ( string parentDsc = null )
		: base (parentDsc, CommandNames, InterCommands ) {
	}

	protected override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"sclModules" => CallName + " sclModules: Load SeClav modules known to the system.",
			"joiners" => CallName + " joiners: Load known component joiners into the system.",
			_ => null
		}, out var helpRes ) ) return helpRes;

		switch ( context.SubAction ) {
		case "sclModules": {
			var sclCmd = context.CmdProc.GetCommandInstance<SeClavRunnerCommand> ();
			if ( sclCmd == null )
				return new CommandResult ( "SeClavRunnerCommand is not loaded." );
			if ( sclCmd is not SeClavRunnerCommand sclRunner )
				return new CommandResult ( "SeClavRunnerCommand is not of correct type." );

			List<SeClav.IModuleInfo> knownModules = [
				new Components.Interfaces.SeClav.SCL_BasicModule ()
				, new Components.Implementations.VScriptedInputProcessor.SCL_Module ()
				];
			foreach ( var module in knownModules ) sclRunner.ModuleManager.RegisterModule ( module );
			return new CommandResult ( $"Loaded {knownModules.Count} SeClav modules:\n" + string.Join ( "\n", knownModules.Select ( m => $"- {m.Name}: {m.Description}" ) ) );
		}
		case "joiners": {
			var core = context.CmdProc.Owner;
			if ( core is not Components.Interfaces.DMainAppCore dCore )
				return new CommandResult ( "Current core is not a DMainAppCore." );
			DMainAppCoreFactory.AddJoiners ( dCore );
			return new CommandResult ( "Loaded known component joiners into the system." );
		}
		default:
			return new CommandResult ( $"Unknown subcommand '{context.SubAction}'." );
		}
	}
}