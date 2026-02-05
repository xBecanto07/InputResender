using System.Collections.Generic;
using Components.Library;
using System.Linq;

namespace SeClav;
internal class SCLRuntime : ISCLRuntime {
	public ISCLParsedScript Script;
	public IReadOnlyList<IDataType> Variables;
	public IReadOnlyList<IDataType> Results;
	private ISCLRuntime.SCLFlags flags;

	private const int VarIdMask = SCLInterpreter.VarIdMask;
	private const int VarIdShift = SCLInterpreter.VarIdShift;


	public SCLRuntime ( ISCLParsedScript script ) {
		Script = script ?? throw new ArgumentNullException ( nameof ( script ) );
		Variables = script.VariableTypes.Select ( t => {
			if ( t < 0 || t >= script.DataTypes.Count )
				throw new IndexOutOfRangeException ( $"Variable type index {t} is out of range <0|{script.DataTypes.Count}>." );
			return script.DataTypes[t].Default;
		} ).ToList ().AsReadOnly ();
		Results = script.ResultTypes.Select ( t => {
			if ( t < 0 || t >= script.DataTypes.Count )
				throw new IndexOutOfRangeException ( $"Result type index {t} is out of range <0|{script.DataTypes.Count}>." );
			return script.DataTypes[t].Default;
		} ).ToList ().AsReadOnly ();

		foreach ( var (id, getter) in script.Getters) {
			if ( id < 0 || id >= Variables.Count )
				throw new IndexOutOfRangeException ( $"Getter ID {id} is out of range <0|{Variables.Count}>." );
			var val = getter ();
			if (val.Definition != Variables[id].Definition )
				throw new InvalidOperationException ( $"Getter for variable ID {id} returned type {val.Definition.Name}({val.Definition.Owner}), expected {Variables[id].Definition.Name}({Variables[id].Definition.Owner})." );
			Variables[id].Assign ( val );
		}
	}

	public IDataType SafeGetVar ( SIdVal varID ) {
		ushort varType = varID.ValueType;
		ushort ID = varID.ValueId;
		// FIXME: Never use 0 as a valid ID! Debugging this is a nightmare. Treat 0 as invalid ID.
		switch ( varType ) {
		case SCLInterpreter.VarTypeID:
			if ( ID >= Variables.Count )
				throw new IndexOutOfRangeException ( $"Variable ID {ID} is out of range <0|{Variables.Count}>." );
			return Variables[ID];
		case SCLInterpreter.ConstTypeID:
			if ( ID >= Script.Constants.Count )
				throw new IndexOutOfRangeException ( $"Constant ID {ID} is out of range <0|{Script.Constants.Count}>." );
			return Script.Constants[ID];
		case SCLInterpreter.ResultTypeID:
			if ( ID >= Results.Count )
				throw new IndexOutOfRangeException ( $"Result ID {ID} is out of range <0|{Results.Count}>." );
			return Results[ID];
		case SCLInterpreter.InvalidTypeID:
			throw new InvalidOperationException ( $"Index type '{SCLInterpreter.InvalidTypeID}' is reserved for future use and cannot be used yet." );
		default:
			throw new ArgumentOutOfRangeException ( nameof ( varID ), $"Invalid variable ID {varID}. The type {varType} is outside of supported range!" );
		}
	}

	public void SafeSetVar ( SIdVal varID, IDataType value ) {
		switch ( varID.ValueType ) {
		case SCLInterpreter.VarTypeID:
			if ( varID.ValueId < 0 )
				throw new AccessViolationException ( $"Detected attemt to write into protected memory at {varID.ValueId}" );
			if ( varID.ValueId >= Variables.Count )
				throw new IndexOutOfRangeException ( $"Variable ID {varID} is out of range." );
			if ( !value.Definition.Equals ( Variables[varID.ValueId].Definition ) )
				throw new InvalidOperationException ( $"Cannot set variable {varID} to type {value.Definition.Name}, expected {Variables[varID.ValueId].Definition.Name}." );
			Variables[varID.ValueId].Assign ( value );
			break;
		case SCLInterpreter.ConstTypeID:
			throw new AccessViolationException ( $"Detected attemt to write into protected memory at constant ID {varID.ValueId}" );
		case SCLInterpreter.ResultTypeID:
			if ( varID.ValueId >= Results.Count )
				throw new IndexOutOfRangeException ( $"Result ID {varID} is out of range." );
			if ( value.Definition != Results[varID.ValueId].Definition )
				throw new InvalidOperationException ( $"Cannot set result {varID} to type {value.Definition.Name}, expected {Results[varID.ValueId].Definition.Name}." );
			Results[varID.ValueId].Assign ( value );
			break;
		case SCLInterpreter.InvalidTypeID:
			throw new InvalidOperationException ( "Index type '0' is reserved for future use and cannot be used yet." );
		default: throw new ArgumentOutOfRangeException ( nameof ( varID ), $"Invalid variable ID {varID}." );
		}
	}
	//public IDataType GetVar ( int varID ) => varID < 0 ? Script.Constants[IDtoConstPtr ( varID )] : Variables[varID];
	public IDataType GetVar ( SIdVal varID ) => (varID.ValueType) switch {
		SCLInterpreter.VarTypeID => Variables[varID.ValueId],
		SCLInterpreter.ConstTypeID => Script.Constants[varID.ValueId],
		SCLInterpreter.ResultTypeID => Results[varID.ValueId],
		SCLInterpreter.InvalidTypeID => throw new InvalidOperationException ( "Index type '0' is reserved for future use and cannot be used yet." ),
		_ => throw new ArgumentOutOfRangeException ( nameof ( varID ), $"Invalid variable ID {varID}." )
	};
	//public void SetVar ( int varID, IDataType value ) => Variables[varID].Assign ( value );
	public void SetVar ( SIdVal varID, IDataType value ) {
		switch ( varID.ValueType ) {
		case SCLInterpreter.VarTypeID: Variables[varID.ValueId].Assign ( value ); break;
		case SCLInterpreter.ResultTypeID: Results[varID.ValueId].Assign ( value ); break;
		default: throw new ArgumentOutOfRangeException ( nameof ( varID ), $"Invalid variable ID {varID}." );
		}
	}

	public void UpdateMemoryInfo ( ref Dictionary<string, string> memInfo ) {
		memInfo["Variables"] = $"{Variables.Count} variables: {string.Join ( " | ", Variables.Select ( ( v, i ) => $"V{i}={v}" ) )}";
		memInfo["Results"] = $"{Results.Count} results : {string.Join ( " | ", Results.Select ( ( r, i ) => $"R{i}={r}" ) )}";
		memInfo["Constants"] = $"{Script.Constants.Count} constants : {string.Join ( " | ", Script.Constants.Select ( ( c, i ) => $"C{i}={c}" ) )}";
	}

	public ISCLRuntime.SCLFlags GetFlags () => flags;
	public void SetFlag ( ISCLRuntime.SCLFlags value ) => flags |= value;
	public void ResetFlag ( ISCLRuntime.SCLFlags value ) => flags &= ~value;
}





public class SCLRuntimeHolder {
	internal SCLRuntime BaseRuntime;
	private readonly HashSet<string> UnassignedExternals = new ();
	private List<string> Errors = [], Log = [];

	public IReadOnlyList<string> RuntimeErrors => Errors.AsReadOnly ();
	public IReadOnlyList<string> RuntimeLog => Log.AsReadOnly ();

	internal SCLRuntimeHolder (SCLRuntime runtime ) {
		if ( runtime == null )
			throw new ArgumentNullException ( nameof ( runtime ) );
		BaseRuntime = runtime;

		List<string> allExternals = runtime.Script.InputVars.Keys
			.Union ( runtime.Script.InputSamplers.Keys )
			.Union ( runtime.Script.ExternFunctions.Keys )
			.ToList ();
		foreach ( var varName in allExternals )
			if ( !UnassignedExternals.Add ( varName ) )
				throw new InvalidOperationException ( $"Duplicate external identifier '{varName}' found in the script." );
	}

	public SCLRuntimeHolder ( SCLScriptHolder scriptHolder ) : this ( new SCLRuntime ( scriptHolder?.ParsedScript ) ) { }

	public IDataType SafeGetVar ( SIdVal varID ) => BaseRuntime.SafeGetVar ( varID );
	public void SafeSetVar ( SIdVal varID, IDataType value ) => BaseRuntime.SafeSetVar ( varID, value );
	public T GetDefinition<T> () where T : DataTypeDefinition
		=> BaseRuntime.Script.DataTypes.FirstOrDefault ( x => x.GetType () == typeof ( T ) ) as T;

	public IDataType Execute ( bool safe ) {
		if ( UnassignedExternals.Count > 0 )
			throw new InvalidOperationException ( $"Cannot execute script: Unassigned external variables: {string.Join ( ", ", UnassignedExternals )}." );
		SCLRunner runner = new ( BaseRuntime.Script );
		return safe ? runner.ExecuteSafe ( BaseRuntime, [], ref Log ) :
			runner.Execute ( BaseRuntime, [] );
	}

	public void ClearErrors () => Errors.Clear ();
	public void ClearLog () => Log.Clear ();

	public void SetExternVar ( string name, IDataType val, CoreBase owner ) {
		if ( !BaseRuntime.Script.InputVars.TryGetValue ( name, out SIdVal varID ) ) {
			// Don't throw here, just log the error and continue
			// Caller can thus always try to set all externals and let the script to choose if it's used or not
			Errors.Add ( $"Input variable '{name}' not found in the script." );
			return;
		}
		try {
			BaseRuntime.SafeSetVar ( varID, val );
			UnassignedExternals.Remove ( name );
		} catch ( Exception ex ) {
			owner?.PushDelayedError ( $"Failed to set external variable '{name}': {ex.Message}", ex );
		}
	}

	public bool TryGetOutputVar (string name, CoreBase owner, out IDataType value) {
		value = null;
		if ( !BaseRuntime.Script.OutputVars.TryGetValue ( name, out SIdVal varID ) ) {
			Errors.Add ( $"Output variable '{name}' not found in the script." );
			return false;
		}
		try {
			value = BaseRuntime.SafeGetVar ( varID );
			return true;
		} catch ( Exception ex ) {
			owner?.PushDelayedError ( $"Failed to get output variable '{name}': {ex.Message}", ex );
			return false;
		}
	}

	public bool TryStoreOutputVar ( string name, CoreBase owner, ref IDataType value ) {
		if ( !BaseRuntime.Script.OutputVars.TryGetValue ( name, out SIdVal varID ) ) {
			Errors.Add ( $"Output variable '{name}' not found in the script." );
			return false;
		}
		try {
			value = BaseRuntime.SafeGetVar ( varID );
			return true;
		} catch ( Exception ex ) {
			owner?.PushDelayedError ( $"Failed to get output variable '{name}': {ex.Message}", ex );
			return false;
		}
	}

	public void SetExternSampler<InT, OutT> ( string name, Func<ISCLRuntime, IDataType, IDataType> sampler, CoreBase owner ) where InT : DataTypeDefinition where OutT : DataTypeDefinition {
		if ( !BaseRuntime.Script.InputSamplers.TryGetValue ( name, out int cmdIndex ) )
			throw new KeyNotFoundException ( $"Input sampler '{name}' not found in the script." );
		try {
			var cmd = BaseRuntime.Script.Commands[cmdIndex];
			if ( cmd is not ExternMapper externCmd )
				throw new InvalidOperationException ( $"Command at index {cmdIndex} for sampler '{name}' is not an extern sampler command." );
			externCmd.Set ( name, GetDefinition<OutT> (), GetDefinition<InT> (), sampler );
			UnassignedExternals.Remove ( name );
		} catch ( Exception ex ) {
			owner?.PushDelayedError ( $"Failed to set external sampler '{name}': {ex.Message}", ex );
		}
	}

	public void SetExternFunction<In1T, OutT> ( string name, Func<ISCLRuntime, IDataType[], IDataType> function, CoreBase owner )
		where In1T : DataTypeDefinition where OutT : DataTypeDefinition
		=> SetExternFcn ( name, [GetDefinition<In1T> ()], GetDefinition<OutT> (), function, owner );

	public void SetExternFunction<In1T, In2T, OutT> ( string name, Func<ISCLRuntime, IDataType[], IDataType> function, CoreBase owner )
		where In1T : DataTypeDefinition where In2T : DataTypeDefinition where OutT : DataTypeDefinition
		=> SetExternFcn ( name, [GetDefinition<In1T> (), GetDefinition<In2T> ()], GetDefinition<OutT> (), function, owner );

	public void SetExternFunction<In1T, In2T, In3T, OutT> ( string name, Func<ISCLRuntime, IDataType[], IDataType> function, CoreBase owner )
		where In1T : DataTypeDefinition where In2T : DataTypeDefinition where In3T : DataTypeDefinition where OutT : DataTypeDefinition
		=> SetExternFcn ( name, [GetDefinition<In1T> (), GetDefinition<In2T> (), GetDefinition<In3T> ()], GetDefinition<OutT> (), function, owner );

	public void SetExternFunction<In1T, In2T, In3T, In4T, OutT> ( string name, Func<ISCLRuntime, IDataType[], IDataType> function, CoreBase owner )
		where In1T : DataTypeDefinition where In2T : DataTypeDefinition where In3T : DataTypeDefinition where In4T : DataTypeDefinition where OutT : DataTypeDefinition
		=> SetExternFcn ( name, [GetDefinition<In1T> (), GetDefinition<In2T> (), GetDefinition<In3T> (), GetDefinition<In4T> ()], GetDefinition<OutT> (), function, owner );

	// More arguments isn't currently supported! That would require either expanding the CmdCall structure or supporting ExtraArguments in SCLRunner.

	private void SetExternFcn ( string name, DataTypeDefinition[] inArgs, DataTypeDefinition outArg, Func<ISCLRuntime, IDataType[], IDataType> function, CoreBase owner ) {
		if ( !BaseRuntime.Script.ExternFunctions.TryGetValue ( name, out int cmdIndex ) )
			throw new KeyNotFoundException ( $"Extern function '{name}' not found in the script." );
		try {
			var cmd = BaseRuntime.Script.Commands[cmdIndex];
			if ( cmd is not ExternFunction externCmd )
				throw new InvalidOperationException ( $"Command at index {cmdIndex} for function '{name}' is not an extern function command." );
			externCmd.Set ( name, outArg, inArgs, function );
			UnassignedExternals.Remove ( name );
		} catch ( Exception ex ) {
			owner?.PushDelayedError ( $"Failed to set external function '{name}': {ex.Message}", ex );
		}
	}
}