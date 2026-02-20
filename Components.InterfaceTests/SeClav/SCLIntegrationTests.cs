using FluentAssertions;
using SeClav;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using Components.Library;

namespace Components.InterfaceTests.SeClav;
public class SCLIntegrationTests {
	readonly SCL_TestModule testModule;
	readonly SCLParsing parser;
	readonly ITestOutputHelper Output;

	public SCLIntegrationTests ( ITestOutputHelper output ) {
		Output = output;
		testModule = new ();
		parser = new SCLParsing ( name => name == testModule.Name ? testModule : null, logger: msg => Output.WriteLine ( msg ), enableLogging: true );
		parser.ProcessLine ( "@using " + testModule.Name );
	}

	private SCLAssertionRuntime RunScript ( SCLParsing parser ) {
		ISCLDebugInfo result = parser.GetResultWithDebugInfo ();
		var runtime = new SCLAssertionRuntime ( result );
		SCLRunner runner = new ( result.Script, 999 );
		runner.ExecuteSafe ( runtime, [], ref runtime.ProgressInfo );
		Output.WriteLine ( "---- Script Execution Log ----" );
		foreach ( string logLine in runtime.ProgressInfo )
			Output.WriteLine ( logLine );
		return runtime;
	}

	private SCLAssertionRuntime RunScript ( SCLParsing parser, Action<SCLRuntimeHolder> setup, params string[] capturedVars ) {
		// If you want the holder too, not just the runtime, use the 'setup' action to store it somewhere 😉 - Haha, thanks man! What a helpful advice! 😂
		ISCLDebugInfo result = parser.GetResultWithDebugInfo ();
		var runtime = new SCLAssertionRuntime ( result );
		SCLRuntimeHolder holder = new ( runtime, 999 );
		setup?.Invoke ( holder );
		runtime.Debugger = holder.SetupDebugger ();
		foreach ( var (id, name) in result.VarNames ) {
			if ( capturedVars.Contains ( name ) ) {
				runtime.Debugger.CapturedVars.Add ( (id.Generic, name) );
			}
		}
		holder.Execute ( safe: true );
		return runtime;
	}

	/*private SCLAssertionRuntime RunScript ( SCLParsing parser, Action<SCLRuntimeHolder, ISCLDebugInfo> setup ) {
		// If you want the holder too, not just the runtime, use the 'setup' action to store it somewhere 😉 - Haha, thanks man! What a helpful advice! 😂
		ISCLDebugInfo result = parser.GetResultWithDebugInfo ();
		var runtime = new SCLAssertionRuntime ( result );
		SCLRuntimeHolder holder = new ( runtime, 999 );
		setup?.Invoke ( holder, result );
		runtime.Debugger = holder.SetupDebugger ();
		holder.Execute ( safe: true );
		return runtime;
	}*/


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
	public void VariableDirectAssignment ( int val ) {
		parser.ProcessLine ( $"TestInt myVar = {val}" );
		var assertionRuntime = RunScript ( parser );
		var (_, Var) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "myVar" );
		Var.Value.Should ().Be ( val );
	}

	[Theory]
	[InlineData ( 42, 10 )]
	[InlineData ( -1, -5 )]
	[InlineData ( 0, 100 )]
	[InlineData ( (int)1e3, (int)-2.5e4 )]
	public void AddExpressionAssignment ( int val1, int val2 ) {
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
			ctx.RegisterVariable ( "praeVar", () => new TestValueInt ( testModule.IntTypeDef, 123 ) );
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
		finalFlags.Value.Should ().Be ( (1 << 1) | (1 << 2) | (1 << 7) );
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
		parser.ProcessLine ( "SET_FLAG 7" );
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
		var assertionRuntime = RunScript ( parser, h => holder = h, "extID" );
		holder.Should ().NotBeNull ();

		holder.TryGetOutputVar ( "extID", null, out var outputVar ).Should ().BeTrue ();
		outputVar.Should ().BeOfType<TestValueInt> ().Subject.Value.Should ().Be ( 42 );

		outputVar = null;
		holder.TryStoreOutputVar ( "extID", null, ref outputVar ).Should ().BeTrue ();
		outputVar.Should ().NotBeNull ().And.BeOfType<TestValueInt> ().Subject.Value.Should ().Be ( 42 );
	}

	[Fact]
	public void RepeatedRunNoReset () {
		// This test makes sure that multiple runs by default execute the script from the start.
		// This specific one makes sure that the behaviour is consistent between assigning a variable directly vs. just declaring it and leaving it to default value.
		parser.ProcessLine ( "TestInt val" );
		parser.ProcessLine ( "val = ADD_INT val 1" );
		SCLRuntimeHolder holder = null;
		var assertionRuntime = RunScript ( parser, h => holder = h, "val" );
		for ( int i = 0; i < 10; i++ )
			holder.Execute ( true );
		var (_, val) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "val" );
		val.Value.Should ().Be ( 1 );
	}

	[Fact]
	public void RepeatedRunResetAssign () {
		parser.ProcessLine ( "TestInt val = 0" );
		parser.ProcessLine ( "val = ADD_INT val 1" );
		SCLRuntimeHolder holder = null;
		var assertionRuntime = RunScript ( parser, h => holder = h, "val" );
		for ( int i = 0; i < 10; i++ )
			holder.Execute ( true );
		var (_, val) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "val" );
		val.Value.Should ().Be ( 1 );
	}

	[Fact]
	public void RandomFSM () {
		const string S0 = "S0", S1 = "S1", S2 = "S2", Init = "Init", DirectHop = "DirectHop";
		parser.ProcessLine ( "@in TestInt stateID" );
		parser.ProcessLine ( "@extFcn TestInt LOG : TestString" );
		parser.ProcessLine ( "LOG \"Init\"" );
		parser.ProcessLine ( "TestInt cnt = 0" );

		parser.ProcessLine ( "--> S0 -a->S1-b->S0 -c->S2			-goNext-> DirectHop" );
		parser.ProcessLine ( "LOG \"S0\"" );
		parser.ProcessLine ( "COMPARE_INT stateID 1" );
		parser.ProcessLine ( "?= emit a" );
		parser.ProcessLine ( "?> emit c" );

		parser.ProcessLine ( "COMPARE_INT cnt 3" );
		parser.ProcessLine ( "?> emit goNext" );
		parser.ProcessLine ( "COMPARE_INT cnt 1" );
		parser.ProcessLine ( "?> emit b" );
		parser.ProcessLine ( "cnt = ADD_INT cnt 1" );

		parser.ProcessLine ( "--> S1 -c-> S2" );
		parser.ProcessLine ( "LOG \"S1\"" );
		parser.ProcessLine ( "?N emit c" );

		parser.ProcessLine ( "-(DirectHop, S1)-> [S2]" );
		parser.ProcessLine ( "LOG \"S2\"" );

		parser.ProcessLine ( "--> DirectHop --> S2" );

		List<string> log = new ();
		SCLRuntimeHolder holder = null;
		var assertionRuntime = RunScript ( parser, h => {
			holder = h;
			holder.SetExternVar ( "stateID", new TestValueInt ( holder.GetDefinition<TestValueIntDef> (), 0 ), null );
			holder.SetExternFunction<TestValueStringDef, TestValueIntDef> ( "LOG", ( runtime, args ) => {
				string message = (args[0] as TestValueString).Value;
				log.Add ( message );
				return new TestValueInt ( holder.GetDefinition<TestValueIntDef> (), 0 );
			}, null );
		} );
		log.Should ().Equal ( Init, S0 );
		log.Clear ();

		holder.Execute ( true );
		log.Should ().Equal ( S0 );
		log.Clear ();

		holder.Execute ( true );
		log.Should ().Equal ( S0, S0, S0, DirectHop, S2 );
		log.Clear ();


		holder.SetExternVar ( "stateID", new TestValueInt ( holder.GetDefinition<TestValueIntDef> (), 1 ), null );
		holder.Execute ( true );
		log.Should ().Equal ( Init, S0, S1, S2 );
		log.Clear ();

		holder.Execute ( true );
		log.Should ().Equal ( Init, S0, S1, S2 );
		log.Clear ();
	}

	[Theory]
	[InlineData ( 0 )]
	[InlineData ( 1 )]
	[InlineData ( 2 )]
	public void FSM_IfElse ( int i ) {
		parser.ProcessLine ( "@in TestInt i" );
		parser.ProcessLine ( "TestInt res = -1" );
		parser.ProcessLine ( "--> S0 -a-> S1 -b-> S2 -c->S3" );
		parser.ProcessLine ( "COMPARE_INT i 1" );
		parser.ProcessLine ( "?< emit a" );
		parser.ProcessLine ( "?= emit b" );
		parser.ProcessLine ( "?> emit c" );
		parser.ProcessLine ( "res = 7" );

		parser.ProcessLine ( "--> [S1]" );
		parser.ProcessLine ( "res = 9" );
		parser.ProcessLine ( "--> [S2]" );
		parser.ProcessLine ( "res = 11" );
		parser.ProcessLine ( "--> [S3]" );
		parser.ProcessLine ( "res = 13" );

		var assertionRuntime = RunScript ( parser, h => {
			h.SetExternVar ( "i", new TestValueInt ( h.GetDefinition<TestValueIntDef> (), i ), null );
		} );
		var (_, val) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "res" );
		val.Value.Should ().Be ( 7 + 2 * (i + 1) );
	}

	[Fact]
	public void FSM_Parallel () {
		parser.ProcessLine ( "TestInt res = 0" );
		parser.ProcessLine ( "--> S0 --|>> S1 -a-|>> S2 -b-> S3" );
		parser.ProcessLine ( "res = ADD_INT res 1" );
		parser.ProcessLine ( "COMPARE_INT res 1" );
		parser.ProcessLine ( "?= emit a" );
		parser.ProcessLine ( "?= emit b" );
		parser.ProcessLine ( "?> emit b" ); // Reverse order for 2nd pass
		parser.ProcessLine ( "?> emit a" );

		parser.ProcessLine ( "--> S1 -s-> F" );
		parser.ProcessLine ( "res = ADD_INT res 2" );
		parser.ProcessLine ( "--> S2 -s-> F" );
		parser.ProcessLine ( "res = ADD_INT res 3" );
		parser.ProcessLine ( "--> S3 -s-> F" );
		parser.ProcessLine ( "res = ADD_INT res 4" );

		parser.ProcessLine ( "-(S1,S2,S3)-> [F] -n-> S0" );
		parser.ProcessLine ( "res = ADD_INT res res" );
		parser.ProcessLine ( "COMPARE_INT res 7" );
		parser.ProcessLine ( "?< emit n" );

		var assertionRuntime = RunScript ( parser );
		var (_, val) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "res" );
		int control = 1; // State 0
		control += 2 + 3 + 4; // States 1|2|3
		control += control; // State F
		control += 1 + 2; // States 0|1 (1 is auto-fired via ϵ-transition)
		control += 4; // Sequential transition is fired first, S2 is never
		// Final state is never started as S2 was skipped 😉

		val.Value.Should ().Be ( control );
	}

	[Fact]
	public void FSM_Rerun () {
		parser.ProcessLine ( "TestInt step = 0" );
		parser.ProcessLine ( "TestString res = \"A \"" );
		parser.ProcessLine ( "COMPARE_INT step 100" );
		parser.ProcessLine ( "--> S0 -a-> S1 -b-> S2 -c-> S3" );
		parser.ProcessLine ( "res = CONCAT_STR res \"B \"" );
		parser.ProcessLine ( "?< emit c" ); // Should never fire as flags should be reset at the start of the state
		parser.ProcessLine ( "COMPARE_INT step 0" );
		parser.ProcessLine ( "?= emit a" );
		parser.ProcessLine ( "emit b" );

		parser.ProcessLine ( "--> S3" );
		parser.ProcessLine ( "res = CONCAT_STR res \"E \"" );
		parser.ProcessLine ( "throw" );

		parser.ProcessLine ( "--> S1 -a-> S0" );
		parser.ProcessLine ( "res = CONCAT_STR res \"N \"" );
		parser.ProcessLine ( "step = ADD_INT step 1" );
		parser.ProcessLine ( "COMPARE_INT step 2" );
		parser.ProcessLine ( "?> emit a" );

		parser.ProcessLine ( "--> [S2] -a-> S0" );
		parser.ProcessLine ( "res = CONCAT_STR res \"F \"" );
		parser.ProcessLine ( "COMPARE_INT step 300" );
		parser.ProcessLine ( "step = 321" );
		parser.ProcessLine ( "?< emit a" );

		/*
		Do setup (res = 'A')
		S0.0 : +B
		  step==0: goto S1
		S1.0 : +N
		  step==1, <=2: suspend
		RESTART
		S1.1 : +N
		  step==2: <=2: suspend
		RESTART
		S1.2 : +N
		  step==3: >2: goto S0
		S0.1 : +B
		  step≠0: goto S2
		S2.0 : +F
		  step<300: set step:=300 & goto S0
		S0.2 : +B
		  step≠0: goto S2 (same as S0.1)
		S2.1 : +F
		  step>300: ACCEPT
		RESTART
		S0.3 : +B (same as S0.1 and S0.2)
		  step≠0: goto S2 (same as S0.1 and S0.2)
		S2.2 : +F (same as S2.1)
		  step>300: ACCEPT
		*/

		SCLRuntimeHolder holder = null;
		var assertionRuntime = RunScript ( parser, ( h ) => holder = h, "step", "res" );
		var (_, val) = assertionRuntime.VarExists<TestValueStringDef, TestValueString> ( "res" );
		const string ControlStart = "A B N ";
		string control = ControlStart;
		val.Value.Should ().Be ( control );

		holder.Execute ( true ); control += "N ";
		//holder.Execute ( true ); control += "N ";
		holder.Execute ( true ); control += "N B F B F ";
		(_, val) = assertionRuntime.VarExists<TestValueStringDef, TestValueString> ( "res" );
		val.Value.Should ().Be ( control );

		holder.ResetStatus (); // Don't reset the whole environment, just move PC to 0.
		holder.Execute ( true );
		val.Value.Should ().Be ( ControlStart );
	}

	[Fact]
	public void FSM_EpsilonTransitionDirect () {
		parser.ProcessLine ( "TestInt res = 0" );
		parser.ProcessLine ( "--> S0 --> S1" );
		parser.ProcessLine ( "res = ADD_INT res 42" ); // This should be skipped

		parser.ProcessLine ( "--> [S1]" );
		parser.ProcessLine ( "res = ADD_INT res 1" );

		var assertionRuntime = RunScript ( parser );
		var (_, val) = assertionRuntime.VarExists<TestValueIntDef, TestValueInt> ( "res" );
		val.Value.Should ().Be ( 1 );
	}
}