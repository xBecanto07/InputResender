using Components.Implementations;
using Components.Interfaces;
using Components.InterfaceTests;
using System.Net;
using Xunit.Abstractions;

namespace Components.ImplementationTests {
	public class VPacketSenderTest : DPacketSenderTest<IPEndPoint> {
		private int Port = VPacketSender.DefPort;
		public VPacketSenderTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) { }
		public override DPacketSender GenerateTestObject () => new VPacketSender ( OwnerCore, Port++ );
	}
}