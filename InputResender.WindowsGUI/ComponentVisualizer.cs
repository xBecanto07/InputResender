using Components.Interfaces;
using System;
using System.Windows.Forms;

namespace InputResender.WindowsGUI {
	public partial class ComponentVisualizer : Form {
		VisualizationData Data;
		public bool IsOpen { get; private set; } = false;

		public ComponentVisualizer () {
			InitializeComponent ();
		}

		public void Init ( VisualizationData data ) {
			if ( Data != null ) Data.Dispose ();
			Data = data;
			UpdateData ();
		}
		public void UpdateData () {
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

		private string TimeToString ( TimeSpan time ) => $"{time.TotalSeconds:F0}s  {time.Milliseconds:000}ms  {time.Microseconds:000}us  {time.Nanoseconds}ns";

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
	}
}
