using Components.Implementations;
using Components.Interfaces;
using Components.InterfaceTests;
using FluentAssertions;
using InputResender.Services;
using InputResender.Services.NetClientService.InMemNet;
using System.Net;
using Xunit.Abstractions;

namespace Components.ImplementationTests; 
public class VPacketSenderTest : DPacketSenderTest<VPacketSender> {
	static int Port = VPacketSender.DefPort;
	public VPacketSenderTest ( ITestOutputHelper outputHelper ) : base ( outputHelper ) { }
	public override VPacketSender GenerateTestObject () => new VPacketSender ( OwnerCore, Port++ );

	protected override IEnumerable<INetPoint> GetLocalPoint ( VPacketSender testObj, int port ) {
		List<INetPoint> allEPs = new ();
		foreach ( var network in testObj.EPList ) foreach ( var ep in network )  allEPs.Add ( ep );

		List<InMemNetPoint> inMemNetPoints = allEPs.OfType<InMemNetPoint> ().ToList ();


		InMemNetPoint IMEP = allEPs.OfType<InMemNetPoint> ().FirstOrDefault ( ep => true, null );
		IMEP.Should ().NotBeNull ();
		yield return IMEP;

		IPNetPoint localhostEP = allEPs.OfType<IPNetPoint> ().FirstOrDefault ( ep => ep.LowLevelEP ().Address.Equals ( IPAddress.Loopback ), null );
		localhostEP.Should ().NotBeNull ();
		yield return localhostEP;

		IPNetPoint privateNetEP = allEPs.OfType<IPNetPoint> ().FirstOrDefault ( ep => IsLocal ( ep.LowLevelEP () ), null );
		privateNetEP.Should ().NotBeNull ();
		yield return privateNetEP;
	}

	private static bool IsLocal ( IPEndPoint ep ) {
		byte[] bytes = ep.Address.GetAddressBytes ();
		if ( bytes.Length == 4 ) {
			if ( bytes[0] == 10 ) return true;
			if ( bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31 ) return true;
			if ( bytes[0] == 192 && bytes[1] == 168 ) return true;
			return false;
		} else throw new System.NotImplementedException ();
	}
}