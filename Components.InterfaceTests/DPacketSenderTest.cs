using Components.Interfaces;
using Components.Library;
using Components.LibraryTests;
using FluentAssertions;
using InputResender.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static Components.Interfaces.DPacketSender;

namespace Components.InterfaceTests {
	public abstract class DPacketSenderTest<TestObjT> : ComponentTestBase<TestObjT> where TestObjT : DPacketSender {
		public override CoreBase CreateCoreBase () => new CoreBaseMock ();
		protected Dictionary<byte, string> Received = new ();
		protected AutoResetEvent resetEvent = new AutoResetEvent (false);
		bool TestFinished = false;

		protected abstract IEnumerable<object> GetLocalPoint ( TestObjT testObj, int port );

		/// <summary>Inform if error found on receiver (B) can be disregarded as minor one or is deemed critical to fail the test.<para>Minor error might be e.g. failure to connect to network, that is not being tested.</para></summary>
		protected virtual bool IsErrorCritical (string msg, Exception e, TestObjT Aobj, object AEP, TestObjT Bobj, object BEP ) => true;

		public DPacketSenderTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) { }

		[Theory]
		[InlineData ( 1 )]
		[InlineData ( 3 )]
		public void SendRecvTest (int reps ) {
			//UDPDeviceLL.OnMessage += Output.WriteLine;

			HMessageHolder data = SetupTest ( out var sender, out var receiver );
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
					byte[] bData = data.InnerMsg;
					bData[0] = (byte)reps;
					bData[1] = (byte)i;
					HMessageHolder specificData = new ( data.Flags, bData );
					sender.Send ( specificData );
				}
				Disconnect ( sender, receiver, aEPIter.Current, bEPIter.Current );

				var logger = OwnerCore.Fetch ( typeof ( DLogger ) );
				if ( logger != null ) ((DLogger)logger).Print ( Output.WriteLine );

				Received.Should ().HaveCount ( reps );
				for ( byte i = 0; i < reps; i++ ) {
					byte[] bData = data.InnerMsg;
					bData[0] = (byte)reps;
					bData[1] = i;
					HMessageHolder specificData = new ( data.Flags, bData );
					long hash = specificData.Span.CalcHash ();
					string expected = $"{reps};{i};{hash}";
					Received.Should ().ContainKey ( i );
					Received[i].Should ().Be ( expected );
				}
				//receiver.Errors.Should ().BeEmpty ();

				foreach (var err in receiver.Errors) {
					try {
						IsErrorCritical ( err.msg, err.e, sender, aEPIter.Current, receiver, bEPIter.Current ).Should ().BeFalse ();
					} catch {
						throw new Exception ( $"Error '{err.msg}' is considered critical!", err.e );
					}
				}

				Output.WriteLine ( $"Finished {aEPIter.Current} -> {bEPIter.Current}" );

				Received.Clear ();
			}
		}

		protected CallbackResult SimpleCallback ( HMessageHolder data, bool processed ) {
			try {
				Received.Add ( data.InnerMsg[1], $"{data.InnerMsg[0]};{data.InnerMsg[1]};{data.Span.CalcHash ()}" );
			} catch ( ArgumentException e ) {
				if ( e.Message.Contains ( "item with the same key" ) )
					throw new ArgumentException ( $"Msg#{data.InnerMsg[1]} was already received. Currently known messages are: {string.Join ( ", ", Received.Keys )}" );
				else throw;
			}
			resetEvent.Set ();
			return TestFinished ? CallbackResult.Stop : CallbackResult.None;
		}

		protected byte[] GenData ( int length ) {
			byte[] ret = new byte[length];
			for ( int i = 0; i < length; i++ ) ret[i] = (byte)i;
			return ret;
		}

		protected virtual HMessageHolder SetupTest (out TestObjT sender, out TestObjT receiver ) {
			Received.Clear ();
			sender = GenerateTestObject ();
			receiver = GenerateTestObject ();
			return new ( HMessageHolder.MsgFlags.None, GenData ( 32 ) );
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