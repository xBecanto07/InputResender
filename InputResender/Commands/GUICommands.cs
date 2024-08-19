using Components.Interfaces;
using InputResender.GUIComponents;
using Components.Library;
using System;
using System.Threading.Tasks;
using System.Linq;
using RetT = Components.Library.ClassCommandResult<InputResender.GUIComponents.MainScreen>;
using System.Windows.Forms;

namespace InputResender.Commands;
public class GUICommands : ACommand<RetT> {
	MainScreen mainScreen;
	Task guiTask;

	override public string Description => "Manages the GUI.";
	override public string Help => $"{parentCommandHelp} {commandNames.First ()} (start|stop)";

	public GUICommands ( string parentHelp = null ) : base (parentHelp) {
		commandNames.Add ("gui");

		interCommands.Add ( "start" );
		interCommands.Add ( "stop" );

		requiredPositionals.Add ( 0, false ); // Start/Stop (maybe more later)
	}

	override protected RetT ExecIner ( CommandProcessor context, ArgParser args, int argID = 1 ) {
		string act = args.String ( argID, "Action" );
		if ( act == "start" ) {
			if ( mainScreen != null ) return new RetT ( mainScreen, "Main screen is already running." );
			var core = context.GetVar<DMainAppCore> ( "ActCore" );

			guiTask = Task.Run ( () => RunGUI ( core, context ) );
			while ( mainScreen == null ) Task.Delay ( 1 );
			return new RetT ( mainScreen, "Main screen started." );
		} else if ( act == "stop" ) {
			if ( mainScreen == null ) return new RetT ( null, "Main screen is not running." );
			//mainScreen.Close ();
			mainScreen = null;
			guiTask.Wait ();
			return new RetT ( null, "Main screen stopped." );
		} else {
			return new RetT ( null, "Unknown action." );
		}
	}

	private void RunGUI ( DMainAppCore core, ICommandProcessor context ) {
		var newScreen = new MainScreen ( context, core, ( s ) => context.ProcessLine ( $"print \"{s}\"" ) );
		newScreen.Show ();
		// Command will return (and thus the screen be considered usable and displayed) right after assigned to the field 'mainScreen'. This allows this task to continue running and handling the GUI even after the command has returned.
		Application.Run ( newScreen );
		mainScreen = newScreen;
	}
}
