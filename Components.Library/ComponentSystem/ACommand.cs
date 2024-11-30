using InputResender.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Components.Library;
public abstract class ACommand {
	public static readonly IReadOnlyCollection<string> HelpSwitches = ["-h", "?", "-?", "--help"];
	protected virtual int RequiredUnnamedArgs => 0;
	public int RequiredArgC => RequiredUnnamedArgs + requiredSwitches.Count + requiredPositionals.Count;

	protected readonly string parentCommandHelp;
	protected readonly HashSet<string> commandNames = new ();
	protected readonly HashSet<string> requiredSwitches = new ();
	protected readonly Dictionary<int, bool> requiredPositionals = new ();
	protected readonly Dictionary<string, ACommand> subCommands = new ();
	protected readonly HashSet<string> interCommands = new ();
	// InterCommand is only registered string switch that 'should' change behaviour inside the command. But SubCommand will switch to another command (i.e. different Object
	public abstract string Description { get; }
	public virtual string CallName { get => BasicCallName ( false ); }
	public virtual string Help { get => BasicHelp (); }
	protected virtual bool PrintHelpOnEmpty { get; } = false;
	protected virtual int ArgsOffset => 0;

	protected string BasicCallName ( bool includeAlts ) =>
		(string.IsNullOrEmpty ( parentCommandHelp ) ? string.Empty : parentCommandHelp + " ") + commandNames.First () + (includeAlts && commandNames.Count > 1 ? $"[.|{string.Join ( "|", commandNames.Skip ( 1 ) )}]" : string.Empty);
	protected string BasicHelp () {
		System.Text.StringBuilder SB = new ( "Usage: " );
		SB.Append ( BasicCallName ( true ) );

		if ( requiredPositionals.Count > 0 ) SB.Append ( " " + string.Join ( " ", requiredPositionals.Keys.Select ( i => requiredPositionals[i] ? $"[{i}]" : i.ToString () ) ) );
		if ( requiredSwitches.Count > 0 ) SB.Append ( " [-" + string.Join ( "][-", requiredSwitches ) + "]" );
		if ( interCommands.Any () ) SB.Append ( " (" + string.Join ( "|", interCommands ) + ")" );
		if ( Description.Contains ( '\n' ) || SB.Length + Description.Length > 90 ) {
			SB.AppendLine ();
			SB.AppendLine ( Description.PrefixAllLines ( " - " ) );
		} else {
			SB.Append ( ": " );
			SB.AppendLine ( Description );
		}

		var subCmds = subCommands.FlipAndUnion ();
		foreach ( var cmd in subCmds ) {
			string cmdHelp = cmd.Key.Help.PrefixAllLines ( " > " );
			SB.Append ( " + " );
			SB.AppendLine ( cmdHelp[3..] );
		}
		return SB.ToString ().Trim ();
	}

	/// <summary>Used to access help of parent command. If null, it's considered the root command. When not command 'history' is not known, it is recommended to provide constructor to your command accepting string parameter and passing it to base constructor.</summary>
	public ACommand ( string parentHelp ) => parentCommandHelp = parentHelp ?? string.Empty;

	protected static void RegisterSubCommand ( ACommand owner, ACommand nwCmd ) {
		if ( owner == null ) throw new ArgumentNullException ( nameof ( owner ) );
		foreach ( string name in nwCmd.commandNames )
			RegisterSubCommand ( owner, nwCmd, name );
	}
	protected static void RegisterSubCommand ( ACommand owner, ACommand nwCmd, string name ) {
		if ( owner == null ) throw new ArgumentNullException ( nameof ( owner ) );
		if ( string.IsNullOrEmpty ( name ) ) name = nwCmd.commandNames.First ();
		if ( owner.subCommands.ContainsKey ( name ) ) owner.subCommands[name] = nwCmd;
		else {
			if ( nwCmd == null ) return;
			owner.subCommands.Add ( name, nwCmd );
		}
	}

	public virtual CommandResult Execute ( CommandProcessor.CmdContext context ) {
		var localContext = context.Sub ( ArgsOffset );
		// empty string should never reach this point, but better to catch any wrongly defined commands
		if ( localContext.Args.ArgC < localContext.ArgID ) return new CommandResult ( new Exception ( "Not enough arguments provided." ) );
		if ( HelpSwitches.Contains ( localContext[0] ) )
			return new CommandResult ( Help );
		//if ( context[0] == "clear" ) return ExecCleanup ( context );

		// Note that erorr result needs to have exception specified, otherwise it will be treated as a successful result.
		if ( localContext.Args.ArgC < RequiredArgC ) return new CommandResult ( new Exception ( $"Invalid argument count. Required: {RequiredArgC}, provided: {localContext.Args.ArgC}" ) );

		foreach ( string sw in requiredSwitches ) {
			if ( !localContext.Args.Present ( sw ) ) return new CommandResult ( new Exception ( $"Switch '{sw}' is required but is not provided." ) );
		}

		if ( requiredPositionals.Count > 0 ) {
			int maxArg = requiredPositionals.Keys.Max ();
			for ( int i = 0; i <= maxArg; i++ ) {
				if ( !requiredPositionals.TryGetValue ( i, out bool rquiresValues ) ) return new CommandResult ( new ArgumentException ( $"Argument #{i} is either considered optional while requesting arguments at higher index or request of value was not defined for it." ) );
				if ( !localContext.Args.Present ( i ) ) return new CommandResult ( new Exception ( $"Argument #{i} is required but is not provided." ) );
				if ( rquiresValues && !localContext.Args.HasValue ( i, true ) ) return new CommandResult ( new Exception ( $"Argument #{i} requires a value but is not provided." ) );
			}
		}
		return ExecIner ( localContext );
	}

	public virtual bool SubCommand ( ArgParser args, out ACommand cmd, ref int argID ) {
		string subCmd = args.String ( argID, null ); // This method is returning bool, so it must allow non-existance of subcommand. If subcommand is required, it should be handled as 'if (!SubCommand(...)) return new ErrorCommandResult(...);'.
		argID++;
		if ( !string.IsNullOrEmpty ( subCmd ) && subCommands.TryGetValue ( subCmd, out cmd ) ) return true;
		argID--;
		cmd = null;
		return false;
	}

	protected virtual CommandResult ExecIner ( CommandProcessor.CmdContext context ) => new ( $"{context.ParentAction}.{context.SubAction} command ({CallName}) not implemented." );
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
	protected bool TryPrintHelp ( ArgParser args, int argID, Func<string> helpFcn, out CommandResult helpRes, bool printOnEmpty = false ) {
		// Should be converted to TryPrintHelp (Args, int offset..)
		helpRes = null;
		string arg = args.String ( argID, null );
		bool isEmpty = string.IsNullOrWhiteSpace ( arg );
		bool isHelp = HelpSwitches.Contains ( arg );
		isEmpty &= printOnEmpty;
		if (!(isEmpty | isHelp) ) return false;
		string msg = helpFcn ();
		if ( string.IsNullOrWhiteSpace ( msg ) ) return false;
		helpRes = new CommandResult ( "Usage: " + msg );
		return true;
	}
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

	public override string ToString () => $"{(IsExit ? "Exit: " : string.Empty)}{(Expection == null ? string.Empty : "Error: ")}{Message}";
}

public class ErrorCommandResult : CommandResult {
	public readonly CommandResult OrigResult;
	public ErrorCommandResult ( CommandResult origResult, Exception ex ) : base ( ex ) => OrigResult = origResult;
	public override string ToString () => $"Error result: {Expection} | {OrigResult}";
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