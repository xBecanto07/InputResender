﻿using System;
using System.Collections.Generic;

namespace Components.Library;
public interface ICommandProcessor {
	void AddCommand ( ACommand cmd );
	void ModifyCommand ( string line, Func<ACommand, ACommand> modifyFcn );
	CommandResult ProcessLine ( string line, bool verbose = false );
	CommandResult ProcessLine<T> ( string line, out T result, bool verbose = false ) where T : CommandResult;
	string Help ();

	void SetVar ( string name, object var );
	T GetVar<T> ( string name );
}

public class CommandProcessor : ICommandProcessor {
	private readonly HashSet<ACommand> SupportedCommands = new ();
	private Action<string> WriteLine;
	private Dictionary<string, object> Vars = new ();

	public CommandProcessor ( Action<string> writeLine ) {
		WriteLine = writeLine;
	}

	public void AddCommand ( ACommand cmd ) {
		if ( SupportedCommands.Contains ( cmd ) ) return;
		SupportedCommands.Add ( cmd );
	}

	/// <summary>Find a command based on a callname. This command is passed to callback function. If this function returns null, no more processing is done. If some reference is returned, it will be either added or replaced in 1st level command list. Removing command is not supported since no use-case has been provided.</summary>
	public void ModifyCommand ( string line, Func<ACommand, ACommand> modifyFcn ) {
		ArgParser args = new ( line, WriteLine );
		var parCmd = ACommand.Search ( args, SupportedCommands );
		if ( parCmd == null ) throw new ArgumentException ( $"Parent command '{args.String ( 0, "Parent command" )}' not found." );
		var newCmd = modifyFcn ( parCmd );
		if ( newCmd == null ) return;
		if ( SupportedCommands.Contains ( parCmd ) ) SupportedCommands.Remove ( parCmd );
		SupportedCommands.Add ( newCmd );
	}

	public string Help () {
		string res = $"Supported commands:{Environment.NewLine}";
		foreach ( var cmd in SupportedCommands )
			res += $"{cmd.Help}{Environment.NewLine}   - {cmd.Description}{Environment.NewLine}";
		return res;
	}

	/// <summary>Processes a line and returns the result. If it's of expected type, output reference is also set to the same result, now with proper type. Retuned and outputed references will be the same only if the result is of expected type.</summary>
	public CommandResult ProcessLine<T> ( string line, out T result, bool verbose = false ) where T : CommandResult {
		result = null;
		CommandResult tmpRes = ProcessLine ( line );

		if ( result == null ) return new ErrorCommandResult ( null, new Exception ( "No result." ) );
		if ( result is not T tResult ) return new ErrorCommandResult ( tmpRes,
			new Exception ( $"Expected result of type {typeof ( T ).Name}, got {result.GetType ().Name}." ) );
		else return tResult;
	}

	public CommandResult ProcessLine ( string line, bool verbose = false ) {
		if ( verbose ) WriteLine ( $"Processing line: '{line}'" );
		ArgParser args = new ( line, WriteLine );
		if ( args.ArgC == 0 ) return new CommandResult ( string.Empty, false );

		var cmd = ACommand.Search ( args, SupportedCommands );
		if ( cmd == null ) return new ErrorCommandResult ( null, new ArgumentException ( $"Command '{args.String ( 0, "Command" )}' not found." ) );

		return cmd.Execute ( this, args );
	}

	// Currently no support for scope, so all variables are considered global.
	public void SetVar ( string name, object var ) => Vars[name] = var;
	public T GetVar<T> ( string name ) {
		if ( !Vars.TryGetValue ( name, out object var ) )
			throw new ArgumentException ( $"Variable '{name}' not found." );
		if ( var is not T tVar )
			throw new ArgumentException ( $"Variable '{name}' is not of type {typeof ( T ).Name}." );
		return tVar;
	}
}