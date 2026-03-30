using Components.LibraryTests;
using Components.Library;
using Components.Interfaces;
using InputResender.Commands;

namespace Components.InterfaceTests; 
public class CommandTestBaseMCore : CommandTestBase<DMainAppCore> {
	protected static DMainAppCore CreateCore () => DMainAppCore.CreateMock ();

	public CommandTestBaseMCore ( CommandFactory testedCmd, DMainAppCore owner = null )
		: base ( owner ?? CreateCore ()
		, testedCmd
	) {
		CmdProc.SetVar ( ActiveCoreVarName, Owner );
	}

	public void SetMCore ( DMainAppCore.CompSelect selector ) {
		DMainAppCore core = DMainAppCore.CreateMock ();
		CmdProc.SetVar ( ActiveCoreVarName, core );
	}
}
