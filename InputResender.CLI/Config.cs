using System;
using System.Xml;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace InputResender.CLI; 
public static class Config {
	private static bool SkipAutoSave = false;
	private static List<string> AdditionalPaths = [];
	private static string homePath = AppDomain.CurrentDomain.BaseDirectory;
	private static string savePath = Path.Combine ( HomePath, "config.xml" );
	private readonly static Dictionary<string, string[]> autoCommands = new () {
		{"initCmds", new string[] { "loadall", "clear", "safemode on", "core new", "core own", "conns force init", "loglevel all", "network callback recv print", "network callback newconn print", "hook manager start" } }
	};
	private static string autostartName = "initCmds";
	private static bool printAutoCommands = true;
	private static int maxOnelinerLength = 125;
	private static PrintFormat responsePrintFormat = PrintFormat.Normal;

	public enum PrintFormat { None, Batch, ErrOnly, Normal, Full }

	public static HashSet<string> ValidAutostartNames { get => autoCommands.Keys.ToHashSet (); }
	public static string AutostartName { get => autostartName; set { autostartName = value; Save (); } }
	public static string HomePath {
		get => homePath; set {
			if ( string.IsNullOrEmpty ( value ) )
				throw new ArgumentException ( "HomePath cannot be empty." );
			if ( File.Exists ( value ) ) value = Path.GetDirectoryName ( value );
			if ( !Directory.Exists ( value ) )
				throw new ArgumentException ( "HomePath must be an existing directory or a valid file path." );
			homePath = value;
			savePath = Path.Combine ( HomePath, "config.xml" );
			Save ();
		}
	}
	public static string SavePath {
		get => savePath; set {
			if ( string.IsNullOrEmpty ( value ) )
				throw new ArgumentException ( "SavePath cannot be empty." );
			if ( Directory.Exists ( value ) )
				value = Path.Combine ( value, "config.xml" );
			else if ( !File.Exists ( value ) ) {
				string dir = Path.GetDirectoryName ( value );
				if ( !Directory.Exists ( dir ) )
					throw new ArgumentException ( "SavePath must be an existing file path or a valid directory." );
			} else if ( Path.GetExtension ( value ) != ".xml" )
				throw new ArgumentException ( "SavePath must have .xml extension." );
			savePath = value;
			Save ();
		}
	}
	public static bool PrintAutoCommands { get => printAutoCommands; set { printAutoCommands = value; Save (); } }
	public static int MaxOnelinerLength { get => maxOnelinerLength; set { maxOnelinerLength = value; Save (); } }
	public static PrintFormat ResponsePrintFormat { get => responsePrintFormat; set { responsePrintFormat = value; Save (); } }

	public static IReadOnlyCollection<string> FetchAutoCommands ( string key ) {
		if ( string.IsNullOrEmpty ( key ) ) return Array.Empty<string> ();
		if ( !autoCommands.TryGetValue ( key, out string[] ret ) )
			return Array.Empty<string> ();
		string[] ret2 = new string[ret.Length];
		ret.CopyTo ( ret2, 0 );
		return ret2;
	}
	public static void AddAutoCommand ( string key, IEnumerable<string> commands ) =>
		autoCommands[key] = commands.ToArray ();


	public static string LoadFileContent(string relPath) {
		if (string.IsNullOrEmpty(relPath)) return null;
		string fullPath = Path.IsPathRooted(relPath) ? relPath : Path.Combine(HomePath, relPath);
		if (!File.Exists(fullPath)) return null;
		return File.ReadAllText(fullPath);
	}


	public static void Save () {
		if ( SkipAutoSave ) return;
		if ( string.IsNullOrEmpty ( SavePath ) ) return;

		XmlDocument doc = new ();
		XmlElement root = doc.CreateElement ( "Config" );
		doc.AppendChild ( root );
		root.AppendChild ( doc.CreateElement ( "HomePath" ) ).InnerText = homePath;
		root.AppendChild ( doc.CreateElement ( "AutostartName" ) ).InnerText = autostartName;
		root.AppendChild ( doc.CreateElement ( "PrintAutoCommands" ) ).InnerText = PrintAutoCommands ? "T" : "F";
		root.AppendChild ( doc.CreateElement ( "MaxOnelinerLength" ) ).InnerText = MaxOnelinerLength.ToString ();
		root.AppendChild ( doc.CreateElement ( "ResponsePrintFormat" ) ).InnerText = ResponsePrintFormat.ToString ();
		
		XmlElement autoCmds = doc.CreateElement ( "AutoCommands" );
		root.AppendChild ( autoCmds );
		foreach ( var pair in autoCommands ) {
			XmlElement key = doc.CreateElement ( pair.Key );
			autoCmds.AppendChild ( key );
			for ( int i = 0; i < pair.Value.Length; i++ ) {
				XmlElement cmd = doc.CreateElement ( $"C{i}" );
				cmd.InnerText = pair.Value[i];
				key.AppendChild ( cmd );
			}
		}

		doc.Save ( SavePath );
	}

	public static bool Load ( string path = null ) {
		bool oldSkipAutoSave = SkipAutoSave;
		SkipAutoSave = true;
		if ( string.IsNullOrEmpty ( path ) ) path = Path.Combine ( HomePath, "config.xml" );

		HomePath = path;

		if (!File.Exists ( SavePath ) ) {
			// Create default config
			//Save (); Moving this to higher level
			SkipAutoSave = oldSkipAutoSave;
			return true;
		} else {
			XmlDocument doc = new ();
			doc.Load ( SavePath );
			XmlElement root = doc.DocumentElement;
			if ( root.Name != "Config" ) throw new ArgumentException ( "Invalid root element." );
			foreach ( XmlNode node in root.ChildNodes ) {
				switch ( node.Name ) {
				case "HomePath":
					// If HomePath is different than current (but has valid config file) switch to loading that config (recursively). Otherwise store this path as alternative path (will be used when searching for extra files)
					if ( node.InnerText == HomePath || node.InnerText == SavePath )
						break;
					HomePath = node.InnerText;
					if ( Load ( HomePath ) ) {
						SkipAutoSave = oldSkipAutoSave;
						return true;
					}
					AdditionalPaths.Add ( HomePath );
					break;
				case "AutostartName": autostartName = node.InnerText; break;
				case "PrintAutoCommands": PrintAutoCommands = node.InnerText == "T"; break;
				case "MaxOnelinerLength": if ( int.TryParse ( node.InnerText, out int maxOnelinerLength ) ) MaxOnelinerLength = maxOnelinerLength; break;
				case "ResponsePrintFormat": if ( Enum.TryParse ( node.InnerText, true, out PrintFormat format ) ) ResponsePrintFormat = format; break;
				case "AutoCommands":
					autoCommands.Clear ();
					foreach ( XmlNode key in node.ChildNodes ) {
						if ( key.NodeType != XmlNodeType.Element ) continue;
						List<string> cmds = new ();
						foreach ( XmlNode cmd in key.ChildNodes ) {
							if ( cmd.NodeType != XmlNodeType.Element ) continue;
							cmds.Add ( cmd.InnerText );
						}
						autoCommands[key.Name] = cmds.ToArray ();
					}
					break;
				}
			}
			SkipAutoSave = oldSkipAutoSave;
			return true;
		}
	}
}