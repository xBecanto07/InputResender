using System.Windows.Forms;
using System.Drawing;

namespace InputResender.GUIComponents {
	partial class MainScreen {
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
		private void InitializeComponent () {
			Label InputProcSelectorLabel;
			PsswdUpdateBtn = new Button ();
			EPUpdateBtn = new Button ();
			ConsoleText = new TextBox ();
			InputProcSelector = new ComboBox ();
			IsActiveCheckBox = new CheckBox ();
			ShortcutCheckBox = new CheckBox ();
			AddProcessorBtn = new Button ();
			RemProcessorBtn = new Button ();
			InputProcSelectorLabel = new Label ();
			SuspendLayout ();
			// 
			// PsswdUpdateBtn
			// 
			PsswdUpdateBtn.Location = new Point ( 12, 12 );
			PsswdUpdateBtn.Name = "PsswdUpdateBtn";
			PsswdUpdateBtn.Size = new Size ( 130, 23 );
			PsswdUpdateBtn.TabIndex = 0;
			PsswdUpdateBtn.Text = "Change Passphrase";
			PsswdUpdateBtn.UseVisualStyleBackColor = true;
			PsswdUpdateBtn.Click += PsswdUpdateBtn_Click;
			// 
			// EPUpdateBtn
			// 
			EPUpdateBtn.Location = new Point ( 148, 12 );
			EPUpdateBtn.Name = "EPUpdateBtn";
			EPUpdateBtn.Size = new Size ( 100, 23 );
			EPUpdateBtn.TabIndex = 1;
			EPUpdateBtn.Text = "Update Target";
			EPUpdateBtn.UseVisualStyleBackColor = true;
			EPUpdateBtn.Click += EPUpdateBtn_Click;
			// 
			// ConsoleText
			// 
			ConsoleText.BackColor = Color.Black;
			ConsoleText.Dock = DockStyle.Bottom;
			ConsoleText.Font = new Font ( "Lucida Console", 12F, FontStyle.Regular, GraphicsUnit.Point );
			ConsoleText.ForeColor = Color.Green;
			ConsoleText.Location = new Point ( 0, 42 );
			ConsoleText.Multiline = true;
			ConsoleText.Name = "ConsoleText";
			ConsoleText.ReadOnly = true;
			ConsoleText.Size = new Size ( 992, 535 );
			ConsoleText.TabIndex = 2;
			// 
			// InputProcSelector
			// 
			InputProcSelector.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			InputProcSelector.FormattingEnabled = true;
			InputProcSelector.Location = new Point ( 769, 13 );
			InputProcSelector.Name = "InputProcSelector";
			InputProcSelector.Size = new Size ( 145, 23 );
			InputProcSelector.TabIndex = 3;
			InputProcSelector.SelectedIndexChanged += InputProcSelector_SelectedIndexChanged;
			// 
			// InputProcSelectorLabel
			// 
			InputProcSelectorLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			InputProcSelectorLabel.AutoSize = true;
			InputProcSelectorLabel.Location = new Point ( 636, 16 );
			InputProcSelectorLabel.Name = "InputProcSelectorLabel";
			InputProcSelectorLabel.Size = new Size ( 128, 15 );
			InputProcSelectorLabel.TabIndex = 4;
			InputProcSelectorLabel.Text = "Active Input Processor:";
			InputProcSelectorLabel.TextAlign = ContentAlignment.MiddleRight;
			// 
			// IsActiveCheckBox
			// 
			IsActiveCheckBox.AutoSize = true;
			IsActiveCheckBox.Location = new Point ( 265, 15 );
			IsActiveCheckBox.Name = "IsActiveCheckBox";
			IsActiveCheckBox.Size = new Size ( 70, 19 );
			IsActiveCheckBox.TabIndex = 5;
			IsActiveCheckBox.Text = "Is Active";
			IsActiveCheckBox.UseVisualStyleBackColor = true;
			IsActiveCheckBox.CheckedChanged += IsActiveCheckBox_CheckedChanged;
			// 
			// ShortcutCheckBox
			// 
			ShortcutCheckBox.AutoSize = true;
			ShortcutCheckBox.Location = new Point ( 350, 15 );
			ShortcutCheckBox.Name = "ShortcutCheckBox";
			ShortcutCheckBox.Size = new Size ( 112, 19 );
			ShortcutCheckBox.TabIndex = 6;
			ShortcutCheckBox.Text = "Active Shortcuts";
			ShortcutCheckBox.UseVisualStyleBackColor = true;
			ShortcutCheckBox.CheckedChanged += ShortcutCheckBox_CheckedChanged;
			// 
			// AddProcessorBtn
			// 
			AddProcessorBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			AddProcessorBtn.FlatStyle = FlatStyle.System;
			AddProcessorBtn.Font = new Font ( "Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point );
			AddProcessorBtn.Location = new Point ( 920, 13 );
			AddProcessorBtn.Name = "AddProcessorBtn";
			AddProcessorBtn.Size = new Size ( 27, 23 );
			AddProcessorBtn.TabIndex = 7;
			AddProcessorBtn.Text = "+";
			AddProcessorBtn.TextAlign = ContentAlignment.BottomCenter;
			AddProcessorBtn.UseVisualStyleBackColor = true;
			AddProcessorBtn.Click += AddProcessorBtn_Click;
			// 
			// RemProcessorBtn
			// 
			RemProcessorBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			RemProcessorBtn.FlatStyle = FlatStyle.System;
			RemProcessorBtn.Font = new Font ( "Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point );
			RemProcessorBtn.Location = new Point ( 953, 13 );
			RemProcessorBtn.Name = "RemProcessorBtn";
			RemProcessorBtn.Size = new Size ( 27, 23 );
			RemProcessorBtn.TabIndex = 8;
			RemProcessorBtn.Text = "-";
			RemProcessorBtn.TextAlign = ContentAlignment.BottomCenter;
			RemProcessorBtn.UseVisualStyleBackColor = true;
			RemProcessorBtn.Click += RemProcessorBtn_Click;
			// 
			// MainScreen
			// 
			AutoScaleDimensions = new SizeF ( 7F, 15F );
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size ( 992, 577 );
			Controls.Add ( RemProcessorBtn );
			Controls.Add ( AddProcessorBtn );
			Controls.Add ( ShortcutCheckBox );
			Controls.Add ( IsActiveCheckBox );
			Controls.Add ( InputProcSelectorLabel );
			Controls.Add ( InputProcSelector );
			Controls.Add ( ConsoleText );
			Controls.Add ( EPUpdateBtn );
			Controls.Add ( PsswdUpdateBtn );
			Name = "MainScreen";
			Text = "MainScreen";
			ResumeLayout ( false );
			PerformLayout ();
		}

		private Button PsswdUpdateBtn;
		private Button EPUpdateBtn;
		private TextBox ConsoleText;
		private ComboBox InputProcSelector;
		private Label InputProcSelectorLabel;
		private CheckBox IsActiveCheckBox;
		private CheckBox ShortcutCheckBox;
		private Button AddProcessorBtn;
		private Button RemProcessorBtn;
	}
}