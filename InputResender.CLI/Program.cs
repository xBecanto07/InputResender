using System;
using Components.Library;
using System.Threading.Tasks;
using Components.Interfaces;
using InputResender.Services;

namespace InputResender.CLI;
public static class Program {
	private static CommandProcessor cmdProcessor;
	private static ConsoleManager console;

	public static void Main ( string[] args, ACommandLoader TLLoader ) {
		console = new ConsoleManager ( Console.WriteLine, Console.ReadLine, Console.Write );
		ArgParser parser = new ( string.Join ( " ", args ), console.WriteLine );
		Config.Load ( parser.String ( "cfg", null ) );

		cmdProcessor = new ( console.WriteLine );
		cmdProcessor.AddCommand ( new BasicCommands ( console.WriteLine, Console.Clear, () => throw new NotImplementedException () ) );
		cmdProcessor.AddCommand ( new FactoryCommandsLoader () );
		if ( TLLoader != null ) cmdProcessor.AddCommand ( TLLoader );

		var startCommands = Config.FetchAutoCommands ( Config.AutostartName );
		foreach ( var cmd in startCommands ) {
			console.WriteLine ( $"$> {cmd}" );
			var res = cmdProcessor.ProcessLine ( cmd );
			if ( Config.PrintAutoCommands ) PrintResult ( res, console, Config.MaxOnelinerLength );
		}

		console.WriteLine ( "Program started. Type 'help' for a list of commands. Type 'exit' to close the program." );
		while ( true ) {
			if (Config.ResponsePrintFormat == Config.PrintFormat.Normal
				|| Config.ResponsePrintFormat == Config.PrintFormat.Full)
				console.Write ( "$> " );
			string line = console.ReadLineBlocking ();
			if ( line == ConsoleManager.EOF ) break;
			line = line.Trim ();
			if ( line == "exit" ) break;
			if ( line == string.Empty ) continue;

			var res = cmdProcessor.ProcessLine ( line );
			cmdProcessor?.Owner?.FlushDelayedMsgs ( console.WriteLine );

			if ( Config.ResponsePrintFormat != Config.PrintFormat.None )
				PrintResult ( res, console, Config.MaxOnelinerLength );
		}

		console.WriteLine ( "Program closed." );
	}

	enum MsgType { None, Result, Error }
	public static string PrintResult ( CommandResult res, ConsoleManager console, int maxOnelinerLength ) {
		if ( res == null ) throw new ArgumentNullException ( nameof ( res ) );
		if ( console == null ) throw new ArgumentNullException ( nameof ( console ) );

		string printRes = null;
		MsgType msgType = MsgType.None;
		bool batch = false;

		if ( res == null ) if ( batch ) return string.Empty; else printRes = "<null>";

		if ( string.IsNullOrWhiteSpace ( res.Message ) ) if ( batch ) return string.Empty; else printRes = "<empty>";

		if ( res is ErrorCommandResult errRes ) msgType = MsgType.Error;
		else msgType = MsgType.Result;
		printRes = res.Message;

		string ret = string.Empty;

		if ( batch ) {
			printRes = msgType switch {
				MsgType.Error => $"\u0001error\u0002 {printRes}\u0003",
				MsgType.Result => $"\u0001result\u0002 {printRes}\u0003",
				_ => printRes
			};
			ret = printRes.Replace ( "\r\n", "\n" );
			console.WriteLine ( ret );
		} else {
			string oneLiner = msgType switch {
				MsgType.Error => $" =:= Error: {printRes}",
				MsgType.Result => $" =:= {printRes}",
				_ => printRes
			};

			bool canAppend = !string.IsNullOrEmpty ( oneLiner );
			var lastMsg = console.LastLine ();
			if ( canAppend ) canAppend &= lastMsg != null;
			if ( canAppend ) canAppend &= lastMsg.StartsWith ( "$> " );
			if ( canAppend ) canAppend &= lastMsg.Length + oneLiner.Length < maxOnelinerLength;
			if ( canAppend ) console.Append ( " =:= " + oneLiner );
			else {
				oneLiner = msgType switch {
					MsgType.Error => $" - Error: {printRes.PrefixAllLines ( " ! " )}",
					MsgType.Result => $" - {printRes}",
					_ => printRes
				};

				if (oneLiner.Length < maxOnelinerLength && !oneLiner.Contains ( "\r\n" ) && !oneLiner.Contains ( '\n' ) ) {
					console.WriteLine ( ret = oneLiner );
				} else {
					printRes = msgType switch {
						MsgType.Error => "Error:" + Environment.NewLine + printRes.PrefixAllLines ( " ! " ),
						MsgType.Result => " Result:" + Environment.NewLine + printRes.PrefixAllLines ( " . " ),
						_ => printRes
					};
					console.WriteLine ( ret = printRes );
				}
			}
		}
		return ret;
	}
}