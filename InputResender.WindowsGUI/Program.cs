using System;
using Components.Library;
using InputResender.WindowsGUI.Commands;

namespace InputResender.WindowsGUI;
internal static class Program {
	[STAThread]
	static void Main ( string[] args ) {
		ApplicationConfiguration.Initialize ();
		ConsoleManager console = new ( Console.WriteLine, Console.ReadLine, Console.Write, Console.Clear, () => Console.ReadKey ( true ).KeyChar );
		console.WriteLine ( "Starting Windows version ..." );
		InputResender.CLI.Program.Main ( args, new TopLevelLoader (), console );
	}

	static void OverwriteConsoleLine (string text) {
		if (Console.CursorTop > 0) Console.SetCursorPosition ( 0, Console.CursorTop - 1 );
		Console.WriteLine ( $"\r{new string ( ' ', Console.WindowWidth - 1 )}\r{text}" );
	}
}