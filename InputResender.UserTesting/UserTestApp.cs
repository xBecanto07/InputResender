using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace InputResender.UserTesting;

public static class UserTestApp {
	public static UserTestBase.ClientState ClientState { get; private set; }
	public static int LogLevel = 5;
	public static readonly AsyncMonothreaded MainAct;
	public static AutoResetEvent TaskSignaler = new AutoResetEvent ( false );
	public static bool ShouldContinue = true;

	static UserTestApp () {
		MainAct = new AsyncMonothreaded ( Run, () => Program.SendSignal () );
	}

	static IEnumerable<Action> Run () {
		yield return InitTest;
		Program.WriteLine ();
		if ( ShouldContinue ) {
			foreach ( var testAct in UserTestBase.RunAll () ) yield return testAct;
			yield return ShowResults;
		}
		yield return FinishTest;
		yield break;
	}

	private static void InitTest () {
		while ( Program.MainForm == null || !Program.Initialized ) TaskSignaler.WaitOne ();
		Program.WriteLine ( "Is this main instance (m) or a second client (s)? (to change log-level first, press (l), to exit press (e)" );
		while ( true ) {
			var key = Program.Read ();
			switch ( key ) {
			case 'm': Program.WriteLine ( "Starting main instance." ); ClientState = UserTestBase.ClientState.Master; break;
			case 's': Program.WriteLine ( "Starting new client." ); ClientState = UserTestBase.ClientState.Slave; break;
			case 'l':
				while ( true ) {
					var nextKey = Program.Read ();
					if ( (nextKey < '0') | (nextKey > '9') ) continue;
					LogLevel = nextKey - '0';
					Program.WriteLine ( $"LogLevel set to {LogLevel}" );
				}
			case 'e': ShouldContinue = false; break;
			default: continue;
			}
			break;
		}
	}

	private static void ShowResults () {
		for ( int i = 0; i < UserTestBase.TestResults.Length; i++ ) {
			var res = UserTestBase.TestResults[i];
			Program.WriteLine ( $"Test #{i} '{res.Name}': {(res.Passed? "Pass" : "Fail")}    {res.Msg}" );
		}
	}

	private static void FinishTest () {
		Program.WriteLine ();
		Program.WriteLine ( "To close this window, press Enter ..." );
		Program.ReadLine ();
		Program.Initialized = false;
		Application.Exit ();
	}

	public static void Continue () => MainAct.Continue ();
	public static void Log (int level, string msg) {
		if ( LogLevel >= level ) Program.WriteLine ( msg );
	}
}