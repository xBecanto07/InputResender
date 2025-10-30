using Components.Library;
using FluentAssertions.Equivalency;
using SeClav;
using SeClav.DataTypes;
using System;
using System.Collections.Generic;

namespace Components.InterfaceTests.SeClav;
internal class TestValueIntDef : DataTypeDefinition {
	public override string Name => "TestInt";
	public override string Description => "Test integer data type.";
	public override IReadOnlySet<ICommand> Commands => null; // 'dot' operator not implemented yet			e
	public override bool TryParse ( ref string line, out IDataType result ) {
		for ( int i = line.Length; i > 0; i-- ) {
			if ( int.TryParse ( line[..i], out int v ) ) {
				result = new TestValueInt ( this, v );
				line = line[i..].TrimStart ();
				return true;
			}
		}
		result = null!;
		return false;
	}
	public override IDataType Default => new TestValueInt ( this, 0 );
}

internal class TestValueStringDef : DataTypeDefinition {
	public override string Name => "TestString";
	public override string Description => "Test string data type.";
	public override IReadOnlySet<ICommand> Commands => null; // 'dot' operator not implemented yet
	public override bool TryParse ( ref string line, out IDataType result ) {
		result = null!;
		if ( line.StartsWith ( '"' ) ) {
			while (true) {
				int endQuote = line.IndexOf ( '"', 1 );
				if ( endQuote < 0 ) break;
				if ( line[endQuote - 1] != '\\' ) {
					result = new TestValueString ( this, line[1..endQuote].Replace ( "\\\"", "\"" ) );
					line = line[(endQuote + 1)..].TrimStart ();
					return true;
				}
				line = line[(endQuote + 1)..];
			}
		}
		return false;
	}
	public override IDataType Default => new TestValueString ( this, "" );
}

internal class TestValueInt : IDataType {
	public DataTypeDefinition Definition { get; }
	public int Value;
	public TestValueInt ( DataTypeDefinition definition, int value ) {
		Definition = definition;
		Value = value;
	}
	public void Assign ( IDataType value ) {
		if ( value is not TestValueInt v )
			throw new InvalidOperationException ( $"Cannot assign value of type '{value.Definition.Name}' to '{Definition.Name}'." );
		Value = v.Value;
	}
	public override string ToString () => Value.ToString ();
	public override bool Equals ( object? obj ) {
		return obj is TestValueInt v && Value == v.Value;
	}
	public override int GetHashCode () => Value.GetHashCode ();
}

internal class TestValueString : IDataType {
	public DataTypeDefinition Definition { get; }
	public string Value;
	public TestValueString ( DataTypeDefinition definition, string value ) {
		Definition = definition;
		Value = value;
	}
	public void Assign ( IDataType value ) {
		if ( value is not TestValueString v )
			throw new InvalidOperationException ( $"Cannot assign value of type '{value.Definition.Name}' to '{Definition.Name}'." );
		Value = v.Value;
	}
	public override string ToString () => Value.ToString ();
	public override bool Equals ( object? obj ) {
		return obj is TestValueString v && Value == v.Value;
	}
	public override int GetHashCode () => Value.GetHashCode ();
}



internal class AssertEqual : ICommand {
	public string CmdCode => "ASSERT_EQ";
	public string CommonName => "Assert Equal";
	public string Description => "Asserts that two values have same type and value. Throws an exception if they are not equal.";
	public int ArgC => 2;
	public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [
		("var1", new SCLT_Any (), "First integer variable to compare"),
		("var2", new SCLT_Same (), "Second integer variable to compare")
	];
	public DataTypeDefinition ReturnType => new SCLT_Same ();
	public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
		var var1 = runtime.GetVar ( args[0] );
		var var2 = runtime.GetVar ( args[1] );
		return var1.Equals ( var2 ) ? var1 : throw new InvalidOperationException ( "Assert Equal failed: Variable values are not the same." );
	}
	public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
		SIdVal var1ID = args[0];
		SIdVal var2ID = args[1];
		IDataType var1 = runtime.SafeGetVar ( args[1] );
		IDataType var2 = runtime.SafeGetVar ( args[2] );
		progress.Add ( $" . {var1} == {var2} ? ({var1.Equals ( var2 )})" );
		if ( var1.Definition.Name != var2.Definition.Name )
			throw new InvalidOperationException ( $"Assert Equal failed: Variable types are not the same: '{var1.Definition.Name}' != '{var2.Definition.Name}'." );
		if ( !var1.Equals ( var2 ) )
			throw new InvalidOperationException ( $"Assert Equal failed: Variable values are not the same: '{var1}' != '{var2}'." );
		return var1;
	}
}

internal class AppendIntToString : ICommand {
	public string CmdCode => "APPEND_INT_TO_STR";
	public string CommonName => "Append Int to String";
	public string Description => "Appends an integer value to a string value.";
	public int ArgC => 2;
	public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [
		("str", new TestValueStringDef (), "String to append to"),
		("i", new TestValueIntDef (), "Integer to append")
	];
	public DataTypeDefinition ReturnType => new TestValueStringDef ();
	public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
		var str = runtime.GetVar ( args[0] ) as TestValueString;
		var i = runtime.GetVar ( args[1] ) as TestValueInt;
		return new TestValueString ( str.Definition, str.Value + i.Value.ToString () );
	}
	public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
		SIdVal strID = args[0];
		SIdVal iID = args[1];
		IDataType str = runtime.SafeGetVar ( strID );
		IDataType i = runtime.SafeGetVar ( iID );
		if ( str is not TestValueString vs )
			throw new InvalidOperationException ( $"Expected string for argument 'str', got '{str.Definition.Name}'." );
		if ( i is not TestValueInt vi )
			throw new InvalidOperationException ( $"Expected integer for argument 'i', got '{i.Definition.Name}'." );
		progress.Add ( $" . \"{str}\" + {i} -> {vs.Value + vi.Value.ToString ()}" );
		return new TestValueString ( str.Definition, vs.Value + vi.Value.ToString () );
	}
}

internal class AddInts : ICommand {
	public string CmdCode => "ADD_INT";
	public string CommonName => "Add Integers";
	public string Description => "Adds two integer values.";
	public int ArgC => 2;
	public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [
		("a", new TestValueIntDef (), "First integer to add"),
		("b", new TestValueIntDef (), "Second integer to add")
	];
	public DataTypeDefinition ReturnType => new TestValueIntDef ();
	public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
		var a = runtime.GetVar ( args[0] ) as TestValueInt;
		var b = runtime.GetVar ( args[1] ) as TestValueInt;
		return new TestValueInt ( a.Definition, a.Value + b.Value );
	}
	public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
		SIdVal aID = args[0];
		SIdVal bID = args[1];
		IDataType a = runtime.SafeGetVar ( aID );
		IDataType b = runtime.SafeGetVar ( bID );
		if ( a is not TestValueInt va )
			throw new InvalidOperationException ( $"Expected integer for argument 'a', got '{a.Definition.Name}'." );
		if ( b is not TestValueInt vb )
			throw new InvalidOperationException ( $"Expected integer for argument 'b', got '{b.Definition.Name}'." );
		progress.Add ( $" . {va} + {vb} -> {va.Value + vb.Value}" );
		return new TestValueInt ( va.Definition, va.Value + vb.Value );
	}
}

internal class ConcatStrs : ICommand {
	public string CmdCode => "CONCAT_STR";
	public string CommonName => "Concatenate Strings";
	public string Description => "Concatenates two string values.";
	public int ArgC => 2;
	public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [
		("str1", new TestValueStringDef (), "First string to concatenate"),
		("str2", new TestValueStringDef (), "Second string to concatenate")
	];
	public DataTypeDefinition ReturnType => new TestValueStringDef ();
	public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
		var str1 = runtime.GetVar ( args[0] ) as TestValueString;
		var str2 = runtime.GetVar ( args[1] ) as TestValueString;
		return new TestValueString ( str1.Definition, str1.Value + str2.Value );
	}
	public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
		SIdVal str1ID = args[0];
		SIdVal str2ID = args[1];
		IDataType str1 = runtime.SafeGetVar ( str1ID );
		IDataType str2 = runtime.SafeGetVar ( str2ID );
		if ( str1 is not TestValueString v1 )
			throw new InvalidOperationException ( $"Expected string for argument 'str1', got '{str1.Definition.Name}'." );
		if ( str2 is not TestValueString v2 )
			throw new InvalidOperationException ( $"Expected string for argument 'str2', got '{str2.Definition.Name}'." );
		progress.Add ( $" . \"{str1}\" + \"{str2}\" -> \"{v1.Value + v2.Value}\"" );
		return new TestValueString ( str1.Definition, v1.Value + v2.Value );
	}
}

internal class SCL_TestModule : DModuleLoader.IModuleInfo {
	public string Name => "TestModule";
	public string Description => "Module for testing purposes";

	public DataTypeDefinition IntTypeDef => IntDef;
	public DataTypeDefinition StrTypeDef => StrDef;

	private readonly DataTypeDefinition IntDef = new TestValueIntDef ();
	private readonly DataTypeDefinition StrDef = new TestValueStringDef ();

	public IReadOnlySet<ICommand> Commands => new HashSet<ICommand> () {
		new AssertEqual (), new AddInts (),
		new ConcatStrs (), new AppendIntToString (),
	};

	public IReadOnlySet<DataTypeDefinition> DataTypes => new HashSet<DataTypeDefinition> () {
		IntDef, StrDef,
	};

	// Prae-directives are provided by tests themselves as needed
	public IReadOnlyDictionary<string, Action<SCLParsingContext, ArgParser>> PraeDirectives => GetPraeDirectives;
	internal Dictionary<string, Action<SCLParsingContext, ArgParser>> GetPraeDirectives = [];
}