using Components.Library;
using FluentAssertions;
using InputResender.Services;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using Xunit;

namespace InputResender.ServiceTests {
	public abstract class INetClientServiceTest<T> where T : INetClientService {
		protected const int PacketSize = 32;
		protected abstract INetClientService GetTestObject ();
		protected BlockingCollection<MessageResult> PackeBuffer;

		protected MessageResult TestNextPacket ( INetClientService sender, INetClientService receiver, byte[] data, bool canDirect, int maxMsgs = int.MaxValue ) {
			PackeBuffer.Should ().HaveCountGreaterThan ( 0 ).And.HaveCountLessThanOrEqualTo ( maxMsgs );
			var recvData = PackeBuffer.Take ();
			if ( canDirect ) recvData.ResultType.Should ().BeOneOf ( MessageResult.Type.Direct, MessageResult.Type.Received );
			else recvData.ResultType.Should ().Be ( MessageResult.Type.Received );
			recvData.Data.Should ().BeEquivalentTo ( data );
			if ( !canDirect ) recvData.Data.Should ().NotBeSameAs ( data );


			return recvData;
		}

		[Fact]
		public void SendRecvDirect () {
			var packet = PacketData ( PacketSize );
			var testObj = GetTestObject ();
			var CTS = new CancellationTokenSource ( 200 );
			testObj.StartListen ( PackeBuffer.Add, CTS.Token );
			testObj.Send ( packet, testObj.EP );
			CTS.Token.WaitHandle.WaitOne ();
			testObj.WaitClose ();
			TestNextPacket ( testObj, testObj, packet, false, 1 );
		}
		[Fact]
		public void SendRecvViaSocket () {
			var packet = PacketData ( PacketSize );
			var sender = GetTestObject ();
			var receiver = GetTestObject ();
			var CTS = new CancellationTokenSource ( 300 );
			receiver.StartListen ( PackeBuffer.Add, CTS.Token );
			sender.Send ( packet, receiver.EP, false, 1 );
			CTS.Token.WaitHandle.WaitOne ();
			TestNextPacket ( sender, receiver, packet, false );
			sender.WaitClose ();
			receiver.WaitClose ();
		}
		[Fact]
		public void SendMultipleDirect () {
			var packet = PacketData ( PacketSize );
			var sender = GetTestObject ();
			var receiver = GetTestObject ();
			var CTS = new CancellationTokenSource ( 600 );
			receiver.StartListen ( PackeBuffer.Add, CTS.Token );
			for ( int i = 0; i < 6; i++ ) {
				sender.Send ( packet, receiver.EP );
				Thread.Sleep ( 10 );
				TestNextPacket ( sender, receiver, packet, false, 6 );
			}
			CTS.Token.WaitHandle.WaitOne ();
			PackeBuffer.Should ().HaveCount ( 0 );
			sender.WaitClose ();
			receiver.WaitClose ();
		}
		[Fact]
		public void SendMultipleViaSocket () {
			var packet = PacketData ( PacketSize );
			var sender = GetTestObject ();
			var receiver = GetTestObject ();
			var CTS = new CancellationTokenSource ( 600 );
			receiver.StartListen ( PackeBuffer.Add, CTS.Token );
			for ( int i = 0; i < 6; i++ ) {
				sender.Send ( packet, receiver.EP, false );
				Thread.Sleep ( 50 );
				TestNextPacket ( sender, receiver, packet, false, 6 );
			}
			CTS.Token.WaitHandle.WaitOne ();
			PackeBuffer.Should ().HaveCount ( 0 );
			sender.WaitClose ();
			receiver.WaitClose ();
		}

		public byte[] PacketData ( int size ) {
			byte[] ret = new byte[size];
			for ( int i = 0; i < size; i++ ) ret[i] = (byte)i;
			return ret;
		}
	}

	public class UDPClientServiceTest : INetClientServiceTest<UDPClientService> {
		protected static int Port = 65123;
		protected override INetClientService GetTestObject () {
			IPEndPoint EP = new IPEndPoint ( IPAddress.Loopback, Port++ );
			return INetClientService.Create ( ClientType.UDP, EP );
		}
	}
}