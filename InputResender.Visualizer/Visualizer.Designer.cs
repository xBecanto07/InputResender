namespace InputResender.Visualizer {
	partial class Visualizer {
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose ( bool disposing ) {
			if ( disposing && (components != null) ) {
				components.Dispose ();
			}
			base.Dispose ( disposing );
		}

		#region Windows Form Designer generated code
		private void InitializeComponent () {
			components = new System.ComponentModel.Container ();
			CompSelect = new System.Windows.Forms.ComboBox ();
			label1 = new System.Windows.Forms.Label ();
			ConsoleOut1 = new System.Windows.Forms.Label ();
			Awakener = new System.Windows.Forms.Timer ( components );
			ConsoleOut2 = new System.Windows.Forms.Label ();
			ConsoleOut3 = new System.Windows.Forms.Label ();
			SuspendLayout ();
			// 
			// CompSelect
			// 
			CompSelect.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			CompSelect.FormattingEnabled = true;
			CompSelect.Items.AddRange ( new object[] { "Stopped", "LowLevel Input", "Input Reader", "Input Parser", "Input Processor", "Tapping Processor", "Text Writer" } );
			CompSelect.Location = new System.Drawing.Point ( 1492, 6 );
			CompSelect.Name = "CompSelect";
			CompSelect.Size = new System.Drawing.Size ( 121, 23 );
			CompSelect.TabIndex = 0;
			CompSelect.SelectedIndexChanged += CompSelect_SelectedIndexChanged;
			// 
			// label1
			// 
			label1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			label1.AutoSize = true;
			label1.Location = new System.Drawing.Point ( 1357, 9 );
			label1.Name = "label1";
			label1.Size = new System.Drawing.Size ( 129, 15 );
			label1.TabIndex = 1;
			label1.Text = "Visualized Component:";
			// 
			// ConsoleOut1
			// 
			ConsoleOut1.BackColor = System.Drawing.Color.Black;
			ConsoleOut1.Dock = System.Windows.Forms.DockStyle.Bottom;
			ConsoleOut1.Font = new System.Drawing.Font ( "Lucida Console", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point );
			ConsoleOut1.ForeColor = System.Drawing.Color.Lime;
			ConsoleOut1.Location = new System.Drawing.Point ( 0, 709 );
			ConsoleOut1.Name = "ConsoleOut1";
			ConsoleOut1.Size = new System.Drawing.Size ( 1625, 622 );
			ConsoleOut1.TabIndex = 2;
			ConsoleOut1.Text = "label2";
			// 
			// ConsoleOut2
			// 
			ConsoleOut2.BackColor = System.Drawing.Color.Black;
			ConsoleOut2.Font = new System.Drawing.Font ( "Lucida Console", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point );
			ConsoleOut2.ForeColor = System.Drawing.Color.Gold;
			ConsoleOut2.Location = new System.Drawing.Point ( 0, 48 );
			ConsoleOut2.Name = "ConsoleOut2";
			ConsoleOut2.Size = new System.Drawing.Size ( 800, 650 );
			ConsoleOut2.TabIndex = 3;
			ConsoleOut2.Text = "label2";
			// 
			// ConsoleOut3
			// 
			ConsoleOut3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			ConsoleOut3.BackColor = System.Drawing.Color.Black;
			ConsoleOut3.Font = new System.Drawing.Font ( "Lucida Console", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point );
			ConsoleOut3.ForeColor = System.Drawing.Color.Magenta;
			ConsoleOut3.Location = new System.Drawing.Point ( 825, 48 );
			ConsoleOut3.Name = "ConsoleOut3";
			ConsoleOut3.Size = new System.Drawing.Size ( 800, 650 );
			ConsoleOut3.TabIndex = 4;
			ConsoleOut3.Text = "label3";
			// 
			// Visualizer
			// 
			AutoScaleDimensions = new System.Drawing.SizeF ( 7F, 15F );
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size ( 1625, 1331 );
			Controls.Add ( ConsoleOut3 );
			Controls.Add ( ConsoleOut2 );
			Controls.Add ( ConsoleOut1 );
			Controls.Add ( label1 );
			Controls.Add ( CompSelect );
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "Visualizer";
			Text = "Form1";
			FormClosing += Visualizer_FormClosing;
			Load += Visualizer_Load;
			ResumeLayout ( false );
			PerformLayout ();
		}

		#endregion

		private System.Windows.Forms.ComboBox CompSelect;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label ConsoleOut1;
		private System.Windows.Forms.Timer Awakener;
		private System.Windows.Forms.Label ConsoleOut2;
		private System.Windows.Forms.Label ConsoleOut3;
	}
}