using System;
using System.Windows.Forms;
using System.Drawing;

namespace InputResender.UserTestingNetCore {
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
			ConsoleText = new TextBox ();
			DebuggerBtn = new Button ();
			textBox1 = new TextBox ();
			SuspendLayout ();
			// 
			// ConsoleText
			// 
			ConsoleText.BackColor = Color.Indigo;
			ConsoleText.Dock = DockStyle.Top;
			ConsoleText.Font = new Font ( "Lucida Console", 11.25F, FontStyle.Regular, GraphicsUnit.Point );
			ConsoleText.ForeColor = Color.Magenta;
			ConsoleText.Location = new Point ( 0, 0 );
			ConsoleText.Multiline = true;
			ConsoleText.Name = "ConsoleText";
			ConsoleText.ReadOnly = true;
			ConsoleText.Size = new Size ( 800, 409 );
			ConsoleText.TabIndex = 0;
			ConsoleText.Text = "Hello, World!";
			// 
			// DebuggerBtn
			// 
			DebuggerBtn.Location = new Point ( 701, 415 );
			DebuggerBtn.Name = "DebuggerBtn";
			DebuggerBtn.Size = new Size ( 75, 23 );
			DebuggerBtn.TabIndex = 1;
			DebuggerBtn.Text = "Debugger";
			DebuggerBtn.UseVisualStyleBackColor = true;
			DebuggerBtn.Click += DebuggerBtn_Click;
			// 
			// textBox1
			// 
			textBox1.Location = new Point ( 12, 416 );
			textBox1.Name = "textBox1";
			textBox1.Size = new Size ( 683, 23 );
			textBox1.TabIndex = 2;
			textBox1.TextChanged += textBox1_TextChanged;
			// 
			// Form1
			// 
			AutoScaleDimensions = new SizeF ( 7F, 15F );
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size ( 800, 450 );
			Controls.Add ( textBox1 );
			Controls.Add ( DebuggerBtn );
			Controls.Add ( ConsoleText );
			Name = "Form1";
			Text = "Form1";
			FormClosing += Form1_FormClosing;
			Load += Form1_Load;
			ResumeLayout ( false );
			PerformLayout ();
		}

		#endregion
		private Button DebuggerBtn;
		public TextBox ConsoleText;
		private TextBox textBox1;
	}
}