using System.Collections.Generic;
using Components.Library;
using System.Linq;

namespace SeClav;
public class SCLRunner : ICommand {
	private readonly ISCLParsedScript Script;
	public string CmdCode => "SCLInterpreter";
	public string CommonName => "SeClav Language Interpreter";
	public string Description => "Executes SeClav Language scripts.";
	public int ArgC => 0; // Arguments for script not supported yet
	public readonly SIdVal NoArg = new ( 0, 0 );
	public readonly SIdVal NoDst = new ( SCLInterpreter.ResultTypeID, 0 );

	internal SCLRunner ( ISCLParsedScript script ) {
		Script = script ?? throw new ArgumentNullException ( nameof ( script ) );
	}
	public SCLRunner ( SCLScriptHolder scriptHolder ) : this ( scriptHolder?.ParsedScript ) { }

	public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => throw new NotImplementedException ();

	public DataTypeDefinition ReturnType => throw new NotImplementedException ();

	public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
		List<string> progress = [];
		return ExecuteSafe ( runtime, args, ref progress ); // No fast version yet
	}
	public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
		progress = [];
		List<SIdVal> argIDs = [];
		try {
			int PC = 0;
			int N = Script.CommandIndices.Count;
			while ( PC < N ) {
				if ( PC < 0 || PC >= N )
					throw new IndexOutOfRangeException ( $"Program Counter (PC) is out of range: {PC}." );
				CmdCall cmdIndices = Script.CommandIndices[PC];
				var cmd = Script.Commands[cmdIndices.opCode.ValueId];
				if ( TestFlags ( runtime, cmdIndices.flags ) == false ) {
					progress.Add ( $"PC={PC}: Skipping {cmd.CmdCode} due to flags {cmdIndices.flags}." );
					PC++;
					continue;
				}

				argIDs.Clear ();
				for ( int i = 0; i < cmd.ArgC; i++ ) {
					SIdVal argID = i switch {
						0 => cmdIndices.arg1,
						1 => cmdIndices.arg2,
						2 => cmdIndices.arg3,
						3 => cmdIndices.arg4,
						_ => throw new NotSupportedException ( $"Command {cmd.CmdCode} has more than 4 arguments, which is not supported yet." )
					};
					argIDs.Add ( argID );
				}

				progress.Add ( $"PC={PC}: Executing '{cmd.CmdCode}' ({cmd.CommonName}) with args [{string.Join ( ", ", argIDs )}] -> {cmdIndices.dst}" );

				IDataType result = cmd.ExecuteSafe ( runtime, argIDs, ref progress );
				if ( cmd.ReturnType != null && !cmdIndices.dst.Equals ( NoDst ) )
					runtime.SetVar ( cmdIndices.dst, result );
				//runtime.Results[cmdIndices.dst].Assign ( result );
				PC++;
			}
			return null;
		} catch ( Exception ex ) { throw; }
	}

	public IDataType Execute ( SCLRuntimeHolder runtime, IReadOnlyList<SIdVal> args )
		=> Execute ( runtime.BaseRuntime, args );
	public IDataType ExecuteSafe ( SCLRuntimeHolder runtime, IReadOnlyList<SIdVal> args, ref List<string> progress )
		=> ExecuteSafe ( runtime.BaseRuntime, args, ref progress );

	private bool TestFlags ( ISCLRuntime runtime, ushort flagReq ) {
		int flagStatus = ((ushort)runtime.GetFlags ()) | 1;
		// Check for '0' isn't currently implemented!
		int mask = 1 << (flagReq & 0x0F);
		mask |= 1 << ((flagReq >> 5) & 0x0F);
		mask |= 1 << ((flagReq >> 10) & 0x0F);
		return ( flagStatus & mask ) == mask;
	}
}

public class SCLInterpreter : ComponentBase<CoreBase> {
	public enum ExecMode { Normal, Safe };
	readonly DModuleLoader ModuleLoader;
	SCLRuntime Runtime;
	SCLRunner Runner;

	public override int ComponentVersion => 1;
	protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => [
		(nameof(LoadScript), typeof(void)),
		(nameof(Run), typeof(void)),
		];

	public SCLInterpreter ( string code, CoreBase owner ) : base ( owner ) {
		ModuleLoader = Owner.Fetch<DModuleLoader> ();
		if ( ModuleLoader == null )
			throw new InvalidOperationException ("DModuleLoader is not available in the owner.");
	}

	public void LoadScript ( string code ) {
		if ( string.IsNullOrWhiteSpace ( code ) )
			throw new ArgumentException ( "Script code cannot be null or empty.", nameof ( code ) );

		SCLParsing parser = new ( ModuleLoader.GetModule );
		foreach ( string line in code.Split ( ['\n', '\r'], StringSplitOptions.RemoveEmptyEntries ) )
			parser.ProcessLine ( line );
		ISCLParsedScript script = parser.GetResult ();

		Runtime = new SCLRuntime ( script );
		Runner = new SCLRunner ( script );
	}

	public List<string> Run ( ExecMode mode = ExecMode.Normal ) {
		if ( Runtime == null || Runner == null )
			throw new InvalidOperationException ( "No script is loaded. Please load a script before running." );
		List<string> log = [];
		switch ( mode ) {
		case ExecMode.Normal: Runner.Execute ( Runtime, [] ); break;
		case ExecMode.Safe: Runner.ExecuteSafe ( Runtime, [], ref log ); break;
		default: throw new ArgumentOutOfRangeException ( nameof ( mode ), $"Execution mode {mode}({(int)mode}) is not supported." );
		}
		return log;
	}



	/// <summary>Take signed pointer and get index into a constants pool</summary>
	public static int IDtoConstPtr ( int id ) => -1 - id;
	/// <summary>Take index of constants pool and calculate valid Ptr</summary>
	public static short ConstPtrToID ( int id ) => (short)(-1 - id);

	internal const int VarIdShift = 16 - 2;
	internal const int VarIdMask = (1 << VarIdShift) - 1;
	public const int VarTypeID = 1, ConstTypeID = 2, ResultTypeID = 3, InvalidTypeID = 0;
	private static SId<DstTag> CrDst ( int type, int val ) => new ( CrID ( type, val ) );
	private static SId<ArgTag> CrArg ( int type, int val ) => new ( CrID ( type, val ) );
	private static SIdVal CrID ( int type, int val ) {
		if ( type == InvalidTypeID ) throw new InvalidOperationException ( "Index type '3' is reserved for future use and cannot be used yet." );
		if ( type < 0 || type > 3 )
			throw new ArgumentOutOfRangeException ( nameof ( type ), $"Type {type} is out of range for variable ID." );
		if ( val < 0 || val > VarIdMask )
			throw new ArgumentOutOfRangeException ( nameof ( val ), $"Value {val} is out of range for variable ID." );
		return new ( type, val );
	}


	public enum DstType { Variable, Result };
	public enum ArgType { Variable, Constant, Command };
	public static SId<OpCodeTag> CrOpCode ( int id ) => new ( 0, id );
	public static SId<DstTag> CrDst ( int id ) => CrDst ( ResultTypeID, id );
	public static SId<DstTag> CrDst ( SId<ArgTag> arg ) => CrDst ( arg.ValueType, arg.ValueId );
	public static SId<ArgTag> CrArgVar ( int id ) => CrArg ( VarTypeID, id );
	public static SId<ArgTag> CrArgCon ( int id ) => CrArg ( ConstTypeID, id );
	public static SId<ArgTag> CrArgRes ( int id ) => CrArg ( ResultTypeID, id );


	public override StateInfo Info => new SCLInterpreterInfo ( this );
	public class SCLInterpreterInfo : StateInfo {
		public readonly string Script;
		public readonly int VarCount;
		public readonly int ConstCount;
		public readonly int ResultCount;
		public readonly int CommandCount;
		public SCLInterpreterInfo ( SCLInterpreter owner ) : base ( owner ) {
			// Should be replaced by better info later
			Script = owner.Runtime.Script.ToString ();
			VarCount = owner.Runtime.Variables.Count;
			ConstCount = owner.Runtime.Script.Constants.Count;
			ResultCount = owner.Runtime.Results.Count;
			CommandCount = owner.Runtime.Script.CommandIndices.Count;
		}
		public override string AllInfo () => $"{base.AllInfo ()}{BR}Variables: {VarCount}{BR}Constants: {ConstCount}{BR}Results: {ResultCount}{BR}Commands: {CommandCount}{BR}Script:{BR}{Script}";
	}
}