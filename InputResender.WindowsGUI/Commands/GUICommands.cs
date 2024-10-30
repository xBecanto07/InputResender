using Components.Interfaces;
using Components.Library;
using System.Threading.Tasks;
using System.Linq;
using RetT = Components.Library.ClassCommandResult<InputResender.WindowsGUI.MainScreen>;
using System.Windows.Forms;
using InputResender.CLI;

namespace InputResender.WindowsGUI.Commands;
public class GUICommands : ACommand {
    public static string MainFormVarName = "MainForm";
    public static string MainFormThreadVarName = "MainFormThread";
    private MainScreen mainScreen;

    override public string Description => "Manages the GUI.";
    override public string Help => $"{parentCommandHelp} {commandNames.First ()} (start|stop)";

    public GUICommands ( string parentHelp = null ) : base ( parentHelp ) {
        commandNames.Add ( "gui" );

        interCommands.Add ( "start" );
        interCommands.Add ( "stop" );

        requiredPositionals.Add ( 0, false ); // Start/Stop (maybe more later)
    }

    override protected RetT ExecIner ( CommandProcessor.CmdContext context ) {
        string act = context[0, "Action"];
        return act switch {
			"start" => StartGUI ( context.CmdProc ),
			"stop" => StopGUI ( context.CmdProc ),
			_ => new RetT ( null, "Unknown action." ),
		};
    }

    private RetT StartGUI ( CommandProcessor cmdProc ) {
		if ( mainScreen != null ) return new RetT ( mainScreen, "Main screen is already running." );
		var core = cmdProc.GetVar<DMainAppCore> ( "ActCore" );

		// Add support for some switch that would decided whether to run the GUI in a new thread or run it synchronously.
		// Consider whether to use thread or only a task.

		var cli = cmdProc.GetVar<CliWrapper> ( CliWrapper.CLI_VAR_NAME );
		mainScreen = new MainScreen ( cli, core, ( s ) => cmdProc.ProcessLine ( $"print \"{s}\"" ) );
        ShowWindow ( mainScreen, cmdProc );
		return new RetT ( mainScreen, "Main screen started." );
	}

    private RetT StopGUI (CommandProcessor cmdProc) {
		if ( mainScreen == null ) return new RetT ( null, "Main screen is not running." );
        var closedScreen = mainScreen;
		mainScreen.RequestStop ();
		mainScreen = null;
		try {
            // This wait() probably shouldn't be needed
            var mainForm = cmdProc.GetVar<Form> ( MainFormVarName );
            if ( mainForm == closedScreen ) {
                var mainTask = cmdProc.GetVar<Task> ( MainFormThreadVarName );
                mainTask?.Wait ();
            }
        } catch { }
		return new RetT ( closedScreen, "Main screen stopped." );
	}

    private void RunGUI ( DMainAppCore core, CommandProcessor context ) {
    }

    public static void ShowWindow ( Form form, CommandProcessor context ) {
        try {
            var mainForm = context.GetVar<Form> ( MainFormVarName );
            if ( mainForm != null ) {
                mainForm.Invoke ( () => form.Show ( mainForm ) );
                return;
            }
        } catch { }
        var mainTask = Task.Run ( () => {
            form.Show ();
            context.SetVar ( MainFormVarName, form );
			Application.Run ( form );
            context.SetVar ( MainFormVarName, null );
            context.SetVar ( MainFormThreadVarName, null );
		} );
        context.SetVar ( MainFormThreadVarName, mainTask );
    }
}