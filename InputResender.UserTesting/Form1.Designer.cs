namespace InputResender.UserTesting {
	partial class Form1 {
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent () {
			components = new System.ComponentModel.Container ();
			ConsoleOUT = new System.Windows.Forms.TextBox ();
			ConsoleIN = new System.Windows.Forms.TextBox ();
			ConsoleOK = new System.Windows.Forms.Button ();
			timer1 = new System.Windows.Forms.Timer ( components );
			SuspendLayout ();
			// 
			// ConsoleOUT
			// 
			ConsoleOUT.BackColor = System.Drawing.Color.Black;
			ConsoleOUT.Font = new System.Drawing.Font ( "Lucida Console", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point );
			ConsoleOUT.ForeColor = System.Drawing.Color.Yellow;
			ConsoleOUT.Location = new System.Drawing.Point ( 12, 12 );
			ConsoleOUT.Multiline = true;
			ConsoleOUT.Name = "ConsoleOUT";
			ConsoleOUT.ReadOnly = true;
			ConsoleOUT.Size = new System.Drawing.Size ( 776, 426 );
			ConsoleOUT.TabIndex = 0;
			ConsoleOUT.TabStop = false;
			ConsoleOUT.Text = "Hello, World!";
			// 
			// ConsoleIN
			// 
			ConsoleIN.BackColor = System.Drawing.Color.Black;
			ConsoleIN.Font = new System.Drawing.Font ( "Lucida Console", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point );
			ConsoleIN.ForeColor = System.Drawing.Color.Snow;
			ConsoleIN.Location = new System.Drawing.Point ( 12, 444 );
			ConsoleIN.Name = "ConsoleIN";
			ConsoleIN.Size = new System.Drawing.Size ( 695, 22 );
			ConsoleIN.TabIndex = 1;
			ConsoleIN.Text = "Input test";
			ConsoleIN.TextChanged += ConsoleIN_TextChanged;
			// 
			// ConsoleOK
			// 
			ConsoleOK.Location = new System.Drawing.Point ( 713, 444 );
			ConsoleOK.Name = "ConsoleOK";
			ConsoleOK.Size = new System.Drawing.Size ( 75, 23 );
			ConsoleOK.TabIndex = 2;
			ConsoleOK.TabStop = false;
			ConsoleOK.Text = "Submit";
			ConsoleOK.UseVisualStyleBackColor = true;
			ConsoleOK.Click += ConsoleOK_Click;
			// 
			// timer1
			// 
			timer1.Tick += timer1_Tick;
			// 
			// Form1
			// 
			AutoScaleDimensions = new System.Drawing.SizeF ( 7F, 15F );
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size ( 800, 477 );
			Controls.Add ( ConsoleOK );
			Controls.Add ( ConsoleIN );
			Controls.Add ( ConsoleOUT );
			Name = "Form1";
			Text = "Form1";
			Load += Form1_Load;
			ResumeLayout ( false );
			PerformLayout ();
		}

		#endregion
		private System.Windows.Forms.TextBox ConsoleIN;
		private System.Windows.Forms.Button ConsoleOK;
		private System.Windows.Forms.TextBox ConsoleOUT;
		public System.Windows.Forms.Timer timer1;
	}
}