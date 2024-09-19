using InputResender.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Components.Library;
public abstract class ACommand {
	public static readonly IReadOnlyCollection<string> HelpSwitches = ["-h", "?", "help"];
	protected virtual int RequiredUnnamedArgs => 0;
	public int RequiredArgC => RequiredUnnamedArgs + requiredSwitches.Count + requiredPositionals.Count;

	protected readonly string parentCommandHelp;
	protected readonly HashSet<string> commandNames = new ();
	protected readonly HashSet<string> requiredSwitches = new ();
	protected readonly Dictionary<int, bool> requiredPositionals = new ();
	protected readonly Dictionary<string, ACommand> subCommands = new ();
	protected readonly HashSet<string> interCommands = new ();
	public abstract string Description { get; }
	public virtual string CallName { get => BasicCallName (); }
	public virtual string Help { get => BasicHelp (); }

	protected string BasicCallName () =>
		(string.IsNullOrEmpty ( parentCommandHelp ) ? string.Empty : parentCommandHelp + " ") + commandNames.First ();
	protected string BasicHelp () {
		System.Text.StringBuilder SB = new ();
		SB.Append ( CallName );
		if ( requiredPositionals.Count > 0 ) SB.Append ( " " + string.Join ( " ", requiredPositionals.Keys.Select ( i => requiredPositionals[i] ? $"[{i}]" : i.ToString () ) ) );
		if ( requiredSwitches.Count > 0 ) SB.Append ( " [-" + string.Join ( "][-", requiredSwitches ) + "]" );
		if ( interCommands.Any () ) SB.Append ( " (" + string.Join ( "|", interCommands ) + ")" );
		SB.AppendLine ();
		SB.AppendLine ( Description.PrefixAllLines ( " - " ) );
		foreach ( ACommand cmd in subCommands.Values ) {
			SB.AppendLine ( cmd.Help.PrefixAllLines ( " > " ) );
		}
		return SB.ToString ().Trim ();
	}

	/// <summary>Used to access help of parent command. If null, it's considered the root command. When not command 'history' is not known, it is recommended to provide constructor to your command accepting string parameter and passing it to base constructor.</summary>
	public ACommand ( string parentHelp ) => parentCommandHelp = parentHelp ?? string.Empty;

	protected static void RegisterSubCommand ( ACommand owner, ACommand nwCmd, string name = null ) {
		if ( owner == null ) throw new ArgumentNullException ( nameof ( owner ) );
		if ( string.IsNullOrEmpty ( name ) ) name = nwCmd.commandNames.First ();
		if ( owner.subCommands.ContainsKey ( name ) ) owner.subCommands[name] = nwCmd;
		else {
			if ( nwCmd == null ) return;
			owner.subCommands.Add ( name, nwCmd );
		}
	}

	public virtual CommandResult Execute ( CommandProcessor.CmdContext context ) {
		// empty string should never reach this point, but better to catch any wrongly defined commands
		if ( context.Args.ArgC < context.ArgID ) return new CommandResult ( new Exception ( "Not enough arguments provided." ) );
		if ( HelpSwitches.Contains ( context[0] ) )
			return new CommandResult ( Help + Environment.NewLine + " - " + Description );
		//if ( context[0] == "clear" ) return ExecCleanup ( context );

		// Note that erorr result needs to have exception specified, otherwise it will be treated as a successful result.
		if ( context.Args.ArgC < RequiredArgC ) return new CommandResult ( new Exception ( $"Invalid argument count. Required: {RequiredArgC}, provided: {context.Args.ArgC}" ) );

		foreach ( string sw in requiredSwitches ) {
			if ( !context.Args.Present ( sw ) ) return new CommandResult ( new Exception ( $"Switch '{sw}' is required but is not provided." ) );
		}

		if ( requiredPositionals.Count > 0 ) {
			int maxArg = requiredPositionals.Keys.Max ();
			for ( int i = 0; i <= maxArg; i++ ) {
				if ( !requiredPositionals.TryGetValue ( i, out bool rquiresValues ) ) return new CommandResult ( new ArgumentException ( $"Argument #{i} is either considered optional while requesting arguments at higher index or request of value was not defined for it." ) );
				if ( !context.Args.Present ( i ) ) return new CommandResult ( new Exception ( $"Argument #{i} is required but is not provided." ) );
				if ( rquiresValues && !context.Args.HasValue ( i, true ) ) return new CommandResult ( new Exception ( $"Argument #{i} requires a value but is not provided." ) );
			}
		}
		return ExecIner ( context );
	}

	public virtual bool SubCommand ( ArgParser args, out ACommand cmd, ref int argID ) {
		string subCmd = args.String ( argID, null ); // This method is returning bool, so it must allow non-existance of subcommand. If subcommand is required, it should be handled as 'if (!SubCommand(...)) return new ErrorCommandResult(...);'.
		argID++;
		if ( !string.IsNullOrEmpty ( subCmd ) && subCommands.TryGetValue ( subCmd, out cmd ) ) return true;
		argID--;
		cmd = null;
		return false;
	}

	protected virtual CommandResult ExecIner ( CommandProcessor.CmdContext context ) => null;
	protected virtual CommandResult ExecCleanup ( CommandProcessor.CmdContext context ) => null;
	public CommandResult Cleanup ( CommandProcessor.CmdContext context ) => ExecCleanup ( context );

	public static ACommand Search ( ArgParser args, ICollection<ACommand> commands, ref int argID ) {
		string command = args.String ( argID, "Command" );
		argID++;
		foreach ( ACommand cmd in commands ) {
			if ( !cmd.commandNames.Contains ( command ) ) continue;
			ACommand ret = cmd;
			// It depends on the command itself how it will process subcommands. It can be selected by implementing the SubCommand for unified selection process in this static method (probably later replaced by a service), or it can be done in the Execute method (e.g. switch (args.String(1, "SubCommand" )) { case "sub1": return Sub2Command.Execute (args, 2); ... }).
			while ( ret.SubCommand ( args, out ACommand sub, ref argID ) ) ret = sub;
			return ret;
		}
		return null;
	}

	override public string ToString () => CallName;
	protected bool TryPrintHelp ( ArgParser args, int argID, Func<string> helpFcn, out CommandResult helpRes ) {
		helpRes = null;
		string arg = args.String ( argID, null );
		if ( !HelpSwitches.Contains ( arg ) ) return false;
		helpRes = new CommandResult ( "Usage: " + helpFcn () );
		return true;
	}
}

public abstract class ACommand<T> : ACommand where T : CommandResult {
	/// <inheritdoc/>
	public ACommand ( string parentHelp ) : base ( parentHelp ) { }

	public sealed override T Execute ( CommandProcessor.CmdContext context ) => (T)base.Execute ( context );
	protected override T ExecIner ( CommandProcessor.CmdContext context ) => null;
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
	public BoolCommandResult ( bool result, string msg = null, bool isExit = false, Exception expection = null ) : base ( result, msg, isExit, expection ) { }
}