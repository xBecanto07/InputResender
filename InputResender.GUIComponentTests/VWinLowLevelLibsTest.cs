using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using InputResender.GUIComponents;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace InputResender.GUIComponentTests {
	public class VWinLowLevelLibsTest : ComponentTestBase<VWinLowLevelLibs> {
		// Inspired by https://www.codeproject.com/Articles/5264831/How-to-Send-Inputs-using-Csharp
		public override CoreBase CreateCoreBase () {
			var ret = new CoreBaseMock ();
			new MInputReader ( ret );
			return ret;
		}
		public override VWinLowLevelLibs GenerateTestObject () => new VWinLowLevelLibs ( OwnerCore );

		static AutoResetEvent waiter;
		static List<(int, VKChange, KeyCode, Input.KeyboardInput)> messages;

		public VWinLowLevelLibsTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) {
			waiter = new AutoResetEvent ( false );
			messages = new List<(int, VKChange, KeyCode, Input.KeyboardInput)> ();
		}

		[Fact]
		public void HL_LL_DataConversion () {
			var inputData = GenerateInputData ();
			var HL_Data = TestObject.GetHighLevelData(OwnerCore.Fetch<DInputReader> (), inputData );
			var LL_Data = TestObject.GetLowLevelData ( HL_Data );
			LL_Data.Should ().Be ( inputData );
		}

		[Fact]
		public void VKChangeConversion () {
			VKChange[] vals = { VKChange.KeyDown, VKChange.KeyUp };
			foreach (var val in vals ) {
				var code = TestObject.GetChangeCode ( val );
				code.Should ().BeGreaterThan ( 0 );
				var en = TestObject.GetChangeType ( code );
				en.Should ().Be ( val );
			}
		}

		[Fact]
		public void InputDataToStringTest () {
			var inputData = GenerateInputData ();
			var vals = ((Input)inputData.Data).Data.ki;
			string ss = inputData.ToString ();
			ss.Should ().Contain ( "K" ).And
				.Contain ( ((KeyCode)vals.vkCode).ToString () ).And
				.Contain ( ((KeyCode)vals.scanCode).ToString () ).And
				.Contain ( vals.time.ToString () );
		}

		[Fact]
		public void SetUnsetHook () {
			var hookID = SetupHook ();

			TestObject.UnhookHookEx ( hookID );
			hookID.Should ().NotBe ( (IntPtr)null );
		}

		[Fact]
		public void SimulateInput () {
			var hookID = SetupHook ();
			var inputData = GenerateInputData ();

			try {
				var sent = TestObject.SimulateInput ( 1, new[] { inputData }, inputData.SizeOf, true );
				TestObject.PrintErrors ( Output.WriteLine );
				sent.Should ().Be ( 1 );
			} finally {
				TestObject.UnhookHookEx ( hookID );
			}

			waiter.WaitOne ( 200 ).Should ().BeTrue ();
			messages.Should ().HaveCount ( 1 );
			messages[0].Item1.Should ().Be ( 0 );
			messages[0].Item2.Should ().Be ( VKChange.KeyDown );
			messages[0].Item3.Should ().Be ( KeyCode.E );
		}

		public nint Callback ( int nCode, nint wParam, nint lParam ) {
			messages.Add ( (nCode, TestObject.GetChangeType ( (int)wParam ), (KeyCode)Marshal.ReadInt32 ( lParam ), new Input.KeyboardInput ( lParam )) );
			Thread.MemoryBarrier ();
			waiter.Set ();
			return nCode;
		}
		public static uint GetPID () => (uint)Process.GetCurrentProcess ().Id;

		public nint SetupHook () {
			for ( int i = 0; i < 3; i++ ) {
				nint hookID = -1;
				using ( Process curProcess = Process.GetCurrentProcess () )
				using ( ProcessModule curModule = curProcess.MainModule ) {

					var moduleHandle = TestObject.GetModuleHandleID ( curModule.ModuleName );
					hookID = TestObject.SetHookEx ( TestObject.HookTypeCode, Callback, moduleHandle, 0 );
				}

				TestObject.PrintErrors ( Output.WriteLine );

				if ( hookID != (IntPtr)null ) return hookID;
			}
			Assert.Fail ( "SetHookEx should not fail" );
			return -1;
		}

		public WinLLInputData GenerateInputData () => WinLLInputData.NewKeyboardData ( TestObject, (ushort)KeyCode.E, (ushort)KeyCode.E, 0, 123456, TestObject.GetMessageExtraInfoPtr () );
	}
}