using Components.LibraryTests;
using Components.Library;
using Components.Interfaces;
using Components.Implementations;
using InputResender.Commands;
using Components.InterfaceTests;

namespace Components.ImplementationTests;
public class CommandTestBaseVCore : CommandTestBaseMCore {
	public CommandTestBaseVCore ( ACommand testedCmd ) : base ( testedCmd ) { }

	public void SetVCore ( DMainAppCore.CompSelect selector ) {
		DMainAppCoreFactory factory = new ();
		DMainAppCore core = factory.CreateVMainAppCore ( selector );
		CmdProc.SetVar ( CoreManagerCommand.ActiveCoreVarName, core );
	}
}
