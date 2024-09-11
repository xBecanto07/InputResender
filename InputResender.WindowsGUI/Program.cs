using System;
using InputResender.WindowsGUI.Commands;

namespace InputResender.WindowsGUI;
internal static class Program {
	[STAThread]
	static void Main ( string[] args ) {
		ApplicationConfiguration.Initialize ();
		Console.WriteLine ( "Starting Windows version ..." );
		InputResender.CLI.Program.Main ( args, new TopLevelLoader () );
	}
}