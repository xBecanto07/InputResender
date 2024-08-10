using Components.Interfaces;
using Components.Library;
using InputResender.Commands;
using InputResender.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using static Components.Interfaces.DPacketSender;

namespace Components.Implementations;
public class ConnectionManagerCommand : ACommand {
	enum CBSel { None, Print }
	CBSel CBSelector = CBSel.Print;
	override public string Description => "Connection manager.";
	private ICommandProcessor lastContext;

	public ConnectionManagerCommand ( string parent = null ) : base ( parent ) {
		commandNames.Add ( "conns" );

		interCommands.Add ( "list" );
		interCommands.Add ( "send" );
		interCommands.Add ( "close" );
		interCommands.Add ( "callback" );
	}

	override protected CommandResult ExecIner ( ICommandProcessor context, ArgParser args, int argID ) {
		var core = context.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		if ( core.Fetch<DPacketSender> () is not VPacketSender sender )
			return new CommandResult ( "No packet sender available." );
		if ( sender == null ) return new CommandResult ( "No packet sender available." );

		switch ( args.String ( argID, "Action" ) ) {
		case "list": {
			System.Text.StringBuilder SB = new ();
			int i = 0;
			foreach ( var conn in sender.ActiveConns ) {
				SB.AppendLine ( $"#{i++}: {conn.Value}" );
			}
			return new CommandResult ( SB.ToString () );
		}
		case "send": {
			byte[] data = System.Text.Encoding.UTF8.GetBytes ( args.String ( argID + 2, "Data" ) );
			var conn = FindConn ( sender, args, argID, out CommandResult errMsg );
			if ( errMsg != null ) return errMsg;

			if ( !conn.Send ( data ) ) return new CommandResult ( $"Failed to send data to '{conn}'." );
			return new CommandResult ( $"Sent {data.Length} bytes to '{conn}'." );
		}
		case "close": {
			var conn = FindConn ( sender, args, argID, out CommandResult errMsg );
			if ( errMsg != null ) return errMsg;

			conn.Close ();
			return new CommandResult ( $"Connection to '{conn}' should be closed." );
		}
		case "callback": {
			var conn = FindConn ( sender, args, argID, out CommandResult errMsg );
			if ( errMsg != null ) return errMsg;
			if ( !Enum.TryParse ( args.String ( argID + 2, "Callback selection" ), true, out CBSelector ) )
				return new CommandResult ( $"Invalid callback selection." );
			switch ( CBSelector ) {
			case CBSel.None:
				if (lastContext == null) return new CommandResult ( "No callback to remove." );
				sender.OnReceive -= RecvCallback;
				lastContext = null;
				return new CommandResult ( "Callback removed." );
			default:
				if ( lastContext != null )
					sender.OnReceive -= RecvCallback; // Ensure only one callback is set.
				lastContext = context;
				sender.OnReceive += RecvCallback;
				return new CommandResult ( "Callback set." );
			}
		}
		default:
			return new CommandResult ( "Unknown action." );
		}
	}

	private CallbackResult RecvCallback ( byte[] data, bool wasProcessed ) {
		switch ( CBSelector ) {
		case CBSel.None: return CallbackResult.Skip | CallbackResult.Stop;
		case CBSel.Print:
			lastContext.ProcessLine ( $"print \"{System.Text.Encoding.UTF8.GetString ( data )}\"" );
			return CallbackResult.None;
		default: return CallbackResult.Skip;
		}
	}

	protected NetworkConnection FindConn ( VPacketSender sender, ArgParser args, int argID, out CommandResult errMsg ) {
		string target = args.String ( argID + 1, "Target" );
		if ( string.IsNullOrEmpty ( target ) ) {
			errMsg = new CommandResult ( "No target specified." );
			return null;
		}

		if ( target.StartsWith ( '#' ) ) {
			if ( !int.TryParse ( target.Substring ( 1 ), out int index ) ) {
				errMsg = new CommandResult ( $"Invalid index '{target}'." );
				return null;
			}
			if ( index < 0 || index >= sender.ActiveConns.Count ) {
				errMsg = new CommandResult ( $"Index out of range." );
				return null;
			}
			target = sender.ActiveConns.ElementAt ( index ).Key.ToString ();
		}

		var ret = sender.ActiveConns.FirstOrDefault ( x => x.Key.ToString () == target, new ( null, null ) );
		if ( ret.Key == null ) {
			errMsg = new CommandResult ( $"Target '{target}' not found." );
			return null;
		} else {
			errMsg = null;
			return ret.Value;
		}
	}
}