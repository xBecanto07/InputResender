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

		interCommands.Add ( "act" );
		interCommands.Add ( "own" );
	}

	protected override RetT ExecIner ( CommandProcessor context, ArgParser args, int argID = 1 ) {
		var core = context.GetVar<CoreBase> ( ActiveCoreVarName );
		if ( core == null ) return new RetT ( null, "No active core." );

		string act = args.String ( argID, "Action" );
		if ( act == "act" ) {
			return new RetT ( core, "Core activated." );
		} else {
			return new RetT ( core, "Unknown action." );
		}
	}
}