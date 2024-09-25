using Components.Interfaces;
using Components.Library;
using InputResender.CLI;
using InputResender.Services;
using System;
using System.Windows.Forms;
using CompGroup = Components.Library.CoreBase.ComponentGroup;

namespace InputResender.WindowsGUI {
	public partial class MainScreen : Form {
		private readonly CliWrapper CLI;
		private readonly Action<string> StdOut;
		protected DMainAppCore Core;
		//protected IPEndPoint TargetEP;
		protected ComponentVisualizer Visualizer;

		protected struct ModifierInfo {
			public KeyCode Key;
			public InputData.Modifier Modifier;
			public bool Locked;
			public ModifierInfo ( KeyCode key, InputData.Modifier mod, bool locked ) { Key = key; Modifier = mod; Locked = locked; }
			public override string ToString () => $"{Key} --> {Modifier}{(Locked ? " (Locked)" : "")}";
		}

		public void Error (Exception ex) {
			StdOut?.Invoke ( ex.Message );
			throw ex;
		}

		public MainScreen ( CliWrapper cli, DMainAppCore core, Action<string> stdout ) {
			Core = core;
			CLI = cli;
			StdOut = stdout;
			if ( stdout == null ) throw new ArgumentNullException ( nameof ( stdout ) );
			if ( core == null ) Error (new ArgumentNullException ( nameof ( core ) ) );
			if ( cli == null ) Error ( new ArgumentNullException ( nameof ( cli ) ) );

			InitializeComponent ();
			Core.MainAppControls.Log = WriteLine;
			UpdateModifiers ();


			EPListLabel.Text = cli.ProcessLine ( "network hostlist", true ).Message;
		}

		public void InvokeOnGUIThread ( Action action ) => Invoke ( action );

		public void RequestStop () {
			InvokeOnGUIThread ( Close );
			// Warning, that later on there might be situations that would prevent closing until something is done, either delaying the closing or not closing at all until some other action is taken.
			// Also some cleanup should be implemented so that GUI can be opened and closed multiple times.
		}

		public void WriteLine ( string line ) {
			ConsoleText.Invoke ( () => ConsoleText.AppendText ( line + Environment.NewLine ) );
			CLI.ProcessLine ( "visualizer update" );
		}

		private void PsswdUpdateBtn_Click ( object sender, EventArgs e ) {
			var res = TextPromptDialog.Show ( "Enter new group passphrase:", DMainAppControls.PsswdRegEx, true );
			if ( !res.DidSubmit ) return;
			CLI.ProcessLine ( $"password add {res.Text}" );
			CLI.ProcessLine ( "visualizer update" );
		}

		private void EPUpdateBtn_Click ( object sender, EventArgs e ) {
			var res = TextPromptDialog.Show ( "Enter new end point (IPv4:Port):", "([\\d]{1,3}\\.[\\d]{1,3}\\.[\\d]{1,3}\\.[\\d]{1,3}):([\\d]{1,5})" );
			if ( !res.DidSubmit ) return;
			CLI.ProcessLine ( $"target set {res.Text}" );
			CLI.ProcessLine ( "visualizer update" );
		}

		private void IsActiveCheckBox_CheckedChanged ( object sender, EventArgs e ) {

			CLI.ProcessLine ( "visualizer update" );
		}

		private void ShortcutCheckBox_CheckedChanged ( object sender, EventArgs e ) {

			CLI.ProcessLine ( "visualizer update" );
		}

		private CoreBase.ComponentInfo[] InputProcessors;
		private void InputProcSelector_SelectedIndexChanged ( object sender, EventArgs e ) {
			int ID = InputProcSelector.SelectedIndex;
			if ( ID < 0 | ID >= InputProcessors.Length ) return;
			Core.SelectNewPriority ( InputProcessors, InputProcessors[ID].Component );
			CLI.ProcessLine ( "visualizer update" );
		}
		private void InputProcSelector_DropDown ( object sender, EventArgs e ) => UpdateInputProcessors ();
		private void AddProcessorBtn_Click ( object sender, EventArgs e ) {
			InputProcessorFactory factory = new InputProcessorFactory ();
			if ( factory.ShowDialog () != DialogResult.OK ) return;
			if ( factory.SelectedCreator == null ) return;
			if ( !factory.SelectedCreator.ShowGUI () ) return;
			if ( factory.SelectedCreator.GetNewProcessor ( Core ) == null ) throw new NullReferenceException ( "Unable to created new input processor!" );
			UpdateInputProcessors ();
		}
		private void RemProcessorBtn_Click ( object sender, EventArgs e ) {
			int ID = InputProcSelector.SelectedIndex;
			if ( ID < 0 | ID >= InputProcessors.Length ) return;
			var comp = InputProcessors[ID];
			Core.Unregister ( comp.Component );
			UpdateInputProcessors ();
		}
		private void UpdateInputProcessors () {
			var asdf = new CompGroup ( Core, CompGroup.ByType<DInputProcessor> () );
			InputProcessors = asdf.GetInfoGroup ();
			InputProcSelector.Items.Clear ();
			foreach ( var processor in InputProcessors ) {
				InputProcSelector.Items.Add ( processor.TypeTree[^1].Name );
			}
			CLI.ProcessLine ( "visualizer update" );
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
			CLI.ProcessLine ( "visualizer update" );
		}

		private void VisualizerBtn_Click ( object sender, EventArgs e ) {
			CLI.ProcessLine ( "visualizer status", out BoolCommandResult res );
			if ( res.Result.Value ) CLI.ProcessLine ( "visualizer stop" );
			else CLI.ProcessLine ( "visualizer start" );
		}
	}
}