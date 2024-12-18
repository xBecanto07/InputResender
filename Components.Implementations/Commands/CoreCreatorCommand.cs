using Components.Library;
using RetT = Components.Library.ClassCommandResult<Components.Interfaces.DMainAppCore>;
using Components.Implementations;
using System.Linq;
using InputResender.Commands;
using Components.Interfaces;

namespace InputResender.CLI;
public class CoreCreatorCommand : ACommand {
    override public string Description => "Creates a new Core instance.";

    public CoreCreatorCommand ( string parentHelp = null ) : base ( parentHelp ) {
        commandNames.Add ( "new" );
        commandNames.Add ( "create" );

        interCommands.Add ( "comp" );
    }


    override protected RetT ExecIner ( CommandProcessor.CmdContext context ) {
        if ( context.SubAction == "comp" ) {
            if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => $"{context.ParentAction} comp <Component>: Add component to core\n\tComponent: Component to add [PacketSender]", out var helpRes ) ) return new RetT ( null, helpRes.Message );

            var Core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );

            switch ( context[1, "Component"].ToLower () ) {
            case "packetsender":
            case "sender":
                var added = new VPacketSender ( Core );
                return new RetT ( Core, $"Added {added} to core." );
            default:
				return new RetT ( Core, $"Unknown component '{context[2, "Component"]}'." );
			}
        } else {
            var selector = DMainAppCore.CompSelect.All;
			selector &= ~DMainAppCore.CompSelect.PacketSender;
			var Core = DMainAppCoreFactory.CreateDefault ( selector, ( c ) => new VInputSimulator ( c ) );
			context.CmdProc.SetVar ( CoreManagerCommand.ActiveCoreVarName, Core );
			return new RetT ( Core, "Core created." );
		}
    }
}