using Components.ImplementationTests;
using InputResender.Services.NetClientService.InMemNet;
using InputResender.ServiceTests.NetServices;
using Xunit.Abstractions;


namespace InputResender.Cmd;
public class Program {
	static void Main ( string[] args ) {
		Outputer outputer = new ();
		VPacketSenderTest test = new ( outputer );
		test.SendRecvTest ( 3 );
	}
}

class Outputer : ITestOutputHelper {
	public void WriteLine ( string message ) {
		System.Console.WriteLine ( message );
	}
	public void WriteLine ( string format, params object[] args ) {
		System.Console.WriteLine ( format, args );
	}
}