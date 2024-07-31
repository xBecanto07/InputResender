using InputResender.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Components.Library;
public abstract class ACommand {
	protected virtual int RequiredUnnamedArgs => 0;
	public int RequiredArgC => RequiredUnnamedArgs + requiredSwitches.Count + requiredPositionals.Count;

	protected readonly string parentCommandHelp;
	protected readonly HashSet<string> commandNames = new ();
	protected readonly HashSet<string> requiredSwitches = new ();
	protected readonly Dictionary<int, bool> requiredPositionals = new ();
	protected readonly Dictionary<string, ACommand> subCommands = new ();
	public abstract string Description { get; }
	public abstract string Help { get; }

	/// <summary>Used to access help of parent command. If null, it's considered the root command. When not command 'history' is not known, it is recommended to provide constructor to your command accepting string parameter and passing it to base constructor.</summary>
	public ACommand ( string parentHelp ) => parentCommandHelp = parentHelp ?? string.Empty;

	protected static void RegisterSubCommand ( ACommand owner, string name, ACommand nwCmd ) {
		if ( owner == null ) throw new ArgumentNullException ( nameof ( owner ) );
		if ( owner.subCommands.ContainsKey (name) ) owner.subCommands[name] = nwCmd;
		else {
			if ( nwCmd == null ) return;
			owner.subCommands.Add ( name, nwCmd );
		}
	}

	public virtual CommandResult Execute ( ICommandProcessor context, ArgParser args, int argID = 1 ) {
		if ( new string[] { "-h", "?", "help" }.Contains ( args.String ( argID, null ) ) )
			return new CommandResult ( Help + Environment.NewLine + " - " + Description );

		// Note that erorr result needs to have exception specified, otherwise it will be treated as a successful result.
		if ( args.ArgC < RequiredArgC ) return new CommandResult ( new Exception ( $"Invalid argument count. Required: {RequiredArgC}, provided: {args.ArgC}" ) );

		foreach ( string sw in requiredSwitches ) {
			if ( !args.Present ( sw ) ) return new CommandResult ( new Exception ( $"Switch '{sw}' is required but is not provided." ) );
		}

		if ( requiredPositionals.Count > 0 ) {
			int maxArg = requiredPositionals.Keys.Max ();
			for ( int i = 0; i <= maxArg; i++ ) {
				if ( !requiredPositionals.TryGetValue ( i, out bool rquiresValues ) ) return new CommandResult ( new ArgumentException ( $"Argument #{i} is either considered optional while requesting arguments at higher index or request of value was not defined for it." ) );
				if ( !args.Present ( i ) ) return new CommandResult ( new Exception ( $"Argument #{i} is required but is not provided." ) );
				if ( rquiresValues && !args.HasValue ( i, true ) ) return new CommandResult ( new Exception ( $"Argument #{i} requires a value but is not provided." ) );
			}
		}

		return ExecIner ( context, args, argID );
	}

	public virtual bool SubCommand ( ArgParser args, out ACommand cmd, int argID ) {
		string subCmd = args.String ( argID, null ); // This method is returning bool, so it must allow non-existance of subcommand. If subcommand is required, it should be handled as 'if (!SubCommand(...)) return new ErrorCommandResult(...);'.
		if ( !string.IsNullOrEmpty ( subCmd ) && subCommands.TryGetValue ( subCmd, out cmd ) ) return true;
		cmd = null;
		return false;
	}

	protected virtual CommandResult ExecIner ( ICommandProcessor context, ArgParser args, int argID ) => null;

	public static ACommand Search ( ArgParser args, ICollection<ACommand> commands ) {
		string command = args.String ( 0, "Command" );
		foreach ( ACommand cmd in commands ) {
			if ( !cmd.commandNames.Contains ( command ) ) continue;
			ACommand ret = cmd;
			// It depends on the command itself how it will process subcommands. It can be selected by implementing the SubCommand for unified selection process in this static method (probably later replaced by a service), or it can be done in the Execute method (e.g. switch (args.String(1, "SubCommand" )) { case "sub1": return Sub2Command.Execute (args, 2); ... }).
			while ( ret.SubCommand ( args, out ACommand sub, 1 ) ) ret = sub;
			return ret;
		}
		return null;
	}
}

public abstract class ACommand<T> : ACommand where T : CommandResult {
	/// <inheritdoc/>
	public ACommand ( string parentHelp ) : base ( parentHelp ) { }

	public sealed override T Execute ( ICommandProcessor context, ArgParser args, int argID = 1 ) => (T)base.Execute ( context, args, argID );
	protected override T ExecIner ( ICommandProcessor context, ArgParser args, int argID ) => null;
}


public class CommandResult {
	public readonly string Message;
	public readonly bool IsExit;
	public readonly Exception Expection;

	public CommandResult ( string message, bool isExit = false, Exception expection = null ) {
		Message = message;
		IsExit = isExit;
		Expection = expection;
	}

	public CommandResult ( Exception ex ) : this ( ex == null ? "Null exception provided" : ex.Message, false, ex ?? new ArgumentNullException ( nameof ( ex ) ) ) { }
}

public class ErrorCommandResult : CommandResult {
	public readonly CommandResult OrigResult;
	public ErrorCommandResult ( CommandResult origResult, Exception ex ) : base ( ex ) => OrigResult = origResult;
}

public class StructCommandResult<T> : CommandResult where T : struct {
	public readonly T? Result;
	public StructCommandResult ( T? result, string msg = null, bool isExit = false, Exception expection = null ) : base ( string.IsNullOrEmpty ( msg ) ? result?.ToString () : msg, isExit, expection ) {
		Result = result;
	}
}

public class ClassCommandResult<T> : CommandResult where T : class {
	public readonly T Result;
	public ClassCommandResult ( T result, string msg = null, bool isExit = false, Exception expection = null ) : base ( string.IsNullOrEmpty ( msg ) ? result?.ToString () : msg, isExit, expection ) {
		Result = result;

	}
}

public class BoolCommandResult : StructCommandResult<bool> {
	public BoolCommandResult ( bool result, bool isExit = false, Exception expection = null ) : base ( result, null, isExit, expection ) { }
}