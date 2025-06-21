using Components.Library;
using System.Linq;
using RetT = Components.Library.ClassCommandResult<Components.Library.CoreBase>;

namespace InputResender.Commands;
public class CoreManagerCommand : ACommand {
	public const string ActiveCoreVarName = "ActCore";
	public enum Act { Create, Select, Delete, List }
	override public string Description => "Offers access to Core functionalities";

	public static string CreateCommand ( Act act ) => $"CreateCore {act.ToString ().ToLower ()}";

	public CoreManagerCommand () : base ( null ) {
		commandNames.Add ( "core" );

		interCommands.Add ( "act" );
		interCommands.Add ( "typeof" );
		interCommands.Add ( "list" );
	}

	protected override RetT ExecIner ( CommandProcessor.CmdContext context ) {
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"act" => "core act: Prints the name of the active core.",
			"typeof" => "core typeof <Type>\n\tType: Subtype of ComponentBase to find what specific variant of the component is registered in active core.",
			"list" => "core list: List all registered components",
			_ => null
		}, out var helpRes ) ) return new ( null, helpRes.Message );

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
		case "list":
			System.Text.StringBuilder SB = new ();
			SB.AppendLine ( $"Core '{core.Name}' has following components:" );
			int compID = 0;
			foreach ( (var key, var comp) in core.RegisteredComponents ) {
				if ( compID % 2 == 0 ) SB.AppendLine ();
				else SB.Append ( " \t| " );
				SB.Append ( $"{key} : {comp.Name} ({comp.VariantName}, {comp.GroupID}, {comp.Priority})".PadRight ( 54, ' ' ) );
				compID++;
			}
			return new ( core, SB.ToString () );
		default: return new RetT ( core, $"Invalid action '{context.SubAction}' for '{context.ParentAction}'." );
		}
	}
}