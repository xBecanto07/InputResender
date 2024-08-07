using System;
using System.Collections.Generic;

namespace Components.Library;
public interface ICommandProcessor {
	void AddCommand ( ACommand cmd );
	/// <summary>Find a command based on a callname. This command is passed to callback function. If this function returns null, no more processing is done. If some reference is returned, it will be either added or replaced in 1st level command list. Removing command is not supported since no use-case has been provided.</summary>
	void ModifyCommand ( string line, Func<ACommand, ACommand> modifyFcn );
	CommandResult ProcessLine ( string line, bool verbose = false );
	CommandResult ProcessLine<T> ( string line, out T result, bool verbose = false ) where T : CommandResult;
	string Help ();
	CommandResult LoadAllCommands ();

	bool SafeMode { get; set; }

	void SetVar ( string name, object var );
	T GetVar<T> ( string name );
}

public class CommandProcessor : ComponentBase, ICommandProcessor {
	private readonly HashSet<ACommand> registeredCmds = new ();
	private Action<string> WriteLine;
	private Dictionary<string, object> Vars = new ();

	public bool SafeMode { get; set; } = false;

	public override int ComponentVersion => 1;
	public override StateInfo Info => null;
	protected override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
		(nameof ( Help ), typeof ( string )),
		(nameof ( ProcessLine ), typeof ( CommandResult )),
		(nameof ( ProcessLine ), typeof ( CommandResult )),
		(nameof ( SetVar ), typeof ( void )),
		(nameof ( GetVar ), typeof ( object )),
		(nameof ( SafeMode ), typeof ( bool )),
		(nameof ( AddCommand ), typeof ( void )),
		(nameof ( ModifyCommand ), typeof ( void )),
		(nameof ( LoadAllCommands ), typeof ( string )),
	};

	public CommandProcessor ( Action<string> writeLine ) {
		WriteLine = writeLine;
	}

	public void AddCommand ( ACommand cmd ) {
		if ( registeredCmds.Contains ( cmd ) ) return;
		registeredCmds.Add ( cmd );
	}

	/// <inheritdoc />
	public void ModifyCommand ( string line, Func<ACommand, ACommand> modifyFcn ) {
		ArgParser args = new ( line, WriteLine );
		var parCmd = ACommand.Search ( args, registeredCmds );
		if ( parCmd == null ) throw new ArgumentException ( $"Parent command '{args.String ( 0, "Parent command" )}' not found." );
		var newCmd = modifyFcn ( parCmd );
		if ( newCmd == null ) return;
		if ( registeredCmds.Contains ( parCmd ) ) registeredCmds.Remove ( parCmd );
		registeredCmds.Add ( newCmd );
	}

	public string Help () {
		string res = $"Supported commands:{Environment.NewLine}";
		foreach ( var cmd in registeredCmds )
			res += $"{cmd.Help}{Environment.NewLine}";
		return res;
	}

	public CommandResult LoadAllCommands () {
		System.Text.StringBuilder SB = new ();
		HashSet<ACommandLoader> loaders = new ();
		foreach ( var cmd in registeredCmds ) {
			if ( cmd is ACommandLoader loader ) loaders.Add ( loader );
		}
		foreach ( var loader in loaders ) {
			var res = ProcessLine ( loader.CallName );
			if ( res is ErrorCommandResult errRes )
				SB.AppendLine ( $"Error processing '{loader.CallName}': {errRes.Message}" );
			else SB.AppendLine ( res.Message );
		}
		return new CommandResult ( SB.ToString () );
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

		var cmd = ACommand.Search ( args, registeredCmds );
		if ( cmd == null ) return new ErrorCommandResult ( null, new ArgumentException ( $"Command '{args.String ( 0, "Command" )}' not found." ) );

		if ( !SafeMode ) return cmd.Execute ( this, args );
		else {
			try {
				return cmd.Execute ( this, args );
			} catch ( Exception e ) {
				return new ErrorCommandResult ( null, e );
			}
		}
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