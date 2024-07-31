using Components.Library;

namespace Components.Interfaces.Commands;
public class NetworkManagerCommand : ACommand<CommandResult> {
    public enum Act { HostList, Connect, Disconnect }
    override public string Description => "Manages network connections.";
    override public string Help => $"{parentCommandHelp} {commandNames.First ()} ({string.Join ( "|", subCommands.Keys )}";

    public NetworkManagerCommand () : base ( null ) {
		commandNames.Add ( "network" );

		subCommands.Add ( "hostlist", new ListHostsNetworkCommand () );
	}

    public static string CreateCommand ( Act act ) => $"NetworkManager {act.ToString ().ToLower ()}";
}

public class ListHostsNetworkCommand : ACommand<CommandResult> {
    override public string Description => "Lists available local hosts.";
    override public string Help => $"{parentCommandHelp} {commandNames.First ()}";

    public ListHostsNetworkCommand ( string parentHelp = null ) : base ( parentHelp ) {
        commandNames.Add ( "hostlist" );
    }

    override protected CommandResult ExecIner ( ICommandProcessor context, ArgParser args, int argID = 1 ) {
        var core = context.GetVar<DMainAppCore> ( "ActCore" );
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