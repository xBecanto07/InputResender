﻿using System;
using System.Xml;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace InputResender.CLI; 
public class Config {
	private static string homePath = AppDomain.CurrentDomain.BaseDirectory;
	private static string savePath = Path.Combine ( HomePath, "config.xml" );
	private readonly static Dictionary<string, string[]> autoCommands = new () {
		{"initCmds", new string[] { "loadall", "clear", "safemode on", "core new", "core own", "loglevel all", "network callback recv print", "network callback newconn print", "hook manager start" } }
	};
	private static string autostartName = "initCmds";
	private static bool printAutoCommands = true;
	private static int maxOnelinerLength = 90;
	private static PrintFormat responsePrintFormat = PrintFormat.Normal;

	public enum PrintFormat { None, Batch, ErrOnly, Normal, Full }

	public static string AutostartName { get => autostartName; set { autostartName = value; Save (); } }
	public static string HomePath { get => homePath; set { homePath = value; Save (); } }
	public static string SavePath { get => savePath; set { savePath = value; Save (); } }
	public static bool PrintAutoCommands { get => printAutoCommands; set { printAutoCommands = value; Save (); } }
	public static int MaxOnelinerLength { get => maxOnelinerLength; set { maxOnelinerLength = value; Save (); } }
	public static PrintFormat ResponsePrintFormat { get => responsePrintFormat; set { responsePrintFormat = value; Save (); } }

	public static IReadOnlyCollection<string> FetchAutoCommands ( string key ) {
		if ( string.IsNullOrEmpty ( key ) ) return Array.Empty<string> ();
		if ( !autoCommands.TryGetValue ( key, out string[] ret ) ) return Array.Empty<string> ();
		string[] ret2 = new string[ret.Length];
		ret.CopyTo ( ret2, 0 );
		return ret2;
	}
	public static void AddAutoCommand ( string key, IEnumerable<string> commands ) =>
		autoCommands[key] = commands.ToArray ();

	private static void SetHomePath ( string path = null ) {

		homePath = path;
	}


	public static void Save () {
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

	public static void Load ( string path = null ) {
		if ( string.IsNullOrEmpty ( path ) ) path = Path.Combine ( homePath, "config.xml" );

		if ( File.Exists ( path ) ) {
			if ( Path.GetExtension ( path ) != ".xml" )
				throw new ArgumentException ( "Invalid file extension." );
			homePath = path;
		} else if ( Directory.Exists ( path ) ) {
			homePath = path;
			path = Path.Combine ( homePath, "config.xml" );
		}
		SavePath = path;

		if (!File.Exists ( homePath ) ) {
			// Create default config
			Save ();
			return;
		} else {
			XmlDocument doc = new ();
			doc.Load ( homePath );
			XmlElement root = doc.DocumentElement;
			if ( root.Name != "Config" ) throw new ArgumentException ( "Invalid root element." );
			foreach ( XmlNode node in root.ChildNodes ) {
				switch ( node.Name ) {
				case "HomePath": homePath = node.InnerText; break;
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
		}
	}
}