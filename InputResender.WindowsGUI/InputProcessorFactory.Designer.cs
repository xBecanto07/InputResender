namespace InputResender.WindowsGUI {
	partial class InputProcessorFactory {
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
			SelectionComboBox = new System.Windows.Forms.ComboBox ();
			SelectionLabel = new System.Windows.Forms.Label ();
			CancelBtn = new System.Windows.Forms.Button ();
			ConfirmBtn = new System.Windows.Forms.Button ();
			SuspendLayout ();
			// 
			// SelectionComboBox
			// 
			SelectionComboBox.FormattingEnabled = true;
			SelectionComboBox.Location = new System.Drawing.Point ( 12, 27 );
			SelectionComboBox.Name = "SelectionComboBox";
			SelectionComboBox.Size = new System.Drawing.Size ( 180, 23 );
			SelectionComboBox.TabIndex = 0;
			// 
			// SelectionLabel
			// 
			SelectionLabel.AutoSize = true;
			SelectionLabel.Location = new System.Drawing.Point ( 12, 9 );
			SelectionLabel.Name = "SelectionLabel";
			SelectionLabel.Size = new System.Drawing.Size ( 180, 15 );
			SelectionLabel.TabIndex = 1;
			SelectionLabel.Text = "Selected processor to be created:";
			// 
			// CancelBtn
			// 
			CancelBtn.Location = new System.Drawing.Point ( 12, 56 );
			CancelBtn.Name = "CancelBtn";
			CancelBtn.Size = new System.Drawing.Size ( 75, 23 );
			CancelBtn.TabIndex = 2;
			CancelBtn.Text = "Cancel";
			CancelBtn.UseVisualStyleBackColor = true;
			CancelBtn.Click += CancelBtn_Click;
			// 
			// ConfirmBtn
			// 
			ConfirmBtn.Location = new System.Drawing.Point ( 117, 56 );
			ConfirmBtn.Name = "ConfirmBtn";
			ConfirmBtn.Size = new System.Drawing.Size ( 75, 23 );
			ConfirmBtn.TabIndex = 3;
			ConfirmBtn.Text = "Confirm";
			ConfirmBtn.UseVisualStyleBackColor = true;
			ConfirmBtn.Click += ConfirmBtn_Click;
			// 
			// InputProcessorFactory
			// 
			AutoScaleDimensions = new System.Drawing.SizeF ( 7F, 15F );
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size ( 201, 91 );
			Controls.Add ( ConfirmBtn );
			Controls.Add ( CancelBtn );
			Controls.Add ( SelectionLabel );
			Controls.Add ( SelectionComboBox );
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "InputProcessorFactory";
			Text = "InputProcessorFactory";
			ResumeLayout ( false );
			PerformLayout ();
		}

		#endregion

		private System.Windows.Forms.ComboBox SelectionComboBox;
		private System.Windows.Forms.Label SelectionLabel;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Button ConfirmBtn;
	}
}