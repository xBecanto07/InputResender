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

	override protected CommandResult ExecIner ( CommandProcessor context, ArgParser args, int argID ) {
		switch ( args.String ( argID, "Action" ) ) {
		case "set": {
			string varName = args.String ( argID + 1, "Variable name" );
			string varType = args.String ( argID + 2, "Variable type" );
			try {
				switch ( varType ) {
				case "string":
					context.SetVar ( varName, args.String ( argID + 3, "Variable value" ) );
					return new CommandResult ( $"Variable '{varName}' set." );
				default: return new CommandResult ( $"Unsupported variable type '{varType}'." );
				}
			} catch ( Exception e ) {
				return new CommandResult ( $"Error setting variable '{varName}': {e.Message}" );
			}
		}
		case "get": {
			string varName = args.String ( argID + 1, "Variable name" );
			string varType = args.String ( argID + 2, "Variable type" );
			try {
				//context.GetVar ( varName, varType );
				switch ( varType ) {
				case "string": return new CommandResult ( $"Variable '{varName}' value: {context.GetVar<string> ( varName )}" );
				case "array.string":
					string arSep = args.String ( argID + 3, null );
					if (string.IsNullOrEmpty(arSep)) arSep = ", ";

					return new CommandResult ( $"Variable '{varName}' value: {string.Join ( arSep, context.GetVar<ICollection<string>> ( varName ) )}" );
				default: return new CommandResult ( $"Unsupported variable type '{varType}'." );
				}
			} catch ( Exception e ) {
				return new CommandResult ( $"Error getting variable '{varName}': {e.Message}" );
			}
		}
		case "add": {
			string varName = args.String ( argID + 1, "Variable name" );
			string varType = args.String ( argID + 2, "Variable type" );
			try {
				switch ( varType ) {
				case "string":
					string origVal = context.GetVar<string> ( varName );
					context.SetVar ( varName, origVal + args.String ( argID + 3, "Variable value" ) );
					return new CommandResult ( $"Variable '{varName}' updated." );
				case "array.string":
					context.GetVar<ICollection<string>> ( varName ).Add ( args.String ( argID + 3, "Variable value" ) );
					return new CommandResult ( $"Variable '{varName}' updated." );
				default: return new CommandResult ( $"Unsupported variable type '{varType}'." );
				}
			} catch ( Exception e ) {
				return new CommandResult ( $"Error updating variable '{varName}': {e.Message}" );
			}
		}
		case "reset": {
			string varName = args.String ( argID + 1, "Variable name" );
			string varType = args.String ( argID + 2, "Variable type" );
			try {
				switch ( varType ) {
				case "string":
					context.SetVar ( varName, string.Empty );
					return new CommandResult ( $"Variable '{varName}' reset." );
				case "array.string":
					context.GetVar<ICollection<string>> ( varName ).Clear ();
					return new CommandResult ( $"Variable '{varName}' reset." );
				default: return new CommandResult ( $"Unsupported variable type '{varType}'." );
				}
			} catch ( Exception e ) {
				return new CommandResult ( $"Error reseting variable '{varName}': {e.Message}" );
			}
		}
		default:
			return new CommandResult ( $"Invalid action '{args.String ( argID, "Action" )}'." );
		}
	}
}
