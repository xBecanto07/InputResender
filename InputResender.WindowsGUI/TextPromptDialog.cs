using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace InputResender.WindowsGUI {
	public partial class TextPromptDialog : Form {
		public PromptResult? Result { get; private set; } = null;
		private readonly string RegexPattern;

		public struct PromptResult {
			public string Text;
			public bool DidSubmit;
			public PromptResult ( string text, bool ok ) { Text = text; DidSubmit = ok; }
		}

		public TextPromptDialog ( string Prompt, string regex = null, bool HideText = false ) {
			InitializeComponent ( Prompt, HideText );
			RegexPattern = regex;
		}

		public static PromptResult Show ( string Prompt, string regex = null, bool HideText = false ) {
			var dialog = new TextPromptDialog ( Prompt, regex, HideText );
			var res = dialog.ShowDialog ();
			if ( (res != DialogResult.OK) & (res != DialogResult.Cancel) ) return new PromptResult ( null, false );
			if ( dialog.Result == null ) throw new NullReferenceException ( "No Result found!" );
			var ret = dialog.Result.Value;
			dialog.Dispose ();
			return ret;
		}

		private void SubmitBtn_Click ( object sender, EventArgs e ) {
			if ( !Regex.IsMatch ( ResultTextBox.Text, RegexPattern ) ) {
				MessageBox.Show ( $"Text '{ResultTextBox.Text}' is not compatible with regex pattern of '{RegexPattern}'!" );
				return;
			}
			Result = new PromptResult ( ResultTextBox.Text, true );
			DialogResult = DialogResult.OK;
			Close ();
		}

		private void CancelBtn_Click ( object sender, EventArgs e ) {
			Result = new PromptResult ( null, false );
			DialogResult = DialogResult.Cancel;
			Close ();
		}
	}
}
