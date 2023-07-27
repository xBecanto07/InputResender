using Components.Interfaces;
using Components.Library;
using System;
using System.Net;
using System.Windows.Forms;

namespace InputResender.GUIComponents {
	public partial class MainScreen : Form {
		protected DMainAppCore Core;
		protected IPEndPoint TargetEP;


		public MainScreen ( DMainAppCore core ) {
			Core = core;
			Core.MainAppControls.Log = ConsoleText.AppendText;
			InitializeComponent ();
		}

		private void PsswdUpdateBtn_Click ( object sender, EventArgs e ) {
			var res = TextPromptDialog.Show ( "Enter new group passphrase:", "[\\x20-\\x7E]{5,}", true );
			if ( !res.DidSubmit ) return;

			Core.MainAppControls.ChangePassword ( res.Text );
		}

		private void EPUpdateBtn_Click ( object sender, EventArgs e ) {
			var res = TextPromptDialog.Show ( "Enter new end point (IPv4:Port):", "([\\d]{1,3}\\.[\\d]{1,3}\\.[\\d]{1,3}\\.[\\d]{1,3}):([\\d]{1,5})" );
			if ( !res.DidSubmit ) return;
			Core.MainAppControls.ChangeTarget ( res.Text );
		}

		private void IsActiveCheckBox_CheckedChanged ( object sender, EventArgs e ) {

		}

		private void ShortcutCheckBox_CheckedChanged ( object sender, EventArgs e ) {

		}

		private void InputProcSelector_SelectedIndexChanged ( object sender, EventArgs e ) {

		}

		private void AddProcessorBtn_Click ( object sender, EventArgs e ) {

		}

		private void RemProcessorBtn_Click ( object sender, EventArgs e ) {

		}
	}
}