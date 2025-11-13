using Components.Library;

namespace SeClav;
public class SCLScriptHolder {
	// This class is simply a public wrapper around ISCLDebugInfo to allow passing it around without exposing the interface itself.
	// As a public it behaves as empty object. Internal code can access the actual script and debug info.

	private ISCLDebugInfo debugInfo;

	internal ISCLParsedScript ParsedScript => debugInfo.Script;
	internal ISCLDebugInfo DebugInfo => debugInfo;
	public IReadOnlyList<(string, Exception)> Errors;
	private List<(string, Exception)> ErrorList;

	internal SCLScriptHolder ( string code, System.Func<string, DModuleLoader.IModuleInfo> moduleLoader ) {
		ErrorList = [];
		Errors = ErrorList.AsReadOnly ();
		if ( string.IsNullOrEmpty ( code ) ) throw new ArgumentNullException ( nameof ( code ) );
		SCLParsing parser = new ( moduleLoader );
		var lines = code.Split ( new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None );

		foreach ( var line in lines ) {
			var trimmed = line.Trim ();
			if ( string.IsNullOrEmpty ( trimmed ) || trimmed.StartsWith ( "#" ) ) continue; // Skip empty lines and comments

			try {
				parser.ProcessLine ( trimmed );
			} catch ( Exception ex ) {
				ErrorList.Add ( ($"Error processing line '{line}': {ex.Message}", ex) );
			}
		}

		debugInfo = parser.GetResultWithDebugInfo ();
	}
}

internal interface ISCLParsedScript {
	IReadOnlyList<DataTypeDefinition> DataTypes { get; }
	IReadOnlyList<ICommand> Commands { get; }
	IReadOnlyList<CmdCall> CommandIndices { get; }
	IReadOnlyList<IDataType> Constants { get; }
	IReadOnlyList<DModuleLoader.IModuleInfo> Modules { get; }
	IReadOnlyList<int> VariableTypes { get; }
	IReadOnlyList<int> ResultTypes { get; }
	IReadOnlyDictionary<int, Func<IDataType>> Getters { get; }
}

internal interface ISCLDebugInfo {
	ISCLParsedScript Script { get; }
	IReadOnlyDictionary<SId<ArgTag>, string> VarNames { get; }
	string CompileLog { get; }
}

public abstract class DataTypeDefinition {
	public abstract string Name { get; }
	public abstract string Description { get; }
	public abstract IReadOnlySet<ICommand> Commands { get; }
	public abstract bool TryParse ( ref string line, out IDataType result );
	public abstract IDataType Default { get; }

	public readonly int GlobalID;
	public readonly string Owner;
	private static int nextGlobalID = 1;
	private static object globalIDLock = new ();
	protected DataTypeDefinition () {
		lock ( globalIDLock )
			GlobalID = nextGlobalID++;
		Owner = string.Join ( "\n", DefineOwner () );
	}

	private IReadOnlyList<string> DefineOwner () {
		var stack = new System.Diagnostics.StackTrace ( true );
		List<string> lines = stack.ToString ().Split ( new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries ).ToList ();
		lines.RemoveAll ( ( line ) => line.Contains ( "at System." ) || line.Contains ( "at Xunit." ) || line.Contains ( "at Reflection" ) );
		lines.Reverse ();
		return lines.AsReadOnly ();
	}
}

public interface IDataType {
	DataTypeDefinition Definition { get; }
	void Assign ( IDataType value );
}

public interface ISCLRuntime {
	[Flags]
	public enum SCLFlags : ushort {
		Empty = 0,
		None = 1,
		If = 2,
		Else = 4,
		Equal = 8,
		Larger = 16,
		Smaller = 32,
	}
	IDataType GetVar ( SIdVal varID );
	void SetVar ( SIdVal varID, IDataType value );
	IDataType SafeGetVar ( SIdVal varID );
	void SafeSetVar ( SIdVal varID, IDataType value );
	SCLFlags GetFlags ();
	void SetFlag ( SCLFlags value );
	void ResetFlag ( SCLFlags value );

	void UpdateMemoryInfo ( ref Dictionary<string, string> memInfo );

	static void SetOrReset (ISCLRuntime runtime, SCLFlags flag, bool set ) {
		if ( set ) runtime.SetFlag ( flag );
		else runtime.ResetFlag ( flag );
	}
}

public interface ICommandGen {
	string CmdCode { get; }
	string CommonName { get; }
	string Description { get; }
}
public interface ICommand : ICommandGen {
	int ArgC { get; }
	// The AssemblyX86 style of dst, src1, src2, ... is recommended
	IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args { get; }
	DataTypeDefinition ReturnType { get; }
	IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args );
	/// <summary>A 'debug' version. Should perform extra checks to catch and specify errors more easily.</summary>
	IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress );
}
public interface IMacro : ICommandGen {
	bool SelectRight { get; } // If true, a guider starts a 'part' instead of ending it
	bool UnorderedGuiders { get; } // If true, guider parts can be in any order
	IReadOnlyList<(int after, string split)> guiders { get; }
	/// <summary>Allow to rewrite textual command based on (guiderID, argument) into separete multiline commands</summary>
	string[] RewriteByGuiders ( (int guiderID, string arg)[] parts );
}

public struct SIdVal {
	private readonly ushort Val;
	public SIdVal ( int type, int id ) => Val = (ushort)((type << SCLInterpreter.VarIdShift) | (id & SCLInterpreter.VarIdMask));
	public override string ToString () => $"{Val >> SCLInterpreter.VarIdShift}${Val & SCLInterpreter.VarIdMask}";
	public readonly ushort ValueType => (ushort)(Val >> SCLInterpreter.VarIdShift);
	public readonly ushort ValueId => (ushort)(Val & SCLInterpreter.VarIdMask);
	public readonly ushort RawValue => Val;
}
public interface IIdTag { }
public struct OpCodeTag : IIdTag { }
public struct DstTag : IIdTag { }
public struct ArgTag : IIdTag { }
public struct ExtraArgsTag : IIdTag { }
public struct SId<TTag> : IEquatable<SId<TTag>> where TTag : struct, IIdTag {
	private readonly SIdVal Val;
	public SId ( int type, int id ) => Val = new SIdVal ( type, id );
	public SId ( SIdVal val ) => Val = val;
	//public static implicit operator ushort ( SId<TTag> op ) => (ushort)(op.Val & SCLInterpreter.VarIdMask);
	public bool Equals ( SId<TTag> other ) => Val.RawValue == other.Val.RawValue;
	public override bool Equals ( object? obj ) => obj is SId<TTag> other && Equals ( other );
	public override int GetHashCode () => Val.RawValue.GetHashCode ();
	public override string ToString () => $"{Val.ValueType}${Val.ValueId}";
	public readonly ushort ValueType => Val.ValueType;
	public readonly ushort ValueId => Val.ValueId;
	public readonly ushort RawValue => Val.RawValue;
	public readonly SIdVal Generic => Val;
}

internal struct CmdCall {
	public enum PtrType { Variable, Constant, ExtraArgument }

	public readonly SIdVal opCode;
	public readonly SIdVal dst;
	public readonly SIdVal arg1;
	public readonly SIdVal arg2;
	public readonly SIdVal arg3;
	public readonly SIdVal arg4;
	public readonly ushort flags;
	public readonly SIdVal extraArgs; // Currently unused, reserved for future use

	public const int MaxDirectArgs = 5;
	public SIdVal ArgAt (int id) {
		return id switch {
			0 => arg1,
			1 => arg2,
			2 => arg3,
			3 => arg4,
			_ => throw new IndexOutOfRangeException ( "CmdCall only supports up to 5 direct arguments." )
		};
	}

	public CmdCall ( SId<OpCodeTag> opCode, SId<DstTag> target, ushort flagReq, params SId<ArgTag>[] args ) {
		if ( args.Length > 4 ) throw new NotSupportedException ( "Cannot directly assign more than 4 arguments per call. Please use index into shared pool of extra arguments." );
		if ( opCode.ValueId < 0 || opCode.ValueId > ushort.MaxValue ) throw new NotSupportedException ( "OpCode must be a positive number between 0 and 65535." );
		this.opCode = opCode.Generic;
		dst = target.Generic;
		flags = flagReq;
		int N = args.Length;
		arg1 = N > 0 ? args[0].Generic : new SIdVal ( 0, 0 );
		arg2 = N > 1 ? args[1].Generic : new SIdVal ( 0, 0 );
		arg3 = N > 2 ? args[2].Generic : new SIdVal ( 0, 0 );
		arg4 = N > 3 ? args[3].Generic : new SIdVal ( 0, 0 );
		extraArgs = new SIdVal ( 0, 0 );
	}

	public CmdCall ( int opCode, List<CmdCall> extras, params int[] args ) {
		throw new NotImplementedException ();
	}
}

internal struct CmdExtraArgs {

}