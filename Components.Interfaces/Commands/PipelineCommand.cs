using Components.Library;
using Components.Library.ComponentSystem;

namespace Components.Interfaces.Commands;
public class PipelineCommand : ACommand {
	private readonly List<(object key, string name, string dsc)> CreatedPipelines = [];
	private readonly HashSet<ComponentUIParametersInfo> RegisteredUIs = [];
	private readonly int MyID;
	//private readonly System.Diagnostics.StackTrace StackTrace;

	public override string Description => "Control the Pipeline system";

	private static List<string> CommandNames = ["pipeline"];
	private static List<(string, Type)> InterCommands = [
		("list", null) , ("delete", null)
		, ("new", null) , ("expand", null)
		];

	public PipelineCommand ( string parentDsc = null )
		: base ( parentDsc, CommandNames, InterCommands ) {
		MyID = base.GetHashCode () & 0xFFFF;
		//StackTrace = new System.Diagnostics.StackTrace ( 0, true );
	}

	protected override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"list" => CallName + " list: List all registered pipelines",
			"delete" => CallName + " delete <ID>: Delete a pipeline entry\n\tID: The ID of the entry to delete",
			"new" => CallName + " new <Name> [<Components>]: Create a new pipeline\n\tName: The name of the new pipeline\n\tComponents: Optional, comma-separated list of components to include",
			"expand" => CallName + " expand <ID>[<Components>]: Expand a pipeline entry by adding components\n\tID: The ID of the entry to expand\n\tComponents: Comma-separated list of components to add",
			_ => null
		}, out var helpRes ) ) return helpRes;
		switch ( context.SubAction ) {
		case "list": {
			if ( CreatedPipelines.Count == 0 ) return new CommandResult ( "No pipelines created." );
			string res = string.Join ( "\n", CreatedPipelines.Select ( ( p, i ) => $"[{i}] {p.name}: ({p.dsc})" ) );
			return new CommandResult ( res );
		}
		case "delete": {
			var selected = FindElement ( context, context.ArgID + 1, CreatedPipelines, ( id, x ) => id == x.name, "Target" );
			var joiner = Fetch<DComponentJoiner> ( context );
			joiner.UnregisterPipeline ( selected.obj.key );
			CreatedPipelines.RemoveAt ( selected.id );
			foreach ( var ui in RegisteredUIs )
				ui.NotifyDataChanged ();
			return new CommandResult ( $"Removed pipeline [{selected.id}] {selected.obj.name} ({selected.obj.dsc})." );
		}
		case "new": {
			string name = context.Args.String ( context.ArgID + 1, "Name" );
			if ( string.IsNullOrEmpty ( name ) ) return new CommandResult ( "Name cannot be empty." );

			var core = GetCore<DMainAppCore> ( context );
			var joiner = Fetch<DComponentJoiner> ( context, core );

			List<ComponentSelector> selectors = [];
			string desc = "";
			for ( int i = context.ArgID + 2; i < context.Args.ArgC; i++ ) {
				string cName = context.Args.String ( i, "Component name", 1, true ); // No other arguments after 'Name'
				selectors.Add ( CreateSelector ( cName, core ) );
				desc += (desc.Length > 0 ? ", " : string.Empty) + cName;
			}
			if ( selectors.Count < 2 ) return new CommandResult ( "At least two components are required to create a pipeline." );
			var pipelineId = joiner.RegisterPipeline ( selectors.ToArray () );
			CreatedPipelines.Add ( (pipelineId, name, desc) );
			foreach ( var ui in RegisteredUIs )
				ui.NotifyDataChanged ();
			return new CommandResult ( $"Created pipeline '{name}' with ID {CreatedPipelines.Count - 1} ({desc})." );
		}
		case "expand": {
			var selected = FindElement ( context, context.ArgID + 1, CreatedPipelines, ( id, x ) => id == x.name, "Target" );

			var core = GetCore<DMainAppCore> ( context );
			var joiner = Fetch<DComponentJoiner> ( context, core );

			List<string> compNames = selected.obj.dsc.Split ( ", " ).ToList ();
			for ( int i = context.ArgID + 2; i < context.Args.ArgC; i++ ) {
				string cName = context.Args.String ( i, "Component name", 1, true ); // No other arguments after 'Name'
				compNames.Add ( cName );
				selected.obj.dsc += ", " + cName; // Must be already longer than 2 components
			}
			List<ComponentSelector> selectors = compNames.Select ( n => CreateSelector ( n, core ) ).ToList ();
			joiner.UnregisterPipeline ( selected.obj.key );
			selected.obj.key = joiner.RegisterPipeline ( selectors.ToArray () );
			CreatedPipelines[selected.id] = selected.obj;
			foreach ( var ui in RegisteredUIs )
				ui.NotifyDataChanged ();
			return new CommandResult ( $"Expanded pipeline '{selected.obj.name}' with ID {selected.id} to ({selected.obj.dsc})." );
		}
		default: return new CommandResult ( $"Invalid action '{context.SubAction}'." );
		}
	}

	public override ComponentUIParametersInfo GetUIDescription () {
		var timeInfo = new UI_TextField.Factory ()
			.WithName ( "TimeInfo2" )
			.WithLabel ( "Current Time (Static)" )
			.WithDescription ( "Shows the time when the UI was created, demonstrating static UI values." )
			.WithPureUpdater ( () => DateTime.Now.ToString ( "yyyy-MM-dd HH:mm:ss" ) )
			.Build ();
		var groupList = new UI_DropDown.Factory ()
			.WithOptionUpdator ( () => CreatedPipelines.Select ( p => p.name ).ToList () )
			.WithEmptyOption ()
			.WithName ( "GroupList2" )
			.WithLabel ( "Pipeline Groups (Dynamic)" )
			.WithDescription ( "List of created pipeline groups, demonstrating dynamic UI values. Use the 'list' subcommand to refresh this list." )
			.ForceDynamic ()
			.AssertDynamic ()
			.Build<UI_DropDown> ();
		var compList = new UI_ListView.Factory ()
			.UpdatedByDropDown ( groupList, ( selID ) => {
				if (selID < 0 || selID >= CreatedPipelines.Count) return ["No group selected."];
				return CreatedPipelines[selID].dsc.Split ( ", " ).ToList ();
				})
			.WithName ( "ComponentList2" )
			.WithLabel ( "Group Content" )
			.WithDescription ( "List of components in the selected pipeline group, demonstrating dependent UI values. Use the 'list' subcommand to refresh this list." )
			.Build ();
		var pipelineCnt = new UI_IntField.Factory ()
			.WithName ( "PipelineCount" )
			.WithLabel ( "Total Pipelines" )
			.WithDescription ( "Total number of created pipelines, demonstrating dynamic UI values. Use the 'list' subcommand to refresh this count." )
			.WithPureUpdater ( () => CreatedPipelines.Count )
			.Build ();

		var ret = new ComponentUIParametersInfo.Factory ()
			.WithGroupID ( MyID )
			.WithComponentType ( GetType () )
			.AddParameters ( timeInfo, groupList, compList, pipelineCnt )
			.WithName ( "Pipeline Command" )
			.WithDescription ( "Command to manage component pipelines. Use subcommands to create, list, delete, or expand pipelines." )
			.Build () as ComponentUIParametersInfo;
		RegisteredUIs.Add ( ret );
		return ret;
	}

	static ComponentSelector CreateSelector ( string name, DMainAppCore core ) {
		var comp = core.Fetch ( typeName: name );
		if (comp == null)
			throw new ArgumentException ( $"Component '{name}' not found in core." );
		return new ComponentSelector ( core, componentType: comp.GetType () );
	}
}