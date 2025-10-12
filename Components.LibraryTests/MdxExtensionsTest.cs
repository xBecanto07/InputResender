using Components.Library;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Components.LibraryTests;

public class MdxExtensionsTest {
	const string A = "a", B = "b", C = "c", D = "d", E = "e";
	[Fact]
	public void OrganizeEmpty () {
		const int Cols = 3;
		var blocks = MdxExtensions.SplitToColumns ( null, Cols, 1 );
		blocks.Should ().BeNull ();

		blocks = MdxExtensions.SplitToColumns ( [], Cols, 1 );
		blocks.Should ().HaveCount ( Cols )
			.And.OnlyContain ( b => b.Count == 0 );
	}

	public static IEnumerable<object[]> OrganizeIntoSingleColumnsData () {
		for ( int i = 0; i < 8; i++ ) {
			List<List<string>> input = (i % 4) switch {
				0 => [[A], [B], [C]],
				1 => [[A, B], [C]],
				2 => [[A], [B, C]],
				3 => [[A, B, C]],
				_ => throw new ArgumentOutOfRangeException ( nameof ( i ), "Variant must be in range 0-3." ),
			};
			List<string> output = [];
			foreach ( var col in input ) {
				output.AddRange ( col );
				if ( i / 4 > 0 ) output.Add ( string.Empty );
			}
			output.RemoveAt ( output.Count - 1 ); // Remove last separator
			yield return new object[] { input, i < 4 ? null : string.Empty, i / 4, output };
		}
	}

	[Theory]
	[MemberData ( nameof ( OrganizeIntoSingleColumnsData ) )]
	public void OrganizeIntoSingleColumn ( List<List<string>> input, string sep, int sepN, List<string> output ) {
		var blocks = MdxExtensions.SplitToColumns ( input, 1, sepN );
		var res = blocks.BuildColumBlocks ( " | ", sep );
		res.Should ().ContainInOrder ( output );
	}


	public static IEnumerable<object[]> OrganizeIntoDoubleColumnsData () {
		string x = string.Empty;
		for ( int i = 1; i <= 4; i++ ) {
			List<List<string>> input = i switch {
				1 => [[A], [B], [C]],
				2 => [[A, B], [C]],
				3 => [[A], [B, C]],
				4 => [[A, B, C]],
				5 => [[A, B], [C, D]],
				_ => throw new ArgumentOutOfRangeException ( nameof ( i ), "Variant must be in range 1-4." ),
			};
			List<string> output = i switch {
				1 => ["a | c", "  |  ", "b |"],
				2 => ["a | c", "b |  "],
				3 => [A, x, B, C],
				4 => [A, B, C],
				5 => ["a | c", "b | d"],
				_ => throw new ArgumentOutOfRangeException ( nameof ( i ), "Variant must be in range 1-4." ),
			};
			yield return new object[] { input, output };
		}
	}

	[Theory]
	[MemberData ( nameof ( OrganizeIntoDoubleColumnsData ) )]
	public void OrganizeIntoDoubleDoubleColumn ( List<List<string>> input, List<string> output ) {
		var blocks = MdxExtensions.SplitToColumns ( input, 2, 1 );
		var res = blocks.BuildColumBlocks ( " | ", "\n" );
		for (int i = 0; i < res.Count; i++ ) res[i] = res[i].TrimEnd ();
		for (int i = 0; i < output.Count; i++ ) output[i] = output[i].TrimEnd ();
		res.Should ().ContainInOrder ( output );
	}
}