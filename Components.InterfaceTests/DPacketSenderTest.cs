using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Components.InterfaceTests {
	public abstract class DPacketSenderTest<EP> : ComponentTestBase<DPacketSender> {
		public override CoreBase CreateCoreBase () => new CoreBaseMock ();
		protected List<long> Received = new List<long> ();
		protected AutoResetEvent resetEvent = new AutoResetEvent (false);

		public DPacketSenderTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) { }

		[Fact]
		public void SendReceive () {
			byte[] data = SetupTest ( out var sender, out var receiver );
			Connect ( sender, receiver );
			receiver.ReceiveAsync ( SimpleCallback );
			sender.Send ( data );
			resetEvent.WaitOne ();
			Disconnect ( sender, receiver );

			var logger = OwnerCore.Fetch ( nameof ( DLogger ) );
			if ( logger != null ) ((DLogger)logger).Print ( Output.WriteLine );

			Received.Should ().HaveCount ( 1 ).And.Contain ( data.CalcHash () );
			receiver.Errors.Should ().BeEmpty ();
		}

		protected bool SimpleCallback ( byte[] data) {
			Received.Add (data.CalcHash ());
			resetEvent.Set ();
			return true;
		}

		protected byte[] GenData ( int length ) {
			byte[] ret = new byte[length];
			for ( int i = 0; i < length; i++ ) ret[i] = (byte)i;
			return ret;
		}

		protected virtual byte[] SetupTest (out DPacketSender sender, out DPacketSender receiver) {
			Received.Clear ();
			sender = GenerateTestObject ();
			receiver = GenerateTestObject ();
			return GenData ( 32 );
		}

		protected void Connect ( DPacketSender A, DPacketSender B ) {
			A.Connect ( B.OwnEP ( 0, 0 ) );
			B.Connect ( A.OwnEP ( 0, 0 ) );
			A.Connections.Should ().Be ( 1 );
			B.Connections.Should ().Be ( 1 );
		}
		protected void Disconnect ( DPacketSender A, DPacketSender B ) {
			A.Disconnect ( B.OwnEP ( 0, 0 ) );
			B.Disconnect ( A.OwnEP ( 0, 0 ) );
			A.Connections.Should ().Be ( 0 );
			B.Connections.Should ().Be ( 0 );
		}
	}

	public class MPacketSenderTest : DPacketSenderTest<MPacketSender> {
		public MPacketSenderTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) { }
		public override DPacketSender GenerateTestObject () => new MPacketSender ( OwnerCore );
	}
}
