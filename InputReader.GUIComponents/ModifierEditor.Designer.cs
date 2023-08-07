namespace InputResender.GUIComponents {
	partial class ModifierEditor {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose ( bool disposing ) {
			if ( disposing && (components != null) ) {
				components.Dispose ();
			}
			base.Dispose ( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent () {
			KeyLabel = new System.Windows.Forms.Label ();
			KeyTextBox = new System.Windows.Forms.TextBox ();
			ModLabel = new System.Windows.Forms.Label ();
			ModifierComboBox = new System.Windows.Forms.ComboBox ();
			CancelBtn = new System.Windows.Forms.Button ();
			ConfirmBtn = new System.Windows.Forms.Button ();
			SuspendLayout ();
			// 
			// KeyLabel
			// 
			KeyLabel.AutoSize = true;
			KeyLabel.Location = new System.Drawing.Point ( 12, 9 );
			KeyLabel.Name = "KeyLabel";
			KeyLabel.Size = new System.Drawing.Size ( 29, 15 );
			KeyLabel.TabIndex = 0;
			KeyLabel.Text = "Key:";
			// 
			// KeyTextBox
			// 
			KeyTextBox.Location = new System.Drawing.Point ( 84, 6 );
			KeyTextBox.Name = "KeyTextBox";
			KeyTextBox.ReadOnly = true;
			KeyTextBox.Size = new System.Drawing.Size ( 121, 23 );
			KeyTextBox.TabIndex = 1;
			KeyTextBox.TabStop = false;
			KeyTextBox.Click += KeyTextBox_Click;
			KeyTextBox.KeyDown += KeyTextBox_KeyDown;
			// 
			// ModLabel
			// 
			ModLabel.AutoSize = true;
			ModLabel.Location = new System.Drawing.Point ( 12, 43 );
			ModLabel.Name = "ModLabel";
			ModLabel.Size = new System.Drawing.Size ( 55, 15 );
			ModLabel.TabIndex = 2;
			ModLabel.Text = "Modifier:";
			// 
			// ModifierComboBox
			// 
			ModifierComboBox.FormattingEnabled = true;
			ModifierComboBox.Location = new System.Drawing.Point ( 84, 40 );
			ModifierComboBox.Name = "ModifierComboBox";
			ModifierComboBox.Size = new System.Drawing.Size ( 121, 23 );
			ModifierComboBox.TabIndex = 3;
			ModifierComboBox.SelectedIndexChanged += ModifierComboBox_SelectedIndexChanged;
			// 
			// CancelBtn
			// 
			CancelBtn.Location = new System.Drawing.Point ( 12, 80 );
			CancelBtn.Name = "CancelBtn";
			CancelBtn.Size = new System.Drawing.Size ( 75, 23 );
			CancelBtn.TabIndex = 4;
			CancelBtn.Text = "Cancel";
			CancelBtn.UseVisualStyleBackColor = true;
			CancelBtn.Click += CancelBtn_Click;
			// 
			// ConfirmBtn
			// 
			ConfirmBtn.Location = new System.Drawing.Point ( 130, 80 );
			ConfirmBtn.Name = "ConfirmBtn";
			ConfirmBtn.Size = new System.Drawing.Size ( 75, 23 );
			ConfirmBtn.TabIndex = 5;
			ConfirmBtn.Text = "Confirm";
			ConfirmBtn.UseVisualStyleBackColor = true;
			ConfirmBtn.Click += ConfirmBtn_Click;
			// 
			// ModifierEditor
			// 
			AutoScaleDimensions = new System.Drawing.SizeF ( 7F, 15F );
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size ( 218, 113 );
			Controls.Add ( ConfirmBtn );
			Controls.Add ( CancelBtn );
			Controls.Add ( ModifierComboBox );
			Controls.Add ( ModLabel );
			Controls.Add ( KeyTextBox );
			Controls.Add ( KeyLabel );
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "ModifierEditor";
			Text = "ModifierEditor";
			ResumeLayout ( false );
			PerformLayout ();
		}

		#endregion

		private System.Windows.Forms.Label KeyLabel;
		private System.Windows.Forms.TextBox KeyTextBox;
		private System.Windows.Forms.Label ModLabel;
		private System.Windows.Forms.ComboBox ModifierComboBox;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Button ConfirmBtn;
	}
}