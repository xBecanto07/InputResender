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
}