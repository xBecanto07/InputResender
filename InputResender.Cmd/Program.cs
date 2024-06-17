using InputResender.Services.NetClientService.InMemNet;
using InputResender.ServiceTests.NetServices;

namespace InputResender.Cmd;
public class Program {
	static void Main ( string[] args ) {
		NetworkConnectionHappyFlowTest testObj = new ();
		for ( int i = 0; i < 6; i++ )
			testObj.MultipleConnections ( nameof ( InMemNetPoint ) );
	}
}