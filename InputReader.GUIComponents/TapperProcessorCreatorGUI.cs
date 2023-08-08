using Components.Implementations;
using Components.Interfaces;
using Components.Library;
using System;
using System.Windows.Forms;

namespace InputResender.GUIComponents {
	internal partial class TapperProcessorCreatorGUI : Form {
		public InputData.Modifier Modifier;
		public readonly KeyCode[] KeyCodes = new KeyCode[5];
		int AssigningKey = -1;
		readonly TextBox[] KeySetters = new TextBox[5];

		public TapperProcessorCreatorGUI () {
			InitializeComponent ();
			KeySetters[0] = ThumbKeyTB;
			KeySetters[1] = IndexKeyTB;
			KeySetters[2] = MiddleKeyTB;
			KeySetters[3] = RingKeyTB;
			KeySetters[4] = LittleKeyTB;
			ModifierComboBox.Items.Clear ();
			ModifierComboBox.Items.AddRange ( Enum.GetNames<InputData.Modifier> () );
			ModifierComboBox.SelectedIndex = 0;
		}

		private void CancelBtn_Click ( object sender, EventArgs e ) {
			DialogResult = DialogResult.Cancel;
			Close ();
		}

		private void ConfirmBtn_Click ( object sender, EventArgs e ) {
			DialogResult = DialogResult.OK;
			Close ();
		}

		private void ThumbKeyTB_Click ( object sender, EventArgs e ) => PromptForKey ( 0 );
		private void IndexKeyTB_Click ( object sender, EventArgs e ) => PromptForKey ( 1 );
		private void MiddleKeyTB_Click ( object sender, EventArgs e ) => PromptForKey ( 2 );
		private void RingKeyTB_Click ( object sender, EventArgs e ) => PromptForKey ( 3 );
		private void LittleKeyTB_Click ( object sender, EventArgs e ) => PromptForKey ( 4 );
		private void PromptForKey ( int ID ) {
			KeySetters[ID].Text = "Press key ...";
			KeySetters[ID].Focus ();
			AssigningKey = ID;
		}

		private void ThumbKeyTB_KeyDown ( object sender, KeyEventArgs e ) => ProcessKey ( 0, e );
		private void IndexKeyTB_KeyDown ( object sender, KeyEventArgs e ) => ProcessKey ( 1, e );
		private void MiddleKeyTB_KeyDown ( object sender, KeyEventArgs e ) => ProcessKey ( 2, e );
		private void RingKeyTB_KeyDown ( object sender, KeyEventArgs e ) => ProcessKey ( 3, e );
		private void LittleKeyTB_KeyDown ( object sender, KeyEventArgs e ) => ProcessKey ( 4, e );
		private void ProcessKey ( int ID, KeyEventArgs e ) {
			if ( AssigningKey < 0 ) { e.SuppressKeyPress = false; return; }
			AssigningKey = -1;
			e.SuppressKeyPress = true;
			if ( e.KeyCode != Keys.Escape ) KeyCodes[ID] = (KeyCode)e.KeyValue;
			KeySetters[ID].Text = KeyCodes[ID].ToString ();
			if ( ID < 4 ) KeySetters[ID + 1].Focus ();
			else ModifierComboBox.Focus ();
		}

		private void ModifierComboBox_SelectedIndexChanged ( object sender, EventArgs e ) {
			Modifier = Enum.Parse<InputData.Modifier> ( (string)ModifierComboBox.SelectedItem );
		}
	}
	public class TapperProcessorCreator : IProcessorCreator {
		KeyCode[] keys;
		InputData.Modifier mod;

		public string CommonName => "TapWithUs Simulator";
		public DInputProcessor GetNewProcessor ( CoreBase targetCore ) {
			if ( targetCore == null ) return null;
			return new VTapperInput ( targetCore, keys, mod );
		}

		public bool ShowGUI () {
			TapperProcessorCreatorGUI gui = new TapperProcessorCreatorGUI ();
			var res = gui.ShowDialog ();
			if ( res != DialogResult.OK ) return false;
			keys = gui.KeyCodes;
			mod = gui.Modifier;
			gui.Dispose ();
			return true;
		}
	}
}