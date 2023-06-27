using Components.Implementations;
using Components.Interfaces;
using Components.InterfaceTests;
using System.Net;

namespace Components.ImplementationTests {
	public class VPacketSenderTest : DPacketSenderTest<IPEndPoint> {
		private int Port = VPacketSender.DefPort;
		public override DPacketSender<IPEndPoint> GenerateTestObject () => new VPacketSender ( OwnerCore, Port++ );
	}
}