using Components.Library;
using SeClav.Commands;
using TOpCode = SeClav.SId<SeClav.OpCodeTag>;
using TDst = SeClav.SId<SeClav.DstTag>;
using TArg = SeClav.SId<SeClav.ArgTag>;
using TExtraArgs = SeClav.SId<SeClav.ExtraArgsTag>;
using Components.Interfaces.SeClav.Parsing;

namespace SeClav;
internal class SCLParsing {
	//private readonly DModuleLoader ModuleLoader;
	private readonly System.Func<string, IModuleInfo> ModuleLoader;
	private readonly SCLParsingStatus Status;
	readonly TOpCode AssignOpCode;
	private readonly List<string> Log = [];
	public Action<string> Logger;
	public bool EnableLogging;
	public readonly IReadOnlyDictionary<string, string> MemoryInfo;

	private readonly ParsingContext parsingContext;

	public SCLParsing ( System.Func<string, IModuleInfo> moduleLoader, Action<string> logger = null, bool enableLogging = true ) {
		ArgumentNullException.ThrowIfNull ( moduleLoader );
		Logger = logger;
		EnableLogging = enableLogging;
		ModuleLoader = moduleLoader;
		Status = new ();
		AssignOpCode = Status.RegisterCustomCmd ( new CmdAssignment () );
		Status.RegisterPraeDirective ( "in", RegisterExternalInput );
		Status.RegisterPraeDirective ( "mapper", RegisterExternalMapper );
		Status.RegisterPraeDirective ( "extFcn", RegisterExternalFunction );
		Status.RegisterPraeDirective ( "out", RegisterExternalOutput );
		MemoryInfo = Status.GetMemoryInfoRef ();

		parsingContext = new ParsingContext ( ModuleLoader, Status, MemoryInfo, AssignOpCode ) {
			EnableLogging = EnableLogging,
			Logger = Logger,
			ProcessLine = ProcessLine
		};
		if ( EnableLogging ) parsingContext.LogAdd ( "SCL parser started" );
	}

	/// <summary>Mostly intented only for tests. Prefer assigning prae directives into module and loading the module instead.</summary>
	internal void RegisterPraeDirective ( string directive, PraeDirective prae ) {
		Status.RegisterPraeDirective ( directive, prae );
	}

	private void RegisterExternalInput ( SCLParsingContext context, ArgParser args ) {
		string typeName = args.String ( 0, "Type of the external input to register", 1, true );
		string varName = args.String ( 1, "Name of the variable to register the external input into", 1, true );
		var typeDef = Status.GetDataType ( typeName );
		int typeID = Status.GetDataTypeID ( typeDef );
		Status.RegisterExterInVariable ( typeID, varName );
	}

	private void RegisterExternalMapper ( SCLParsingContext context, ArgParser args ) {
		if (args.ArgC != 4)
			throw new InvalidOperationException ( $"@mapper directive expects exactly 3 arguments: output type, mapper name, input type, separated by colon ':'. Got {args.ArgC - 1}." );
		if ( args.String ( 2, null, 1, true ) != ":" )
			throw new InvalidOperationException ( $"@mapper directive expects exactly 3 arguments: output type, mapper name, input type, separated by colon ':'. Got '{args.String ( 2, null, 1, true )}' instead of ':'." );

		string outName = args.String ( 0, "Name of the output variable to register the external mapper into", 1, true );
		string mapperName = args.String ( 1, "Name of the mapper to register", 1, true );
		string inName = args.String ( 3, "Name of the input variable to register the external mapper from", 1, true );

		var outTypeDef = Status.GetDataType ( outName );
		var inTypeDef = Status.GetDataType ( inName );
		Status.RegisterExterSampler ( mapperName, inTypeDef, outTypeDef );
	}

	private void RegisterExternalFunction ( SCLParsingContext context, ArgParser args ) {
		const int ARGS_START = 3;
		if ( args.ArgC < ARGS_START + 1 ) throw new InvalidOperationException ( $"@mapper directive expects at least 3 arguments: output type, function name, input type, separated by colon ':'. Got {args.ArgC - 1}." );
		if ( args.String ( 2, null, 1, true ) != ":" ) throw new InvalidOperationException ( $"@mapper directive expects exactly 3 arguments: output type, function name, input type, separated by colon ':'. Got '{args.String ( 2, null, 1, true )}' instead of ':'." );

		string returnTypeName = args.String ( 0, "Name of the return type", 1, true );
		string funcName = args.String ( 1, "Name of the function to register", 1, true );
		var returnType = Status.GetDataType ( returnTypeName );

		var argTypes = new DataTypeDefinition[args.ArgC - ARGS_START];
		for ( int i = ARGS_START; i < args.ArgC; i++ ) {
			string argTypeName = args.String ( i, $"Name of argument type {i - ARGS_START + 1}", 1, true );
			var argType = Status.GetDataType ( argTypeName );
			argTypes[i - ARGS_START] = argType;
		}
		Status.RegisterExterFunction ( funcName, returnType, argTypes );
	}

	private void RegisterExternalOutput ( SCLParsingContext context, ArgParser args ) {
		string typeName = args.String ( 0, "Type of the external output to register", 1, true );
		string varName = args.String ( 1, "Name of the variable to register the external output from", 1, true );
		var typeDef = Status.GetDataType ( typeName );
		int typeID = Status.GetDataTypeID ( typeDef );
		Status.RegisterExterOutVariable ( typeID, varName );
	}

	public ISCLParsedScript GetResult () => new SCLParsingStatus.SCLParsedScript ( Status );
	public ISCLDebugInfo GetResultWithDebugInfo () => new SCLParsingStatus.SCLDebugInfo ( Status, Log );

	/*
	.......┌──────┐....┌───────┐.....<DataType>┌──────┐.<VarName>.┌───────┐...............
	.......│Empty.│....│Module.│.....┌────────►│DefVar├──────────►│VarName│...............
	.......└------┘....└-------┘.....│.........└──────┘...........└-------┘...............
	.............▲......▲..........┌───►.........┌──────┐.............│...................
	............∅│......│@using....│...└────────►│SetVar├─────────┬───┘...................
	.............│......│..........│...<VarName>.└──────┘.......=.│.......................
	.............├──────┴┐.........│..............................▼.......................
	....────────►│.Start.├─────────┘........................┌──────┐............┌────────┐
	.............└-------┘......................┌───────────┤Assign├───────────►│Constant│
	........<Command>│..........┌──────────┐....│<Command>..└──────┘.<Constant>.└--------┘
	.................└─────────►│Expression│◄───┘.........................................
	............................└─────┬────┘..........┌───────┐...........................
	..................................│...............│.<Arg>.│...........................
	..................................│.ϵ.....┌───────┴─┐.....│...........................
	..................................└──────►│Arguments│◄────┘...........................
	..........................................└---------┘.................................
	*/

	public void ProcessLine ( string line ) {
		if ( EnableLogging ) parsingContext.LogAdd ( $"Processing line: {line}" );
		string originalLine = line;
		line = line.Trim ();
		int commentIndex = line.IndexOf ( '#' );
		if ( commentIndex == 0 || commentIndex > 0 && line[commentIndex - 1] == '\\' )
			line = line[..commentIndex].TrimEnd ();

		if ( string.IsNullOrEmpty ( line ) ) return;
		if ( SubMacroParser.Parse ( line, parsingContext, out var macroResult ) ) {
			macroResult.MustEndLine ();
			macroResult.Apply ();
			return;
		}
		if ( SubPraeParser.Parse ( line, parsingContext, out var praeResult ) ) {
			praeResult.MustEndLine ();
			praeResult.Apply ();
			return;
		}
		if ( SubUsingParser.Parse ( line, parsingContext, out var usingResult ) ) {
			usingResult.MustEndLine ();
			usingResult.Apply ();
			return;
		}
		if ( SubDataTypeParser.Parse ( line, parsingContext, out var dataTypeResult ) ) {
			dataTypeResult.MustEndLine ();
			dataTypeResult.Apply ();
			return;
		}
		if ( SubAssignmentParser.Parse ( line, parsingContext, out var assignmentResult ) ) {
			assignmentResult.MustEndLine ();
			assignmentResult.Apply ();
			return;
		}
		if ( SubCommandParser.Parse ( line, parsingContext, out var commandResult ) ) {
			commandResult.MustEndLine ();
			commandResult.Apply ();
			return;
		}
		throw new InvalidOperationException ( $"Could not parse line: '{originalLine}'." );
	}

	/*
	private ushort GetFlag (ref string line) {
		int flagReq = 0;
		if (line.StartsWith('?')) {
			line = line[1..];
			for (int i = 0; i < 3 && line.Length > 0; i++) {
				if ( line[0] == ' ' ) break;
				int linePos = line[0] switch {
					// Numerical
					'0' => throw new InvalidOperationException ( $"Testing for '0' flag is not allowed!" ),
								  '1' => 1, '2' => 2, '3' => 3,
					'4' => 4, '5' => 5, '6' => 6, '7' => 7,
					'8' => 8, '9' => 9, 'A' => 10, 'B' => 11,
					'C' => 12, 'D' => 13, 'E' => 14, 'F' => 15,
					// Aliases
					'N' => 0, // NONE
					'!' => 1, // IF
					'~' => 2, // ELSE
					'=' => 3, // EQUAL
					'>' => 4, // LARGER
					'<' => 5, // SMALLER
					_ => throw new InvalidOperationException ( $"Invalid flag character '{line[0]}' in command flags." )
				};
				flagReq |= linePos << (i * 5); // 4bit + sign bit per flag
				line = line[1..];
			}
			line = line.Trim ();
		}
		return (ushort)flagReq;
	}

	private bool TryParseMacro ( string line ) {
		string originalLine = line;
		string token = GetIdentifier ( ref line );
		if ( string.IsNullOrEmpty ( token ) ) return false;
		IMacro macro = Status.TryGetMacro ( token );
		if ( macro == null ) return false;

		var parts = ProcessMacro ( macro, line );
		string[] rewrittenLines = macro.RewriteByGuiders ( parts );
		if ( rewrittenLines == null ) // Macro rejected the input
			throw new InvalidOperationException ( $"Macro '{macro.CmdCode}' could not process the input line ({originalLine})." );
		foreach ( string rewrittenLine in rewrittenLines )
			ProcessLine ( rewrittenLine );
		return true;
	}

	private bool TryParseUsing ( string line ) {
		string originalLine = line;
		if ( !line.StartsWith ( "@using ", out string moduleName ) ) return false;
		var module = ModuleLoader ( moduleName );
		if ( module == null ) throw new InvalidOperationException ( $"Module '{moduleName}' not found." );
		if ( EnableLogging ) LogAdd ( $"Module '{moduleName}' loaded: {module.Description}, {module.Commands.Count} commands, {module.DataTypes.Count} data types." );
		Status.RegisterModule ( module );
		return true;
	}

	private bool TryParsePrae (string line) {
		string originalLine = line;
		if ( !line.StartsWith ( "@", out string metaCommand ) ) return false;
		line = line[1..]; // '@' shouldn't be part of the command but shouldn't be followed by whitespace either
		string cmdToken = GetIdentifier ( ref line );
		if ( cmdToken == null ) return false;

		return Status.TryRunPraeDirective ( cmdToken, new ArgParser ( line, null ) );
	}

	private bool TryParseDataType (string line) {
		string originalLine = line;
		string nextToken = GetIdentifier ( ref line );
		if ( string.IsNullOrEmpty ( nextToken ) ) return false;
		DataTypeDefinition dataType = Status.GetDataType ( nextToken );
		if ( dataType == null ) return false;
		// Migth be valid token, just not a data type (invalid token would throw an exception)

		int typeDef = Status.GetDataTypeID ( dataType );
		//TArg varID = ParseVariableName ( typeDef, ref line );
		TArg varID = Status.RegisterVariable ( typeDef, GetIdentifier ( ref line ) );

		if ( EnableLogging ) LogAdd ( $"Defining variable '{Status.GetVarInfo ( SCLInterpreter.CrDst ( varID ) ).name}' of type '{dataType.Name}'." );

		ushort flags = GetFlag ( ref line );
		if (flags != 0) 
			throw new InvalidOperationException ( $"Defining variable '{Status.GetVarInfo ( SCLInterpreter.CrDst ( varID ) ).name}' does not support conditional execution." );

		TryParseAssignment ( SCLInterpreter.CrDst ( varID ), ref line, flags );
		return true;
	}

	private bool TryParseAssignment ( string line ) {
		string originalLine = line;
		ushort flags = GetFlag ( ref line );
		string token = GetIdentifier ( ref line );
		if ( string.IsNullOrEmpty ( token ) ) return false;
		//TArg sId = Status.GetVarID ( token );
		if ( !Status.TryGetVarID ( token, out TArg sId ) )
			return false;

		return TryParseAssignment ( SCLInterpreter.CrDst ( sId ), ref line, flags );
	}

	private bool TryParseAssignment ( TDst varID, ref string line, ushort flagReq ) {
		string originalLine = line;
		if ( !line.StartsWith ( '=' ) ) return false;
		line = line[1..].TrimStart ();

		// Try to parse constant
		var dataType = Status.GetTypeOfVar ( varID );
		if ( dataType.TryParse ( ref line, out IDataType result ) ) {
			TArg constID = Status.AddConstant ( result );
			Status.PushCommand ( new ( AssignOpCode, varID, flagReq, new TArg ( varID.Generic ), constID ) );
			if ( EnableLogging ) LogAdd ( $"Assigning constant value to variable '{Status.GetVarInfo ( varID ).name}'." );
			return true;
		}

		string token = GetIdentifier ( ref line );
		if ( string.IsNullOrEmpty ( token ) ) throw new InvalidOperationException ( $"Expected command or value after '=' for variable '{Status.GetVarInfo ( varID ).name}', but got {line}." );

		// Try to assign from existing variable
		try {
			TArg srcVar = Status.GetVarID ( token );
			Status.PushCommand ( new ( AssignOpCode, varID, flagReq, new TArg ( varID.Generic ), srcVar ) );
			if ( EnableLogging ) LogAdd ( $"Assigning variable '{Status.GetVarInfo ( srcVar ).name}' to variable '{Status.GetVarInfo ( varID ).name}'." );
			return true;
		} catch { }

		// Try to parse command
		if ( TryParseCommand ( ref line, varID, token ) ) return true;
		throw new InvalidOperationException ( $"Expected command or value after '=' for variable '{Status.GetVarInfo(varID).name}', but got '{token}'." );
	}



	//private bool TryParseCommand ( ref string line, string token = null ) {
	//	ICommand cmd = TryParseCommandInner ( ref line, token );
	//	if ( cmd == null ) return false;

	//	ParseCommand ( cmd, line, null );
	//	return true;
	//}

	/// <summary>Tries to parse next token/line as a command. It <paramref name="dst"/> is specified and the command has a return type, the result is stored in <paramref name="dst"/>. If <paramref name="dst"/> is null, the return value is ignored.</summary>
	private bool TryParseCommand ( ref string line, TDst? dst, string token = null ) {
		string originalLine = line;

		ushort flagReq = GetFlag ( ref line );

		ICommand cmd = TryParseCommandInner ( ref line, token );
		if ( cmd == null ) return false;

		ParseCommand ( cmd, line, dst, flagReq );
		return true;
	}
	/// <summary>Tries to parse next token/line as a command. If successful (i.e. token is a valid command) and the command has a return type, the result is either stored in <paramref name="dst"/> (if specified) or a new temporary variable is created to hold the result (and <paramref name="dst"/> is set to that variable).</summary>
	private bool TryParseCommand ( ref string line, out TDst? dst, string token = null ) {
		string originalLine = line;
		dst = null;
		ICommand cmd = TryParseCommandInner ( ref line, token );
		if ( cmd == null ) return false;

		if ( cmd.ReturnType != null ) dst = Status.RegisterResult ( cmd.ReturnType );
		ushort flags = GetFlag ( ref line );
		ParseCommand ( cmd, line, dst, flags );
		return true;
	}
	private ICommand TryParseCommandInner (ref string line, string token = null ) {
		string originalLine = line;
		token ??= GetIdentifier ( ref line );
		if ( string.IsNullOrEmpty ( token ) ) return null;
		ICommand cmd = Status.TryGetCommand ( token );
		if ( cmd == null ) return null;
		return cmd;
	}



	private void ParseCommand ( ICommand cmd, string line, TDst? dst, ushort flagReq ) {
		string originalLine = line;
		if ( cmd.ReturnType == null && dst != null )
			throw new InvalidOperationException ( $"Command '{cmd.CmdCode}' does not return a value, but result ID {dst.Value} is specified." );

		int N = cmd.ArgC;
		TArg[] args = new TArg[N];
		for ( int i = 0; i < N; i++ ) {
			// The 'constant' assignment was set to higher priority in the TryParseAssignment
			// But this seems to be a bug as not providing a token (should be renamed to 'identifier') shouldn't throw right away, only to skip to another method. So generally speaking, only when all methods fail, an exception should be thrown. The remaining question though is, should there be priority in methods or should there be mechanism to treat multiple possible methods as bad as none? Well, the 'textbook' way would be 'yes' as you can't have e.g. a method with a name of already existing constant. To exclude this obvious answer we can reinterpret the question: "Where the check for naming conlfict should be?" I'd say that the simplest way currently is to use the priority for now.
			// And, could at least some parts of this be merged with the TryParseAssignment?

			// 🙄 While these two methods are doing very similar things, there are very minor differences all around making merging probably near impossible. Theoretically (but too complicated for now) it would be possible to either create a shared structure for assignment and then get out the parsing result, or do the parsing in two passes. 1st pass should probably be shared, 2nd pass should be just a 'code' construction.

			var type = Status.GetDataType ( cmd, i );

			// Constant argument
			if ( type.TryParse ( ref line, out IDataType arg ) ) {
				args[i] = Status.AddConstant ( arg );
				AssertArg ( "constant" );
				continue;
			}

			string token = GetIdentifier ( ref line );
			if ( string.IsNullOrEmpty ( token ) )
				throw new InvalidOperationException ( $"Expected argument {i + 1} for command '{cmd.CmdCode}'." );

			// Variable argument
			if ( Status.TryGetVarID ( token, out TArg varID ) ) {
				var varType = Status.GetTypeOfVar ( varID );
				if ( type != varType )
					throw new InvalidOperationException ( $"Argument {i + 1} for command '{cmd.CmdCode}' expects type '{type.Name}', but variable '{token}' is of type '{varType.Name}'." );
				args[i] = varID;
				AssertArg ( "variable" );
				continue;
			}

			// Inter-result argument
			if ( TryParseCommand ( ref line, out TDst? interResult, token ) ) {
				if ( interResult == null )
					throw new InvalidOperationException ( $"Argument {i + 1} for command '{cmd.CmdCode}' expects type '{type.Name}', but the command '{token}' does not return a value." );
				var interType = Status.GetTypeOfVar ( interResult.Value );
				if ( type != interType )
					throw new InvalidOperationException ( $"Argument {i + 1} for command '{cmd.CmdCode}' expects type '{type.Name}', but the command '{token}' returns type '{interType.Name}'." );
				args[i] = SCLInterpreter.CrArgRes ( interResult.Value.ValueId );
				AssertArg ( "inter-result variable" );
				continue;
			}

			throw new InvalidOperationException ( $"Argument {i + 1} for command '{cmd.CmdCode}' expects type '{type.Name}', but got '{token}'." );


			void AssertArg(string argType) {
				if ( Status.GetTypeOfVar ( args[i] ) == null )
					throw new InvalidOperationException ( $"Internal error: Argument {i + 1} for command '{cmd.CmdCode}' could not resolve type of {argType} '{args[i]}'." );
			}
		}

		CmdCall call = new (
			SCLInterpreter.CrOpCode ( Status.GetCommandID ( cmd ) ),
			dst ?? SCLInterpreter.CrDst ( 0 ),
			flagReq,
			args
			);
		if ( EnableLogging ) LogAdd ( $"Command '{cmd.CmdCode}' parsed with {N} argument(s)." );
		Status.PushCommand ( call );
	}



	private static string GetIdentifier ( ref string line ) {
		string originalLine = line;
		// Somewhen it would be better to specify what is a valid identifier
		// We'd like to allow also non-ASCII characters, but not all (e.g. excluding apostrophes, diacritics, etc. at start or end)
		if ( string.IsNullOrEmpty ( line ) ) return null;
		for ( int i = 0; i < line.Length; i++ ) {
			if ( char.IsLetter ( line[i] ) || line[i] == '_' ) continue;
			if ( i > 0 && ( char.IsDigit ( line[i] ) ) ) continue;
			if ( i == 0 ) return null;
			string ret = line[..i];
			line = line[i..].TrimStart ();
			return ret;
		}
		return line;
	}*/
}