using Components.Library;
using SeClav.Commands;
using TOpCode = SeClav.SId<SeClav.OpCodeTag>;
using TDst = SeClav.SId<SeClav.DstTag>;
using TArg = SeClav.SId<SeClav.ArgTag>;
using TExtraArgs = SeClav.SId<SeClav.ExtraArgsTag>;

namespace SeClav;
internal class SCLParsing {
	//private readonly DModuleLoader ModuleLoader;
	private readonly System.Func<string, DModuleLoader.IModuleInfo> ModuleLoader;
	private readonly SCLParsingStatus Status;
	readonly TOpCode AssignOpCode;
	private List<string> Log;
	public bool EnableLogging = true;
	public readonly IReadOnlyDictionary<string, string> MemoryInfo;

	public SCLParsing ( System.Func<string, DModuleLoader.IModuleInfo> moduleLoader ) {
		ArgumentNullException.ThrowIfNull ( moduleLoader );
		Log = ["SCL parser started"];
		ModuleLoader = moduleLoader;
		Status = new ();
		AssignOpCode = Status.RegisterCustomCmd ( new CmdAssignment () );
		MemoryInfo = Status.GetMemoryInfoRef ();
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
		if ( EnableLogging ) Log.Add ( $"Processing line: {line}" );
		string originalLine = line;
		line = line.Trim ();
		int commentIndex = line.IndexOf ( '#' );
		if ( commentIndex == 0 || commentIndex > 0 && line[commentIndex - 1] == '\\' )
			line = line[..commentIndex].TrimEnd ();

		if ( string.IsNullOrEmpty ( line ) ) return;
		if ( TryParsePrae ( line ) ) return;
		if ( TryParseUsing ( line ) ) return;
		if ( TryParseDataType ( line ) ) return;
		if ( TryParseAssignment ( line ) ) return;
		if ( TryParseCommand ( ref line, null ) ) return;
		throw new InvalidOperationException ( $"Could not parse line: '{originalLine}'." );
	}

	private bool TryParseUsing (string line) {
		string originalLine = line;
		if ( !line.StartsWith ( "@using ", out string moduleName ) ) return false;
		var module = ModuleLoader ( moduleName );
		if ( module == null ) throw new InvalidOperationException ( $"Module '{moduleName}' not found." );
		if ( EnableLogging ) Log.Add ( $"Module '{moduleName}' loaded: {module.Description}, {module.Commands.Count} commands, {module.DataTypes.Count} data types." );
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

		if ( EnableLogging ) Log.Add ( $"Defining variable '{Status.GetVarInfo ( SCLInterpreter.CrDst ( varID ) ).name}' of type '{dataType.Name}'." );

		TryParseAssignment ( SCLInterpreter.CrDst ( varID ), ref line );
		return true;
	}

	private bool TryParseAssignment (  string line ) {
		string originalLine = line;
		string token = GetIdentifier ( ref line );
		if ( string.IsNullOrEmpty ( token ) ) return false;
		TArg sId = Status.GetVarID ( token );
		return TryParseAssignment ( SCLInterpreter.CrDst ( sId ), ref line );
	}

	private bool TryParseAssignment ( TDst varID, ref string line ) {
		string originalLine = line;
		if ( !line.StartsWith ( '=' ) ) return false;
		line = line[1..].TrimStart ();

		// Try to parse constant
		var dataType = Status.GetTypeOfVar ( varID );
		if ( dataType.TryParse ( ref line, out IDataType result ) ) {
			TArg constID = Status.AddConstant ( result );
			Status.PushCommand ( new ( AssignOpCode, varID, new TArg ( varID.Generic ), constID ) );
			if ( EnableLogging ) Log.Add ( $"Assigning constant value to variable '{Status.GetVarInfo ( varID ).name}'." );
			return true;
		}

		string token = GetIdentifier ( ref line );
		if ( string.IsNullOrEmpty ( token ) ) throw new InvalidOperationException ( $"Expected command or value after '=' for variable '{Status.GetVarInfo ( varID ).name}', but got {line}." );

		// Try to assign from existing variable
		try {
			TArg srcVar = Status.GetVarID ( token );
			Status.PushCommand ( new ( AssignOpCode, varID, new TArg ( varID.Generic ), srcVar ) );
			if ( EnableLogging ) Log.Add ( $"Assigning variable '{Status.GetVarInfo ( srcVar ).name}' to variable '{Status.GetVarInfo ( varID ).name}'." );
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
		ICommand cmd = TryParseCommandInner ( ref line, token );
		if ( cmd == null ) return false;

		ParseCommand ( cmd, line, dst );
		return true;
	}
	/// <summary>Tries to parse next token/line as a command. If successful (i.e. token is a valid command) and the command has a return type, the result is either stored in <paramref name="dst"/> (if specified) or a new temporary variable is created to hold the result (and <paramref name="dst"/> is set to that variable).</summary>
	private bool TryParseCommand ( ref string line, out TDst? dst, string token = null ) {
		string originalLine = line;
		dst = null;
		ICommand cmd = TryParseCommandInner ( ref line, token );
		if ( cmd == null ) return false;

		if ( cmd.ReturnType != null ) dst = Status.RegisterResult ( cmd.ReturnType );
		ParseCommand ( cmd, line, dst );
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



	private void ParseCommand ( ICommand cmd, string line, TDst? dst ) {
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
			args
			);
		if ( EnableLogging ) Log.Add ( $"Command '{cmd.CmdCode}' parsed with {N} argument(s)." );
		Status.PushCommand ( call );
	}



	private static string GetIdentifier ( ref string line ) {
		string originalLine = line;
		// Somewhen it would be better to specify what is a valid identifier
		// We'd like to allow also non-ASCII characters, but not all (e.g. excluding apostrophes, diacritics, etc. at start or end)
		if ( string.IsNullOrEmpty ( line ) ) return null;
		for ( int i = 0; i < line.Length; i++ ) {
			if ( char.IsLetter ( line[i] ) || line[i] == '_' ) continue;
			if ( i == 0 ) return null;
			string ret = line[..i];
			line = line[i..].TrimStart ();
			return ret;
		}
		return line;
	}
}