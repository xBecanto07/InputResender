using System;
using Components.Library;
using System.Threading.Tasks;
using Components.Interfaces;
using InputResender.Services;
using InputResender.Commands;
using Components.Interfaces.Commands;

namespace InputResender;
internal static class Program {
	private static CommandProcessor cmdProcessor;
	private static DMainAppCore mainAppCore;

	[STAThread]
	static void Main ( string[] args ) {
		ApplicationConfiguration.Initialize ();

		ArgParser parser = new ( string.Join ( " ", args ), Console.WriteLine );
		Config.Load ( parser.String ( "cfg", null ) );

		cmdProcessor = new ( Console.WriteLine );
		cmdProcessor.AddCommand ( new BasicCommands ( Console.WriteLine, Console.Clear, () => throw new NotImplementedException () ) );
		cmdProcessor.AddCommand ( new CoreManagerCommand () );
		cmdProcessor.AddCommand ( new NetworkManagerCommand () );
		cmdProcessor.AddCommand ( new GUICommands () );

		var startCommands = Config.FetchAutoCommands ( Config.AutostartName );
		foreach ( var cmd in startCommands ) {
			cmdProcessor.ProcessLine ( cmd );
		}

		Console.WriteLine ( "Program started. Type 'help' for a list of commands. Type 'exit' to close the program." );
		while ( true ) {
			string line = Console.ReadLine ();
			if ( line == "exit" ) break;
			var res = cmdProcessor.ProcessLine ( line );
			if ( res == null ) continue;
			if ( string.IsNullOrWhiteSpace ( res.Message ) ) continue;
			if ( res.Message.Contains ( Environment.NewLine ) )
				Console.WriteLine ( " Result:" + Environment.NewLine + res.Message.PrefixAllLines ( " . " ) );
			else Console.WriteLine ( " res: " + res.Message );
		}

		Console.WriteLine ( "Program closed." );
	}
}