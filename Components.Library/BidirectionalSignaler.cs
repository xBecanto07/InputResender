using System.Collections.Generic;
using System.Diagnostics;
using SignalCombo = System.Tuple<System.Threading.AutoResetEvent, System.Threading.AutoResetEvent>;
using System;
using System.Runtime.CompilerServices;

namespace Components.Library {
	public class BidirectionalSignaler : IDisposable {
		private static List<BidirectionalSignaler> AllSignalers = new ();
		public enum WaitResult { None, Signaled, Canceled, Timeout }
		public enum Command { None, Signal, Wait, Echo }
		[Flags]
		public enum State { None = 0, Free = 1, Signaled = 2, WaitingSignal = 4, Echo = 8, WaitingEcho = 16, Canceled = 0x100 }
		public State ActState { get; private set; } = State.Free;
		private List<(int, Command, State, string[])> LastCommands;
		public readonly string Name;
		public string CommonName;
		public string[] LastCommandList { get => PrintLastCommand (); }
		public string AllLastCommands { get => string.Join ( Environment.NewLine, LastCommandList ); }
		public AutoResetEvent WaitOpDone, SignalOpDone, EchoOpDone;
		Exception latestException;
		public string Info { get; private set; } = "";
		AutoResetEvent canWait, canSignal, locker;
		TwoWayWaiter waiter;

		class TwoWayWaiter {
			CancellationTokenSource CTS;
			AutoResetEvent waiter, setter;
			public enum State { Free, Waiting, Signaled }
			/// <summary>Not thread-safe</summary>
			public State ActState { get; private set; } = State.Free;
			public WaitHandle WaitHandle => waiter;

			public TwoWayWaiter () { waiter = new ( false ); setter = new ( false ); CTS = new (); }
			public WaitResult WaitOne ( int ms = -1, Action midAct = null ) {
				if ( CTS.IsCancellationRequested ) return WaitResult.Canceled;
				if ( ms > 0 & ms < 20 ) midAct = null;
				Monitor.Enter ( waiter );
				bool res = Await ( waiter, ms, midAct );
				Monitor.Exit ( waiter );
				if ( CTS.IsCancellationRequested ) return WaitResult.Canceled;
				return res ? WaitResult.Signaled : WaitResult.Timeout;
			}
			public void Echo () {
				Monitor.Enter ( waiter );
				setter.Set ();
				Monitor.Exit ( waiter );
			}
			public WaitResult Set ( int ms = -1 ) {
				if ( CTS.IsCancellationRequested ) return WaitResult.Canceled;
				Monitor.Enter ( setter );
				bool res = WaitHandle.SignalAndWait ( waiter, setter, ms, false );
				Monitor.Exit ( setter );
				if ( CTS.IsCancellationRequested ) return WaitResult.Canceled;
				return res ? WaitResult.Signaled : WaitResult.Timeout;
			}
			public void Reset () {
				Monitor.Enter ( CTS );
				CTS.Cancel ();
				while ( !Monitor.TryEnter ( waiter ) ) waiter.Set (); waiter.WaitOne ( 0 );
				while ( !Monitor.TryEnter ( setter ) ) setter.Set (); setter.WaitOne ( 0 );
				ActState = State.Free;
				Monitor.Exit ( CTS );
			}
			public static bool Await ( WaitHandle waitHandle, int ms, Action midAct ) {
				bool res;
				if ( midAct == null ) res = waitHandle.WaitOne ( ms );
				else {
					res = waitHandle.WaitOne ( 10 );
					if ( !res ) {
						midAct ();
						res = waitHandle.WaitOne ( ms > 0 ? ms - 10 : ms );
					}
				}
				return res;
			}
		}

		public BidirectionalSignaler ( string name = "NoName" ) {
			CommonName = name;
			AllSignalers.Add ( this );
			SignalOpDone = new ( false ); WaitOpDone = new ( false ); EchoOpDone = new ( false );
			waiter = new ();
			canWait = new ( true ); canSignal = new ( true ); locker = new ( true );
		}

		public bool Wait ( EventWaitHandle opStarted = null ) {
			Log ( " +? Waiting" );
			Lock ();
			Log ( " += Waiting" );
			bool shouldSignalOpStart = true;
			Command ShouldFinish = Command.Wait;
			if ( ActState.HasFlag ( State.WaitingEcho ) ) {
				Log ( "Wait: Cannot start new Wait, last operation didn't finish and is waiting for echo!" );
				latestException = new InvalidOperationException ();
			} else {
				WaitOpDone.Reset ();
				if ( !canWait.WaitOne ( 50 ) ) {
					Log ( "Wait: Already waiting, throwing InvalidOpEx" );
					latestException = new InvalidOperationException ();
				} else {
					Log ( "Wait: Waiting for signal ..." );
					if ( !ActState.HasFlag ( State.Signaled ) ) ActState |= State.WaitingSignal;
					switch ( WaitEvent ( State.Signaled, ref ShouldFinish, () => TrySignal ( opStarted, ref shouldSignalOpStart ) ) ) {
					case WaitResult.Signaled:
						Log ( "Wait: Signal received" );
						TrySignal ( ref ShouldFinish );
						break;
					case WaitResult.Canceled:
						Log ( "Wait: canceling operation" );
						latestException = new OperationCanceledException ();
						break;
					}
					canWait.Set ();
					waiter.Echo ();
				}
				TrySignal ( ref ShouldFinish );
			}

			Log ( " - Waiting" );
			Unlock ();
			TrySignal ( ref ShouldFinish );
			TrySignal ( opStarted, ref shouldSignalOpStart );
			Throw ();
			return true;
		}
		public void SignalAndWait ( EventWaitHandle opStarted = null ) {
			Log ( " +? Signal" );
			Lock ();
			Log ( " += Signal" );
			bool shouldSignalOpStart = true;
			Command ShouldFinish = Command.Signal;
			if ( ActState.HasFlag ( State.WaitingEcho ) ) {
				Log ( "SignalAndWait: Cannot signal now, last operation didn't finish and is waiting for echo!" );
				latestException = new InvalidOperationException ();
			} else {
				SignalOpDone.Reset ();
				EchoOpDone.Reset ();
				bool isLocked = true;
				Log ( "SignalAndWait: Waiting for permission to send signal" );
				if ( TwoWayWaiter.Await ( canSignal, -1, () => { isLocked = false; Unlock (); TrySignal ( opStarted, ref shouldSignalOpStart ); } ) ) {
					if ( !isLocked ) Lock ();
					TrySignal ( opStarted, ref shouldSignalOpStart );
					Log ( "SignalAndWait: Sending signal" );
					SetState ( State.Signaled, State.WaitingSignal );
					ActState |= State.WaitingEcho;
					Log ( "SignalAndWait: Waiting for echo" );
					switch ( WaitEvent ( State.Echo, ref ShouldFinish, () => TrySignal ( opStarted, ref shouldSignalOpStart ) ) ) {
					case WaitResult.Signaled:
						Log ( "SignalAndWait: Echo received" );
						ActState = State.Free;
						canSignal.Set ();
						TrySignal ( ref ShouldFinish );
						break;
					case WaitResult.Canceled:
						Log ( "SignalAndWait: canceling operation" );
						latestException = new OperationCanceledException ();
						break;
					}
					waiter.Echo ();
				}
			}

			Log ( " - Signal" );
			Unlock ();
			TrySignal ( ref ShouldFinish );
			TrySignal ( opStarted, ref shouldSignalOpStart );
			Throw ();
		}
		private EventWaitHandle this[Command cmd] => cmd switch { Command.Wait => WaitOpDone, Command.Signal => SignalOpDone, Command.Echo => EchoOpDone, _ => null };
		private void TrySignal ( ref Command cmd ) {
			if ( cmd != Command.None ) { this[cmd]?.Set (); cmd = Command.None; }
		}
		private void TrySignal ( EventWaitHandle signaler, ref bool shouldSignal ) {
			if ( shouldSignal ) { signaler?.Set (); shouldSignal = false; }
		}
		public void SignalBack ( EventWaitHandle opStarted = null ) {
			Log ( " +? Echo" );
			Lock ();
			Log ( " += Echo" );
			bool shouldSignalOpStart = true;
			Command ShouldFinish = Command.Echo;
			if ( ActState.HasFlag ( State.WaitingEcho ) ) {
				EchoOpDone.Reset ();
				TrySignal ( opStarted, ref shouldSignalOpStart );
				EchoOpDone.Set ();
				Log ( "Sending echo signal" );
				SetState ( State.Echo, State.WaitingEcho );
			} else {
				Log ( $"Cannot Echo during during state of {ActState}" );
				latestException = new InvalidOperationException ();
			}
			Log ( " - Echo" );
			Unlock ();
			TrySignal ( ref ShouldFinish );
			TrySignal ( opStarted, ref shouldSignalOpStart );
			Throw ();
		}
		public bool Reset ( EventWaitHandle opStarted = null ) {
			Log ( " +? Reseting ..." );
			bool shouldSignalOpStart = true;
			Lock ();
			Log ( " += Reseting ...\r\n" );
			SetState ( State.Canceled );
			//Info = " - Reseted\r\n";
			LastCommands?.Clear ();
			ActState = State.Free;
			Unlock ();
			TrySignal ( opStarted, ref shouldSignalOpStart );
			Throw ();
			return true;
		}

		private WaitResult SetState ( State newState, State clear = State.None, [CallerMemberName] string caller = null ) {
			ActState |= newState;
			string log = $"Setting state with flag {newState} (called by {caller}\r\n  New state: {ActState})";
			if ( clear != State.None ) { ActState &= ~clear; log += $", clearing flag {clear}"; }
			Log ( log );
			Unlock ();
			var res = waiter.Set ();
			Lock ();
			Log ( $"SetState returning to {caller}" );
			return res;
		}
		private WaitResult WaitEvent ( State targetState, ref Command cmd, Action opStartAct, [CallerMemberName] string caller = null ) {
			Log ( $"|Waiting for {targetState} (called by {caller})" );
			//if ( ActState.HasFlag ( targetState ) ) return WaitResult.Signaled;
			//if ( ActState.HasFlag ( State.Canceled ) ) return WaitResult.Canceled;

			WaitResult ret = WaitResult.None;
			WaitResult semiRet = WaitResult.None;
			Command inCmd = cmd;
			do {
				Unlock ();
				semiRet = waiter.WaitOne ( midAct: () => {
					TrySignal ( ref inCmd );
					opStartAct ();
				} );
				Lock ();
				if ( ActState.HasFlag ( targetState ) ) ret = WaitResult.Signaled;
				else if ( ActState.HasFlag ( State.Canceled ) ) ret = WaitResult.Canceled;
			} while ( ret == WaitResult.None );
			return ret;
		}
		private void Throw () {
			if ( latestException == null ) return;
			Exception e = latestException;
			latestException = null;
			Log ( $"Throwing exception ({e.Message})" );
			WaitOpDone.Set ();
			throw e;
		}
		public void Dispose () { AllSignalers.Remove ( this ); }
		public override string ToString () => $"Signaler({CommonName}:{Name}):{ActState}";
		public string[] PrintLastCommand () {
			Lock ();
			if ( LastCommands == null ) return Array.Empty<string> ();
			int N = LastCommands.Count;
			string[] ret = new string[N];
			for ( int i = 0; i < N; i++ ) {
				(int ID, Command cmd, State state, string[] stack) info = LastCommands[i];
				string ss = $"[{info.ID}]: {info.state}+{info.cmd}  1:{info.stack[0]}";
				if ( info.stack.Length > 1 ) ss += $"2:{info.stack[1]}";
				if ( info.stack.Length > 2 ) ss += $"2:{info.stack[2]}";
				if ( info.stack.Length > 3 ) ss += $"2:{info.stack[3]}";
				ret[N - i - 1] = ss;
			}
			Unlock ();
			return ret;
		}
		public void ClearInfo () {
			Lock ();
			Info ??= "";
			if ( Info != "" ) {
				int lastClear = Info.LastIndexOf ( "Clearing" ) + "Clearing".Length;
				int BRL = " ...\r\n".Length;
				if ( lastClear + BRL == Info.Length ) Info = string.Concat ( Info.AsSpan ( 0, Info.Length - BRL ), " ( 2x) ...\r\n" );
				else if ( lastClear + BRL + 6 == Info.Length && int.TryParse ( Info.AsSpan ( lastClear + 2, 2 ), out int res ) && res < 99 ) {
					Info = string.Concat ( Info.AsSpan ( 0, lastClear ), $" ({res + 1,2}x) ..\r\n" );
				}
				Log ( "\r\nClearing ..." );
				//LastCommands.Clear ();
				LastCommands?.Add ( (-1, (Command)69, (State)69, Array.Empty<string> ()) );
			}
			Unlock ();
		}
		public void Lock ( [CallerMemberName] string caller = null ) {
			Interlocked.MemoryBarrierProcessWide ();
			Interlocked.MemoryBarrier ();
			locker.WaitOne ();
			Interlocked.MemoryBarrier ();
			Interlocked.MemoryBarrierProcessWide ();
			Log ( $">> Locked by {caller}" );
		}
		public void Unlock ( [CallerMemberName] string caller = null ) {
			Log ( $">> Unlocked by {caller}" );
			Interlocked.MemoryBarrierProcessWide ();
			Interlocked.MemoryBarrier ();
			if ( locker.WaitOne ( 0 ) ) throw new SynchronizationLockException ( "Lock was already released!" );
			locker.Set ();
			Interlocked.MemoryBarrier ();
			Interlocked.MemoryBarrierProcessWide ();
		}
		public void Log ( string msg ) {
			lock ( this ) {
				Info += msg + "\r\n";
			}
		}
	}
	public class SingleUseSignaler {
		readonly EventWaitHandle Waiter;
		public bool Signaled { get; private set; } = false;

		public SingleUseSignaler ( EventWaitHandle waiter ) { Waiter = waiter; }
		public void Signal () {
			if ( Signaled ) throw new OperationCanceledException ( "Waiter was already signaled!" );
			Waiter.Set ();
			Signaled = true;
		}
	}
	public class SingleUseWaiter {
		readonly EventWaitHandle Waiter;

		public SingleUseWaiter ( EventWaitHandle waiter ) { Waiter = waiter; }
		public void Wait () { Waiter.Set (); }
	}
}