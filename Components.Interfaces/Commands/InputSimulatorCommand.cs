using Components.Library;
using InputResender.Commands;
using System;
using System.Collections.Generic;

namespace Components.Interfaces.Commands; 
public class InputSimulatorCommand : ACommand {
	public override string Description => "Can simulate user hardware input";

	private static List<string> CommandNames = ["sim"];
	private static List<(string, Type)> InterCommands = [
		  ("mousemove", null),
		  ("keydown", null),
		  ("keyup", null),
		  ("keypress", null),
	 ];

	public InputSimulatorCommand ( string parentDsc = null )
		: base ( parentDsc, CommandNames, InterCommands ) {}

	/*public static HKeyboardEventDataHolder KeyPress ( ComponentBase owner, KeyCode key, VKChange press, int deviceID = 1 ) {
	HHookInfo hookInfo = new HHookInfo ( owner, deviceID, press );
	float val = 0, delta = 0;
	switch ( press ) {
	case VKChange.KeyDown: val = 1; delta = 1; break;
	case VKChange.KeyUp: val = 0; delta = -1; break;
	}
	return new HKeyboardEventDataHolder ( owner, hookInfo, (int)key, val, delta );
}

public static HMouseEventDataHolder MouseMove ( ComponentBase owner, int X, int Y, int deviceID = 1 ) {
	HHookInfo hookInfo = new HHookInfo ( owner, deviceID, VKChange.MouseMove );
	return new HMouseEventDataHolder ( owner, hookInfo, X, Y );
}*/

	protected override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		//if (TryPrintHelp(context.Args, context.ArgID + 1, () => "sim <Action> [Options]\n\tAction: {mousemove|keydown|keyup|keypress}\n\tOptions: Action specific options", out var helpRes ) ) return helpRes;
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"mousemove" => $"sim mousemove [-x <Xaxis>] [-y <Yaxis>]: Simulate mouse move event\n\t-x <Xaxis> = 0: X axis movement\n\t-y <Yaxis> = 0: Y axis movement",
			"keydown" => "sim keydown <Key>: Simulate key down event (not yet implemented)\n\tKey: {Windows-style keycode}",
			"keyup" => "sim keyup <Key>: Simulate key up event (not yet implemented)\n\tKey: {Windows-style keycode}",
			"keypress" => $"sim keypress <Key>: Simulate key press event (keydown, keyup)\n\tKey: {{{string.Join ( "|", Enum.GetNames<KeyCode> () )}}}",
			_ => null
		}, out var helpRes ) ) return helpRes;

		DMainAppCore core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		DInputSimulator sim = core.Fetch<DInputSimulator> ();
		if ( sim == null ) throw new Exception ( "No InputSimulater registered in core" );
		DHookManager hookManager = core.Fetch<DHookManager> ();
		if ( hookManager == null ) throw new Exception ( "No HookManager registered in core" );

		switch (context.SubAction) {
		case "keydown": {
			int sent = Simulate ( context, 1, sim, true, false );
			return new CommandResult ( $"Sent {sent} key down events." );
		}
		case "keyup": {
			int sent = Simulate ( context, 1, sim, false, true );
			return new CommandResult ( $"Sent {sent} key up events." );
		}
		case "keypress": {
			int sent = Simulate ( context, 1, sim, true, true );
			return new CommandResult ( $"Sent {sent} keyboard input (keypress) events." );
		}
		case "mousemove": {
			return new CommandResult ( "Under construction" );
			/*
			context.Args.RegisterSwitch ( 'x', "Xaxis", "0" );
			context.Args.RegisterSwitch ( 'y', "Yaxis", "0" );
			int x = context.Args.Int ( "-x", "X axis" ).Value;
			int y = context.Args.Int ( "-y", "Y axis" ).Value;
			if ( x == 0 && y == 0 ) return new CommandResult ( "Specify at least one axis." );
			var asdf = HInputEventDataHolder.MouseMove ( sim, x, y );
			int sent = sim.Simulate ( asdf );
			return new CommandResult ( $"Sent {sent} mouse move events." );*/
		}
		default: return new CommandResult ( $"Invalid action '{context.SubAction}'." );
		}
	}

	int Simulate ( CommandProcessor.CmdContext context, int offset, DInputSimulator sim, bool keyDown, bool keyUp) {
		List<KeyCode> keys = GetKeysFromArgs ( context, offset );

		List<HInputEventDataHolder> events = [];
		for ( int i = 0; i < keys.Count; i++ ) {
			if ( keyDown ) events.Add ( HInputEventDataHolder.KeyDown ( sim, null, keys[i] ) );
			if ( keyUp ) events.Add ( HInputEventDataHolder.KeyUp ( sim, null, keys[i] ) );
		}

		sim.AllowRecapture = true;
		return sim.Simulate ( [.. events] );
	}

	public static List<KeyCode> GetKeysFromArgs (CommandProcessor.CmdContext context, int offset) {
		List<KeyCode> keys = [context.Args.EnumC<KeyCode> ( context.ArgID + offset, "Key", true )];
		for ( int i = context.ArgID + offset + 1; i < context.Args.ArgC; i++ ) {
			keys.Add ( context.Args.EnumC<KeyCode> ( i, "Key", true ) );
			// Not allowing other arguments after first key
		}
		return keys;
	}
}
