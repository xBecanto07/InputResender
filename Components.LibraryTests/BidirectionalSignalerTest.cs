using Components.Library;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Components.LibraryTests {
	public class BidirectionalSignalerTest :IDisposable {
		const int Timeout = 150;
		const int Repetitions = 20;
		readonly BidirectionalSignaler signaler;
		readonly ITestOutputHelper Output;
		readonly AutoResetEvent OpStart;

		public BidirectionalSignalerTest (ITestOutputHelper outputHelper) {
			Output = outputHelper;
			OpStart = new AutoResetEvent ( false );
			signaler = new BidirectionalSignaler ( "Tested signaler" );
		}

		public void Dispose () {
			Output.WriteLine ( signaler.Info );
			signaler.Dispose ();
		}

		[Fact]
		public void NormalOrder () {
			// Wait - Signal - Echo
			for ( int i = 0; i < Repetitions; i++ ) {
				Task wait = WaitTask ();
				signaler.WaitOpDone.WaitOne ();
				IsRunning ( wait );
				Task signal = SignalTask ();
				signaler.SignalOpDone.WaitOne ();
				IsFinished ( wait );
				IsRunning ( signal );
				SignalBack ().Should ().BeNull ();
				OpStart.WaitOne ( 0 ).Should ().BeTrue ();
				signaler.EchoOpDone.WaitOne ();
				IsFinished ( signal );
				signaler.ClearInfo ();
			}
		}
		[Fact]
		public void Reverseorder () {
			// Signal - Wait
			for ( int i = 0; i < Repetitions; i++ ) {
				Task signal = SignalTask ();
				IsRunning ( signal );
				Task wait = WaitTask ();
				signaler.SignalOpDone.WaitOne ();
				signaler.WaitOpDone.WaitOne ();
				IsFinished ( wait );
				SignalBack ().Should ().BeNull ();
				OpStart.WaitOne ( 0 ).Should ().BeTrue ();
				IsFinished ( signal );
				signaler.ClearInfo ();
			}
		}

		[Fact]
		public void EchoDuringWrongStateThrowsInvalidOperationException () {
			for ( int i = 0; i < Repetitions; i++ ) {
				FinishesWithException<InvalidOperationException> ( SignalBack );
				OpStart.WaitOne ( 0 ).Should ().BeTrue ();
				WaitTask ();
				FinishesWithException<InvalidOperationException> ( SignalBack );
				OpStart.WaitOne ( 0 ).Should ().BeTrue ();
				signaler.ClearInfo ();
			}
		}
		[Fact]
		public void OtherActionWhenEchoRequestedThrowsInvalidOperationException () {
			for ( int i = 0; i < Repetitions; i++ ) {
				Task wait = WaitTask ();
				signaler.WaitOpDone.WaitOne ();
				Task signal = SignalTask ();
				signaler.SignalOpDone.WaitOne ();
				FinishesWithException<InvalidOperationException> ( Wait );
				FinishesWithException<InvalidOperationException> ( Signal );
				OpStart.WaitOne ( 0 ).Should ().BeTrue ();
				SignalBack ().Should ().BeNull ();
				OpStart.WaitOne ( 0 ).Should ().BeTrue ();
				IsFinished ( wait );
				IsFinished ( signal );
				signaler.ClearInfo ();
			}
		}
		[Fact]
		public void WaitAfterWaitBlocksShortTime () {
			for ( int i = 0; i < Repetitions; i++ ) {
				var waitTask = WaitTask ();
				signaler.WaitOpDone.WaitOne ();
				DateTime startTime = DateTime.Now;
				Task<Exception> wait = WaitTask ();
				FinishesWithException<InvalidOperationException> ( wait, Timeout * 2 );
				(DateTime.Now - startTime).Should ().BeGreaterThan ( TimeSpan.FromMilliseconds ( 40 ) );
				signaler.ActState.Should ().HaveFlag ( BidirectionalSignaler.State.WaitingSignal );

				var signalTask = SignalTask ();
				signaler.SignalOpDone.WaitOne ();
				SignalBack ().Should ().BeNull ();
				OpStart.WaitOne ( 0 ).Should ().BeTrue ();
				IsFinished ( waitTask );
				IsFinished ( signalTask );
				signaler.ClearInfo ();
			}
		}
		[Fact]
		public void SignalBlocksUntilRelease () {
			for ( int i = 0; i < Repetitions; i++ ) {
				Task signal1 = SignalTask ();
				Task signal2 = SignalTask ();
				IsRunning ( signal1 );
				IsRunning ( signal2 );
				WaitTask ().Wait ( Timeout ).Should ().BeTrue ();
				SignalBack ().Should ().BeNull ();
				OpStart.WaitOne ( 0 ).Should ().BeTrue ();
				WaitTask ().Wait ( Timeout ).Should ().BeTrue ();
				SignalBack ().Should ().BeNull ();
				OpStart.WaitOne ( 0 ).Should ().BeTrue ();
				signaler.ClearInfo ();
			}
		}
		[Fact]
		public void ResetStopsWait () {
			for ( int i = 0; i < Repetitions; i++ ) {
				Task<Exception> wait = WaitTask ();
				signaler.WaitOpDone.WaitOne ();
				IsRunning ( wait );
				signaler.Reset ().Should ().BeTrue ();
				FinishesWithException<OperationCanceledException> ( wait );
				signaler.ClearInfo ();
			}
		}

		[Fact]
		public void SignalWaitEchoPassesOnce () {
			// This must pass, because it will be hardcoded that SignalBack is called after Signal
			for ( int i = 0; i < Repetitions; i++ ) {
				Task signal = SignalTask ();
				Task wait = WaitTask ();
				signaler.SignalOpDone.WaitOne ();
				signaler.WaitOpDone.WaitOne ();
				signal.IsFaulted.Should ().BeFalse (); // Is waiting for echo
				wait.Wait ( Timeout ).Should ().BeTrue ();
				signaler.ActState.Should ().HaveFlag ( BidirectionalSignaler.State.Signaled );
				SignalBack ().Should ().BeNull ();
				OpStart.WaitOne ( 0 ).Should ().BeTrue ();
				signaler.ActState.Should ().Be ( BidirectionalSignaler.State.Free );
				FinishesWithException<InvalidOperationException> ( SignalBack );
				OpStart.WaitOne ( 0 ).Should ().BeTrue ();
				signaler.ClearInfo ();
			}
		}

		private Exception Wait () {
			OpStart.Reset ();
			try { signaler.Wait ( OpStart ); return null; } catch ( Exception e ) { return e; }
		}
		private Exception Signal () {
			OpStart.Reset ();
			try { signaler.SignalAndWait ( OpStart ); return null; } catch ( Exception e ) { return e; }
		}
		private Exception SignalBack () {
			OpStart.Reset ();
			try { signaler.SignalBack ( OpStart ); return null; } catch ( Exception e ) { return e; }
		}
		private Task<Exception> WaitTask () { var ret = Task.Run ( Wait ); OpStart.WaitOne (); return ret; }
		private Task<Exception> SignalTask () { var ret = Task.Run ( Signal ); OpStart.WaitOne(); return ret; }
		//private Task<Exception> SignalBackTask () => Task.Run ( SignalBack );
		private static void IsRunning ( Task t) {
			if ( !t.Wait ( 0 ) ) return;
			Assert.Fail ( "Process is not running!" );
		}
		private static void IsFinished ( Task t, int ms = Timeout ) {
			if ( t.Wait ( ms ) ) return;
			Assert.Fail ( "Process is running!" );
		}
		private static void FinishesWithException<T> ( Func<Exception> act, int ms = Timeout ) where T : Exception =>
			FinishesWithException<T> ( Task.Run ( act ), ms );
		private static void FinishesWithException<T> ( Task<Exception> t, int ms = Timeout ) where T : Exception {
			if ( !t.Wait ( ms ) ) {
				//Debugger.Launch ();
				//Debugger.Break ();
				Assert.Fail ( "Task didn't finish" );
			}
			t.Wait ( ms ).Should ().BeTrue ();
			t.Result.Should ().NotBeNull ().And.BeOfType<T> ();
		}
	}
}