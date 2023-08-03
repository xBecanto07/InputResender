using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace InputResender.UserTesting;

public static class UserTestApp {
	public static UserTestBase.ClientState ClientState { get; set; } = UserTestBase.ClientState.Unknown;
	public static int LogLevel = 5;
	public static AsyncMonothreaded MainAct;
	public static AutoResetEvent TaskSignaler = new AutoResetEvent ( false );
	public static bool ShouldContinue = false;
	public static AsyncMonothreaded.MainWorkerDelegate TestMethod = null;
	public static readonly AutoResetEvent TestWaiter = new AutoResetEvent ( false );

	public static void Init () {
		if ( MainAct != null ) throw new OperationCanceledException ( "Another test is already running!" );

		var Act = (ClientState != UserTestBase.ClientState.Unknown && TestMethod != null) ? TestMethod : Run;
		MainAct = new AsyncMonothreaded ( Act, () => Program.SendSignal (), Finish );
	}

	static IEnumerable<Action> Run () {
		ShouldContinue = true;
		if ( ClientState == UserTestBase.ClientState.Unknown ) yield return InitTest;
		if ( ShouldContinue ) {
			foreach ( var testAct in UserTestBase.RunAll () ) yield return testAct;
			yield return ShowResults;
		}
		Program.WriteLine ();
		Program.WriteLine ( "To close this window, press Enter ..." );
		yield return () => Program.ReadLine ();
		Application.Exit ();
	}

	static void Finish () {
		MainAct = null;
		ClientState = UserTestBase.ClientState.Unknown;
		TestMethod = null;
		Thread.MemoryBarrier ();
		TestWaiter.Set ();
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
		Program.WriteLine ();
	}

	private static void ShowResults () {
		for ( int i = 0; i < UserTestBase.TestResults.Length; i++ ) {
			var res = UserTestBase.TestResults[i];
			Program.WriteLine ( $"Test #{i} '{res.Name}': {(res.Passed ? "Pass" : "Fail")}    {res.Msg}" );
		}
	}

	public static void Continue () {
		if ( MainAct != null ) MainAct.Continue ();
	}
	public static void Log (int level, string msg) {
		if ( LogLevel >= level ) Program.WriteLine ( msg );
	}
}