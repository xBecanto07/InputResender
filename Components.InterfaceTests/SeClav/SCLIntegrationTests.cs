using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using SeClav;

namespace Components.InterfaceTests.SeClav; 
public class SCLIntegrationTests {
	readonly SCL_TestModule testModule;
	readonly SCLParsing parser;

	public SCLIntegrationTests () {
		testModule = new ();
		parser = new SCLParsing ( name => name == testModule.Name ? testModule : null );
		parser.ProcessLine ( "@using " + testModule.Name );
	}

	private SCLAssertionRuntime RunScript ( SCLParsing parser ) {
		ISCLDebugInfo result = parser.GetResultWithDebugInfo ();
		var runtime = new SCLAssertionRuntime ( result );
		SCLRunner runner = new ( result.Script );
		runner.ExecuteSafe ( runtime, [], ref runtime.ProgressInfo );
		return runtime;
	}

	[Fact]
	public void VariableDeclaration () {
		parser.ProcessLine ( "TestInt myVar" );
		var assertionRuntime = RunScript ( parser );
		var (_, Var) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "myVar" );
		Var.Should ().Be ( Var.Definition.Default );
	}

	[Theory]
	[InlineData ( 42 )]
	[InlineData ( -1 )]
	[InlineData ( 0 )]
	public void VariableDirectAssignment (int val) {
		parser.ProcessLine ( $"TestInt myVar = {val}" );
		var assertionRuntime = RunScript ( parser );
		var (_, Var) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "myVar" );
		Var.Value.Should ().Be ( val );
	}

	[Theory]
	[InlineData ( 42, 10)]
	[InlineData ( -1, -5)]
	[InlineData ( 0, 100 )]
	[InlineData ( (int)1e3, (int)-2.5e4 )]
	public void AddExpressionAssignment (int val1, int val2) {
		parser.ProcessLine ( $"TestInt myVar = ADD_INT {val1} {val2}" );
		var assertionRuntime = RunScript ( parser );
		var (_, Var) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "myVar" );
		Var.Value.Should ().Be ( val1 + val2 );
	}

	[Fact]
	public void ConcatNestedCommands () {
		const string SUM = "ADD_INT 40 2";
		const string EXP = "APPEND_INT_TO_STR \"40+2=\" " + SUM;
		const string FULL = "CONCAT_STR \"Expected: \" " + EXP;
		parser.ProcessLine ( "TestString myVar = " + FULL );
		var assertionRuntime = RunScript ( parser );
		var (_, Var) = assertionRuntime.VarExists<TestValueStringDef, TestValueString> ( "myVar" );
		Var.Value.Should ().Be ( "Expected: 40+2=42" );
	}
}
