using Components.Interfaces;
using Components.Library;
using FluentAssertions;
using InputResender.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using static Components.Interfaces.DPacketSender;

namespace Components.InterfaceTests {
	public abstract class DPacketSenderTest<TestObjT> : ComponentTestBase<TestObjT> where TestObjT : DPacketSender {
		public override CoreBase CreateCoreBase () => new CoreBaseMock ();
		protected List<long> Received = new List<long> ();
		protected AutoResetEvent resetEvent = new AutoResetEvent (false);
		bool TestFinished = false;

		protected abstract IEnumerable<object> GetLocalPoint ( TestObjT testObj, int port );

		public DPacketSenderTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) { }

		[Theory]
		[InlineData ( 1 )]
		[InlineData ( 3 )]
		public void SendRecvTest (int reps ) {
			//UDPDeviceLL.OnMessage += Output.WriteLine;

			byte[] data = SetupTest ( out var sender, out var receiver );
			var aEPIter = GetLocalPoint ( sender, 0 ).GetEnumerator ();
			var bEPIter = GetLocalPoint ( receiver, 0 ).GetEnumerator ();
			Output.WriteLine ( $"Starting test with {reps} repetitions, A: {sender}, B: {receiver}" );
			while ( true ) {
				if ( !aEPIter.MoveNext () ) break;
				if ( !bEPIter.MoveNext () ) break;
				Output.WriteLine ( $"Testing {aEPIter.Current} -> {bEPIter.Current}" );
				Connect ( sender, receiver, aEPIter.Current, bEPIter.Current );
				receiver.OnReceive += SimpleCallback;
				for ( int i = 0; i < reps; i++ ) {
					Output.WriteLine ( $"Sending message #{i}" );
					receiver.OnReceive += SimpleCallback;
					sender.Send ( data );
				}
				Disconnect ( sender, receiver, aEPIter.Current, bEPIter.Current );

				var logger = OwnerCore.Fetch ( typeof ( DLogger ) );
				if ( logger != null ) ((DLogger)logger).Print ( Output.WriteLine );
				long hash = data.CalcHash ();
				Received.Should ().HaveCount ( reps ).And.OnlyContain ( val => val == hash );
				receiver.Errors.Should ().BeEmpty ();
				Output.WriteLine ( $"Finished {aEPIter.Current} -> {bEPIter.Current}" );

				Received.Clear ();
			}
		}

		protected CallbackResult SimpleCallback ( byte[] data, bool processed ) {
			Received.Add (data.CalcHash ());
			resetEvent.Set ();
			return TestFinished ? CallbackResult.Stop : CallbackResult.None;
		}

		protected byte[] GenData ( int length ) {
			byte[] ret = new byte[length];
			for ( int i = 0; i < length; i++ ) ret[i] = (byte)i;
			return ret;
		}

		protected virtual byte[] SetupTest (out TestObjT sender, out TestObjT receiver ) {
			Received.Clear ();
			sender = GenerateTestObject ();
			receiver = GenerateTestObject ();
			return GenData ( 32 );
		}

		protected void Connect ( TestObjT A, TestObjT B, object aEP, object bEP ) {
			A.Connect ( bEP );
			//B.Connect ( aEP ); // Connection is establish both ways
			A.Connections.Should ().Be ( 1 );
			B.Connections.Should ().Be ( 1 );
			A.IsEPConnected ( bEP ).Should ().BeTrue ();
			B.IsEPConnected ( aEP ).Should ().BeTrue ();
		}
		protected void Disconnect ( TestObjT A, TestObjT B, object aEP, object bEP ) {
			A.Disconnect ( bEP );
			A.Connections.Should ().Be ( 0 );
			B.Connections.Should ().Be ( 0 );
			A.IsEPConnected ( bEP ).Should ().BeFalse ();
			B.IsEPConnected ( aEP ).Should ().BeFalse ();
		}
	}

	public class MPacketSenderTest : DPacketSenderTest<MPacketSender> {
		public MPacketSenderTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) { }
		public override MPacketSender GenerateTestObject () => new MPacketSender ( OwnerCore );
		protected override IEnumerable<MPacketSender> GetLocalPoint ( MPacketSender testObj, int port ) {
			yield return testObj;
		}
	}
}