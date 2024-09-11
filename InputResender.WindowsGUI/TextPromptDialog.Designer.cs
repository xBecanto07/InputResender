using System.Windows.Forms;
using System.Drawing;

namespace InputResender.WindowsGUI {
	partial class TextPromptDialog {
		private Label PromptLabel;
		private TextBox ResultTextBox;
		private Button CancelBtn;
		private Button SubmitBtn;

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>Clean up any resources being used.</summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose ( bool disposing ) {
			if ( disposing && (components != null) ) {
				components.Dispose ();
			}
			base.Dispose ( disposing );
		}

		/// <summary>Required method for Designer support - do not modify the contents of this method with the code editor.</summary>
		private void InitializeComponent ( string Prompt, bool HideText = false ) {
			PromptLabel = new Label ();
			ResultTextBox = new TextBox ();
			CancelBtn = new Button ();
			SubmitBtn = new Button ();
			SuspendLayout ();

			PromptLabel.AutoSize = true;
			PromptLabel.Location = new Point ( 12, 9 );
			PromptLabel.Name = "PromptLabel";
			PromptLabel.Size = new Size ( 38, 15 );
			PromptLabel.TabIndex = 0;
			PromptLabel.Text = Prompt;

			ResultTextBox.Location = new Point ( 12, 27 );
			ResultTextBox.Name = "ResultTextBox";
			ResultTextBox.Size = new Size ( 300, 23 );
			ResultTextBox.TabIndex = 1;
			if ( HideText ) ResultTextBox.UseSystemPasswordChar = true;

			CancelBtn.Location = new Point ( 40, 56 );
			CancelBtn.Name = "CancelBtn";
			CancelBtn.Size = new Size ( 75, 23 );
			CancelBtn.TabIndex = 2;
			CancelBtn.Text = "Cancel";
			CancelBtn.UseVisualStyleBackColor = true;
			CancelBtn.Click += CancelBtn_Click;

			SubmitBtn.Location = new Point ( 201, 56 );
			SubmitBtn.Name = "SubmitBtn";
			SubmitBtn.Size = new Size ( 75, 23 );
			SubmitBtn.TabIndex = 3;
			SubmitBtn.Text = "Submit";
			SubmitBtn.UseVisualStyleBackColor = true;
			SubmitBtn.Click += SubmitBtn_Click;

			AutoScaleDimensions = new SizeF ( 7F, 15F );
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size ( 322, 90 );
			Controls.Add ( SubmitBtn );
			Controls.Add ( CancelBtn );
			Controls.Add ( ResultTextBox );
			Controls.Add ( PromptLabel );
			Name = "TextPromptDialog";
			Text = "TextPromptDialog";
			ResumeLayout ( false );
			PerformLayout ();
		}
	}
}