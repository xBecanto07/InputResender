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
			ActiveTask = new System.Windows.Forms.CheckBox ();
			Awakener = new System.Windows.Forms.Timer ( components );
			SuspendLayout ();
			// 
			// ConsoleOUT
			// 
			ConsoleOUT.BackColor = System.Drawing.Color.Black;
			ConsoleOUT.Dock = System.Windows.Forms.DockStyle.Top;
			ConsoleOUT.Font = new System.Drawing.Font ( "Lucida Console", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point );
			ConsoleOUT.ForeColor = System.Drawing.Color.Yellow;
			ConsoleOUT.Location = new System.Drawing.Point ( 0, 0 );
			ConsoleOUT.Multiline = true;
			ConsoleOUT.Name = "ConsoleOUT";
			ConsoleOUT.ReadOnly = true;
			ConsoleOUT.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
			ConsoleOUT.Size = new System.Drawing.Size ( 940, 658 );
			ConsoleOUT.TabIndex = 0;
			ConsoleOUT.TabStop = false;
			ConsoleOUT.Text = "Hello, World!";
			// 
			// ConsoleIN
			// 
			ConsoleIN.BackColor = System.Drawing.Color.Black;
			ConsoleIN.Font = new System.Drawing.Font ( "Lucida Console", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point );
			ConsoleIN.ForeColor = System.Drawing.Color.Snow;
			ConsoleIN.Location = new System.Drawing.Point ( 12, 665 );
			ConsoleIN.Multiline = true;
			ConsoleIN.Name = "ConsoleIN";
			ConsoleIN.Size = new System.Drawing.Size ( 530, 23 );
			ConsoleIN.TabIndex = 1;
			ConsoleIN.Text = "Input test";
			ConsoleIN.TextChanged += ConsoleIN_TextChanged;
			// 
			// ConsoleOK
			// 
			ConsoleOK.Location = new System.Drawing.Point ( 548, 664 );
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
			timer1.Enabled = true;
			timer1.Tick += timer1_Tick;
			// 
			// ActiveTask
			// 
			ActiveTask.AutoSize = true;
			ActiveTask.Location = new System.Drawing.Point ( 695, 668 );
			ActiveTask.Name = "ActiveTask";
			ActiveTask.Size = new System.Drawing.Size ( 84, 19 );
			ActiveTask.TabIndex = 3;
			ActiveTask.Text = "Active Task";
			ActiveTask.UseVisualStyleBackColor = true;
			ActiveTask.CheckedChanged += ActiveTask_CheckedChanged;
			// 
			// Awakener
			// 
			Awakener.Tick += Awakener_Tick;
			// 
			// Form1
			// 
			AutoScaleDimensions = new System.Drawing.SizeF ( 7F, 15F );
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size ( 940, 700 );
			Controls.Add ( ActiveTask );
			Controls.Add ( ConsoleOK );
			Controls.Add ( ConsoleIN );
			Controls.Add ( ConsoleOUT );
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			MaximizeBox = false;
			Name = "Form1";
			Text = "Form1";
			Activated += Form1_Activated;
			Load += Form1_Load;
			ResumeLayout ( false );
			PerformLayout ();
		}

		#endregion
		private System.Windows.Forms.TextBox ConsoleIN;
		private System.Windows.Forms.Button ConsoleOK;
		private System.Windows.Forms.TextBox ConsoleOUT;
		public System.Windows.Forms.Timer timer1;
		public System.Windows.Forms.CheckBox ActiveTask;
		private System.Windows.Forms.Timer Awakener;
	}
}