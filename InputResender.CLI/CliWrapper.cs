﻿using Components.Library;
using System;
using System.Threading.Tasks;

namespace InputResender.CLI;
public class CliWrapper {
	public const string CLI_VAR_NAME = "CLIobj";
	// Both console and cmdProc don't necessary need to be readonly, but it should be ensured that it won't change during processing of single command. If need arises, some approach to enable changing it between calls can be added.
	public readonly ConsoleManager Console;
	public readonly CommandProcessor CmdProc;
	public bool VerboseMode = false;

	private bool PrintStart => Config.ResponsePrintFormat == Config.PrintFormat.Normal
		|| Config.ResponsePrintFormat == Config.PrintFormat.Full;

	public CliWrapper ( ConsoleManager console ) {
		Console = console ?? throw new ArgumentNullException ( nameof ( console ) );
		CmdProc = new ( Console.WriteLine );
	}

	public CommandResult ProcessLineBlocking () => ProcessLine ( Console.ReadLineBlocking );
	public CommandResult ProcessLine () => ProcessLine ( Console.ReadLine );
	private CommandResult ProcessLine (Func<string> readLineFcn) {
		if ( PrintStart ) Console.Write ( "$> " );
		string s = readLineFcn ();
		if ( s == null ) return null;
		return ProcessLine ( s, false );
	}

	public Task<CommandResult> ProcessLineAsync () {
		Task<CommandResult> ret = new ( ProcessLineBlocking );
		ret.Start ();
		return ret;
	}

	public CommandResult ProcessLine ( string line, bool printLine = true ) {
		return ProcessLine ( line, printLine, () => CmdProc.ProcessLine ( line, VerboseMode, Console ) );

		/*// Should line=="" be handled extra here?
		if ( line == null ) throw new ArgumentNullException ( nameof ( line ) );

		if ( printLine ) Console.WriteLine ( PrintStart ? $"$> {line}" : line );
		var res = CmdProc.ProcessLine ( line, VerboseMode, Console );
		CmdProc?.Owner?.FlushDelayedMsgs ( Console.WriteLine );
		if ( Config.ResponsePrintFormat != Config.PrintFormat.None ) Program.PrintResult ( res, Console, Config.MaxOnelinerLength );
		return res;*/
	}

	public CommandResult ProcessLine<T> (string line, out T result, bool printLine = true) where T : CommandResult {
		T resT = null;
		var res = ProcessLine (line, printLine, () => CmdProc.ProcessLine (line, out resT, VerboseMode, Console));
		result = resT;
		return res;


		/*if ( line == null ) throw new ArgumentNullException ( nameof ( line ) );

		if ( printLine ) Console.WriteLine ( PrintStart ? $"$> {line}" : line );
		var res = CmdProc.ProcessLine ( line, out result, VerboseMode, Console );
		CmdProc?.Owner?.FlushDelayedMsgs ( Console.WriteLine );
		if ( Config.ResponsePrintFormat != Config.PrintFormat.None ) Program.PrintResult ( res, Console, Config.MaxOnelinerLength );
		return res;*/
	}
	private CommandResult ProcessLine (string line, bool printLine, Func<CommandResult> processFcn) {
		// Should line=="" be handled extra here?
		if ( line == ConsoleManager.EOF ) return null;
		if ( line == null ) throw new ArgumentNullException ( nameof ( line ) );

		if ( printLine ) Console.WriteLine ( PrintStart ? $"$> {line}" : line );
		var res = processFcn ();
		CmdProc?.Owner?.FlushDelayedMsgs ( Console.WriteLine );
		if ( Config.ResponsePrintFormat != Config.PrintFormat.None ) Program.PrintResult ( res, Console, Config.MaxOnelinerLength );
		return res;
	}

	public void Stop () {
		throw new NotImplementedException ();
		// Current workflow is: ConsoleManager is listening for 'exit' line, returning EOF when receiving it. This than should be captured and propagated further up the call chain.
		// This isn't great solution but works for now (when 'exit' entered via console).
		// When exit command is called directly through ProcessLine, then Exit() from BasicCommands would be called, which now throws NotImplementedException. Instead of that it probably should call this Stop() which would set local flag, informing any active (not passive waiting, but such approach is not used now) to stop any execution.
	}
}