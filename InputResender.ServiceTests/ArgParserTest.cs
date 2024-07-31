using System;
using InputResender.Services;
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;
using System.Collections.Generic;
using Components.Library;

namespace InputResender.ServiceTests;
public class ArgParserTest {
	ITestOutputHelper Output;

	public ArgParserTest ( ITestOutputHelper output ) {
		Output = output;
	}

	private struct Instance {
		public readonly int ID;
		public readonly string Key;
		public readonly string Dsc;
		public readonly string ExpString;
		public readonly double? ExpDouble;
		public readonly int? ExpInt;

		public Instance ( int id, string dsc, string expString ) {
			ID = id;
			Key = null;
			Dsc = dsc;
			ExpString = expString;
			ExpDouble = double.TryParse ( expString, out double dVal ) ? dVal : null;
			ExpInt = int.TryParse ( expString, out int iVal ) ? iVal : null;
		}

		public Instance ( string key, string dsc, string expString ) {
			ID = int.MaxValue;
			Key = key;
			Dsc = dsc;
			ExpString = expString;
			ExpDouble = double.TryParse ( expString, out double dVal ) ? dVal : null;
			ExpInt = int.TryParse ( expString, out int iVal ) ? iVal : null;
		}

		public readonly bool IsRequiredSwitch => Key != null & Dsc != null & ExpString != null;
		public readonly string String ( ArgParser obj ) =>
			(Key == null ? obj.String ( ID, Dsc ) : obj.String ( Key, Dsc )).Should ().Be ( ExpString ).And.Subject;
		public readonly double? Double ( ArgParser obj ) =>
			(Key == null ? obj.Double ( ID, Dsc ) : obj.Double ( Key, Dsc )).Should ().Be ( ExpDouble ).And.Subject;
		public readonly int? Int ( ArgParser obj ) =>
			(Key == null ? obj.Int ( ID, Dsc ) : obj.Int ( Key, Dsc )).Should ().Be ( ExpInt ).And.Subject;
	}
	private void ReadingValidArgs ( string line, params Instance[] args ) {
		System.Text.StringBuilder SB = new ();
		ArgParser parser = new ( line, ( s ) => SB.AppendLine ( s ) );

		for ( int i = 0; i < args.Length; i++ )
			AssertArg ( i );

		void AssertDsc ( string dsc, bool isMissing ) {
			if ( !isMissing ) SB.ToString ().Should ().BeEmpty ();
			else if ( dsc != null ) SB.ToString ().Should ().Contain ( dsc ).And.ContainAny ( "issing", "not found" );
			SB.Clear ();
		}
		void AssertArg ( int ID ) {
			AssertDsc ( args[ID].Dsc, args[ID].String ( parser ) == null );
			AssertDsc ( args[ID].Dsc, args[ID].Double ( parser ) == null );
			AssertDsc ( args[ID].Dsc, args[ID].Int ( parser ) == null );
			if ( args[ID].IsRequiredSwitch ) parser.Present ( args[ID].Key );
		}
	}



	[Theory]
	[InlineData ( null )]
	[InlineData ( "" )]
	public void ReadingOfEmptyLine ( string line ) {
		ReadingValidArgs ( line, new ( 0, "Non-existing argument #1", null ), new ( "nonexisting", "Non-existing argument #2", null ) );
	}

	[Theory]
	[InlineData ( "-p 5 arg1 2 arg2 3" )]
	[InlineData ( "arg1 2 arg2 3 -p 5" )]
	[InlineData ( "arg1 2 -p 5 arg2 3" )]
	[InlineData ( "-o 1 arg1 2 -p 5 arg2 3 argN 9" )]
	[InlineData ( "-o 1 -p 5 arg1 2 arg2 3 argN 9" )]
	[InlineData ( "arg1 2 arg2 3 argN 9 -o 1 -p 5" )]
	public void ThreeArgs ( string line ) {
		ReadingValidArgs ( line, new ( "-p", "Positional argument", "5" ), new ( "arg1", "First argument", "2" ), new ( "arg2", "Second argument", "3" ) );
	}
}