using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputResender.UnitTests.IntegrationTests;
//public class BasicInputSimulationTest : InputSimulationTest {
//	protected override string[] InitCmds => Array.Empty<string> ();
//}

public class WindowsInputSimulationTest : InputSimulationTest {
	protected override string[] InitCmds => ["windows load --force", "windows msgs start"];
}

public abstract class InputSimulationTest : BaseIntegrationTest {
	protected abstract string[] InitCmds { get; }

	public InputSimulationTest () : base ( GeneralInitCmds ) {
		foreach ( string cmd in InitCmds ) cliWrapper.ProcessLine ( cmd );
	}
}