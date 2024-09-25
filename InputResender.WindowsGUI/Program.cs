using System;
using Components.Library;
using InputResender.WindowsGUI.Commands;

namespace InputResender.WindowsGUI;
internal static class Program {
	[STAThread]
	static void Main ( string[] args ) {
		ApplicationConfiguration.Initialize ();
		ConsoleManager console = new ( Console.WriteLine, Console.ReadLine, Console.Write, Console.Clear );
		console.WriteLine ( "Starting Windows version ..." );
		InputResender.CLI.Program.Main ( args, new TopLevelLoader (), console );
	}
}