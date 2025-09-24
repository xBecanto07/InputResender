using Components.Library;
using SeClav.Commands;
using SeClav.DataTypes;
using TOpCode = SeClav.SId<SeClav.OpCodeTag>;
using TDst = SeClav.SId<SeClav.DstTag>;
using TArg = SeClav.SId<SeClav.ArgTag>;
using TExtraArgs = SeClav.SId<SeClav.ExtraArgsTag>;

namespace SeClav;
internal class SCLParsingStatus {
	internal class SCLParsedScript : ISCLParsedScript {
		public IReadOnlyList<DataTypeDefinition> DataTypes { get; }
		public IReadOnlyList<ICommand> Commands { get; }
		public IReadOnlyList<CmdCall> CommandIndices { get; }
		public IReadOnlyList<IDataType> Constants { get; }
		public IReadOnlyList<DModuleLoader.IModuleInfo> Modules { get; }
		public IReadOnlyList<int> VariableTypes { get; }
		public IReadOnlyList<int> ResultTypes { get; }

		public SCLParsedScript ( SCLParsingStatus parsingStatus ) {
			// Also could have/extract from status some disassembly info (closer discussion in diary)

			//SCLParsing parser = new ( moduleLoader );

			//foreach ( string line in code.Split ( ['\n', '\r'], StringSplitOptions.RemoveEmptyEntries ) )
			//	parser.ProcessLine ( line );

			DataTypes = parsingStatus.dataTypes.ToList ().AsReadOnly ();
			Commands = parsingStatus.commands.ToList ().AsReadOnly ();
			CommandIndices = parsingStatus.commandIndices.ToList ().AsReadOnly ();
			Modules = parsingStatus.modules.ToList ().AsReadOnly ();
			Constants = parsingStatus.constants.ToList ().AsReadOnly ();
			VariableTypes = parsingStatus.variables.Select ( v => v.dataType ).ToList ().AsReadOnly ();
			ResultTypes = parsingStatus.results.Select ( r => parsingStatus.dataTypes.IndexOf ( r.Definition ) ).ToList ().AsReadOnly ();
			//Variables = parser.variables.Select ( v => DataTypes[v.dataType].Default ).ToList ().AsReadOnly ();
		}
	}

	internal class SCLDebugInfo : ISCLDebugInfo {
		public ISCLParsedScript Script { get; }
		public IReadOnlyDictionary<SId<ArgTag>, string> VarNames { get; }
		public string CompileLog { get; }
		public string MemoryInfo { get; }

		public SCLDebugInfo ( SCLParsingStatus parsingStatus, List<string> Log = null ) {
			Script = new SCLParsedScript ( parsingStatus );
			VarNames = parsingStatus.variables
				.Select ( ( v, i ) => (SCLInterpreter.CrArgVar ( i ), v.name) )
				.ToDictionary ( kv => kv.Item1, kv => kv.Item2 );
			//CompileLog = string.Join ( Environment.NewLine, Log );
			CompileLog = Log is null ? "No log provided ..." : string.Join ( Environment.NewLine, Log );
			MemoryInfo = string.Join ( Environment.NewLine, parsingStatus.MemoryInfo.Select ( kv => $"{kv.Key}: {string.Join ( ", ", kv.Value )}" ) );
		}
	}


	readonly List<DataTypeDefinition> dataTypes = [];
	readonly List<ICommand> commands = [];
	readonly List<CmdCall> commandIndices = [];
	readonly List<(int dataType, string name)> variables = [];
	readonly List<DModuleLoader.IModuleInfo> modules = [];
	readonly List<IDataType> constants = [];
	readonly List<IDataType> results = []; // FIXME: This should only be <int> as we don't yet know the value, only the data type

	readonly Dictionary<string, DataTypeDefinition> dataTypeMap = [];
	readonly Dictionary<string, ICommand> commandMap = [];

	readonly Dictionary<string, string> MemoryInfo = [];
	public IReadOnlyDictionary<string, string> GetMemoryInfoRef () => MemoryInfo;

	public SCLParsedScript GetResult () => new SCLParsedScript ( this );

	public SCLParsingStatus () {
		dataTypeMap["void"] = new SCLT_Void ();
		dataTypes.Add ( dataTypeMap["void"] );
		results.Add ( new SCLT_Void.VoidData ( dataTypes[0] as SCLT_Void ) );
	}

	public void RegisterModule ( DModuleLoader.IModuleInfo module ) {
		ArgumentNullException.ThrowIfNull ( module );
		if ( modules.Any ( m => m.Name == module.Name ) )
			throw new InvalidOperationException ( $"Module '{module.Name}' is already registered." );
		modules.Add ( module );
		foreach ( var cmd in module.Commands ) {
			if ( commandMap.ContainsKey ( cmd.CmdCode ) )
				throw new InvalidOperationException ( $"Command '{cmd.CmdCode}' is already defined." );
			commandMap[cmd.CmdCode] = cmd;
		}
		foreach ( var dt in module.DataTypes ) {
			if ( dataTypeMap.ContainsKey ( dt.Name ) )
				throw new InvalidOperationException ( $"Data type '{dt.Name}' is already defined." );
			dataTypeMap[dt.Name] = dt;
		}
	}

	public (int dataType, string name) GetVarInfo ( TDst varID ) {
		if ( varID.ValueId >= variables.Count )
			throw new ArgumentOutOfRangeException ( nameof ( varID ), $"Variable ID {varID} is out of range (0..{variables.Count - 1})." );
		return variables[varID.ValueId];
	}
	public (int dataType, string name) GetVarInfo ( TArg varID ) {
		if ( varID.ValueId >= variables.Count )
			throw new ArgumentOutOfRangeException ( nameof ( varID ), $"Variable ID {varID} is out of range (0..{variables.Count - 1})." );
		return variables[varID.ValueId];
	}

	public TOpCode RegisterCustomCmd ( ICommand cmd ) {
		ArgumentNullException.ThrowIfNull ( cmd );
		if ( commands.Any ( c => c.CmdCode == cmd.CmdCode ) )
			throw new InvalidOperationException ( $"Command '{cmd.CmdCode}' is already registered." );
		commands.Add ( cmd );
		return SCLInterpreter.CrOpCode ( commands.Count - 1 );
	}

	public ICommand TryGetCommand ( string cmd ) {
		ArgumentNullException.ThrowIfNull ( cmd );
		if ( !commandMap.TryGetValue ( cmd, out ICommand command ) ) return null;
		commands.Add ( command );
		return command;
	}

	public int GetCommandID ( ICommand cmd ) {
		ArgumentNullException.ThrowIfNull ( cmd );
		if ( !commands.Contains ( cmd ) ) {
			if ( !commandMap.ContainsKey ( cmd.CmdCode ) )
				throw new InvalidOperationException ( $"Command '{cmd.CmdCode}' ({cmd.CommonName}) is not registered nor present in any loaded module." );
			commands.Add ( cmd );
		}
		return commands.IndexOf ( cmd );
	}

	private void TranslateDataType (ref DataTypeDefinition dataType) {
		if ( dataTypeMap.TryGetValue ( dataType.Name, out var existing ) )
			dataType = existing;
		else throw new InvalidOperationException ( $"Data type '{dataType.Name}' is not registered." );
	}

	public DataTypeDefinition GetDataType ( string name ) {
		ArgumentNullException.ThrowIfNull ( name );
		return dataTypeMap.GetValueOrDefault ( name, null );
	}

	public DataTypeDefinition GetDataType ( ICommand cmd, int id) {
		ArgumentNullException.ThrowIfNull ( cmd );
		if ( id < 0 || id >= cmd.ArgC )
			throw new ArgumentOutOfRangeException ( nameof ( id ), $"Command '{cmd.CmdCode}' ({cmd.CommonName}) has only {cmd.ArgC} arguments, index {id} is out of range." );
		return GetDataType ( cmd.Args[id].type.Name );
	}

	public int GetDataTypeID ( DataTypeDefinition dataType ) {
		ArgumentNullException.ThrowIfNull ( dataType );
		TranslateDataType ( ref dataType );
		int typeDef = dataTypes.IndexOf ( dataType );
		if ( typeDef < 0 ) { typeDef = dataTypes.Count; dataTypes.Add ( dataType ); }
		return typeDef;
	}

	//public List<DataTypeDefinition> MapDataTypes ( ICollection<DataTypeDefinition> defs ) {
	//	ArgumentNullException.ThrowIfNull ( defs );
	//	List<DataTypeDefinition> mapped = [];
	//	foreach ( var def in defs ) {
	//		int id = dataTypes.IndexOf ( def );
	//		mapped.Add ( id >= 0 ? dataTypes[id] : null );
	//	}
	//	return mapped;
	//}

	public TArg RegisterVariable ( int dataType, string name ) {
		ArgumentNullException.ThrowIfNull ( name );
		if ( string.IsNullOrEmpty ( name ) ) throw new ArgumentException ( "Variable name cannot be empty.", nameof ( name ) );
		if ( variables.Any ( v => v.name == name ) )
			throw new InvalidOperationException ( $"Variable '{name}' is already defined." );
		var varID = SCLInterpreter.CrArgVar ( variables.Count );
		variables.Add ( (dataType, name) );
		UpdateMemoryInfo ();
		return varID;
	}

	public bool TryGetVarID ( string name, out TArg varID ) {
		ArgumentNullException.ThrowIfNull ( name );
		int index = variables.FindIndex ( v => v.name == name );
		if ( index < 0 ) {
			varID = default;
			return false;
		}
		varID = SCLInterpreter.CrArgVar ( index );
		return true;
	}
	public TArg GetVarID ( string name ) {
		//return SCLInterpreter.CrArgVar ( index );
		if ( TryGetVarID ( name, out var varID ) ) return varID;
		throw new KeyNotFoundException ( $"Variable '{name}' not found." );
	}

	public TArg AddConstant ( IDataType value ) {
		ArgumentNullException.ThrowIfNull ( value );
		constants.Add ( value );
		UpdateMemoryInfo ();
		return SCLInterpreter.CrArgCon ( constants.Count - 1 );
	}

	public TDst RegisterResult ( DataTypeDefinition dataType ) {
		int typeDef = GetDataTypeID ( dataType );
		results.Add ( dataTypes[typeDef].Default );
		UpdateMemoryInfo ();
		return SCLInterpreter.CrDst ( results.Count - 1 );
	}

	public DataTypeDefinition GetTypeOfVar ( TDst varID ) {
		switch (varID.ValueType) {
			case SCLInterpreter.ResultTypeID:
				if ( varID.ValueId >= results.Count )
					throw new ArgumentOutOfRangeException ( nameof ( varID ), $"Result ID {varID} is out of range (0..{results.Count - 1})." );
				return results[varID.ValueId].Definition;
			case SCLInterpreter.VarTypeID:
				if ( varID.ValueId >= variables.Count )
					throw new ArgumentOutOfRangeException ( nameof ( varID ), $"Variable ID {varID} is out of range (0..{variables.Count - 1})." );
				return dataTypes[variables[varID.ValueId].dataType];
			case SCLInterpreter.ConstTypeID:
				if ( varID.ValueId >= constants.Count )
					throw new ArgumentOutOfRangeException ( nameof ( varID ), $"Constant ID {varID} is out of range (0..{constants.Count - 1})." );
				return constants[varID.ValueId].Definition;
		default:
				throw new ArgumentOutOfRangeException ( nameof ( varID ), $"Variable ID {varID} is not of variable type ({SCLInterpreter.VarTypeID})." );
		}
			throw new ArgumentOutOfRangeException ( nameof ( varID ), $"Variable ID {varID} is out of range (0..{variables.Count - 1})." );
	}
	public DataTypeDefinition GetTypeOfVar ( TArg varID ) => GetTypeOfVar ( new TDst ( varID.Generic ) );

	public void PushCommand ( CmdCall cmd ) {
		commandIndices.Add ( cmd );
		UpdateMemoryInfo ();
	}


	void UpdateMemoryInfo () {
		MemoryInfo[$"Variables ({SCLInterpreter.VarTypeID})"] = string.Join ( ", ", variables.Select ( v => $"{v.name}:{dataTypes[v.dataType].Name}" ) );
		MemoryInfo[$"Constants ({SCLInterpreter.ConstTypeID})"] = string.Join ( ", ", constants.Select ( ( c, i ) => $"{i}:{c.Definition.Name}={c}" ) );
		MemoryInfo[$"Results ({SCLInterpreter.ResultTypeID})"] = string.Join ( ", ", results.Select ( ( r, i ) => $"{i}:{r.Definition.Name}" ) );
		System.Text.StringBuilder sb = new ();
		for ( int i = 0; i < commandIndices.Count; i++ ) {
			if ( i > 0 ) sb.Append ( ", " );
			var c = commandIndices[i];
			sb.Append ( $"{i}:{commands[c.opCode.ValueId].CmdCode}(" );
			for ( int a = 0; a < CmdCall.MaxDirectArgs; a++ ) {
				var arg = c.ArgAt ( a );
				if ( arg.RawValue == 0 ) break;
				if ( a > 0 ) sb.Append ( ", " );
				switch ( arg.ValueType ) {
					case SCLInterpreter.ResultTypeID:
						sb.Append ( $"R{arg.ValueId}" );
						break;
					case SCLInterpreter.VarTypeID:
						sb.Append ( $"V{arg.ValueId}" );
						break;
					case SCLInterpreter.ConstTypeID:
						sb.Append ( $"C{arg.ValueId}" );
						break;
					default:
						sb.Append ( $"?{arg.RawValue}" );
						break;
				}
				sb.Append ( '<' );
				sb.Append ( GetTypeOfVar ( new TDst ( arg ) ).Name );
				sb.Append ( '>' );
			}
			sb.Append ( ')' );
		}
	}
}