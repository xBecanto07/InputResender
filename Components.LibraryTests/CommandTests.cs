﻿using Components.Library;
using Components.Library.ComponentSystem;
using FluentAssertions;
using InputResender.Commands;
using System.Collections.Generic;
using Xunit;

namespace Components.LibraryTests; 
public class BasicCommandTest : CommandTestBase {
	BasicCommandsWrapper CmdWrapper;

	public BasicCommandTest () : this ( new BasicCommandsWrapper () ) { }
	private BasicCommandTest (BasicCommandsWrapper cmdWrapper ) : base (cmdWrapper.command) { CmdWrapper = cmdWrapper; }

	[Fact]
	public void PrintCommand () {
		AssertCorrectMsg ( "print SimpleMsg", "SimpleMsg" );
		CmdWrapper.prints.Should ().ContainSingle ().Which.Should ().Be ( "SimpleMsg" );
		AssertCorrectMsg ( "print \"Quoted message\"", "Quoted message" );
		CmdWrapper.prints.Should ().HaveCount ( 2 ).And.Contain ( "Quoted message" );
		CmdWrapper.prints.Clear ();
	}

	[Fact]
	public void ClearCommand () {
		AssertCorrectMsg ( "clear", "clear" );
		CmdWrapper.clears.Should ().Be ( 1 );
		CmdWrapper.clears = 0;
	}

	[Fact]
	public void ExitCommand () {
		AssertCorrectMsg ( "exit", "exit" );
		CmdWrapper.exits.Should ().Be ( 1 );
		CmdWrapper.exits = 0;
	}

	internal class BasicCommandsWrapper {
		public readonly List<string> prints = new ();
		public int clears = 0;
		public int exits = 0;
		public readonly BasicCommands command;

		public BasicCommandsWrapper () {
			command = new BasicCommands ( prints.Add, () => clears++, () => exits++ );
		}
	}
}

public class ContextVarCommandTest : CommandTestBase {
	public ContextVarCommandTest () : base ( new ContextVarCommands () ) { }

	[Fact]
	public void GetNonexistingVar () {
		AssertCorrectMsg ( "context get string NonexistingVar", "Error getting variable 'NonexistingVar': Variable 'NonexistingVar' not found." );
	}

	[Fact]
	public void SetGetVar () {
		AssertCorrectMsg ( "context set string TestVar TestValue", "Variable 'TestVar' set." );
		AssertCorrectMsg ( "context get string TestVar", "Variable 'TestVar' value: TestValue" );
	}

	[Fact]
	public void AddVar () {
		AssertCorrectMsg ( "context set string TestVar TestValue", "Variable 'TestVar' set." );
		AssertCorrectMsg ( "context add string TestVar AddedValue", "Variable 'TestVar' updated." );
		AssertCorrectMsg ( "context get string TestVar", "Variable 'TestVar' value: TestValueAddedValue" );
	}

	[Fact]
	public void ResetVar () {
		AssertCorrectMsg ( "context set string TestVar TestValue", "Variable 'TestVar' set." );
		AssertCorrectMsg ( "context reset string TestVar", "Variable 'TestVar' reset." );
		AssertCorrectMsg ( "context get string TestVar", "Variable 'TestVar' value: " );
	}
}

public class CoreManagerCommandTest : CommandTestBase {
	public CoreManagerCommandTest () : base ( new CoreManagerCommand () ) { }

	[Fact]
	public void ActiveCore () {
		AssertMissingCore ( "core act" );
		CoreBaseMock mockCore = new ();
		CmdProc.SetVar ( CoreManagerCommand.ActiveCoreVarName, mockCore );
		AssertCorrectMsg ( "core act TestCore", $"Active core: '{mockCore.Name}'" );
	}

	[Fact]
	public void TypeofCoreCompoent () {
		AssertMissingCore ( "core typeof " + nameof ( ComponentMock ) );
		CoreBaseMock mockCore = new ();
		CmdProc.SetVar ( CoreManagerCommand.ActiveCoreVarName, mockCore );
		AssertCorrectMsg("core typeof asdf", "Invalid type name: asdf" );
		AssertCorrectMsg("core typeof " + nameof ( ComponentMock ), $"Component of type {nameof ( ComponentMock )} not found." );
		// TODO: Bug where fetch seems to not work properly in this scenario. It works fine when using proper core (e.g.: with VMainAppCore, calling 'core typeof DInputReader' returns: 'For definition 'DInputReader' was found variant 'VInputReader_KeyboardHook'.').
		//new ComponentMock ( mockCore );
		//AssertCorrectMsg("core typeof " + nameof ( ComponentBase ), $"For definition '{nameof ( ComponentBase )}' was found variant '{nameof ( ComponentMock )}'." );
	}
}