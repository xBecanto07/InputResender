using Components.Library;
using InputResender.Commands;
using System.Net;

namespace Components.Interfaces;
/*public class MainAppCommands : ACommand {
	PasswordManagerCommand passwordManager;
	public override string Description => "Main application commands.";

	public MainAppCommands ( ACommand parent = null ) : base ( parent?.CallName ) {
		commandNames.Add 
	}
}*/

public class PasswordManagerCommand : ACommand {
	public override string Description => "Password management";

	public PasswordManagerCommand ( ACommand parent = null ) : base ( parent?.CallName ) {
		commandNames.Add ( "password" );
		commandNames.Add ( "pw" );

		interCommands.Add ( "add" );
		interCommands.Add ( "print" );
	}

	protected override CommandResult ExecIner ( ICommandProcessor context, ArgParser args, int argID ) {
		DMainAppCore core = context.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		switch ( args.String ( argID, "Action" ) ) {
		case "add":
			if ( string.IsNullOrWhiteSpace ( args.String ( argID + 1, "password" ) ) ) return new CommandResult ( "Password cannot be empty." );
			core.DataSigner.Key = core.DataSigner.GenerateIV (System.Text.Encoding.UTF8.GetBytes ( args.String ( argID + 1, "password" ) ) );
			return new CommandResult ( $"Password set to {core.DataSigner.Key.CalcHash ().ToShortCode ()}" );
		case "print":
			return new CommandResult ( $"Current password: {core.DataSigner.Key.CalcHash ().ToShortCode ()}" );
		default:
			return new CommandResult ( "Missing or unknown subcommand." );
		}
	}
}

public class TargetManagerCommand : ACommand {
	protected IPEndPoint TargetEP;
	public override string Description => "Target management";
	public TargetManagerCommand ( ACommand parent = null ) : base ( parent?.CallName ) {
		commandNames.Add ( "target" );
		commandNames.Add ( "tEP" );

		interCommands.Add ( "set" );
	}

	protected override CommandResult ExecIner ( ICommandProcessor context, ArgParser args, int argID ) {
		DMainAppCore core = context.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		switch ( args.String ( argID, "Action" ) ) {
		case "set":
			string target = args.String ( argID + 1, "Target end point" );
			if ( !IPEndPoint.TryParse ( target, out IPEndPoint EP ) )
				return new CommandResult ( $"Provided target '{target}' is not a valid end point." );
			if ( TargetEP != null ) core.PacketSender.Disconnect ( TargetEP );
			TargetEP = EP;
			core.PacketSender.Connect ( TargetEP );
			return new CommandResult ( $"Target set to {TargetEP}" );
		default:
			return new CommandResult ( "Missing or unknown subcommand." );
		}
	}
}

public class HookManagerCommand : ACommand {
	ICommandProcessor activeContext = null;
	HHookInfo hookInfo;
	public override string Description => "Input hook management";
	public HookManagerCommand ( ACommand parent = null ) : base ( parent?.CallName ) {
		commandNames.Add ( "hook" );

		interCommands.Add ( "status" );
		interCommands.Add ( "start" );
	}

	protected override CommandResult ExecIner ( ICommandProcessor context, ArgParser args, int argID ) {
		DMainAppCore core = context.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		switch ( args.String ( argID, "Action" ) ) {
		case "status": return new CommandResult ( $"Hook status: {(hookInfo != null ? "active" : "inactive")}" );
		case "start":
			if ( hookInfo != null ) return new CommandResult ( "Hook already active." );
			if ( context is not ComponentBase cb ) return new CommandResult ( "Invalid context type." );
			hookInfo = new HHookInfo ( cb, 0, VKChange.KeyDown, VKChange.KeyUp );
			var hookIDs = core.InputReader.SetupHook ( hookInfo, HookFastCallback, HookCallback );
			return new CommandResult ( $"Hook started with following IDs: {string.Join ( ", ", hookIDs )}" );
		default:
			return new CommandResult ( "Missing or unknown subcommand." );
		}
	}

	private bool HookFastCallback ( DictionaryKey hookID, HInputEventDataHolder e ) => activeContext != null;
	private void HookCallback ( DictionaryKey hookID, HInputEventDataHolder e ) {
		Action<DictionaryKey, HInputEventDataHolder> callback;
		try {
			callback = activeContext.GetVar<Action<DictionaryKey, HInputEventDataHolder>> ( HookCallbackManagerCommand.CBVarName );
		} catch ( ArgumentException ex ) {
			callback = null;
		} catch { throw; }

		if ( callback != null ) callback ( hookID, e );
		else activeContext.ProcessLine ($"print \"No callback to handle hook event {e}\"" );
	}
}

public class HookCallbackManagerCommand : ACommand {
	public const string CBVarName = "HookCallback";
	public override string Description => "Input hook callback selector";
	public HookCallbackManagerCommand ( ACommand parent = null ) : base ( parent?.CallName ) {
		commandNames.Add ( "hookcb" );

		interCommands.Add ( "list" );
		interCommands.Add ( "set" );
		interCommands.Add ( "active" );
	}

	readonly Action<ICommandProcessor, HInputEventDataHolder>[] PossibleCallbacks = new[] {
		PrintCB
	};

	protected override CommandResult ExecIner ( ICommandProcessor context, ArgParser args, int argID ) {
		switch ( args.String ( argID, "Action" ) ) {
		case "list":
			return new CommandResult ( $"Available callbacks: {string.Join ( ", ", PossibleCallbacks.Select ( (cb, i) => $"{i}: {cb.Method.Name}" ) )}" );
		case "set":
			string cbSel = args.String ( argID + 1, "Callback selector" );
			if ( int.TryParse( cbSel, out int idx ) ) {
				if ( idx < 0 || idx >= PossibleCallbacks.Length ) return new CommandResult ( $"Invalid callback index of {idx}." );
				context.SetVar ( CBVarName, PossibleCallbacks[idx] );
			}
			var cb = PossibleCallbacks.FirstOrDefault ( cb => cb.Method.Name == cbSel );
			if ( cb == null ) return new CommandResult ( $"Callback '{cbSel}' not found." );
			context.SetVar ( CBVarName, cb );
			return new CommandResult ( $"Hook callback set to {cb.Method.Name}." );
		case "active":
			try {
				var callback = context.GetVar<Action<DictionaryKey, HInputEventDataHolder>> ( CBVarName );
				return new CommandResult ( $"Active callback: {callback.Method.Name}" );
			} catch ( ArgumentException ex ) {
				return new CommandResult ( "No active callback." );
			} catch { throw; }
		default:
			return new CommandResult ( "Missing or unknown subcommand." );
		}
	}

	private static void PrintCB ( ICommandProcessor context, HInputEventDataHolder e ) {

	}
}