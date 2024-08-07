using Components.Library;
using System.Linq;
using RetT = Components.Library.ClassCommandResult<Components.Library.CoreBase>;

namespace InputResender.Commands;
public class CoreManagerCommand : ACommand<RetT> {
    public const string ActiveCoreVarName = "ActCore";
	public enum Act { Create, Select, Delete, List }
    override public string Description => "Offers access to Core functionalities";

    public static string CreateCommand ( Act act ) => $"CreateCore {act.ToString ().ToLower ()}";

    public CoreManagerCommand () : base ( null ) {
        commandNames.Add ( "core" );
    }

	/*protected override RetT ExecIner ( ICommandProcessor context, ArgParser args, int argID = 1 ) {
        return new RetT (null, null, false, new NotImplementedException () );
	}*/
}