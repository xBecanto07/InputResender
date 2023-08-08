using System;
using System.Windows.Forms;

namespace InputResender.GUIComponents {
	public partial class InputProcessorFactory : Form {
		public IProcessorCreator SelectedCreator = null;
		readonly IProcessorCreator[] Creators = new IProcessorCreator[] {
			new PassthroughProcessorCreator (),
			new TapperProcessorCreator ()
		};

		public InputProcessorFactory () {
			InitializeComponent ();
			SelectionComboBox.Items.Clear ();
			foreach (var creator in  Creators) {
				SelectionComboBox.Items.Add ( creator.CommonName );
			}
		}

		private void CancelBtn_Click ( object sender, EventArgs e ) {
			DialogResult = DialogResult.Cancel;
			Close ();
		}

		private void ConfirmBtn_Click ( object sender, EventArgs e ) {
			DialogResult = DialogResult.OK;
			SelectedCreator = Creators[SelectionComboBox.SelectedIndex];
			Close ();
		}
	}
}
