using System;
using System.Collections.Generic;

namespace InputResender.Services;
public interface ICommand {
	protected IReadOnlySet<string> CommandNames { get; }
	public string Description { get; }
	public string Help { get; }

	CommandResult Execute ( ArgParser args, int argID = 1 );
	protected bool SubCommand (ArgParser args, out ICommand cmd, int argID );

	public static ICommand Search (ArgParser args, ICollection<ICommand> commands) {
		string command = args.String ( 0, "Command" );
		foreach ( ICommand cmd in commands ) {
			if ( !cmd.CommandNames.Contains ( command ) ) continue;
			ICommand ret = cmd;
			// It depends on the command itself how it will process subcommands. It can be selected by implementing the SubCommand for unified selection process in this static method (probably later replaced by a service), or it can be done in the Execute method (e.g. switch (args.String(1, "SubCommand" )) { case "sub1": return Sub2Command.Execute (args, 2); ... }).
			while ( ret.SubCommand ( args, out ICommand sub, 1 ) ) ret = sub;
			return ret;
		}
		return null;
	}
}

public abstract class ACommand<T> : ICommand where T : CommandResult {
	protected readonly HashSet<string> commandNames = new ();
	protected readonly HashSet<string> requiredSwitches = new ();
	protected readonly HashSet<string> requiredPositionals = new ();
	protected virtual int RequiredUnnamedArgs => 0;
	IReadOnlySet<string> ICommand.CommandNames => commandNames;
	public int RequiredArgC => RequiredUnnamedArgs + requiredSwitches.Count + requiredPositionals.Count;

	public abstract string Description { get; }
	public abstract string Help { get; }

	public CommandResult Execute ( ArgParser args, int argID = 1 ) {
		// Note that erorr result needs to have exception specified, otherwise it will be treated as a successful result.
		if ( args.ArgC < RequiredArgC ) return new CommandResult ( new Exception ( $"Invalid argument count. Required: {RequiredArgC}, provided: {args.ArgC}" ) );

		foreach ( string sw in requiredSwitches ) {
			if ( !args.Present ( sw ) ) return new CommandResult ( new Exception ( $"Switch '{sw}' is required but is not provided." ) );
		}

		foreach ( string pos in requiredPositionals ) {
			if ( !args.Present ( pos ) ) return new CommandResult ( new Exception ( $"Positional argument '{pos}' is required but is not provided." ) );
		}

		return ExecIner ( args, argID );
	}

	protected abstract CommandResult ExecIner ( ArgParser args, int argID );
	bool ICommand.SubCommand ( ArgParser args, out ICommand cmd, int argID ) { cmd = null; return false; }
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

	public CommandResult (Exception ex) : this ( ex.Message, false, ex ) { }
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