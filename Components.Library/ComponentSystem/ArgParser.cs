using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Cryptography;

namespace Components.Library;
public class ArgParser {
	private class Arg {
		public int Position;
		public string Name;
		public string Value;

		override public string ToString () => $"{Name}#{Position} = {Value}";

		public Arg ( string line, ref int pos, int ID ) {
			Position = ID;
			Name = string.Empty;
			Value = string.Empty;

			bool readingName = true;
			bool isEscaped = false;
			bool isQuoted = false;
			string arg = string.Empty;

			for ( int N = line.Length; pos < N; pos++ ) {
				if ( isEscaped ) {
					switch ( line[pos] ) {
						case 'n': arg += '\n'; break;
						case 'r': arg += '\r'; break;
						case 't': arg += '\t'; break;
						case '0': arg += '\0'; break;
						default: arg += line[pos]; break;
					}
					isEscaped = false;
					continue;
				}
				if ( line[pos] == '\\' ) { isEscaped = true; continue; }
				if ( line[pos] == '"' ) isQuoted = !isQuoted;
				else arg += line[pos];
				if ( isQuoted ) continue;

				if ( pos == N - 1 ) {
					// End of line
					if ( readingName ) Name = arg;
					else Value = arg;
					return;
				}
				if ( char.IsWhiteSpace ( line[pos] ) || char.IsSeparator ( line[pos] ) ) {
					if ( readingName ) Name = arg[..^1];
					else Value = arg[..^1];
					return;
				}
				if ( line[pos] == '=' ) {
					if ( readingName ) {
						Name = arg[..^1];
						readingName = false;
						arg = string.Empty;
					} else {
						throw new ArgumentException ( $"Unexpected '=' at {pos}" );
					}
				}
			}
		}
	}

	private readonly List<Arg> Args;
	private readonly Action<string> Output;
	public enum ErrLvl { None, Minimal, Normal, Full }
	public ErrLvl ErrorLevel;

	public ArgParser ( string args, Action<string> output, ErrLvl errLvl = ErrLvl.Normal ) {
		ErrorLevel = errLvl;
		Args = new ();
		Output = output;

		if ( string.IsNullOrEmpty ( args ) ) return;

		int N = args.Length;
		for ( int i = 0; i < N; i++ ) {
			if ( char.IsWhiteSpace ( args[i] ) ) continue;
			if ( char.IsSeparator ( args[i] ) ) continue;

			int start = i;
			Arg nextArg = new ( args, ref i, Args.Count );
			if ( string.IsNullOrEmpty ( nextArg.Name ) ) {
				i = start;
				nextArg = new ( args, ref i, Args.Count );
				throw new ArgumentException ( $"Unexpected character '{args[i]}' at {i}." );
			}

			if ( nextArg.Name.StartsWith ( '-' ) ) {
				foreach ( var arg in Args ) {
					if ( arg.Name != nextArg.Name ) continue;
					Error ( $"Argument '{nextArg.Name}' is duplicated.", 1, true, 0 );
					return;
				}
			}

			Args.Add ( nextArg );
		}
	}

	/// <summary>Return all arguments as a single line (starting from given index).</summary>
	public string Line ( int start = 0 ) {
		System.Text.StringBuilder SB = new ();

		for ( int i = start; i < ArgC; i++ ) {
			SB.Append ( Args[i].Name );
			string val = Args[i].Value;
			if ( !string.IsNullOrEmpty ( val ) ) {
				SB.Append ( '=' );
				if ( val.Contains ( ' ' ) ) SB.Append ( $"'{val}'" );
				else if ( val.Contains ( '=' ) ) SB.Append ( $"'{val}'" );
				else SB.Append ( val );
				SB.Append ( val );
			}
			SB.Append ( ' ' );
		}
		return SB.ToString ();
	}

	public int ArgC => Args.Count;
	private Arg this[int id] => (id >= ArgC | id < -ArgC) ? null : Args[id];
	private Arg this[string key] {
		get => Args.FirstOrDefault ( arg => arg.Name == key );
	}

	private string Get ( string key, string dsc, bool shouldThrow = false ) {
		var arg = this[key];
		if ( arg == null ) return dsc == null ? string.Empty : Error ( $"Argument '{key}' not found. {dsc}", 2, shouldThrow, string.Empty );

		if ( !GetSepValue ( arg ) ) return dsc == null ? string.Empty : Error ( $"Argument '{key}' has no value. {dsc}", 3, shouldThrow, string.Empty );
		return arg.Value;
	}
	private string Get ( int id, string dsc = null, bool shouldThrow = false ) {
		var arg = this[id];
		if ( arg == null ) return dsc == null ? string.Empty : Error ( $"Argument #{id} not found. {dsc}", 4, shouldThrow, string.Empty );
		return arg.Name;
	}

	private bool GetSepValue ( Arg arg ) {
		if ( !string.IsNullOrEmpty ( arg.Value ) ) return true;
		int next = arg.Position + 1;
		if ( next >= ArgC || !string.IsNullOrEmpty ( Args[next].Value ) || Args[next].Name.StartsWith ( '-' ) ) {
			return false;
		}

		arg.Value = Args[next].Name;
		Args.RemoveAt ( next );
		for ( int i = next; i < ArgC; i++ ) Args[i].Position--;
		return true;
	}

	private T? Parse<T> ( string arg, object id, string dsc, Func<string, T> parser, bool shouldThrow = false ) where T : struct {
		if ( arg == null ) return Error<T?> ( $"Argument '{id}' not found. {dsc}", 5, shouldThrow, null );
		try {
			return parser ( arg );
		} catch ( FormatException ) {
			return Error<T?> ( $"Argument '{id}' is not {typeof ( T ).Name}. {dsc}", 6, shouldThrow, null );
		} catch ( Exception ex ) {
			return Error<T?> ( $"Problem with parsing argument '{id}' ({arg}). {dsc}\n\t{ex.Message}", 7, shouldThrow, null );
		}
	}

	public double? Double ( int id, string dsc, bool shouldThrow = false ) => Parse ( Get ( id, dsc ), id, dsc, double.Parse );
	public double? Double ( string key, string dsc, bool shouldThrow = false ) => Parse ( Get ( key, dsc ), key, dsc, double.Parse );
	public int? Int ( int id, string dsc, bool shouldThrow = false ) => Parse ( Get ( id, dsc ), id, dsc, int.Parse );
	public int? Int ( string key, string dsc, bool shouldThrow = false ) => Parse ( Get ( key, dsc ), key, dsc, int.Parse );
	public string String ( int id, string dsc, int min = 0, bool shouldThrow = false ) {
		string ret = Get ( id, dsc );
		if ( ret != null && ret.Length < min )
			return Error ( $"Argument #{id} is too short. {dsc}", 8, shouldThrow, string.Empty );
		return ret;
	}
	public string String ( string key, string dsc, int min = 0, bool shouldThrow = false ) {
		string ret = Get ( key, dsc );
		if ( ret != null && ret.Length < min )
			return Error ( $"Argument '{key}' is too short. {dsc}", 9, shouldThrow, string.Empty );
		return ret;
	}
	public bool Present ( int id ) => Args.Any ( arg => arg.Position == id );
	public bool Present ( string key ) => Args.Any ( arg => arg.Name == key );

	public bool HasValue ( int id, bool tryLoadValue ) => HasValue ( this[id], tryLoadValue );
	public bool HasValue ( string key, bool tryLoadValue ) => HasValue ( this[key], tryLoadValue );
	private bool HasValue ( Arg arg, bool tryLoadValue ) {
		if ( arg == null ) return false;
		if ( !string.IsNullOrEmpty ( arg.Value ) ) return true;
		if ( !tryLoadValue ) return false;
		return GetSepValue ( arg );
	}

	public string Log () {
		System.Text.StringBuilder SB = new ();
		foreach ( var arg in Args )
			SB.AppendLine ( $"Arg #{arg.Position}: '{arg.Name}' = '{arg.Value}'" );
		return SB.ToString ();
	}
	public T EnumC<T> ( int id, string dsc, bool shouldThrow = false ) where T : struct {
		if ( !Enum.TryParse ( String ( id, dsc ), true, out T ret ) )
			return Error ( $"Argument #{id} is not a valid {typeof ( T ).Name}. {dsc}", 10, shouldThrow, default ( T ) );
		if ( !Enum.IsDefined ( typeof ( T ), ret ) )
			return Error ( $"Argument #{id} was not found in {typeof ( T ).Name}. {dsc}", 11, shouldThrow, default ( T ) );
		return ret;
	}

	private T Error<T> ( string message, int errID, bool shouldThrow, T defVal ) {
		if ( shouldThrow ) throw new ArgumentException ( message );
		if ( ErrorLevel != ErrLvl.None ) {
			if ( ErrorLevel == ErrLvl.Minimal ) Output ( $"ArgParseErr#{errID}" );
			else if ( ErrorLevel == ErrLvl.Normal ) Output ( message );
			else if ( ErrorLevel == ErrLvl.Full ) Output ( $"{message} (ArgParseErr#{errID})\n{Log ().PrefixAllLines ( " - " )}" );
			else Output ( message );
		}
		return defVal;
	}
}