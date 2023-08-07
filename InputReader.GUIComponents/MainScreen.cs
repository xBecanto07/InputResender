using Components.Interfaces;
using Components.Library;
using System;
using System.Collections.Generic;
using System.Net;
using System.Windows.Forms;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace InputResender.GUIComponents {
	public partial class MainScreen : Form {
		protected DMainAppCore Core;
		protected IPEndPoint TargetEP;
		protected struct ModifierInfo {
			public KeyCode Key;
			public InputData.Modifier Modifier;
			public bool Locked;
			public ModifierInfo ( KeyCode key, InputData.Modifier mod, bool locked ) { Key = key; Modifier = mod; Locked = locked; }
			public override string ToString () => $"{Key} --> {Modifier}{(Locked ? " (Locked)" : "")}";
		}

		public MainScreen ( DMainAppCore core ) {
			Core = core;
			InitializeComponent ();
			//Core.MainAppControls.Log = ConsoleText.AppendText;
			UpdateModifiers ();
		}

		private void PsswdUpdateBtn_Click ( object sender, EventArgs e ) {
			var res = TextPromptDialog.Show ( "Enter new group passphrase:", "[\\x20-\\x7E]{5,}", true );
			if ( !res.DidSubmit ) return;

			Core.MainAppControls.ChangePassword ( res.Text );
		}

		private void EPUpdateBtn_Click ( object sender, EventArgs e ) {
			var res = TextPromptDialog.Show ( "Enter new end point (IPv4:Port):", "([\\d]{1,3}\\.[\\d]{1,3}\\.[\\d]{1,3}\\.[\\d]{1,3}):([\\d]{1,5})" );
			if ( !res.DidSubmit ) return;
			Core.MainAppControls.ChangeTarget ( res.Text );
		}

		private void IsActiveCheckBox_CheckedChanged ( object sender, EventArgs e ) {

		}

		private void ShortcutCheckBox_CheckedChanged ( object sender, EventArgs e ) {

		}

		private void InputProcSelector_SelectedIndexChanged ( object sender, EventArgs e ) {

		}

		private void AddProcessorBtn_Click ( object sender, EventArgs e ) {

		}

		private void RemProcessorBtn_Click ( object sender, EventArgs e ) {

		}

		private void AddCustModBtn_Click ( object sender, EventArgs e ) {
			var res = GetModifier ();
			if ( res == null ) return;
			Core.InputProcessor.SetCustomModifier ( res.Value.key, res.Value.mod );
			UpdateModifiers ();

		}

		private void EditCustModBtn_Click ( object sender, EventArgs e ) {
			var inputProc = Core.InputProcessor;
			var selected = ModifierList.SelectedItems;
			int N = selected.Count;
			foreach ( ModifierInfo mod in selected ) {
				if ( mod.Locked ) {
					if ( N == 1 ) MessageBox.Show ( $"Modifier {mod} cannot be modified. It is combination reserved by system." );
					continue;
				}
				var res = GetModifier ( mod.Key, mod.Modifier );
				if ( res == null ) continue;
				inputProc.SetCustomModifier ( mod.Key, InputData.Modifier.None );
				inputProc.SetCustomModifier ( res.Value.key, res.Value.mod );
			}
			UpdateModifiers ();
		}

		private (KeyCode key, InputData.Modifier mod)? GetModifier ( KeyCode key = KeyCode.None, InputData.Modifier mod = InputData.Modifier.None ) {
			var modDialog = new ModifierEditor ( key, mod );
			var diagRes = modDialog.ShowDialog ();
			key = modDialog.Key;
			mod = modDialog.Modifier;
			modDialog.Dispose ();
			if ( diagRes == DialogResult.OK ) {
				if ( Core.InputProcessor.Modifiers.TryGetValue ( key, out var info ) ) {
					if ( info.readOnly ) {
						MessageBox.Show ( $"Key {key} cannot be assigned as {mod} modifier, this is combination reserved by system." );
						return null;
					}
				}
				return (key, mod);
			}
			return null;
		}

		private void RemCustModBtn_Click ( object sender, EventArgs e ) {
			var selected = ModifierList.SelectedItems;
			var inputProc = Core.InputProcessor;
			int N = selected.Count;
			foreach ( ModifierInfo mod in selected ) {
				if ( mod.Locked ) {
					if ( N == 1 ) MessageBox.Show ( $"Modifier {mod} cannot be removed. It is combination reserved by system." );
					continue;
				}
				inputProc.SetCustomModifier ( mod.Key, InputData.Modifier.None );
			}
			UpdateModifiers ();
		}

		private void UpdateModifiers () {
			var list = ModifierList.Items;
			list.Clear ();
			foreach ( var mod in Core.InputProcessor.Modifiers )
				list.Add ( new ModifierInfo ( mod.Key, mod.Value.mod, mod.Value.readOnly ) );
		}
	}
}