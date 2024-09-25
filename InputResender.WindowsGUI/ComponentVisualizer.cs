using Components.Interfaces;
using Components.Library;
using InputResender.WindowsGUI.Commands;
using System;
using System.Windows.Forms;

namespace InputResender.WindowsGUI; 
public partial class ComponentVisualizer : Form {
	VisualizationData Data;
	private bool IsOpen { get; set; } = false;

	private ComponentVisualizer () {
		InitializeComponent ();
	}

	private void Init ( VisualizationData data ) {
		if ( Data != null ) Data.Dispose ();
		Data = data;
		UpdateData ();
	}
	private void UpdateData () {
		Data.Update ();
		LowLevelInputGenericInfo.Text = Data.GeneralInfo[typeof ( DLowLevelInput )];
		InputReaderGenericInfo.Text = Data.GeneralInfo[typeof ( DInputReader )];
		InputParserGenericInfo.Text = Data.GeneralInfo[typeof ( DInputParser )];
		InputProcessorGenericInfo.Text = Data.GeneralInfo[typeof ( DInputProcessor )];
		DataSignerGenericInfo.Text = Data.GeneralInfo[typeof ( DDataSigner )];
		PacketSenderGenericInfo.Text = Data.GeneralInfo[typeof ( DPacketSender )];
		ShortcutWorkerGenericInfo.Text = Data.GeneralInfo[typeof ( DShortcutWorker )];
		CommandWorkerGenericInfo.Text = Data.GeneralInfo[typeof ( DCommandWorker )];
		InputSimulatorGenericInfo.Text = Data.GeneralInfo[typeof ( DInputSimulator )];
		MainAppControlsGenericInfo.Text = Data.GeneralInfo[typeof ( DMainAppControls )];
		EventVectorGenericInfo.Text = Data.GeneralInfo[typeof ( DEventVector )];
		ElapsedTimeLabel.Text = TimeToString ( Data.LastUpdateTime );
	}

	private static string TimeToString ( TimeSpan time ) => $"{time.TotalSeconds:F0}s  {time.Milliseconds:000}ms  {time.Microseconds:000}us  {time.Nanoseconds}ns";

	private void ComponentVisualizer_Load ( object sender, EventArgs e ) {
		IsOpen = true;
	}

	private void ComponentVisualizer_FormClosing ( object sender, FormClosingEventArgs e ) {
		IsOpen = false;
		if ( Data != null ) {
			Data.Dispose ();
			Data = null;
		}
	}



	public class ComponentVisualizerCommands : ACommand {
		ComponentVisualizer visualizer;

		public override string Description => "Manages visualizer of active core";

		public ComponentVisualizerCommands ( string parentHelp = null ) : base ( parentHelp ) {
			commandNames.Add ( "visualizer" );
			requiredPositionals.Add ( 0, false );
			interCommands.Add ( "start" );
			interCommands.Add ( "stop" );
			interCommands.Add ( "update" );
			interCommands.Add ( "status" );
		}

		protected override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
			switch ( context.SubAction ) {
			case "start":
				if ( visualizer != null ) return new CommandResult ( "Visualizer is already running." );
				if ( context.CmdProc.Owner == null ) return new CommandResult ( "No active core." );
				if ( context.CmdProc.Owner is not DMainAppCore core ) return new CommandResult ( $"Active core is not a MainAppCore, but a {context.CmdProc.Owner.GetType ().Name}." );
				visualizer = new ();
				visualizer.Init ( new VisualizationData ( (DMainAppCore)context.CmdProc.Owner ) );
				GUICommands.ShowWindow ( visualizer, context.CmdProc );
				return new ClassCommandResult<ComponentVisualizer> ( visualizer, "Visualizer started." );
			case "stop":
				if ( visualizer == null ) return new CommandResult ( "Visualizer is not running." );
				visualizer.Invoke ( visualizer.Close );
				visualizer.Dispose ();
				visualizer = null;
				return new CommandResult ( "Visualizer stopped." );
			case "update":
				if ( visualizer == null ) return new CommandResult ( "Visualizer is not running." );
				visualizer.Invoke ( visualizer.UpdateData );
				return new CommandResult ( "Visualizer updated." );
			case "status":return new BoolCommandResult ( visualizer != null, visualizer == null ? "Visualizer is not running." : "Visualizer is running." );
			default: return new CommandResult ( $"Unknown action '{context.SubAction}'." );
			}
		}
	}
}
