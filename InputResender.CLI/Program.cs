using System;
using Components.Library;
using System.Threading.Tasks;
using Components.Interfaces;
using InputResender.Services;

namespace InputResender.CLI;
public static class Program {
	public static bool StartMain ( string[] args, ACommandLoader<DMainAppCore> TLLoader, CliWrapper cliWrapper ) {
		ArgParser parser = new ( string.Join ( " ", args ), cliWrapper.Console.WriteLine );
		if ( !Config.Load ( parser.String ( "cfg", null ) ) )
			Config.Save (); // Couldn't load configuration, save the current one

		DMainAppCore core = cliWrapper.CmdProc.Owner;
		if ( core == null )
			throw new InvalidOperationException (
				"Provided CliWrapper does not have an owner set for its CommandProcessor!"
			);
		cliWrapper.CmdProc.SetVar ( CliWrapper.CLI_VAR_NAME, cliWrapper );

		cliWrapper.CmdProc.AddCommand ( new BasicCommands<DMainAppCore> ( core, cliWrapper.Console.WriteLine, cliWrapper.Console.Clear, () => { /* Cleanup is done after main loop */ } ) );
		cliWrapper.CmdProc.AddCommand ( new FactoryCommandsLoader ( core ) );
		cliWrapper.CmdProc.AddCommand ( new InputCommandsLoader ( core ) );
		if ( TLLoader != null ) cliWrapper.CmdProc.AddCommand ( TLLoader );

		var startCommands = Config.FetchAutoCommands ( Config.AutostartName );
		foreach ( var cmd in startCommands ) {
			if ( cmd == "exit" ) return false;
			if ( Config.PrintAutoCommands ) cliWrapper.ProcessLine ( cmd, true );
			else cliWrapper.CmdProc.ProcessLine ( cmd );
		}

		cliWrapper.Console.WriteLine ( "Program started. Type 'help' for a list of commands. Type 'exit' to close the program." );
		return true;
	}

	public static void MainRun ( CliWrapper cliWrapper ) {
		while ( true ) {
			var res = cliWrapper.ProcessLineBlocking ();
			if ( res == null ) break;
		}

		cliWrapper.CmdProc.Owner.Close ();

		cliWrapper.Console.WriteLine ( "Program closed." );
	}

	public static void Main ( string[] args, DMainAppCore core, ACommandLoader<DMainAppCore> TLLoader, ConsoleManager console ) {
		CliWrapper cliWrapper = new ( core, console );
		if ( !StartMain ( args, TLLoader, cliWrapper ) ) return;
		MainRun ( cliWrapper );
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
			if ( canAppend ) console.Append ( oneLiner );
			else {
				oneLiner = msgType switch {
					MsgType.Error => $" - Error: {printRes.PrefixAllLines ( " ! " )}",
					MsgType.Result => $" - {printRes}",
					_ => printRes
				};

				if ( oneLiner.Length < maxOnelinerLength && !oneLiner.Contains ( "\r\n" ) && !oneLiner.Contains ( '\n' ) ) {
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