//#define VirtConsole
using System;
using System.Windows.Forms;
using Components.Library;
using InputResender.WindowsGUI.Commands;

namespace InputResender.WindowsGUI;
internal static class Program {
	[STAThread]
	static void Main ( string[] args ) {
#if VirtConsole
		ApplicationConfiguration.Initialize ();
		Application.Run ( new ConsoleWindow () );
#else
		ConsoleManager console = new ( Console.WriteLine, Console.ReadLine, Console.Write, Console.Clear, () => Console.ReadKey ( true ).KeyChar, OverwriteConsoleLine );
		console.WriteLine ( "Starting Windows version ..." );
		InputResender.CLI.Program.Main ( args, new TopLevelLoader ( console ), console );
#endif
	}

#if !VirtConsole
	static void OverwriteConsoleLine (string text) {
		if (Console.CursorTop > 0) Console.SetCursorPosition ( 0, Console.CursorTop - 1 );
		Console.WriteLine ( $"\r{new string ( ' ', Console.WindowWidth - 1 )}\r{text}" );
	}
#endif
}