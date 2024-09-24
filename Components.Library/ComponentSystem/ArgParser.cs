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
		/// <summary>Marks that the value was explicitly specified (e.g. "A=1":true vs "A 1":false).</summary>
		public bool ValSpecified;

		override public string ToString () => $"{Name}#{Position} = {Value}";

		public Arg ( int pos, string name, string value, bool valSpec ) {
			Position = pos;
			Name = name;
			Value = value;
			ValSpecified = valSpec;
		}

		public Arg ( string line, ref int pos, int ID ) {
			Position = ID;
			Name = string.Empty;
			Value = string.Empty;
			ValSpecified = false;

			bool readingName = true;
			bool isEscaped = false;
			bool isQuoted = false;
			string arg = string.Empty;

			for ( int N = line.Length; pos < N; pos++ ) {
				if ( isEscaped ) {
					arg += line[pos] switch {
						'n' => '\n',
						'r' => '\r',
						't' => '\t',
						'0' => '\0',
						_ => (object)line[pos],
					};
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
						ValSpecified = true;
						arg = string.Empty;
					} else {
						throw new ArgumentException ( $"Unexpected '=' at {pos}" );
					}
				}
			}
		}
	}

	private readonly Dictionary<string, Arg> SwitchesName;
	private readonly Dictionary<char, Arg> SwitchesChar;
	/// <summary>Positional args only. Position-independent switches should be extracted via <see cref="ArgParser.RegisterSwitch(char, string, string)"/> before usage (they'll be stored in SwitchesName and SwitchesChar).</summary>
	private readonly List<Arg> Args;
	private readonly Action<string> Output;
	public enum ErrLvl { None, Minimal, Normal, Full }
	public ErrLvl ErrorLevel;

	public const int ErrArgDup = 1;
	public ArgParser ( string args, Action<string> output, ErrLvl errLvl = ErrLvl.Normal ) {
		ErrorLevel = errLvl;
		Args = new ();
		SwitchesChar = new ();
		SwitchesName = new ();
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

			/*if ( nextArg.Name.StartsWith ( '-' ) ) {
				foreach ( var arg in Args ) {
					if ( arg.Name != nextArg.Name ) continue;
					arg.Value = nextArg.Value;
					//Error ( $"Argument '{nextArg.Name}' is duplicated.", ErrArgDup, true, 0 );
					//return;
				}
			}*/

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
		get {
			if ( key.StartsWith ( "--" ) ) return SwitchesName.ContainsKey ( key ) ? SwitchesName[key] : null;
			if ( key.StartsWith ( "-" ) && key.Length == 2 ) return SwitchesChar.ContainsKey ( key[1] ) ? SwitchesChar[key[1]] : null;
			return Args.FirstOrDefault ( arg => arg.Name == key );
		}
	}

	public const int ErrArgNotFoundByName = 2;
	public const int ErrArgNoValue = 3;
	public const int ErrEmptyKey = 12;
	public const int ErrSwitchNameNotFound = 13;
	public const int ErrSwitchCharNotFound = 14;
	private string Get ( string key, string dsc, bool shouldThrow = false ) {
		Arg arg = null;
		if ( string.IsNullOrEmpty ( key ) ) return Error ( "Key cannot be empty", ErrEmptyKey, shouldThrow, string.Empty );
		if ( key.StartsWith ( "--" ) ) {
			if ( !SwitchesName.ContainsKey ( key ) )
				return Error ( $"Switch {key} not found.", ErrSwitchNameNotFound, shouldThrow, string.Empty );
			arg = SwitchesName[key];
		} else if ( key.StartsWith ( "-" ) && key.Length == 2 ) {
			if ( !SwitchesChar.ContainsKey ( key[1] ))
				return Error ($"Switch {key} not found.", ErrSwitchCharNotFound, shouldThrow, string.Empty );
			arg = SwitchesChar[key[1]];
		} else arg = this[key];

		if ( arg == null ) return dsc == null ? string.Empty : Error ( $"Argument '{key}' not found. {dsc}", ErrArgNotFoundByName, shouldThrow, string.Empty );

		if ( !GetSepValue ( arg ) ) return dsc == null ? string.Empty : Error ( $"Argument '{key}' has no value. {dsc}", ErrArgNoValue, shouldThrow, string.Empty );
		return arg.Value;
	}
	public const int ErrArgNotFoundByID = 4;
	private string Get ( int id, string dsc = null, bool shouldThrow = false ) {
		var arg = this[id];
		if ( arg == null ) return dsc == null ? string.Empty : Error ( $"Argument #{id} not found. {dsc}", ErrArgNotFoundByID, shouldThrow, string.Empty );
		return string.IsNullOrEmpty ( arg.Value ) ? arg.Name : arg.Value;
	}

	private bool GetSepValue ( Arg arg ) {
		if ( !string.IsNullOrEmpty ( arg.Value ) ) return true;
		int next = arg.Position + 1;
		if ( next >= ArgC || !string.IsNullOrEmpty ( Args[next].Value ) ) {
			return false;
		}

		string s = Args[next].Name;
		if ( string.IsNullOrEmpty ( s ) ) return false;
		if ( s.StartsWith ( "--" ) ) return false;
		if ( s.Length == 2 && s[0] == '-' && char.IsAsciiLetter ( s[1] ) ) return false;

		arg.Value = s;
		Args.RemoveAt ( next );
		for ( int i = next; i < ArgC; i++ ) Args[i].Position--;
		return true;
	}

	public const int ErrArgParseNotFound = 5;
	public const int ErrArgParseBadType = 6;
	public const int ErrArgParseProblem = 7;
	private T? Parse<T> ( string arg, object id, string dsc, Func<string, T> parser, bool shouldThrow = false ) where T : struct {
		if ( arg == null ) return Error<T?> ( $"Argument '{id}' not found. {dsc}", ErrArgParseNotFound, shouldThrow, null );
		try {
			return parser ( arg );
		} catch ( FormatException ) {
			return Error<T?> ( $"Argument '{id}' is not {typeof ( T ).Name}. {dsc}", ErrArgParseBadType, shouldThrow, null );
		} catch ( Exception ex ) {
			return Error<T?> ( $"Problem with parsing argument '{id}' ({arg}). {dsc}\n\t{ex.Message}", ErrArgParseProblem, shouldThrow, null );
		}
	}

	public double? Double ( int id, string dsc, bool shouldThrow = false ) => Parse ( Get ( id, dsc ), id, dsc, double.Parse );
	public double? Double ( string key, string dsc, bool shouldThrow = false ) => Parse ( Get ( key, dsc ), key, dsc, double.Parse );
	public int? Int ( int id, string dsc, bool shouldThrow = false ) => Parse ( Get ( id, dsc ), id, dsc, int.Parse );
	public int? Int ( string key, string dsc, bool shouldThrow = false ) => Parse ( Get ( key, dsc ), key, dsc, int.Parse );

	public const int ErrStringTooShortByID = 8;
	public string String ( int id, string dsc, int min = 0, bool shouldThrow = false ) {
		string ret = Get ( id, dsc );
		if ( ret != null && ret.Length < min )
			return Error ( $"Argument #{id}({ret}) is too short. {dsc}", ErrStringTooShortByID, shouldThrow, string.Empty );
		return ret;
	}
	public const int ErrStringTooShortByName = 9;
	public string String ( string key, string dsc, int min = 0, bool shouldThrow = false ) {
		string ret = Get ( key, dsc );
		if ( ret != null && ret.Length < min )
			return Error ( $"Argument '{key}'({ret}) is too short. {dsc}", ErrStringTooShortByName, shouldThrow, string.Empty );
		return ret;
	}
	public bool Present ( int id ) => this[id] != null;
	public bool Present ( string key ) => this[key] != null;

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

	public void RegisterSwitch ( char sw, string name, string defVal = null ) {
		if ( string.IsNullOrEmpty ( name ) ) throw new ArgumentNullException ( nameof ( name ) );
		if ( !char.IsAsciiLetter ( sw ) ) throw new ArgumentException ( $"Switch '{sw}' is not a valid character.", nameof ( sw ) );

		string swStr = $"-{sw}";
		name = $"--{name}";
		Arg arg = null;
		for ( int i = ArgC - 1; i >= 0; i-- ) {
			if ( Args[i].Name != swStr && Args[i].Name != name ) continue;
			Arg extracted = ExtractSwitch ( i, defVal );
			if ( extracted == null ) continue;

			if ( arg == null ) arg = extracted;
			else if ( extracted.Value == null ) continue;
			else if ( extracted.ValSpecified ) {
				if ( string.IsNullOrEmpty ( arg.Value ) ) {
					arg.Value = string.IsNullOrEmpty ( extracted.Value ) ? defVal : extracted.Value;
					arg.ValSpecified = true;
				} else continue;
			} else if ( arg.Value == null ) arg.Value = extracted.Value;
			else if ( arg.Value != defVal ) continue;
			else arg.Value = extracted.Value;
		}

		if (arg != null) {
			if ( arg.Value == null ) arg.Value = defVal ?? string.Empty;
			SwitchesChar[sw] = arg;
			SwitchesName[name] = arg;
		} else if (defVal != null) {
			arg = new ( -1, name, defVal, true );
			SwitchesChar[sw] = arg;
			SwitchesName[name] = arg;
		}
	}
	private Arg ExtractSwitch ( int id, string defVal = null ) {
		Arg ret = Args[id];

		if ( defVal != null ) {
			if (string.IsNullOrEmpty(ret.Value)) {
				if ( ret.ValSpecified ) ret.Value = defVal;
				else {
					if ( !GetSepValue ( ret ) ) ret.Value = null;
					else if (string.IsNullOrEmpty(ret.Value)) ret.Value = defVal;
					// ret.Value has changed due to GetSepValue()
				}
			}

			/*if ( ret.ValSpecified ) {
				if ( string.IsNullOrEmpty ( ret.Value ) ) ret.Value = defVal;
			} else if ( !ret.ValSpecified && string.IsNullOrEmpty ( ret.Value ) ) {
				// Value not assigned, try to get value from next argument or assign default

				if ( !GetSepValue ( ret ) ) ret.Value = null;
				else if ( ret.Value == defVal ) ret.Value = null;
				else if ( string.IsNullOrEmpty ( ret.Value ) ) ret.Value = defVal;
			}*/
		}

		Args.RemoveAt ( id );
		for ( int i = ArgC - 1; i >= id; i-- ) Args[i].Position--;
		return ret;
	}

	public const int ErrEnumInvalid = 10;
	public const int ErrEnumUndefined = 11;
	public T EnumC<T> ( int id, string dsc, bool shouldThrow = false ) where T : struct {
		string arg = Get ( id, dsc );
		if ( !Enum.TryParse ( arg, true, out T ret ) )
			return Error ( $"Argument #{id}({arg}) is not a valid {typeof ( T ).Name}. {dsc}", ErrEnumInvalid, shouldThrow, default ( T ) );
		if ( !Enum.IsDefined ( typeof ( T ), ret ) )
			return Error ( $"Argument #{id}({arg}) was not found in {typeof ( T ).Name}. {dsc}", ErrEnumUndefined, shouldThrow, default ( T ) );
		return ret;
	}

	private T Error<T> ( string message, int errID, bool shouldThrow, T defVal ) {
		if ( shouldThrow | (Output == null) ) throw new ArgumentException ( $"{message} (ArgParseErr#{errID})" );
		if ( ErrorLevel != ErrLvl.None ) {
			if ( ErrorLevel == ErrLvl.Minimal ) Output ( $"ArgParseErr#{errID}" );
			else if ( ErrorLevel == ErrLvl.Normal ) Output ( message );
			else if ( ErrorLevel == ErrLvl.Full ) Output ( $"{message} (ArgParseErr#{errID})\n{Log ().PrefixAllLines ( " - " )}" );
			else Output ( message );
		}
		return defVal;
	}
}