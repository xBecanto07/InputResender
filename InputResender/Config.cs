using System;
using System.Xml;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace InputResender; 
internal static class Config {
	private static string homePath = AppDomain.CurrentDomain.BaseDirectory;
	private readonly static Dictionary<string, string[]> autoCommands = new ();
	private static string autostartName;

	public static string AutostartName { get => autostartName; set { autostartName = value; Save (); } }
	public static string HomePath { get => homePath; set { homePath = value; Save (); } }
	public static IReadOnlyCollection<string> FetchAutoCommands ( string key ) {
		if ( string.IsNullOrEmpty ( key ) ) return Array.Empty<string> ();
		if ( !autoCommands.TryGetValue ( key, out string[] ret ) ) return Array.Empty<string> ();
		string[] ret2 = new string[ret.Length];
		ret.CopyTo ( ret2, 0 );
		return ret2;
	}
	public static void AddAutoCommand ( string key, IEnumerable<string> commands ) =>
		autoCommands[key] = commands.ToArray ();


	public static void Save () {
		XmlDocument doc = new ();
		XmlElement root = doc.CreateElement ( "Config" );
		doc.AppendChild ( root );
		root.AppendChild ( doc.CreateElement ( "HomePath" ) ).InnerText = homePath;
		root.AppendChild ( doc.CreateElement ( "AutostartName" ) ).InnerText = autostartName;
		
		XmlElement autoCmds = doc.CreateElement ( "AutoCommands" );
		root.AppendChild ( autoCmds );
		foreach ( var pair in autoCommands ) {
			XmlElement key = doc.CreateElement ( pair.Key );
			autoCmds.AppendChild ( key );
			for ( int i = 0; i < pair.Value.Length; i++ ) {
				XmlElement cmd = doc.CreateElement ( i.ToString () );
				cmd.InnerText = pair.Value[i];
				key.AppendChild ( cmd );
			}
		}
	}
	public static void Load ( string path = null ) {
		if ( string.IsNullOrEmpty ( path ) ) path = Path.Combine ( homePath, "config.xml" );

		if ( File.Exists ( path)) {
			if ( Path.GetExtension ( path ) != ".xml" )
				throw new ArgumentException ( "Invalid file extension." );
			homePath = Path.GetDirectoryName ( path );
		} else if ( Directory.Exists ( path ) ) {
			homePath = path;
			path = Path.Combine ( homePath, "config.xml" );
		}

		if (!File.Exists ( path ) ) {
			// Create default config
			Save ();
			return;
		} else {
			XmlDocument doc = new ();
			doc.Load ( path );
			XmlElement root = doc.DocumentElement;
			if ( root.Name != "Config" ) throw new ArgumentException ( "Invalid root element." );
			foreach ( XmlNode node in root.ChildNodes ) {
				switch ( node.Name ) {
				case "HomePath": homePath = node.InnerText; break;
				case "AutostartName": autostartName = node.InnerText; break;
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
		}
	}
}