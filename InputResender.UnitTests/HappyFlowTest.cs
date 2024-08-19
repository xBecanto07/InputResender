using Components.Factories;
using Components.Implementations;
using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using System.Text;
using Xunit;
using OutpuHelper = Xunit.Abstractions.ITestOutputHelper;

namespace InputResender.UnitTests {
	public abstract class DHappyFlowTest<AppCore> where AppCore : DMainAppCore {
		protected AppCore Sender, Receiver;
		protected List<InputData> SentInput, ReceivedInput;
		protected byte[] Key, IV;
		protected AutoResetEvent receivedEvent;
		protected readonly OutpuHelper Output;

		public DHappyFlowTest( OutpuHelper outputHelper ) {
			Output = outputHelper;
			receivedEvent = new AutoResetEvent ( false );
			SentInput = new List<InputData>();
			ReceivedInput = new List<InputData>();
			Sender = GenerateAppCore ();
			Sender.LogFcn = Output.WriteLine;
			Receiver = GenerateAppCore ();
			Receiver.LogFcn = Output.WriteLine;
		}

		protected abstract AppCore GenerateAppCore ();

		protected void Init () {
			Key = Sender.DataSigner.GenerateIV ( Encoding.UTF8.GetBytes ( "Password" ) );
			IV = Sender.DataSigner.GenerateIV ( BitConverter.GetBytes ( 42 ) );
			Sender.DataSigner.Key = Key;
			Receiver.DataSigner.Key = Key;

			Receiver.Initialize ();

			Sender.PacketSender.Connect ( Receiver.PacketSender.OwnEP ( 0, 0 ) );
			Receiver.PacketSender.Connect ( Sender.PacketSender.OwnEP ( 0, 0 ) );
			Receiver.PacketSender.OnReceive += RecvCB;
		}

		protected bool ProcessInput ( DictionaryKey key, HInputEventDataHolder inputData) {
			var parsedInput = Sender.InputParser.ProcessInput ( inputData );
			Sender.InputProcessor.ProcessInput ( parsedInput );
			return false;
		}
		private void ProcessedCallback ( InputData inputData ) {
			SentInput.Add ( inputData );
			var packet = Sender.DataSigner.Encrypt ( (HMessageHolder)inputData.Serialize (), IV );
			Sender.PacketSender.Send ( packet );
		}

		protected DPacketSender.CallbackResult RecvCB ( HMessageHolder data, bool isProcessed ) {
			InputData recvData;
			if (data == null) {
				recvData = null;
			} else {
				HMessageHolder decoded = Receiver.DataSigner.Decrypt ( data, IV );
				recvData = (InputData)new InputData ( Receiver.Fetch<DPacketSender> () ).Deserialize ( decoded.InnerMsg );
			}
			lock (ReceivedInput) {
				ReceivedInput.Add ( recvData );
				receivedEvent.Set ();
			}
			return DPacketSender.CallbackResult.None;
		}

		protected void WaitForInput (int N) {
			while ( true ) {
				lock ( ReceivedInput ) {
					if ( ReceivedInput.Count > 1 ) return;
				}
				receivedEvent.WaitOne ();
			}
		}

		[Fact]
		public void SendRecv () {
			Init ();
			var data = new InputData ( Receiver.Fetch<DPacketSender> () ) { Cmnd = InputData.Command.KeyPress, DeviceID = 1, Key = KeyCode.E, X = 1 };
			var packet = Sender.DataSigner.Encrypt ( (HMessageHolder)data.Serialize (), IV );
			Sender.PacketSender.Send ( packet );
			receivedEvent.WaitOne ();
			ReceivedInput.Should ().HaveCount ( 1 );
			ReceivedInput[0].Should ().Be ( data );
		}

		[Fact]
		public void MainProcess () {
			Init ();

			Sender.CommandWorker.RegisterCallback ( ProcessedCallback );
			HHookInfo hookInfo = new HHookInfo ( Sender.InputReader, 1, VKChange.KeyDown, VKChange.KeyUp );
			Sender.MainAppControls.ChangeHookStatus ( hookInfo, true );

			var pressHolder = Sender.InputReader.SimulateKeyInput ( hookInfo, VKChange.KeyDown, KeyCode.E );
			var releaseHolder = Sender.InputReader.SimulateKeyInput ( hookInfo, VKChange.KeyUp, KeyCode.E );
			WaitForInput ( 2 );
			SentInput.Should ().HaveCount ( 2 );
			Receiver.PacketSender.Errors.Should ().BeEmpty ();
			ReceivedInput.Should ().Equal ( SentInput );
			ReceivedInput[0].Should ().NotBeNull ();
			ReceivedInput[1].Should ().NotBeNull ();
			ReceivedInput[0].DeviceID.Should ().Be ( pressHolder.HookInfo.DeviceID ).And.Be ( ReceivedInput[1].DeviceID );
			ReceivedInput[1].Pressed.Should ().BeFalse ( "Second event will release up the key" );
			ReceivedInput[0].Pressed.Should ().BeTrue ( "First event will press down the key" );
			ReceivedInput[0].Cmnd.Should ().Be ( InputData.Command.KeyPress );
			ReceivedInput[0].Key.Should ().Be ( KeyCode.E ).And.Be ( ReceivedInput[1].Key );
		}
	}

	public class MHappyFlowTest : DHappyFlowTest<MMainAppCore> {
		public MHappyFlowTest ( OutpuHelper outputHelper ) : base ( outputHelper ) { } 
		protected override MMainAppCore GenerateAppCore () => new MMainAppCore ();
	}

	public class VHappyFlowTest : DHappyFlowTest<VMainAppCore> {
		DMainAppCoreFactory CoreFactory;

		public VHappyFlowTest ( OutpuHelper outputHelper ) : base ( outputHelper ) { }
		protected override VMainAppCore GenerateAppCore () {
			if ( CoreFactory == null ) CoreFactory = new DMainAppCoreFactory ();
			var ret = CoreFactory.CreateVMainAppCore ( DMainAppCore.CompSelect.All & ~DMainAppCore.CompSelect.LLInput );
			new MLowLevelInput ( ret );
			return ret;
		}
	}
}
