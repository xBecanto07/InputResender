using Components.Library;
using InputResender.Commands;
using InputResender.Services;
using InputResender.Services.NetClientService.InMemNet;
using System.Net;

namespace Components.Interfaces;
public class PasswordManagerCommand : ACommand {
	public override string Description => "Password management";

	public PasswordManagerCommand ( ACommand parent = null ) : base ( parent?.CallName ) {
		commandNames.Add ( "password" );
		commandNames.Add ( "pw" );

		interCommands.Add ( "add" );
		interCommands.Add ( "print" );
	}

	protected override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if (TryPrintHelp(context.Args, context.ArgID + 1, () => context.SubAction switch {
			"add" => $"{context.ParentAction} add <password>: Set password\n\t<password>: Password to set\n\tExample: {context.ParentAction} add myPassword",
			"print" => $"{context.ParentAction} print: Print current password (hashed)",
			_ => null
		}, out var helpRes ) ) return helpRes;

		DMainAppCore core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		switch ( context.SubAction ) {
		case "add":
			if ( string.IsNullOrWhiteSpace ( context[1, "password"] ) ) return new CommandResult ( "Password cannot be empty." );
			core.DataSigner.Key = core.DataSigner.GenerateIV (System.Text.Encoding.UTF8.GetBytes ( context[1, "password"] ) );
			return new CommandResult ( $"Password set to {core.DataSigner.Key.CalcHash ().ToShortCode ()}" );
		case "print":
			return new CommandResult ( $"Current password: {core.DataSigner.Key.CalcHash ().ToShortCode ()}" );
		default:
			return new CommandResult ( "Missing or unknown subcommand." );
		}
	}
}

public class TargetManagerCommand : ACommand {
	protected INetPoint TargetEP;
	public override string Description => "Target management";
	public TargetManagerCommand ( ACommand parent = null ) : base ( parent?.CallName ) {
		commandNames.Add ( "target" );
		commandNames.Add ( "tEP" );

		interCommands.Add ( "set" );
	}

	protected override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if (TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"set" => $"{context.ParentAction} set <EndPoint>: Set target end point\n\tEndPoint: IP end point or InMemNet point",
			_ => null
		}, out var helpRes ) ) return helpRes;
		DMainAppCore core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		switch ( context.SubAction ) {
		case "set":
			if ( context[1, "Target end point"] == "none" ) {
				if ( TargetEP != null ) core.PacketSender.Disconnect ( TargetEP );
				TargetEP = null;
				return new ( "Target disconnected." );
			}
			if ( IPEndPoint.TryParse ( context[1], out var IPEP ) ) {
				if ( TargetEP != null ) {
					try { core.PacketSender.Disconnect ( TargetEP ); } catch { }
				}
				TargetEP = new IPNetPoint ( IPEP );
				try {
					core.PacketSender.Connect ( TargetEP );
					return new ( $"Target set to IP end point {TargetEP}" );
				} catch {
					TargetEP = null;
					return new ( $"Failed to connect to IP end point {IPEP}" );
				}
			} else if ( InMemNetPoint.TryParse ( context[1], out var INMEP ) ) {
				if ( TargetEP != null ) {
					try { core.PacketSender.Disconnect ( TargetEP ); } catch { }
				}
				TargetEP = INMEP;
				try {
					core.PacketSender.Connect ( TargetEP );
					return new ( $"Target set to InMemNet point {TargetEP}" );
				} catch ( Exception e ) {
					TargetEP = null;
					return new ( $"Failed to connect to InMemNet point {INMEP}: {e.Message}" );
				}
			} else return new ( $"Provided target '{context[1]}' is not a valid end point." );
		default:
			return new ( "Missing or unknown subcommand." );
		}
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

	readonly Action<DictionaryKey, HInputEventDataHolder>[] PossibleCallbacks = new[] {
		PrintCB
	};

	protected override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if (TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"list" => $"{context.ParentAction} list: List available callbacks",
			"set" => $"{context.ParentAction} set <Callback_name>: Set active callback\n\t<Callback_name>: Name of the callback to set",
			"active" => $"{context.ParentAction} active: Print active callback",
			_ => null
		}, out var helpRes ) ) return helpRes;
		// This command is not setting the callbacks themselfs, but only sets global variable to an Action that can be used to process HInputEventDataHolder type of callback.
		switch ( context.SubAction ) {
		case "list":
			return new CommandResult ( $"Available callbacks: {string.Join ( ", ", PossibleCallbacks.Select ( (cb, i) => $"{i}: {cb.Method.Name}" ) )}" );
		case "set":
			if ( int.TryParse( context[1, "Callback selector"], out int idx ) ) {
				if ( idx < 0 || idx >= PossibleCallbacks.Length ) return new CommandResult ( $"Invalid callback index of {idx}." );
				context.CmdProc.SetVar ( CBVarName, PossibleCallbacks[idx] );
			}
			var cb = PossibleCallbacks.FirstOrDefault ( cb => cb.Method.Name == context[1] );
			if ( cb == null ) return new CommandResult ( $"Hook callback '{context[1]}' not found." );
			context.CmdProc.SetVar ( CBVarName, cb );
			return new CommandResult ( $"Hook callback set to {cb.Method.Name}." );
		case "active":
			try {
				var callback = context.CmdProc.GetVar<Action<DictionaryKey, HInputEventDataHolder>> ( CBVarName );
				return new CommandResult ( $"Active callback: {callback.Method.Name}" );
			} catch ( ArgumentException ex ) {
				return new CommandResult ( "No active callback." );
			} catch { throw; }
		default:
			return new CommandResult ( $"Missing or unknown subcommand '{context.SubAction}'." );
		}
	}

	private static void PrintCB ( DictionaryKey hookID, HInputEventDataHolder e ) {

	}
}