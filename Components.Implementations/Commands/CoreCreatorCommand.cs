using Components.Library;
using RetT = Components.Library.ClassCommandResult<Components.Interfaces.DMainAppCore>;
using Components.Implementations;
using System.Linq;
using InputResender.Commands;

namespace InputResender.CLI;
public class CoreCreatorCommand : ACommand {
    override public string Description => "Creates a new Core instance.";
    override public string Help => $"{parentCommandHelp} {commandNames.First ()}";

    public CoreCreatorCommand ( string parentHelp = null ) : base ( parentHelp ) {
        commandNames.Add ( "new" );
        commandNames.Add ( "create" );
    }


    override protected RetT ExecIner ( CommandProcessor.CmdContext context ) {
        var Core = DMainAppCoreFactory.CreateDefault ( ( c ) => new VInputSimulator ( c ) );
        context.CmdProc.SetVar ( CoreManagerCommand.ActiveCoreVarName, Core );
        return new RetT ( Core, "Core created." );
    }
}