using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Components.Library.ComponentSystem;
public class ContextVarCommands : ACommand {
	override public string Description => "Context variable access commands.";
	public ContextVarCommands ( string parentDsc = null ) : base ( parentDsc ) {
		commandNames.Add ( "context" );
		interCommands.Add ( "set" );
		interCommands.Add ( "get" );
		interCommands.Add ( "reset" );
		interCommands.Add ( "add" );
	}

	override protected CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		switch ( context.SubAction ) {
		case "set": {
			if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => "context set <Variable type> <Variable name> <Variable value>\n\tVariable type: Type of variable: <string>\n\tVariable name: Name of the variable\n\tVariable value: Value to be assigned", out var helpRes ) ) return helpRes;
			string varType = context[1, "Variable type"];
			string varName = context[2, "Variable name"];
			try {
				switch ( varType ) {
				case "string":
					context.CmdProc.SetVar ( varName, context[3, "Variable value"] );
					return new CommandResult ( $"Variable '{varName}' set." );
				default: return new CommandResult ( $"Unsupported variable type '{varType}'." );
				}
			} catch ( Exception e ) {
				return new CommandResult ( $"Error setting variable '{varName}': {e.Message}" );
			}
		}
		case "get": {
			if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => "context get <Variable type> <Variable name> [<Array separator>]\n\tVariable type: Type of variable: <string|array.string>\n\tVariable name: Name of the variable\n\tArray separator: Separator for array values", out var helpRes ) ) return helpRes;
			string varType = context[1, "Variable type"];
			string varName = context[2, "Variable name"];
			try {
				//context.GetVar ( varName, varType );
				switch ( varType ) {
				case "string": return new CommandResult ( $"Variable '{varName}' value: {context.CmdProc.GetVar<string> ( varName )}" );
				case "array.string":
					string arSep = context[3];
					if ( string.IsNullOrEmpty ( arSep ) ) arSep = ", ";

					return new CommandResult ( $"Variable '{varName}' value: {string.Join ( arSep, context.CmdProc.GetVar<ICollection<string>> ( varName ) )}" );
				default: return new CommandResult ( $"Unsupported variable type '{varType}'." );
				}
			} catch ( Exception e ) {
				return new CommandResult ( $"Error getting variable '{varName}': {e.Message}" );
			}
		}
		case "add": {
			if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => "context add <Variable type> <Variable name> <Variable value>\n\tVariable type: Type of variable: <string|array.string>\n\tVariable name: Name of the variable\n\tVariable value: Value to be added", out var helpRes ) ) return helpRes;
			string varType = context[1, "Variable type"];
			string varName = context[2, "Variable name"];
			try {
				switch ( varType ) {
				case "string":
					string origVal = context.CmdProc.GetVar<string> ( varName );
					context.CmdProc.SetVar ( varName, origVal + context[3, "Variable value"] );
					return new CommandResult ( $"Variable '{varName}' updated." );
				case "array.string":
					context.CmdProc.GetVar<ICollection<string>> ( varName ).Add ( context[3, "Variable value"] );
					return new CommandResult ( $"Variable '{varName}' updated." );
				default: return new CommandResult ( $"Unsupported variable type '{varType}'." );
				}
			} catch ( Exception e ) {
				return new CommandResult ( $"Error updating variable '{varName}': {e.Message}" );
			}
		}
		case "reset": {
			if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => "context reset <Variable type> <Variable name>\n\tVariable type: Type of variable: <string|array.string>\n\tVariable name: Name of the variable", out var helpRes ) ) return helpRes;
			string varType = context[1, "Variable type"];
			string varName = context[2, "Variable name"];
			try {
				switch ( varType ) {
				case "string":
					context.CmdProc.SetVar ( varName, string.Empty );
					return new CommandResult ( $"Variable '{varName}' reset." );
				case "array.string":
					context.CmdProc.GetVar<ICollection<string>> ( varName ).Clear ();
					return new CommandResult ( $"Variable '{varName}' reset." );
				default: return new CommandResult ( $"Unsupported variable type '{varType}'." );
				}
			} catch ( Exception e ) {
				return new CommandResult ( $"Error reseting variable '{varName}': {e.Message}" );
			}
		}
		default:
			return new CommandResult ( $"Invalid action '{context.SubAction}'." );
		}
	}
}