using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Components.InterfaceTests {
	public abstract class DPacketSenderTest<EP> : ComponentTestBase<DPacketSender<EP>> {
		public override CoreBase CreateCoreBase () => new CoreBaseMock ();
		protected List<long> Received = new List<long> ();

		[Fact]
		public void SendReceive () {
			byte[] data = SetupTest ( out var sender, out var receiver );
			Connect ( sender, receiver );
			receiver.ReceiveAsync ( SimpleCallback );
			sender.Send ( data );
			Thread.Sleep ( 5 );
			Disconnect ( sender, receiver );
			Received.Should ().HaveCount ( 1 ).And.Contain ( data.CalcHash () );
		}

		[Fact]
		public void MsgIsBuffered () {
			byte[] data = SetupTest ( out var sender, out var receiver );
			Connect ( sender, receiver );
			sender.Send ( data );
			receiver.ReceiveAsync ( SimpleCallback );
			Disconnect ( sender, receiver );
			Received.Should ().HaveCount ( 1 ).And.Contain ( data.CalcHash () );
		}

		protected bool SimpleCallback ( byte[] data) {
			Received.Add (data.CalcHash ());
			return true;
		}

		protected byte[] GenData ( int length ) {
			byte[] ret = new byte[length];
			for ( int i = 0; i < length; i++ ) ret[i] = (byte)i;
			return ret;
		}

		protected virtual byte[] SetupTest (out DPacketSender<EP> sender, out DPacketSender<EP> receiver) {
			Received.Clear ();
			sender = GenerateTestObject ();
			receiver = GenerateTestObject ();
			return GenData ( 32 );
		}

		protected void Connect ( DPacketSender<EP> A, DPacketSender<EP> B ) {
			A.Connect ( B.OwnEP ( 0, 0 ) );
			B.Connect ( A.OwnEP ( 0, 0 ) );
			A.Connections.Should ().Be ( 1 );
			B.Connections.Should ().Be ( 1 );
		}
		protected void Disconnect ( DPacketSender<EP> A, DPacketSender<EP> B ) {
			A.Disconnect ( B.OwnEP ( 0, 0 ) );
			B.Disconnect ( A.OwnEP ( 0, 0 ) );
			A.Connections.Should ().Be ( 0 );
			B.Connections.Should ().Be ( 0 );
		}
	}

	public class MPacketSenderTest : DPacketSenderTest<MPacketSender> {
		public override DPacketSender<MPacketSender> GenerateTestObject () => new MPacketSender ( OwnerCore );
	}
}
