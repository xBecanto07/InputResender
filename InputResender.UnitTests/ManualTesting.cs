using InputResender.UserTesting;
using System;
using Xunit;
using Xunit.Abstractions;
using SBld = System.Text.StringBuilder;

namespace InputResender.UnitTests {
	public class ManualTesting {
		ITestOutputHelper Output;
		SBld SB;

		public ManualTesting ( ITestOutputHelper output ) { Output = output; SB = new SBld (); }

		[Fact]
		public void InputCaptureTest () {
			var testObj = new ClientSideHookUserTest ( SB );
			TestBase ( testObj, testObj.HookTest );
		}
		[Fact]
		public void ClientSideIntegrationTest () {
			var testObj = new ClientSideIntegrationTest ( SB );
			TestBase ( testObj, testObj.InputProcessingTest );
		}
		[Fact]
		public void TapperInputTest () {
			var testObj = new TapperInputUserTest ( SB );
			TestBase ( testObj, testObj.WritingTest );
		}

		private void TestBase (UserTestBase testObj, AsyncMonothreaded.MainWorkerDelegate testMethod) {
			if ( testMethod.Target != testObj ) throw new OperationCanceledException ( "Invalid test: test method must be part of testing object instance!" );
			lock (UserTestApp.TestWaiter) {
				//if ( UserTestApp.MainAct != null ) throw new OperationCanceledException ( "Another test is in process..." );

				UserTestApp.TestMethod = testMethod;
				UserTestApp.ClientState = UserTestBase.ClientState.Master;
				UserTestApp.Init ();
				Program.Main ();
				UserTestApp.TestWaiter.WaitOne ();
			}
			var res = testObj.Result;
			if ( res.Passed ) testObj.SB.AppendLine ( $"{Environment.NewLine}Passed with message: {res.Msg}" );
			Output.WriteLine ( testObj.SB.ToString () );
			if ( !res.Passed ) Assert.Fail ( res.Msg ?? "" );
			testObj.Dispose ();
		}
	}
}