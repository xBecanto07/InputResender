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
		interCommands.Add ( "typeof" );
	}

	protected override RetT ExecIner ( CommandProcessor.CmdContext context ) {
		var core = context.CmdProc.GetVar<CoreBase> ( ActiveCoreVarName );
		if ( core == null ) return new RetT ( null, "No active core." );

		switch ( context.SubAction ) {
		case "act":
			return new RetT ( core, $"Active core: '{core.Name}'" );
		case "typeof":
			Type reqType = MdxExtensions.FindType ( context[1, "Type"] );
			if ( reqType == null ) return new RetT ( core, $"Invalid type name: {context[1]}" );
			var reqComp = core.Fetch ( reqType );
			if ( reqComp == null ) return new RetT ( core, $"Component of type {reqType.Name} not found." );
			return new RetT ( core, $"For definition '{reqType.Name}' was found variant '{reqComp.GetType ().Name}'." );
		default: return new RetT ( core, "Invalid action." );
		}
	}
}