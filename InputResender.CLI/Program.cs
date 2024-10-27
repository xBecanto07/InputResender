using System;
using Components.Library;
using System.Threading.Tasks;
using Components.Interfaces;
using InputResender.Services;

namespace InputResender.CLI;
public static class Program {
	private static CliWrapper cliWrapper;

	public static void Main ( string[] args, ACommandLoader TLLoader, ConsoleManager console ) {
		cliWrapper = new ( console );
		ArgParser parser = new ( string.Join ( " ", args ), console.WriteLine );
		Config.Load ( parser.String ( "cfg", null ) );

		cliWrapper.CmdProc.SetVar ( CliWrapper.CLI_VAR_NAME, cliWrapper );
		cliWrapper.CmdProc.AddCommand ( new BasicCommands ( console.WriteLine, console.Clear, () => throw new NotImplementedException () ) );
		cliWrapper.CmdProc.AddCommand ( new FactoryCommandsLoader () );
		cliWrapper.CmdProc.AddCommand ( new InputCommandsLoader () );
		if ( TLLoader != null ) cliWrapper.CmdProc.AddCommand ( TLLoader );

		var startCommands = Config.FetchAutoCommands ( Config.AutostartName );
		foreach ( var cmd in startCommands ) {
			if ( Config.PrintAutoCommands ) cliWrapper.ProcessLine ( cmd, true );
			else cliWrapper.CmdProc.ProcessLine ( cmd );
		}

		console.WriteLine ( "Program started. Type 'help' for a list of commands. Type 'exit' to close the program." );
		while ( true ) {
			var res = cliWrapper.ProcessLineBlocking ();
			if ( res == null ) break;
		}

		console.WriteLine ( "Program closed." );
	}

	enum MsgType { None, Result, Error }
	public static string PrintResult ( CommandResult res, ConsoleManager console, int maxOnelinerLength ) {
		if ( console == null ) throw new ArgumentNullException ( nameof ( console ) );

		string printRes = null;
		MsgType msgType = MsgType.None;
		bool batch = false;

		if ( res == null ) {
			return batch ? string.Empty : "<null>";
		}

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