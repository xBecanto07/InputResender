namespace InputResender.WindowsGUI {
	partial class ConsoleWindow {
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
			textBox1 = new System.Windows.Forms.TextBox ();
			SuspendLayout ();
			// 
			// textBox1
			// 
			textBox1.BackColor = System.Drawing.SystemColors.InfoText;
			textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			textBox1.ForeColor = System.Drawing.SystemColors.Info;
			textBox1.Location = new System.Drawing.Point ( 0, 0 );
			textBox1.Multiline = true;
			textBox1.Name = "textBox1";
			textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			textBox1.Size = new System.Drawing.Size ( 784, 861 );
			textBox1.TabIndex = 0;
			textBox1.TextChanged += textBox1_TextChanged;
			// 
			// ConsoleWindow
			// 
			AutoScaleDimensions = new System.Drawing.SizeF ( 7F, 15F );
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size ( 784, 861 );
			Controls.Add ( textBox1 );
			Name = "ConsoleWindow";
			Text = "ConsoleWindow";
			Load += ConsoleWindow_Load;
			ResumeLayout ( false );
			PerformLayout ();
		}

		#endregion

		private System.Windows.Forms.TextBox textBox1;
	}
}