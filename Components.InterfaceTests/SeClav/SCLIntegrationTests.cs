using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using SeClav;
using Xunit.Abstractions;

namespace Components.InterfaceTests.SeClav; 
public class SCLIntegrationTests {
	readonly SCL_TestModule testModule;
	readonly SCLParsing parser;
	readonly ITestOutputHelper Output;

	public SCLIntegrationTests (ITestOutputHelper output) {
		Output = output;
		testModule = new ();
		parser = new SCLParsing ( name => name == testModule.Name ? testModule : null, logger: msg => Output.WriteLine ( msg ), enableLogging: true );
		parser.ProcessLine ( "@using " + testModule.Name );
	}

	private SCLAssertionRuntime RunScript ( SCLParsing parser ) {
		ISCLDebugInfo result = parser.GetResultWithDebugInfo ();
		var runtime = new SCLAssertionRuntime ( result );
		SCLRunner runner = new ( result.Script );
		runner.ExecuteSafe ( runtime, [], ref runtime.ProgressInfo );

		Output.WriteLine ( "---- Script Execution Log ----" );
		foreach ( string logLine in runtime.ProgressInfo )
			Output.WriteLine ( logLine );
		return runtime;
	}

	private SCLAssertionRuntime RunScript ( SCLParsing parser, Action<SCLRuntimeHolder> setup ) {
		// If you want the holder too, not just the runtime, use the 'setup' action to store it somewhere 😉 - Haha, thanks man! What a helpful advice! 😂
		ISCLDebugInfo result = parser.GetResultWithDebugInfo ();
		var runtime = new SCLAssertionRuntime ( result );
		SCLRuntimeHolder holder = new ( runtime );
		setup?.Invoke ( holder );
		holder.Execute ( safe: true );
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

	[Fact]
	public void PraeDirectiveIsCalled () {
		bool wasCalled = false;
		parser.RegisterPraeDirective ( "test_prae", ( ctx, parser ) => { wasCalled = true; } );
		parser.ProcessLine ( "@test_prae" );
		wasCalled.Should ().BeTrue ();

		// Prae-directive shouldn't be called again during script execution
		wasCalled = false;
		RunScript ( parser );
		wasCalled.Should ().BeFalse ();
	}

	[Fact]
	public void PraeRegisterVariableBasic () {
		parser.RegisterPraeDirective ( "register_var", ( ctx, _ ) => {
			ctx.RegisterVariable ("praeVar", () => new TestValueInt ( testModule.IntTypeDef, 123 ) );
		} );
		parser.ProcessLine ( "@register_var" );
		parser.ProcessLine ( "TestString result = APPEND_INT_TO_STR \"Value is: \" praeVar" );
		var assertionRuntime = RunScript ( parser );
		var (_, praeVar) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "praeVar" );
		praeVar.Value.Should ().Be ( 123 );
		var (_, resultVar) = assertionRuntime.VarExists<TestValueStringDef, TestValueString> ( "result" );
		resultVar.Value.Should ().Be ( "Value is: 123" );
	}

	[Fact]
	public void PraeRegisterVariableViaArgs () {
		parser.RegisterPraeDirective ( "register_var", ( ctx, parser ) => {
			string varName = parser.String ( 0, "Variable Name", shouldThrow: true );
			int varValue = parser.Int ( 1, "Variable Value", shouldThrow: true ).Value;
			ctx.RegisterVariable ( varName, () => new TestValueInt ( testModule.IntTypeDef, varValue ) );
		} );
		parser.ProcessLine ( "@register_var dynamicVar 456" );
		parser.ProcessLine ( "TestString result = APPEND_INT_TO_STR \"Value is: \" dynamicVar" );
		var assertionRuntime = RunScript ( parser );
		var (_, dynamicVar) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "dynamicVar" );
		dynamicVar.Value.Should ().Be ( 456 );
		var (_, resultVar) = assertionRuntime.VarExists<TestValueStringDef, TestValueString> ( "result" );
		resultVar.Value.Should ().Be ( "Value is: 456" );
	}

	[Fact]
	public void SetResetReadFlags () {
		parser.ProcessLine ( "TestInt flagVar = READ_FLAGS" );
		parser.ProcessLine ( "SET_FLAG 7" );
		parser.ProcessLine ( "TestInt afterSet = READ_FLAGS" );
		parser.ProcessLine ( "RESET_FLAG 7" );
		parser.ProcessLine ( "TestInt afterReset = READ_FLAGS" );
		parser.ProcessLine ( "SET_FLAG 1" );
		parser.ProcessLine ( "SET_FLAG 2" );
		parser.ProcessLine ( "SET_FLAG 3" );
		parser.ProcessLine ( "SET_FLAG 7" );
		parser.ProcessLine ( "RESET_FLAG 3" );
		parser.ProcessLine ( "RESET_FLAG 9" );
		parser.ProcessLine ( "TestInt finalFlags = READ_FLAGS" );
		var assertionRuntime = RunScript ( parser );
		var (_, flagVar) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "flagVar" );
		var (_, afterSet) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "afterSet" );
		var (_, afterReset) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "afterReset" );
		var (_, finalFlags) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "finalFlags" );
		flagVar.Value.Should ().Be ( 0 );
		afterSet.Value.Should ().Be ( 1 << 7 );
		afterReset.Value.Should ().Be ( 0 );
		finalFlags.Value.Should ().Be ( ( 1 << 1 ) | ( 1 << 2 ) | ( 1 << 7 ) );
	}

	[Fact]
	public void MultiSetReset_RightGuiders () {
		parser.ProcessLine ( "SET_RESET_FLAGS + 1 + 2 + 6" );
		parser.ProcessLine ( "TestInt flagVar1 = READ_FLAGS" );
		parser.ProcessLine ( "SET_RESET_FLAGS - 1 - 6 + 5 + 7" );
		parser.ProcessLine ( "TestInt flagVar2 = READ_FLAGS" );
		parser.ProcessLine ( "SET_RESET_FLAGS - 5 + 4 - 7" ); // Can be unordered
		parser.ProcessLine ( "TestInt flagVar3 = READ_FLAGS" );
		var assertionRuntime = RunScript ( parser );
		var (_, flagVar1) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "flagVar1" );
		var (_, flagVar2) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "flagVar2" );
		var (_, flagVar3) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "flagVar3" );
		flagVar1.Value.Should ().Be ( (1 << 1) | (1 << 2) | (1 << 6) );
		flagVar2.Value.Should ().Be ( (1 << 2) | (1 << 5) | (1 << 7) );
		flagVar3.Value.Should ().Be ( (1 << 2) | (1 << 4) );
	}

	[Fact]
	public void MultiConcat_LeftGuider () {
		parser.ProcessLine ( "JOIN_STRINGS concatResult = Flags: | 1|,2|,6|" );
		var assertionRuntime = RunScript ( parser );
		var (_, concatResult) = assertionRuntime.VarExists<TestValueStringDef, TestValueString> ( "concatResult" );
		concatResult.Value.Should ().Be ( "Flags:1,2,6" );
	}

	[Fact]
	public void UseInvalidGuider_LeftGuider () {
		Action parse = () => parser.ProcessLine ( "Join_strings result = Values: < 1<,2<,3<" );
		parse.Should ().Throw<InvalidOperationException> ();

		parse = () => parser.ProcessLine ( "Join strings result2 = Values: | 1|,2|,6=asdf" );
		parse.Should ().Throw<InvalidOperationException> ();
	}

	[Fact]
	public void UseInvalidGuider_RightGuider () {
		Action parse = () => parser.ProcessLine ( "ADD_OR_APPEND -> result a . s . d . 40 + 2" );
		parse.Should ().Throw<InvalidOperationException> ();
	}

	[Fact]
	public void ConditionalCommand_FlagBased () {
		parser.ProcessLine("SET_FLAG 7");
		parser.ProcessLine ( "TestInt flagVar1 = READ_FLAGS" );
		parser.ProcessLine ( "?5 SET_FLAG 2" );
		parser.ProcessLine ( "TestInt flagVar2 = READ_FLAGS" );
		parser.ProcessLine ( "?7 SET_FLAG 2" );
		parser.ProcessLine ( "TestInt flagVar3 = READ_FLAGS" );
		var assertionRuntime = RunScript ( parser );
		var (_, flagVar1) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "flagVar1" );
		var (_, flagVar2) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "flagVar2" );
		var (_, flagVar3) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "flagVar3" );
		flagVar1.Value.Should ().Be ( 1 << 7 );
		flagVar2.Value.Should ().Be ( 1 << 7 );
		flagVar3.Value.Should ().Be ( (1 << 7) | (1 << 2) );
	}

	[Fact]
	public void ConditionalAssigmentSkipped_FlagBased () {
		parser.ProcessLine ( "SET_FLAG 7" );
		parser.ProcessLine ( "TestInt val = 5" );
		parser.ProcessLine ( "?5 val = 3" );
		var assertionRuntime = RunScript ( parser );
		var (_, val) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "val" );
		val.Value.Should ().Be ( 5 );
	}

	[Fact]
	public void ConditionalAssigmentProcessed_FlagBased () {
		parser.ProcessLine ( "SET_FLAG 7" );
		parser.ProcessLine ( "TestInt val = 5" );
		parser.ProcessLine ( "?7 val = 3" );
		var assertionRuntime = RunScript ( parser );
		var (_, val) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "val" );
		val.Value.Should ().Be ( 3 );
	}

	[Fact]
	public void ConditionalAssigment_CommandBased () {
		parser.ProcessLine ( "TestInt val = 8" );
		parser.ProcessLine ( "COMPARE_INT val 6" ); // val > 6
		parser.ProcessLine ( "?N val = 0" );
		parser.ProcessLine ( "?= val = 1" );
		parser.ProcessLine ( "?> val = 2" );
		parser.ProcessLine ( "?< val = 3" );
		var assertionRuntime = RunScript ( parser );
		var (_, val) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "val" );
		val.Value.Should ().Be ( 2 );
	}

	[Fact]
	public void SettingExternalVariable () {
		parser.ProcessLine ( "@in TestInt extID" );
		parser.ProcessLine ( "TestInt resultID = ADD_INT extID 10" );
		var assertionRuntime = RunScript ( parser, holder => holder.SetExternVar ( "extID", new TestValueInt ( holder.GetDefinition<TestValueIntDef> (), 32 ), null ) );
		var (_, val) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "resultID" );
		val.Value.Should ().Be ( 42 );
	}

	[Fact]
	public void SettingExternalMapper () {
		parser.ProcessLine ( "@mapper TestString inBuffer : TestInt" );
		parser.ProcessLine ( "TestString val1 = inBuffer 42" );
		parser.ProcessLine ( "TestInt id2 = 8" );
		parser.ProcessLine ( "id2 = ADD_INT id2 10" );
		parser.ProcessLine ( "TestString val2 = inBuffer id2" );
		var assertionRuntime = RunScript ( parser, holder => {
			holder.SetExternSampler<TestValueIntDef, TestValueStringDef> ( "inBuffer", ( runtime, val ) => new TestValueString ( holder.GetDefinition<TestValueStringDef> (), $"Value is: {(val as TestValueInt).Value}" ), null );
		} );
		var (_, val1) = assertionRuntime.VarExists<TestValueStringDef, TestValueString> ( "val1" );
		var (_, val2) = assertionRuntime.VarExists<TestValueStringDef, TestValueString> ( "val2" );
		val1.Value.Should ().Be ( "Value is: 42" );
		val2.Value.Should ().Be ( "Value is: 18" );
	}

	[Fact]
	public void SettingExternalFunction () {
		parser.ProcessLine ( "@extFcn TestInt FMADD : TestInt TestInt TestInt" );
		parser.ProcessLine ( "TestInt result = FMADD 5 10 2" );
		var assertionRuntime = RunScript ( parser, holder => {
			holder.SetExternFunction<TestValueIntDef, TestValueIntDef, TestValueIntDef, TestValueIntDef> ( "FMADD", ( runtime, args ) => {
				int a = (args[0] as TestValueInt).Value;
				int b = (args[1] as TestValueInt).Value;
				int c = (args[2] as TestValueInt).Value;
				return new TestValueInt ( args[0].Definition, a * b + c );
			}, null );
		} );
	}

	[Fact]
	public void SettingExternalOutput () {
		parser.ProcessLine ( "@out TestInt extID" );
		parser.ProcessLine ( "extID = ADD_INT 32 10" );
		SCLRuntimeHolder holder = null;
		var assertionRuntime = RunScript ( parser, h => holder = h );
		holder.Should ().NotBeNull ();

		holder.TryGetOutputVar ("extID", null, out var outputVar).Should ().BeTrue ();
		outputVar.Should ().BeOfType<TestValueInt> ().Subject.Value.Should ().Be ( 42 );

		outputVar = null;
		holder.TryStoreOutputVar ("extID", null, ref outputVar).Should ().BeTrue ();
		outputVar.Should ().NotBeNull ().And.BeOfType<TestValueInt> ().Subject.Value.Should ().Be ( 42 );
	}

	[Fact]
	public void RepeatedRun () {
		parser.ProcessLine ( "TestInt val" );
		parser.ProcessLine ( "val = ADD_INT val 1" );
		SCLRuntimeHolder holder = null;
		var assertionRuntime = RunScript ( parser, h => holder = h );
		for ( int i = 0; i < 10; i++ )
			holder.Execute ( true );
		var (_, val) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "val" );
		val.Value.Should ().Be ( 1 );
	}
}