namespace InputResender.WindowsGUI {
	partial class TapperProcessorCreatorGUI {
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
			KeyPromptLabel = new System.Windows.Forms.Label ();
			ThumbKeyLabel = new System.Windows.Forms.Label ();
			IndexKeyLabel = new System.Windows.Forms.Label ();
			ThumbKeyTB = new System.Windows.Forms.TextBox ();
			MiddleKeyLabel = new System.Windows.Forms.Label ();
			IndexKeyTB = new System.Windows.Forms.TextBox ();
			MiddleKeyTB = new System.Windows.Forms.TextBox ();
			RingKeyTB = new System.Windows.Forms.TextBox ();
			RingKeyLabel = new System.Windows.Forms.Label ();
			LittleKeyTB = new System.Windows.Forms.TextBox ();
			LittleKeyLabel = new System.Windows.Forms.Label ();
			label2 = new System.Windows.Forms.Label ();
			ModifierComboBox = new System.Windows.Forms.ComboBox ();
			CancelBtn = new System.Windows.Forms.Button ();
			ConfirmBtn = new System.Windows.Forms.Button ();
			SuspendLayout ();
			// 
			// label1
			// 
			KeyPromptLabel.AutoSize = true;
			KeyPromptLabel.Location = new System.Drawing.Point ( 12, 9 );
			KeyPromptLabel.Name = "KeyPromptLabel";
			KeyPromptLabel.Size = new System.Drawing.Size ( 176, 15 );
			KeyPromptLabel.TabIndex = 0;
			KeyPromptLabel.Text = "Assign keys to individual fingers";
			// 
			// ThumbKeyLabel
			// 
			ThumbKeyLabel.AutoSize = true;
			ThumbKeyLabel.Location = new System.Drawing.Point ( 12, 37 );
			ThumbKeyLabel.Name = "ThumbKeyLabel";
			ThumbKeyLabel.Size = new System.Drawing.Size ( 48, 15 );
			ThumbKeyLabel.TabIndex = 1;
			ThumbKeyLabel.Text = "Thumb:";
			// 
			// IndexKeyLabel
			// 
			IndexKeyLabel.AutoSize = true;
			IndexKeyLabel.Location = new System.Drawing.Point ( 12, 66 );
			IndexKeyLabel.Name = "IndexKeyLabel";
			IndexKeyLabel.Size = new System.Drawing.Size ( 39, 15 );
			IndexKeyLabel.TabIndex = 2;
			IndexKeyLabel.Text = "Index:";
			// 
			// MiddleKeyLabel
			// 
			MiddleKeyLabel.AutoSize = true;
			MiddleKeyLabel.Location = new System.Drawing.Point ( 12, 95 );
			MiddleKeyLabel.Name = "MiddleKeyLabel";
			MiddleKeyLabel.Size = new System.Drawing.Size ( 47, 15 );
			MiddleKeyLabel.TabIndex = 4;
			MiddleKeyLabel.Text = "Middle:";
			// 
			// RingKeyLabel
			// 
			RingKeyLabel.AutoSize = true;
			RingKeyLabel.Location = new System.Drawing.Point ( 12, 124 );
			RingKeyLabel.Name = "RingKeyLabel";
			RingKeyLabel.Size = new System.Drawing.Size ( 34, 15 );
			RingKeyLabel.TabIndex = 8;
			RingKeyLabel.Text = "Ring:";
			// 
			// LittleKeyLabel
			// 
			LittleKeyLabel.AutoSize = true;
			LittleKeyLabel.Location = new System.Drawing.Point ( 12, 153 );
			LittleKeyLabel.Name = "LittleKeyLabel";
			LittleKeyLabel.Size = new System.Drawing.Size ( 36, 15 );
			LittleKeyLabel.TabIndex = 10;
			LittleKeyLabel.Text = "Little:";
			// 
			// ThumbKeyTB
			// 
			ThumbKeyTB.Location = new System.Drawing.Point ( 66, 34 );
			ThumbKeyTB.Name = "ThumbKeyTB";
			ThumbKeyTB.ReadOnly = true;
			ThumbKeyTB.Size = new System.Drawing.Size ( 122, 23 );
			ThumbKeyTB.TabIndex = 3;
			ThumbKeyTB.KeyDown += ThumbKeyTB_KeyDown;
			ThumbKeyTB.Click += ThumbKeyTB_Click;
			// 
			// IndexKeyTB
			// 
			IndexKeyTB.Location = new System.Drawing.Point ( 66, 63 );
			IndexKeyTB.Name = "IndexKeyTB";
			IndexKeyTB.ReadOnly = true;
			IndexKeyTB.Size = new System.Drawing.Size ( 122, 23 );
			IndexKeyTB.TabIndex = 5;
			IndexKeyTB.KeyDown += IndexKeyTB_KeyDown;
			IndexKeyTB.Click += IndexKeyTB_Click;
			// 
			// MiddleKeyTB
			// 
			MiddleKeyTB.Location = new System.Drawing.Point ( 66, 92 );
			MiddleKeyTB.Name = "MiddleKeyTB";
			MiddleKeyTB.ReadOnly = true;
			MiddleKeyTB.Size = new System.Drawing.Size ( 122, 23 );
			MiddleKeyTB.TabIndex = 6;
			MiddleKeyTB.KeyDown += MiddleKeyTB_KeyDown;
			MiddleKeyTB.Click += MiddleKeyTB_Click;
			// 
			// RingKeyTB
			// 
			RingKeyTB.Location = new System.Drawing.Point ( 66, 121 );
			RingKeyTB.Name = "RingKeyTB";
			RingKeyTB.ReadOnly = true;
			RingKeyTB.Size = new System.Drawing.Size ( 122, 23 );
			RingKeyTB.TabIndex = 7;
			RingKeyTB.KeyDown += RingKeyTB_KeyDown;
			RingKeyTB.Click += RingKeyTB_Click;
			// 
			// LittleKeyTB
			// 
			LittleKeyTB.Location = new System.Drawing.Point ( 66, 150 );
			LittleKeyTB.Name = "LittleKeyTB";
			LittleKeyTB.ReadOnly = true;
			LittleKeyTB.Size = new System.Drawing.Size ( 122, 23 );
			LittleKeyTB.TabIndex = 9;
			LittleKeyTB.KeyDown += LittleKeyTB_KeyDown;
			LittleKeyTB.Click += LittleKeyTB_Click;
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new System.Drawing.Point ( 12, 203 );
			label2.Name = "label2";
			label2.Size = new System.Drawing.Size ( 52, 15 );
			label2.TabIndex = 11;
			label2.Text = "Modifier";
			// 
			// ModifierComboBox
			// 
			ModifierComboBox.FormattingEnabled = true;
			ModifierComboBox.Location = new System.Drawing.Point ( 66, 200 );
			ModifierComboBox.Name = "ModifierComboBox";
			ModifierComboBox.Size = new System.Drawing.Size ( 122, 23 );
			ModifierComboBox.TabIndex = 12;
			ModifierComboBox.SelectedIndexChanged += ModifierComboBox_SelectedIndexChanged;
			// 
			// CancelBtn
			// 
			CancelBtn.Location = new System.Drawing.Point ( 12, 241 );
			CancelBtn.Name = "CancelBtn";
			CancelBtn.Size = new System.Drawing.Size ( 75, 23 );
			CancelBtn.TabIndex = 13;
			CancelBtn.Text = "Cancel";
			CancelBtn.UseVisualStyleBackColor = true;
			CancelBtn.Click += CancelBtn_Click;
			// 
			// ConfirmBtn
			// 
			ConfirmBtn.Location = new System.Drawing.Point ( 113, 241 );
			ConfirmBtn.Name = "ConfirmBtn";
			ConfirmBtn.Size = new System.Drawing.Size ( 75, 23 );
			ConfirmBtn.TabIndex = 14;
			ConfirmBtn.Text = "Confirm";
			ConfirmBtn.UseVisualStyleBackColor = true;
			ConfirmBtn.Click += ConfirmBtn_Click;
			// 
			// TapperProcessorCreator
			// 
			AutoScaleDimensions = new System.Drawing.SizeF ( 7F, 15F );
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size ( 204, 278 );
			Controls.Add ( ConfirmBtn );
			Controls.Add ( CancelBtn );
			Controls.Add ( ModifierComboBox );
			Controls.Add ( label2 );
			Controls.Add ( LittleKeyLabel );
			Controls.Add ( LittleKeyTB );
			Controls.Add ( RingKeyLabel );
			Controls.Add ( RingKeyTB );
			Controls.Add ( MiddleKeyTB );
			Controls.Add ( IndexKeyTB );
			Controls.Add ( MiddleKeyLabel );
			Controls.Add ( ThumbKeyTB );
			Controls.Add ( IndexKeyLabel );
			Controls.Add ( ThumbKeyLabel );
			Controls.Add ( KeyPromptLabel );
			Name = "TapperProcessorCreator";
			Text = "PasstrhoughProcessorCreator";
			ResumeLayout ( false );
			PerformLayout ();
		}

		#endregion

		private System.Windows.Forms.Label KeyPromptLabel;
		private System.Windows.Forms.Label ThumbKeyLabel;
		private System.Windows.Forms.Label IndexKeyLabel;
		private System.Windows.Forms.TextBox ThumbKeyTB;
		private System.Windows.Forms.Label MiddleKeyLabel;
		private System.Windows.Forms.TextBox IndexKeyTB;
		private System.Windows.Forms.TextBox MiddleKeyTB;
		private System.Windows.Forms.TextBox RingKeyTB;
		private System.Windows.Forms.Label RingKeyLabel;
		private System.Windows.Forms.TextBox LittleKeyTB;
		private System.Windows.Forms.Label LittleKeyLabel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox ModifierComboBox;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Button ConfirmBtn;
	}
}