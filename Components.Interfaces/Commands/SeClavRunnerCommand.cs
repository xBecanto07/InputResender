using Components.Library;
using InputResender.Commands;
using SeClav;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Components.Interfaces.Commands;
public class SeClavRunnerCommand : ACommand {
	public readonly SeClavModuleManagerCommand ModuleManager;
	private readonly Dictionary<string, SCLScriptHolder> ParsedScripts;
	private readonly Func<string, string> FileLoader;

	public SCLScriptHolder TryGetParsedScript ( string scriptName )
		=> ParsedScripts.GetValueOrDefault ( scriptName );

	public override string Description => "Parse and run SeClav scripts.";

	private static List<string> CommandNames = ["seclav"];
	private static List<(string, Type)> InterCommands = [
		("module", typeof(SeClavModuleManagerCommand))
		, ("list", null)
		, ("parse", null)
		, ("run", null)
		];

	public SeClavRunnerCommand ( Func<string, string> fileLoader, string parentDsc = null ) : base ( parentDsc, CommandNames, InterCommands ) {
		ArgumentNullException.ThrowIfNull ( fileLoader );

		FileLoader = fileLoader;
		ModuleManager = new ( CallName );
		ParsedScripts = new ();
	}

	protected override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"parse" => CallName + " parse <Script>: Parse a SeClav script\n\tScript: The script to run, enclosed in quotes",
			"run" => CallName + " run <Script> [force]: Run a previously parsed SeClav script\n\tScript: The script to run, enclosed in quotes\n\tforce: Optional hint to force reparse if the script was already parsed",
			"list" => CallName + " list: List all parsed SeClav scripts",
			"module" => CallName + " module <Subcommand> [Options]: Manage valid modules for SeClav scripts\n\tSubcommand: {{list|add|remove|info}}\n\tOptions: Subcommand specific options",
			_ => null
		}, out var helpRes ) ) return helpRes;

		switch ( context.SubAction ) {
		case "list": return new CommandResult ( "Parsed Scripts:\n" +
			( ParsedScripts.Count == 0 ? "  (none)" :
			string.Join ( "\n", ParsedScripts.Keys.Select ( k => $"  - {k}" ) ) ) );
		case "module": return ModuleManager.Execute ( context.Sub () );
		case "parse": {
			string script = context.Args.String ( context.ArgID + 1, "Script" );
			if ( string.IsNullOrEmpty ( script ) )
				return new CommandResult ( "Script cannot be empty." );
			context.Args.RegisterSwitch ( 'f', "force" );
			bool alreadyContains = ParsedScripts.ContainsKey ( script );
			bool shouldForce = context.Args.Present ( "--force" );
			if ( alreadyContains && !shouldForce )
				return new CommandResult ( $"Script '{script}' is already parsed. Use 'force' option to reparse." );

			string code = FileLoader ( script );
			if ( string.IsNullOrEmpty ( code ) )
				return new CommandResult ( $"Failed to load script from '{script}'." );

			SCLScriptHolder parsed = new ( code, script, ModuleManager.ModuleLoader );
			ParsedScripts[script] = parsed;
			if ( parsed.Errors.Count > 0 ) {
				string errMsg = $"Script '{script}' parsed with {parsed.Errors.Count} errors:\n" + string.Join ( "\n", parsed.Errors.Select ( e => e.Item1 ) );
				return new CommandResult ( errMsg );
			}
			return new CommandResult ( $"Script '{script}' parsed successfully with {parsed.ParsedScript.Commands.Count} commands and {parsed.ParsedScript.DataTypes.Count} data types." );
		}
		case "run": {
			string script = context.Args.String ( context.ArgID + 1, "Script", shouldThrow: true );
			if ( string.IsNullOrEmpty ( script ) )
				return new CommandResult ( "Script cannot be empty." );
			if ( !ParsedScripts.TryGetValue ( script, out var parsed ) )
				return new CommandResult ( $"Script '{script}' is not parsed. Please parse it first." );
			if ( parsed.Errors.Count > 0 )
				return new CommandResult ( $"Script '{script}' has errors and cannot be run." );
			// Here should be the actual execution of the script. For now, just return a success message.
			return new CommandResult ( $"Script '{script}' executed successfully." );
		}
		default: return new CommandResult ( $"Unknown subcommand '{context.SubAction}'." );
		}
	}
}

public class SeClavModuleManagerCommand : ACommand {
	private readonly Dictionary<string, DModuleLoader.IModuleInfo> loadedModules = [];
	private readonly Dictionary<string, DModuleLoader.IModuleInfo> availableModules = [];

	public override string Description => "Manage valid modules for SeClav scripts";

	private static List<string> CommandNames = ["module"];
	private static List<(string, Type)> InterCommands = [
		("list", null)
		, ("add", null)
		, ("remove", null)
		, ("info", null)
		];

	public SeClavModuleManagerCommand ( string parentDsc = null )
		: base (parentDsc, CommandNames, InterCommands ) {
		loadedModules = [];
		availableModules = [];
		// Maybe should contain command to search DLL for modules?
	}

	protected override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"list" => CallName + " list: List all registered modules",
			"add" => CallName + " add <ModuleName>: Add a module to the valid modules list\n\tModuleName: Name of the module to add",
			"remove" => CallName + " remove <ModuleName>: Remove a module from the valid modules list\n\tModuleName: Name of the module to remove",
			"info" => CallName + " info <ModuleName>: Get information about a specific module\n\tModuleName: Name of the module to get info about",
			_ => null
		}, out var helpRes ) ) return helpRes;

		switch ( context.SubAction ) {
		case "list": {
			if ( loadedModules.Count == 0 )
				return new CommandResult ( "No modules loaded." );
			string res = "Loaded Modules:\n" + string.Join ( "\n", loadedModules.Keys );
			return new CommandResult ( res );
		}
		case "add": {
			string moduleName = context.Args.String ( context.ArgID + 1, "Module Name" );
			if ( string.IsNullOrEmpty ( moduleName ) )
				return new CommandResult ( "Module name cannot be empty." );
			if ( loadedModules.ContainsKey ( moduleName ) )
				return new CommandResult ( $"Module '{moduleName}' is already loaded." );
			if ( !availableModules.TryGetValue ( moduleName, out var module ) )
				return new CommandResult ( $"Module '{moduleName}' is not available." );
			loadedModules[moduleName] = module;
			return new CommandResult ( $"Module '{moduleName}' added." );
		}
		case "remove": {
			string moduleName = context.Args.String ( context.ArgID + 1, "Module Name" );
			if ( string.IsNullOrEmpty ( moduleName ) )
				return new CommandResult ( "Module name cannot be empty." );
			if ( !loadedModules.ContainsKey ( moduleName ) )
				return new CommandResult ( $"Module '{moduleName}' is not loaded." );
			loadedModules.Remove ( moduleName );
			return new CommandResult ( $"Module '{moduleName}' removed." );
		}
		case "info": {
			string moduleName = context.Args.String ( context.ArgID + 1, "Module Name" );
			if ( string.IsNullOrEmpty ( moduleName ) )
				return new CommandResult ( "Module name cannot be empty." );
			if ( !availableModules.TryGetValue ( moduleName, out var module ) )
				return new CommandResult ( $"Module '{moduleName}' is not available." );

			string res = $"Module Name: {module.Name}\nDescription: {module.Description}\nCommands: {string.Join ( ", ", module.Commands )}\nData Types: {string.Join ( ", ", module.DataTypes )}";
			return new CommandResult ( res );
		}
		default: return new CommandResult ( $"Unknown subcommand '{context.SubAction}'." );
		}
	}


	public void RegisterModule ( DModuleLoader.IModuleInfo module ) {
		ArgumentNullException.ThrowIfNull ( module );
		if ( availableModules.ContainsKey ( module.Name ) )
			throw new InvalidOperationException ( $"Module with name '{module.Name}' is already registered." );
		availableModules[module.Name] = module;
	}

	public DModuleLoader.IModuleInfo ModuleLoader ( string moduleName )
		=> availableModules.TryGetValue ( moduleName, out var module ) ? module : null;
}