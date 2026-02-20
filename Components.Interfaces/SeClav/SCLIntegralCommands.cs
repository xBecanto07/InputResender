using SeClav;
using SeClav.DataTypes;
using System.Xml.Linq;

namespace SeClav.Commands;
public class CmdAssignment : ICommand {
	private readonly DataTypeDefinition retType = new SCLT_Same ();
	private readonly List<(string, DataTypeDefinition, string)> commands = [
		("variable", new SCLT_Any (), "The variable to assign the value to."),
		("value", new SCLT_Same (), "The value to assign to the variable.")
	];

	public string CmdCode => "set";
	public string CommonName => "Assign";
	public string Description => "Assigns a value to a variable.";
	public int ArgC => 2;
	public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => commands;
	public DataTypeDefinition ReturnType => retType;

	public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
		IDataType ret = runtime.GetVar ( args[1] );
		runtime.SetVar ( args[0], ret );
		return ret;
	}

	public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
		IDataType ret = runtime.SafeGetVar ( args[1] );
		var tar = runtime.SafeGetVar ( args[0] );
		if ( ret.Definition != tar.Definition ) throw new InvalidOperationException ( $"Cannot directly assign value of type {ret.Definition.Name} to variable of type {tar.Definition.Name}!" );
		runtime.SafeSetVar ( args[0], ret );
		return ret;
	}
}

public class EmitMacro ( Action<ushort, string> callback ) : IMacro {
	public bool SelectRight => false;
	public bool UnorderedGuiders => true;
	public IReadOnlyList<(int after, string split)> guiders => [];
	public string CmdCode => "emit";
	public string CommonName => "Emit Event";
	public string Description => "Emits an event (FSM transition) with the specified name.";
	public string[] RewriteByGuiders ( ushort flags, (int guiderID, string arg)[] parts ) {
		callback ( flags, parts[0].arg );
		return [];
	}
}

public class ThrowCmd : ICommand {
	public string CmdCode => "throw";
	public string CommonName => "Throw Exception";
	public string Description => "Throws an exception with the specified message.";
	public int ArgC => 0;
	public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [];
	public DataTypeDefinition ReturnType => new SCLT_Void ();
	public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
		throw new Exception ( "Exception thrown by SCL script." );
	}
	public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
		throw new Exception ( "Exception thrown by SCL script." );
	}
}