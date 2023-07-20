using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InputResender.UserTesting;

public static class UserTestApp {
	public static UserTestBase.ClientState ClientState { get; private set; }
	public static int LogLevel = 5;
	static IEnumerable<bool> activeAct;
	static IEnumerator<bool> activeEnumerator;
	private static char lastChar;

	static IEnumerable<bool> Run () {
		while ( Program.MainForm == null ) yield return true;
		Program.WriteLine ( "Is this main instance (m) or a second client (s)? (to change log-level first, press (l), to exit press (e)" );
		while ( true ) {
			foreach ( var waiting in WaitForRead () ) yield return true;
			var key = lastChar;
			switch ( key ) {
			case 'm': Program.WriteLine ( "Starting main instance." ); ClientState = UserTestBase.ClientState.Master; break;
			case 's': Program.WriteLine ( "Starting new client." ); ClientState = UserTestBase.ClientState.Slave; break;
			case 'l':
				while ( true ) {
					foreach ( var waiting in WaitForRead () ) yield return true;
					var nextKey = lastChar;
					if ( (nextKey < '0') | (nextKey > '9') ) { yield return true; continue; }
					LogLevel = nextKey - '0';
					Program.WriteLine ( $"LogLevel set to {LogLevel}" );
				}
			case 'e': yield return false; break;
			default: yield return true; continue;
			}
			break;
		}
		Program.WriteLine ();

		foreach ( var res in UserTestBase.RunAll () ) {
			if ( res == null ) yield return true;
			else {
				for ( int i = 0; i < res.Length; i++ ) {
					Program.WriteLine ( $"Test #{i} '{res[i].Item1}': {(res[i].Item2 ? "Pass" : "Fail")}    {res[i].Item3}" );
				}
			}
		}
		Program.WriteLine ();
		Program.WriteLine ( "To close this window, press any key ..." );
		foreach ( var waiting in WaitForRead () ) yield return true;
		Application.Exit ();
		yield break;
	}

	private static IEnumerable<bool> WaitForRead () {
		lastChar = '\0';
		while ( true ) {
			char? C = Program.Read ();
			if ( C == null ) yield return true;
			else {
				lastChar = C.Value;
				yield return false;
				yield break;
			}
		}
	}

	public static bool Continue () {
		if ( activeAct == null ) {
			activeAct = Run ();
			activeEnumerator = activeAct.GetEnumerator ();
		}
		return activeEnumerator.MoveNext ();
	}
}