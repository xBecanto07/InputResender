using Components.Implementations;
using Components.InterfaceTests;
using InputResender.Commands;
using Xunit;

namespace Components.ImplementationTests; 
public class ConnectionManagerCommandTest : CommandTestBaseVCore {
	public ConnectionManagerCommandTest () : base ( new ConnectionManagerCommand () ) { }

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

public class HookManagerCommandTest : CommandTestBaseMCore {
	public HookManagerCommandTest () : base ( new HookManagerCommand () ) { }

	[Fact]
	public void TestManager () {
		// I probably don't need any active core for some basic info (e.g. hook manager status), but it doesn't make quite sense to separate very limited functionality to allow without core, when core is expected to be always active in real use.
		AssertMissingCore ( "hook manager status" );
		SetMCore ( Interfaces.DMainAppCore.CompSelect.None );
		AssertCorrectMsg ( "hook manager status", "Hook manager not started." );
		AssertCorrectMsg ( "hook manager start", "Hook manager started." );
		AssertCorrectMsg ( "hook manager status", "Hook manager is running." );
	}
}