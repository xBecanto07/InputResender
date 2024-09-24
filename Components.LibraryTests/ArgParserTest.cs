﻿using System;
using System.Collections.Generic;
using System.Linq;
using Components.Library;
using FluentAssertions;
using Xunit;

namespace Components.LibraryTests; 
public class ArgParserTest {
	[Fact]
	public void BasicArgs () {
		var parser = new ArgParser ( "A1 A2 A3", null );
		parser.String ( 0, "Argument 1" ).Should ().Be ( "A1" );
		parser.String ( 1, "Argument 2" ).Should ().Be ( "A2" );
		parser.String ( 2, "Argument 3" ).Should ().Be ( "A3" );
		Action extraArg = () => parser.String ( 3, "Extra argument" );
		AssertError ( extraArg, "Argument #3 not found", ArgParser.ErrArgNotFoundByID );

		parser.String ( 0, null ).Should ().Be ( "A1" );
		parser.String ( 1, null ).Should ().Be ( "A2" );
		parser.String ( 2, null ).Should ().Be ( "A3" );
		parser.String ( 3, null ).Should ().NotBeNull ().And.BeEmpty ();
	}

	[Fact]
	public void NamedArgs () {
		var parser = new ArgParser ( "X=1 L=2 A1=3", null );
		parser.Int (0, "X").Should ().Be ( 1 );
		parser.Int (1, "L").Should ().Be ( 2 );
		parser.Int (2, "A1").Should ().Be ( 3 );
		Action extraArg = () => parser.Int (3, "A2");
		AssertError ( extraArg, "Argument #3 not found", ArgParser.ErrArgNotFoundByID );

		parser.Int ( "X", "X" ).Should ().Be ( 1 );
		parser.Int ( "L", "L" ).Should ().Be ( 2 );
		parser.Int ( "A1", "A1" ).Should ().Be ( 3 );
		extraArg = () => parser.Int ( "A2", "A2" );
		AssertError ( extraArg, "Argument 'A2' not found", ArgParser.ErrArgNotFoundByName );
	}

	[Fact]
	public void NamedArgsReversedOrderByID () {
		var parser = new ArgParser ( "X=1 L=2 A1=3", null );
		Action extraArg = () => parser.Int ( 3, "A2" );
		AssertError ( extraArg, "Argument #3 not found", ArgParser.ErrArgNotFoundByID );
		parser.Int ( 2, "A1" ).Should ().Be ( 3 );
		parser.Int ( 1, "L" ).Should ().Be ( 2 );
		parser.Int ( 0, "X" ).Should ().Be ( 1 );
	}

	[Fact]
	public void NamedArgsReversedOrderByName () {
		var parser = new ArgParser ( "X=1 L=2 A1=3", null );
		Action extraArg = () => parser.Int ( "A2", "A2" );
		AssertError ( extraArg, "Argument 'A2' not found", ArgParser.ErrArgNotFoundByName );
		parser.Int ( "A1", "A1" ).Should ().Be ( 3 );
		parser.Int ( "L", "L" ).Should ().Be ( 2 );
		parser.Int ( "X", "X" ).Should ().Be ( 1 );
	}

	[Fact]
	public void SwitchesRecognizedAndLoaded () {
		var parser = new ArgParser ( "-a --all -a --something -b -f=5", null );
		parser.RegisterSwitch ( 'a', "all", "0" );
		parser.RegisterSwitch ( 'b', "bsdf", "0" );
		parser.RegisterSwitch ( 'f', "fdsa", "0" );
		parser.RegisterSwitch ( 's', "something", "0" );

		parser.Present ( "-a" ).Should ().BeTrue ();
		parser.Present ( "--all" ).Should ().BeTrue ();
		parser.Present ( "--something" ).Should ().BeTrue ();
		parser.Present ( "-b" ).Should ().BeTrue ();
		parser.Present ( "-f" ).Should ().BeTrue ();

		parser.HasValue ( "-a", true ).Should ().BeTrue ();
		parser.HasValue ( "--all", true ).Should ().BeTrue ();
		parser.HasValue ( "--something", true ).Should ().BeTrue ();
		parser.HasValue ( "-b", true ).Should ().BeTrue ();
		parser.HasValue ( "-f", true ).Should ().BeTrue ();

		parser.Int ( "-a", "all" ).Should ().Be ( 0 );
		parser.Int ( "--all", "all" ).Should ().Be ( 0 );
		parser.Int ( "--something", "something" ).Should ().Be ( 0 );
		parser.Int ( "-b", "bsdf" ).Should ().Be ( 0 );
		parser.Int ( "-f", "f" ).Should ().Be ( 5 );
	}

	[Fact]
	public void UndefinedSwitchHasDefaultValue () {
		var parser = new ArgParser ( "-a 3", null );
		parser.RegisterSwitch ( 'b', "beta", "5" );
		parser.Present ( "-b" ).Should ().BeTrue ();
		parser.HasValue ( "-b", true ).Should ().BeTrue ();
		parser.Int ( "-b", "beta" ).Should ().Be ( 5 );
	}

	[Fact]
	public void OptionalSwitchStayesUndefined () {
		var parser = new ArgParser ( "-a 5", null );
		parser.RegisterSwitch ( 'b', "beta", null );
		parser.Present ( "-b" ).Should ().BeFalse ();
		AssertError ( () => parser.Int ( "-b", "beta" ), "Switch -b not found.", ArgParser.ErrSwitchCharNotFound );
	}

	[Fact]
	public void SwitchesMultipleValuesExplicit () {
		var parser = new ArgParser ( "-a -a=1 -a=2 -a=3", null );
		parser.RegisterSwitch ( 'a', "asdf", "0" );
		parser.Present ( "-a" ).Should ().BeTrue ();
		parser.HasValue ( "-a", true ).Should ().BeTrue ();
		parser.Int ( "-a", "asdf" ).Should ().Be ( 3 );
	}

	[Fact]
	public void SwitchWithSingleValueExplicit () {
		var parser = new ArgParser ("-a -a -a=4 -a -a", null);
		parser.RegisterSwitch ( 'a', "asdf", "0" );
		parser.Present ( "-a" ).Should ().BeTrue ();
		parser.HasValue ( "-a", true ).Should ().BeTrue ();
		parser.Int ( "-a", "asdf" ).Should ().Be ( 4 );
	}

	[Fact]
	public void SwitchesMultipleValuesReversedOrderExplicit () {
		var parser = new ArgParser ( "-a=3 -a=2 -a=1 -a", null );
		parser.RegisterSwitch ( 'a', "a", "0" );
		parser.Present ( "-a" ).Should ().BeTrue ();
		parser.HasValue ( "-a", true ).Should ().BeTrue ();
		parser.Int ( "-a", "asdf" ).Should ().Be ( 1 );
	}

	[Fact]
	public void SwitchesMultipleValuesImplicit () {
		var parser = new ArgParser ( "-a -a 1 -a 2 -a 3", null );
		parser.RegisterSwitch ( 'a', "asdf", "0" );
		parser.Present ( "-a" ).Should ().BeTrue ();
		parser.HasValue ( "-a", true ).Should ().BeTrue ();
		parser.Int ( "-a", "asdf" ).Should ().Be ( 3 );
	}

	[Fact]
	public void SwitchWithSingleValueImplicit () {
		var parser = new ArgParser ( "-a -a -a 4 -a -a", null );
		parser.RegisterSwitch ( 'a', "asdf", "0" );
		parser.Present ( "-a" ).Should ().BeTrue ();
		parser.HasValue ( "-a", true ).Should ().BeTrue ();
		parser.Int ( "-a", "asdf" ).Should ().Be ( 4 );
	}

	[Fact]
	public void SwitchesMultipleValuesReversedOrderImplicit () {
		var parser = new ArgParser ( "-a 3 -a 2 -a 1 -a", null );
		parser.RegisterSwitch ( 'a', "a", "0" );
		parser.Present ( "-a" ).Should ().BeTrue ();
		parser.HasValue ( "-a", true ).Should ().BeTrue ();
		parser.Int ( "-a", "asdf" ).Should ().Be ( 1 );
	}

	[Fact]
	public void ValueCanBeEnteredWithoutEquals () {
		var parser = new ArgParser ( "-a 1 -b=2", null );
		parser.RegisterSwitch ( 'a', "a", "0" );
		parser.RegisterSwitch ( 'b', "b", "0" );
		parser.Present ( "-a" ).Should ().BeTrue ();
		parser.Present ( "-b" ).Should ().BeTrue ();
		parser.Int ( "-a", "asdf" ).Should ().Be ( 1 );
		parser.Int ( "-b", "bvcx" ).Should ().Be ( 2 );
	}

	[Fact]
	public void DefaultValueIsKeptWithEqualSign () {
		var parser = new ArgParser ( "-a= -b=5", null );
		parser.RegisterSwitch ( 'a', "asdf", "2" );
		parser.RegisterSwitch ( 'b', "bvcx", "3" );
		parser.Int ( "-a", "aasdf" ).Should ().Be ( 2 );
		parser.Int ( "-b", "bvcx" ).Should ().Be ( 5 );
	}

	[Fact]
	public void SwitchAndArgumentCanHaveNegativeValue () {
		var parser = new ArgParser ( "-a=-1 b -3 -c -5 d=-7", null );
		parser.RegisterSwitch ( 'a', "a", "0" );
		parser.RegisterSwitch ( 'b', "b", "0" );
		parser.RegisterSwitch ( 'c', "cvbn", "0" );
		parser.RegisterSwitch ( 'd', "d", "0" );
		parser.Int ( "-a", "sdfa" ).Should ().Be ( -1 );
		parser.Int ( "b", "b" ).Should ().Be ( -3 );
		parser.Int ( "-c", "cvbn" ).Should ().Be ( -5 );
		parser.Int ( "d", "d" ).Should ().Be ( -7 );
	}



	protected void AssertError ( Action action, string msg, int errCode ) =>
		action.Should ().Throw<ArgumentException> ().Which.Message.Should ()
		.StartWith ( msg ).And.Contain ( $"ArgParseErr#{errCode}" );
}