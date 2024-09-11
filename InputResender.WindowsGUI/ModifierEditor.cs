using Components.Interfaces;
using Components.Library;
using System;
using System.Windows.Forms;

namespace InputResender.WindowsGUI {
	public partial class ModifierEditor : Form {
		private bool WaitingForPress = false;
		public KeyCode Key = KeyCode.None;
		public InputData.Modifier Modifier = InputData.Modifier.None;

		public ModifierEditor ( KeyCode key = KeyCode.None, InputData.Modifier mod = InputData.Modifier.None ) {
			InitializeComponent ();
			ModifierComboBox.Items.Clear ();
			ModifierComboBox.Items.AddRange ( Enum.GetNames<InputData.Modifier> () );
			KeyTextBox.Text = (Key = key).ToString ();
			ModifierComboBox.SelectedItem = (Modifier = mod).ToString ();
		}

		private void KeyTextBox_Click ( object sender, EventArgs e ) {
			KeyTextBox.Text = "Press key ...";
			KeyTextBox.Focus ();
			WaitingForPress = true;
		}

		private void KeyTextBox_KeyDown ( object sender, KeyEventArgs e ) {
			if ( !WaitingForPress ) { e.SuppressKeyPress = false; return; }
			WaitingForPress = false;
			e.SuppressKeyPress = true;
			if ( e.KeyCode != Keys.Escape ) Key = (KeyCode)e.KeyValue;
			KeyTextBox.Text = Key.ToString ();
			ModifierComboBox.Focus ();
		}

		private void ConfirmBtn_Click ( object sender, EventArgs e ) {
			DialogResult = DialogResult.OK;
			Close ();
		}

		private void CancelBtn_Click ( object sender, EventArgs e ) {
			DialogResult = DialogResult.Cancel;
			Close ();
		}

		private void ModifierComboBox_SelectedIndexChanged ( object sender, EventArgs e ) {
			Modifier = Enum.Parse<InputData.Modifier> ( (string)ModifierComboBox.SelectedItem );
		}
	}
}