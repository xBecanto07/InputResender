using Components.LibraryTests;
using Components.Library;
using Components.Interfaces;
using InputResender.Commands;

namespace Components.InterfaceTests; 
public class CommandTestBaseMCore : CommandTestBase {
	public CommandTestBaseMCore ( ACommand testedCmd ) : base ( testedCmd ) {}

	public void SetMCore ( DMainAppCore.CompSelect selector ) {
		DMainAppCore core = DMainAppCore.CreateMock ();
		CmdProc.SetVar ( CoreManagerCommand.ActiveCoreVarName, core );
	}
}
