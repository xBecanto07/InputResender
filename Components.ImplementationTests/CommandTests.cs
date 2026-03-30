using Components.Implementations;
using Components.InterfaceTests;
using InputResender.Commands;
using Xunit;

namespace Components.ImplementationTests; 
public class ConnectionManagerCommandTest : CommandTestBaseVCore {
	public ConnectionManagerCommandTest () : base ( owner => new ConnectionManagerCommand ( owner ) ) { }

	[Fact]
	public void TestList () {
		// A test that when NetworkSender component is not present in active core, a proper exception is thrown would be nice. But that might complicate stuff with (maybe) non-existing Destroy(core) method.
		AssertMissingCore ( "conns list" );
		SetVCore ( Interfaces.DMainAppCore.CompSelect.PacketSender );
		AssertCorrectMsg ( "conns list", "<No connection>" );
	}

	[Fact]
	public void TestCallback () {
		AssertMissingCore ( "conns callback none" );
		SetVCore ( Interfaces.DMainAppCore.CompSelect.PacketSender );
		AssertCorrectMsg ( "conns callback none", "No callback to remove." );
		AssertCorrectMsg ( "conns callback none", "No callback to remove." );
		AssertCorrectMsg ( "conns callback print", "Callback set to 'Print'." );
		AssertCorrectMsg ( "conns callback none", "Callback removed." );
	}
}