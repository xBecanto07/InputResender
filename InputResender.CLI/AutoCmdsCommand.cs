using Components.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using Components.Interfaces;

namespace InputResender.CLI;
public class AutoCmdsCommand : DCommand<DMainAppCore> {
	public override string Description => "Manage automatic commands that run on specific events";
	protected override bool PrintHelpOnEmpty => true;
	private static List<string> CommandNames = ["auto", "autocmd", "autocmds"];
	private static List<(string, Type)> InterCommands = [
		("list", null),
		("run", null),
		("load", null)
		];
	public AutoCmdsCommand ( DMainAppCore owner, string parentDsc = null )
		: base ( owner, parentDsc, CommandNames, InterCommands ) { }
	protected override CommandResult ExecIner ( CommandProcessor<DMainAppCore>.CmdContext context ) {
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"list" => CallName + " list: List all automatic commands",
			"run" => CallName + " run <Name>: Run command group\n\tName: The name of the command group to run",
			"load" => CallName + " load <Path>: Load automatic commands from a file (currently not implemented)\n\tPath: The path to the file containing the commands",
			_ => Help
		}, out var helpRes ) ) return helpRes;
		switch ( context.SubAction ) {
		case "list": {
			context.Args.RegisterSwitch ( 'c', "compact" );
			var ACnames = Owner.Fetch<Config> ().ValidAutostartNames.ToArray ();
			if ( ACnames.Length == 0 )
				return new CommandResult ( "No automatic commands configured." );
			System.Text.StringBuilder sb = new ();
			bool verbose = !context.Args.Present ( "--compact" );
			sb.AppendLine ( "Automatic command groups:" );
			for ( int i = 0; i < ACnames.Length; i++ ) {
				sb.AppendLine ( $"[{i}] {ACnames[i]}:" );
				if ( verbose ) {
					var cmds = Owner.Fetch<Config> ().FetchAutoCommands ( ACnames[i] ).ToList ();
					if ( cmds == null || cmds.Count == 0 ) {
						sb.AppendLine ( "\t<No commands>" );
						continue;
					}
					for ( int j = 0; j < cmds.Count; j++ ) {
						sb.AppendLine ( $"\t#{j,2} |> {cmds[j]}" );
					}
				}
			}
			return new CommandResult ( sb.ToString () );
		}
		case "load": {
			// Placeholder for future implementation
			return new CommandResult ( "The 'load' command is not implemented yet." );
		} case "run": {
			string name = context.Args.String ( context.ArgID + 1, "Name" );
			if ( string.IsNullOrEmpty ( name ) )
				return new CommandResult ( "Name cannot be empty." );
			var cmds = Owner.Fetch<Config> ().FetchAutoCommands ( name );
			if ( cmds == null || cmds.Count == 0 )
				return new CommandResult ( $"No commands found for group '{name}'." );
			var cliWrapper = context.CmdProc.GetVar<CliWrapper> ( CliWrapper.CLI_VAR_NAME );
			int done = 0;
			foreach ( var cmd in cmds ) {
				if ( cmd.StartsWith ( '#' ) ) continue;
				if ( Owner.Fetch<Config> ().PrintAutoCommands ) cliWrapper.ProcessLine ( cmd, true );
				else cliWrapper.CmdProc.ProcessLine ( cmd );
				done++;
			}
			return new CommandResult ( $"Executed {done} commands from group '{name}'." );
		}
		default:
			return new CommandResult ( $"Unknown subcommand '{context.SubAction}'." );
		}
	}
}