using Components.Library;
using InputResender.Commands;
using InputResender.Services;
using InputResender.Services.NetClientService;
using InputResender.Services.NetClientService.InMemNet;

namespace Components.Interfaces.Commands;
public class NetworkManagerCommand : ACommand {
	public enum Act { HostList, Connect, Disconnect }
	override public string Description => "Manages network connections.";

	public NetworkManagerCommand () : base ( null ) {
		commandNames.Add ( "network" );

		subCommands.Add ( "hostlist", new ListHostsNetworkCommand ( this ) );
		subCommands.Add ( "conn", new NetworkConnsManagerCommand ( this ) );
		subCommands.Add ( "callback", new NetworkCallbacks ( this ) );
		subCommands.Add ( "info", new EndPointInfoCommand ( this ) );
	}

	public static string CreateCommand ( Act act ) => $"NetworkManager {act.ToString ().ToLower ()}";
}

public class ListHostsNetworkCommand : ACommand {
	override public string Description => "Lists available local hosts.";

	public ListHostsNetworkCommand ( NetworkManagerCommand parentHelp ) : base ( parentHelp.CallName ) {
		commandNames.Add ( "hostlist" );
	}

	override protected CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => "network hostlist: Lists available local hosts", out var helpRes ) ) return helpRes;

		var core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		var sender = core.Fetch<DPacketSender> ();
		if ( sender == null ) return new CommandResult ( "No packet sender available." );

		var SB = new System.Text.StringBuilder ();
		foreach ( var net in sender.EPList ) {
			foreach ( var EP in net ) {
				string ss = EP.ToString ();
				if ( ss.StartsWith ( "127.0.0.1" ) ) continue;
				if ( ss.StartsWith ( "::1" ) ) continue;
				if ( ss.Contains ( "localhost" ) ) continue;
				SB.AppendLine ( EP.ToString () );
			}
		}
		return new CommandResult ( SB.ToString () );
	}
}

public class NetworkConnsManagerCommand : ACommand {
	public override string Description => "Manages network connections.";

	public NetworkConnsManagerCommand ( NetworkManagerCommand parent ) : base ( parent.CallName ) {
		commandNames.Add ( "conn" );

		interCommands.Add ( "list" );
		interCommands.Add ( "send" );
	}

	override protected CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"list" => "network conn list: Lists all connections",
			"send" => "network conn send <Message>: Sends message to all connections\n\t<Message>: Message to send",
			_ => null
		}, out var helpRes ) ) return helpRes;

		var core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		var sender = core.Fetch<DPacketSender> ();
		if ( sender == null ) return new CommandResult ( "No packet sender available." );

		switch ( context.SubAction ) {
		case "list":
			return new CommandResult ( string.Join ( '\n', sender.Connections ) );
		case "send":
			byte[] data = System.Text.Encoding.UTF8.GetBytes ( context[1, "Message"] );
			HMessageHolder msgHolder = new ( HMessageHolder.MsgFlags.None, data );
			sender.Send ( msgHolder );
			return new CommandResult ( $"Sent {data.Length} bytes." );
		default: return new CommandResult ( $"Unknown action '{context.SubAction}'." );
		}
	}
}

public class NetworkCallbacks : ACommand {
	private enum CallbackType { None, Print, Fcn }
	private CallbackType NewConnCB = CallbackType.None;
	private CallbackType RecvCB = CallbackType.None;
	private CommandProcessor.CmdContext lastContext;

	public const string RECVCBVarName = "RecvCB";
	public const string NEWCONNCBVarName = "NewConnCB";

	override public string Description => "Manages network callbacks.";

	public NetworkCallbacks ( NetworkManagerCommand parent ) : base ( parent.CallName ) {
		commandNames.Add ( "callback" );

		interCommands.Add ( "list" );
		interCommands.Add ( "recv" );
		interCommands.Add ( "newconn" );
	}

	override protected CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if (TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"list" => "network callback list: Lists current callbacks",
			"recv" => $"network callback recv <Callback>: Sets receive callback\n\t<Callback>: {{{string.Join ( ", ", Enum.GetNames ( typeof ( CallbackType ) ) )}}}",
			"newconn" => $"network callback newconn <Callback>: Sets new connection callback\n\t<Callback>: {{{string.Join ( ", ", Enum.GetNames ( typeof ( CallbackType ) ) )}}}",
		}, out var helpRes ) ) return helpRes;

		lastContext = context;
		var core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		var sender = core.Fetch<DPacketSender> ();
		if ( sender == null ) return new CommandResult ( "No packet sender available." );

		switch ( context.SubAction ) {
		case "list":
			return new CommandResult ( $"New connection callback (newconn): {NewConnCB}\nReceive callback (recv): {RecvCB}" );
		case "recv":
			var recvcb = context.Args.EnumC<CallbackType> ( context.ArgID + 1, "Callback (onReceive) type", true );
			if ( recvcb == RecvCB ) return new CommandResult ( $"Callback already set to {recvcb}." );
			else if ( recvcb == CallbackType.None ) {
				RecvCB = CallbackType.None;
				sender.OnReceive -= RecvCallback;
				return new CommandResult ( "Callback removed." );
			} else {
				RecvCB = recvcb;
				sender.OnReceive += RecvCallback;
				return new CommandResult ( $"Callback set to {recvcb}." );
			}
		case "newconn":
			var newconncb = context.Args.EnumC<CallbackType> ( context.ArgID + 1, "Callback (onNewConn) type", true );
			if ( newconncb == NewConnCB ) return new CommandResult ( $"Callback already set to {newconncb}." );
			else if ( newconncb == CallbackType.None ) {
				NewConnCB = CallbackType.None;
				sender.OnNewConn -= NewConnCallback;
				return new CommandResult ( "Callback removed." );
			} else {
				NewConnCB = newconncb;
				sender.OnNewConn += NewConnCallback;
				return new CommandResult ( $"Callback set to {newconncb}." );
			}
		default: return new CommandResult ( $"Unknown action '{context.SubAction}'." );
		}
	}

	private DPacketSender.CallbackResult RecvCallback ( NetMessagePacket msg, bool wasProcessed ) {
		byte[] binData = msg.Data.InnerMsg;
		switch ( RecvCB ) {
		case CallbackType.None: return DPacketSender.CallbackResult.Skip;
		case CallbackType.Print:
			string printInfo = $"Received {binData.Length} bytes ({(wasProcessed ? "already processed" : "not processed")}): ";
			if ( binData.All ( b => b >= 32 && b < 127 ) ) printInfo += $"'{System.Text.Encoding.UTF8.GetString ( binData )}'";
			else printInfo += $"{binData.ToHex ()}";
			lastContext.CmdProc.ProcessLine ( $"print \"{printInfo}\"" );
			return DPacketSender.CallbackResult.None;
		case CallbackType.Fcn:
			try {
				var CB = lastContext.CmdProc.GetVar<DPacketSender.OnReceiveHandler> ( RECVCBVarName );
				var ret = CB ( msg, wasProcessed );
				return ret;
			} catch ( Exception ex ) {
				lastContext.CmdProc.Owner.PushDelayedError ( "Issue with Recv callback function.", ex );
				return DPacketSender.CallbackResult.Skip;
			}
		default: return DPacketSender.CallbackResult.Skip;
		}
	}

	private void NewConnCallback ( object connInfo ) {
		switch ( NewConnCB ) {
		case CallbackType.None: return;
		case CallbackType.Print: lastContext.CmdProc.ProcessLine ( $"print \"New connection: {connInfo}\"" ); return;
		case CallbackType.Fcn:
			try {
				var CB = lastContext.CmdProc.GetVar<Action<object>> ( NEWCONNCBVarName );
				CB ( connInfo );
			} catch (Exception ex) {
				lastContext.CmdProc.Owner.PushDelayedError ( "Issue with NewConn callback function.", ex );
			}
			return;
		}
	}
}

public class EndPointInfoCommand : ACommand {
	public override string Description => "Gets information about a specific endpoint.";
	public EndPointInfoCommand (NetworkManagerCommand parentHelp) : base (parentHelp.CallName) {
		commandNames.Add ( "info" );
	}

	protected override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"info" => "network info <EndPoint>: Gets information about a specific endpoint\n\t<EndPoint>: IP end point or InMemNet point",
			_ => null
		}, out var helpRes ) ) return helpRes;

		var core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		var sender = core.Fetch<DPacketSender> ();
		if ( sender == null ) return new CommandResult ( "No packet sender available." );
		string ret = string.Empty;
		// This is now constructing new EP, which therefore will not have reference to proper NetDevice, thus throwing NotBoundException... (at least with InMemNetPoint, might work with IPNetPoint)
		if ( InMemNetPoint.TryParse ( context[0, "EndPoint"], out var IMEP ) )
			ret = sender.GetEPInfo ( IMEP );
		else if ( IPNetPoint.TryParse ( context[0, "EndPoint"], out var IPP ) )
			ret = sender.GetEPInfo ( IPP );
		else return new CommandResult ( $"Invalid endpoint: {context[0]}" );
		return new ( ret );
	}
}