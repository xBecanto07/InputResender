using FluentAssertions;
using InputResender.Services;
using System;
using System.Net;
using System.Threading;
using Xunit;

namespace InputResender.ServiceTests {
	public abstract class INetClientServiceTest<T> where T : INetClientService {
		protected const int PacketSize = 32;
		protected abstract INetClientService GetTestObject ();

		[Fact]
		public void TestGeneratedObject () {
			var client = GetTestObject ();
			client.Should ().NotBeNull ();
			client.Should ().BeAssignableTo<T> ();
			client.Dispose ();
			var disposeCheck = client.Start;
			disposeCheck.Should ().Throw<ObjectDisposedException> ();
		}

		[Fact]
		public void SendRecvDirect () {
			var packet = PacketData ( PacketSize );
			var testObj = GetTestObject ();
			testObj.Start ();
			var recvTask = testObj.RecvAsync ();
			testObj.Send ( packet );
			Thread.Sleep ( 10 );
			recvTask.Status.Should ().Be ( System.Threading.Tasks.TaskStatus.RanToCompletion );
			recvTask.Result.Should ().Equal ( packet ).And.NotBeSameAs ( packet );
			testObj.Stop ();
		}
		[Fact]
		public void SendRecvViaSocket () {
			var packet = PacketData ( PacketSize );
			var sender = GetTestObject ();
			var receiver = GetTestObject ();
			receiver.Start ();
			var recvTask = receiver.RecvAsync ();
			sender.Send ( packet, receiver.EP, false );
			Thread.Sleep ( 10 );
			recvTask.Status.Should ().Be ( System.Threading.Tasks.TaskStatus.RanToCompletion );
			recvTask.Result.Should ().Equal ( packet ).And.NotBeSameAs ( packet );
			receiver.Stop ();
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
			return INetClientService.Create ( INetClientService.ClientType.UDP, EP );
		}
	}
}