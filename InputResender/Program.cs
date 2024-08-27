using System;
using Components.Library;
using System.Threading.Tasks;
using Components.Interfaces;
using InputResender.Services;
using InputResender.Commands;
using Components.Interfaces.Commands;
using Components.Factories;

namespace InputResender;
internal static class Program {
	private static CommandProcessor cmdProcessor;
	private static DMainAppCore mainAppCore;
	private static ConsoleManager console;

	[STAThread]
	static void Main ( string[] args ) {
		ApplicationConfiguration.Initialize ();

		console = new ConsoleManager ( Console.WriteLine, Console.ReadLine, Console.Write );
		ArgParser parser = new ( string.Join ( " ", args ), console.WriteLine );
		Config.Load ( parser.String ( "cfg", null ) );

		cmdProcessor = new ( console.WriteLine );
		cmdProcessor.AddCommand ( new BasicCommands ( console.WriteLine, Console.Clear, () => throw new NotImplementedException () ) );
		cmdProcessor.AddCommand ( new TopLevelLoader () );

		var startCommands = Config.FetchAutoCommands ( Config.AutostartName );
		foreach ( var cmd in startCommands ) {
			console.WriteLine ( $"$> {cmd}" );
			var res = cmdProcessor.ProcessLine ( cmd );
			if ( Config.PrintAutoCommands ) {
				bool canAppend = res is not ErrorCommandResult;
				if ( canAppend ) canAppend &= res != null;
				var lastMsg = console.LastLine ();
				if ( canAppend ) canAppend &= lastMsg != null;
				if ( canAppend ) canAppend &= lastMsg.StartsWith ( "$> " );
				if ( canAppend ) canAppend &= $"{lastMsg} =:= {res.Message}".Length < Config.MaxOnelinerLength;


				if ( canAppend ) console.Append ( " =:= " + res.Message );
				else console.WriteLine ( " - " + res.Message );
			}
		}

		console.WriteLine ( "Program started. Type 'help' for a list of commands. Type 'exit' to close the program." );
		while ( true ) {
			string line = console.ReadLine ();
			if ( line == ConsoleManager.EOF ) break;
			if ( line == null ) {
				Task.Delay ( 1 ).Wait ();
				continue;
			}
			line = line.Trim ();
			if ( line == "exit" ) break;
			if ( line == string.Empty ) continue;

			var res = cmdProcessor.ProcessLine ( line );
			cmdProcessor?.Owner?.FlushDelayedMsgs ( console.WriteLine );

			string printRes = null;
			if ( res == null ) printRes = "<null>";
			else if ( string.IsNullOrWhiteSpace ( res.Message ) ) printRes = "<empty>";
			else if ( res.Message.Contains ( Environment.NewLine ) )
				printRes = " Result:" + Environment.NewLine + res.Message.PrefixAllLines ( " . " );
			else printRes = " res: " + res.Message;

			bool canAppend = res is not ErrorCommandResult;
			if ( canAppend ) canAppend &= printRes != null;
			var lastMsg = console.LastLine ();
			if ( canAppend ) canAppend &= lastMsg != null;
			if ( canAppend ) canAppend &= lastMsg.StartsWith ( line );
			if ( canAppend ) canAppend &= $"$> {lastMsg} =:= {printRes}".Length < Config.MaxOnelinerLength;
			if ( canAppend ) console.Append ( " =:= " + printRes );
			else console.WriteLine ( $" . {printRes}" );
		}

		console.WriteLine ( "Program closed." );
	}
}