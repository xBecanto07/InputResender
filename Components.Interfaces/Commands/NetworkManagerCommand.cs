using Components.Library;
using InputResender.Commands;

namespace Components.Interfaces.Commands;
public class NetworkManagerCommand : ACommand<CommandResult> {
    public enum Act { HostList, Connect, Disconnect }
    override public string Description => "Manages network connections.";

    public NetworkManagerCommand () : base ( null ) {
		commandNames.Add ( "network" );

		subCommands.Add ( "hostlist", new ListHostsNetworkCommand ( this ) );
		subCommands.Add ( "conn", new NetworkConnsManagerCommand ( this ) );
	}

    public static string CreateCommand ( Act act ) => $"NetworkManager {act.ToString ().ToLower ()}";
}

public class ListHostsNetworkCommand : ACommand<CommandResult> {
    override public string Description => "Lists available local hosts.";

    public ListHostsNetworkCommand ( NetworkManagerCommand parentHelp ) : base ( parentHelp.CallName ) {
        commandNames.Add ( "hostlist" );
    }

    override protected CommandResult ExecIner ( ICommandProcessor context, ArgParser args, int argID = 1 ) {
        var core = context.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
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

	override protected CommandResult ExecIner ( ICommandProcessor context, ArgParser args, int argID ) {
		var core = context.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		var sender = core.Fetch<DPacketSender> ();
		if ( sender == null ) return new CommandResult ( "No packet sender available." );

        string action = args.String ( argID + 1, "Action" );
		switch ( action ) {
		case "list":
            return new CommandResult ( string.Join ( '\n', sender.Connections ) );
		case "send":
            string msg = args.String ( argID + 1, "Message" );
            byte[] data = System.Text.Encoding.UTF8.GetBytes ( msg );
            sender.Send ( data );
			return new CommandResult ( $"Sent {data.Length} bytes." );
		default:
			return new CommandResult ( $"Unknown action '{action}'." );
		}
	}
}