using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Components.Factories;
using Components.Implementations;
using Components.Interfaces;
using Components.Library;
using InputResender.GUIComponents;
using static System.Runtime.InteropServices.JavaScript.JSType;
using SBld = System.Text.StringBuilder;

namespace InputResender.Visualizer {
	public partial class Visualizer : Form {
		HHookInfo HookInfo;
		DMainAppCoreFactory CoreFactory;
		DMainAppCore Core;
		bool Active = false;
		int ActID = 0;
		VTapperInput Tapper;
		MTextWriter TextWriter;
		List<string>[] Lines = new List<string>[ConsCnt];
		const int MaxLines = 40;
		const int ConsCnt = 3;
		Label[] Consoles = new Label[ConsCnt];
		bool[] TextUpdated = new bool[ConsCnt];

		public Visualizer () {
			for ( int i = 0; i < ConsCnt; i++ ) Lines[i] = new List<string> ( MaxLines );
			InitializeComponent ();
			Consoles[0] = ConsoleOut1; Consoles[1] = ConsoleOut2; Consoles[2] = ConsoleOut3;
		}

		private void Visualizer_Load ( object sender, EventArgs e ) {
			if ( CoreFactory == null ) CoreFactory = new DMainAppCoreFactory ();
			Core = CoreFactory.CreateVMainAppCore ();
			Tapper = new VTapperInput ( new CoreBaseMock (), new KeyCode[5] { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.Space }, InputData.Modifier.None );
			TextWriter = new MTextWriter ( Core );

			Core.InputProcessor.Callback = ProcessedCallback;
			Tapper.Callback = ProcessedCallback;
			HookInfo = new HHookInfo ( Core.InputReader, 1, VKChange.KeyDown, VKChange.KeyUp );
			SetupHook ();
			CompSelect.SelectedIndex = ActID;
		}

		private void CompSelect_SelectedIndexChanged ( object sender, EventArgs e ) {
			Active = CompSelect.SelectedIndex != 0;
			if ( !Active ) Log ( "Hook is now sleeping" );
			else Log ( $"Hook is active and printing state of {CompSelect.Text}{Environment.NewLine}" );
			ActID = CompSelect.SelectedIndex;
		}

		private bool Callback ( DictionaryKey key, HInputEventDataHolder inputData ) {
			return !Active;
		}

		private void DelayedCallback ( DictionaryKey key, HInputEventDataHolder inputData ) {
			if ( !Active ) return;
			if ( ActID == 0 ) return;
			if ( inputData != null ) {
				Log ( Environment.NewLine, 0 );
				int sel = inputData.ValueX < 1 ? 2 : 1;
				Log ( $"{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}", sel );
				Core.LogFcn = ( ss ) => Log ( ss, sel );
				Tapper.Owner.LogFcn = Core.LogFcn;
			}
			if ( ActID >= 1 ) {
				var eventList = VWinLowLevelLibs.EventList;
				if ( eventList == null || eventList.Count < 1 ) {
					Log ( $"Empty or non-existing EventList. Event logging might be disabled in the LL component?" );
					if ( ActID == 1 ) return;
				}
				var latestEvent = eventList[0];
				Log ( $"{Core.LowLevelInput.GetType ().Name}: {latestEvent.nCode} | {latestEvent.changeCode:X} | {Print ( latestEvent.inputData )}" );
				//var parsedLL = Core.LowLevelInput.ParseHookData ( HookInfo.HookIDs[0], latestEvent.changeCode, latestEvent.inputData );
				if ( ActID == 1 ) return;
			}
			if ( ActID >= 2 ) {
				Log ( $"{Core.InputReader.GetType ().Name}: {Print ( inputData )}" );
				if ( ActID == 2 ) return;
			}
			var combo = Core.InputParser.ProcessInput ( inputData );
			if ( ActID >= 3 ) {
				Log ( $"{Core.InputParser.GetType ().Name}: {Print ( combo )}" );
				if ( ActID == 3 ) return;
			}
			if ( ActID >= 5 ) Tapper.ProcessInput ( combo );
			else Core.InputProcessor.ProcessInput ( combo );
		}
		private void ProcessedCallback ( InputData cmd ) {
			if ( ActID < 5 ) {
				Log ( $"{Core.InputProcessor.GetType ().Name}: {Print ( cmd )}" );
				if ( ActID == 4 ) return;
			}
			if ( ActID >= 5 ) {
				if ( cmd == null ) {
					Log ( $"{Tapper.GetType ().Name}: Input skipped." );
					return;
				}
				Log ( $"{Tapper.GetType ().Name}: {Print ( cmd )} \t{Tapper.PrintState ()}" );
				if ( ActID == 5 ) return;
			}
			bool skipped = !TextWriter.Type ( cmd );
			if ( ActID >= 6 ) {
				Log ( $"{TextWriter.GetType ().Name}: Written {TextWriter.Text} (skip={skipped})" );
				Invoke ( () => {
					textBox1.Text = TextWriter.Text;
				} );
				if ( ActID == 6 ) return;
			}
		}

		public void Log ( string ss, int sel = 0 ) {
			string[] ssAr = ss.Replace ( "\r", null ).Split ( '\n' );
			for ( int i = 0; i < ssAr.Length; i++ ) {
				if ( Lines[sel].Count >= MaxLines ) Lines[sel].RemoveAt ( MaxLines - 1 );
				Lines[sel].Insert ( 0, ssAr[i] );
			}
			TextUpdated[sel] = true;
		}

		private void Visualizer_FormClosing ( object sender, FormClosingEventArgs e ) {
			MessageBox.Show ( "Closing form and clearing data." );
			ReleaseHook ();
		}

		private static string Print ( HWInput.KeyboardInput data ) => $"{Print ( data.vkCode )};{Print ( data.scanCode )};{data.dwFlags:X}{(data.IsValidated () ? '+' : '?')}";
		private static string Print ( int code ) => $"{(KeyCode)code}({code:X2})";
		private static string Print ( HInputEventDataHolder data ) => $"{Print ( data.InputCode )} ↓{data.ValueX:F1} Δ{data.DeltaX:F1}";
		private static string Print ( HInputEventDataHolder[] combo ) {
			SBld lSB = new SBld ();
			int N = combo.Length;
			lSB.Append ( $"[{N}]" );
			for ( int i = 0; i < N; i++ ) lSB.Append ( $" {i}=({Print ( combo[i] )})" );
			return lSB.ToString ();
		}
		private static string Print ( InputData cmd ) => $"{cmd.Cmnd} '{cmd.Key}' {cmd.Modifiers}";
		private static string Print ( VInputReader_KeyboardHook reader, HHookInfo hookInfo ) {
			SBld lSB = new SBld ();
			foreach ( var hook in hookInfo.HookIDs )
				lSB.Append ( $"{hook}: msg#{reader.GetHook ( hook ).hook.MsgID}   " );
			return lSB.ToString ();
		}

		private void SetupHook () {
			var hookIDs = Core.InputReader.SetupHook ( HookInfo, Callback, DelayedCallback );
			foreach ( var ID in hookIDs ) HookInfo.AddHookID ( ID );
		}
		private void ReleaseHook () {
			Core.InputReader.ReleaseHook ( HookInfo );
			foreach ( var ID in HookInfo.HookIDs ) HookInfo.RemoveHookID ( ID );
		}

		private void Awakener_Tick ( object sender, EventArgs e ) {
			SBld lSB = new SBld ();
			for ( int sel = 0; sel < ConsCnt; sel++ ) {
				if ( !TextUpdated[sel] ) continue;
				lSB.Clear ();
				Consoles[sel].ResetText ();
				for ( int i = 0; i < Lines[sel].Count; i++ )
					lSB.AppendLine ( Lines[sel][i] );
				string s = lSB.ToString ();
				Consoles[sel].Text = s;
				TextUpdated[sel] = false;
			}
		}
	}
}