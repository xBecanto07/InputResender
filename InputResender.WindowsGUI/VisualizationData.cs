using Components.Interfaces;
using Components.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SBld = System.Text.StringBuilder;

namespace InputResender.WindowsGUI {
	public class VisualizationData : IDisposable {
		readonly DMainAppCore Core;

		public VisualizationData ( DMainAppCore core ) {
			Core = core;
		}
		public void Dispose () {

		}

		public void Update () {
			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();
			generalInfo[typeof ( DLowLevelInput )] = Core.LowLevelInput.Info.AllInfo ();
			generalInfo[typeof ( DInputReader )] = Core.InputReader.Info.AllInfo ();
			generalInfo[typeof ( DInputParser )] = Core.InputParser.Info.AllInfo ();
			generalInfo[typeof ( DInputProcessor )] = Core.InputProcessor.Info.AllInfo ();
			generalInfo[typeof ( DDataSigner )] = Core.DataSigner.Info.AllInfo ();
			generalInfo[typeof ( DPacketSender )] = Core.PacketSender.Info.AllInfo ();
			generalInfo[typeof ( DEventVector )] = Core.EventVector.Info.AllInfo ();
			generalInfo[typeof ( DMainAppControls )] = Core.MainAppControls.Info.AllInfo ();
			generalInfo[typeof ( DShortcutWorker )] = Core.ShortcutWorker.Info.AllInfo ();
			generalInfo[typeof ( DCommandWorker )] = Core.CommandWorker.Info.AllInfo ();
			generalInfo[typeof ( DInputSimulator )] = Core.Fetch<DInputSimulator> ().Info.AllInfo ();
			LastUpdateTime = stopwatch.Elapsed;
		}

		public TimeSpan LastUpdateTime { get; private set; }
		private readonly Dictionary<Type, string> generalInfo = new Dictionary<Type, string> ();
		public IReadOnlyDictionary<Type, string> GeneralInfo { get => generalInfo.AsReadOnly (); }
	}
}