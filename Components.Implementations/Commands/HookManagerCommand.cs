using Components.Interfaces;
using Components.Library;
using InputResender.Commands;

namespace Components.Implementations;
// Better question than is: Does the 'newer' version needs to be under Implementations? On what variant and how it depends?
public class HookManagerCommand : ACommand {
    HCallbackHolder<DHookManager.HookCallback> hookCallback;
    public enum CallbackFcn { None, Print, Aggregate }
    CallbackFcn CbFcn = CallbackFcn.None;
    CommandProcessor.CmdContext lastContext;
    List<string> aggregatedEvetns = new ();

    override public string Description => "Input hook manager.";
    // Cmd example: "hook add print keydown mousemove"
    public HookManagerCommand ( string parentDsc = null ) : base ( parentDsc ) {
        commandNames.Add ( "hook" );
        interCommands.Add ( "manager" );
        interCommands.Add ( "add" );
        interCommands.Add ( "remove" );
        interCommands.Add ( "list" );
        interCommands.Add ( "debug" );
    }

    protected override CommandResult ExecCleanup ( CommandProcessor.CmdContext context ) {
        hookCallback?.Unregister ();
        return new CommandResult ( "Hook callback unregistered." );
    }

    bool debugMode = false;

    override protected CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		DMainAppCore core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		switch ( context.SubAction ) {
        case "manager": {
            if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => $"hook manager <Sub-action>\n\tSub-action: {{{string.Join ( "|", ["start", "status"] )}}}", out var helpRes1 ) ) return helpRes1;

			if ( TryPrintHelp ( context.Args, context.ArgID + 2, () => context[1, "Sub-action"] switch {
                "start" => "hook manager start: Start hook manager",
				"status" => "hook manager status: Check if hook manager is running",
                _ => null
			}, out var helpRes2 ) ) return helpRes2;
			var manager = core.Fetch<DHookManager> ();
            switch ( context[1, "Sub-action"] ) {
            case "start":
                if ( manager != null ) return new CommandResult ( "Hook manager already started." );
                manager = new VHookManager ( core );
                return new CommandResult ( "Hook manager started." );
            case "status": return new CommandResult ( manager == null ? "Hook manager not started." : "Hook manager is running." );
            default:
                return new CommandResult ( $"Invalid sub-action '{context[1]}'." );
            }
        }
        case "add": {
            /// Hook callback propagation (setup):
            /// 1. <see cref="VHookManager.AddHook"/> (under <see cref="DHookManager.AddHook(int, VKChange[])"/> - add hook to manager
            /// 2. <see cref="VInputReader_KeyboardHook.SetupHook"/> (under <see cref="DInputReader.SetupHook(HHookInfo, Func{DictionaryKey, HInputEventDataHolder, bool}, Action{DictionaryKey, HInputEventDataHolder})"/> - Inter-step separating action into fast and delayed callbacks
            /// 3. <see cref="VWinLowLevelLibs.SetHookEx"/> (under <see cref="DLowLevelInput.SetHookEx(HHookInfo, Func{DictionaryKey, HInputData, bool})"/> - Create struct containing the hook and register it
            /// 4. <see cref="VWinLowLevelLibs.SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId)"/> - Call the Windows API to set the hook

            /// Hook callback propagation (on key press):
            /// 1. <see cref="Hook.LLCallback(int, IntPtr, IntPtr"/> - event origin, called by Windows API
            /// 2. <see cref="VInputReader_KeyboardHook.LocalCoolback(DictionaryKey, HInputData"/> - Call fast and queue delayed callbacks
            /// 3. <see cref="VHookManager.FastCB"/> or <see cref="VHookManager.DelayedCB"/> - Loop through all registered callbacks, stop at first consuming one
            /// 4. <see cref="HookManagerCommand.HookCallback(HInputEventDataHolder)"/> - Callback assign via command

            if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => $"hook add <CbAction> <VKChange1> [<VKChange2> ...]\n\tCbAction: {{{string.Join ( "|", Enum.GetNames<CallbackFcn> () )}}}\n\tVKChange: {{{string.Join ( "|", Enum.GetNames<VKChange> () )}}}", out var helpRes ) ) return helpRes;

            CallbackFcn cbFcn = context.Args.EnumC<CallbackFcn> ( context.ArgID + 1, "Callback action", true );
            lastContext = context;
            CbFcn = cbFcn;
            if ( cbFcn == CallbackFcn.Aggregate ) {
                try { context.CmdProc.GetVar<List<string>> ( "hookEvents" ); } catch ( Exception ) { context.CmdProc.SetVar ( "hookEvents", aggregatedEvetns = new () ); }
            }

            List<VKChange> actionList = new ();
            for ( int i = context.ArgID + 2; i < context.Args.ArgC; i++ ) {
                var act = context.Args.EnumC<VKChange> ( i, i == context.ArgID + 2 ? "Action" : null );
                if ( act == VKChange.None ) return new CommandResult ( "Invalid action." );
                actionList.Add ( act );
            }
            var hookManager = core.Fetch<DHookManager> ();
            if ( hookManager == null ) return new CommandResult ( "No hook manager available." );

            var newHooks = hookManager.AddHook ( 0, actionList.ToArray () );
            if (newHooks == null || !newHooks.Any ()) return new CommandResult ( "No hooks added." );

            var cb = hookManager.AddCallback ( DHookManager.CBType.Delayed, 0 );
            cb.callback = HookCallback;

            string[] hookInfo = new string[newHooks.Count];
            int j = 0;
            foreach ( var hook in newHooks ) hookInfo[j++] = core.Fetch<DInputReader> ().PrintHookInfo ( hook );


            return new CommandResult ( $"Hook added ({string.Join ( ", ", hookInfo )})." );
        }
        case "remove": {
            if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => "hook remove <VKChange1> [<VKChange2> ...]: Not currently implemented.", out var helpRes ) ) return helpRes;
			return new CommandResult ( "Removing hooks is not implemented." );
        }
        case "list": {
            if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => "hook list: Not currently implemented.", out var helpRes ) ) return helpRes;
			return new CommandResult ( "Listing hooks is not implemented." );
        }
        case "debug": {
			if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => "hook debug (start|stop): Starts or stops debugging mode for hooks.", out var helpRes ) ) return helpRes;
            switch ( context[1, "sub-action"] ) {
            case "start":
                if ( debugMode ) return new ( "Debug mode for hooks is already on" );
                lastContext = context;
                core.LowLevelInput.OnEvent += OnLLEvent;
                debugMode = true;
                return new ( "Debugging mode for hooks now active" );
			case "stop":
				if ( debugMode ) return new ( "Debug mode for hooks is already off" );
				core.LowLevelInput.OnEvent -= OnLLEvent;
				debugMode = false;
				return new ( "Debugging mode for hooks now disabled" );
			default: return new ( $"Invalid sub-action '{context[1]}'" );
            }
		}
        default: return new CommandResult ( $"Invalid action '{context.SubAction}'." );
        }
    }

    private void OnLLEvent (string info) {
        lastContext.CmdProc.ProcessLine ( $"print \"{info}\"" );
    }

    private bool HookCallback ( HInputEventDataHolder e ) {
        switch ( CbFcn ) {
        case CallbackFcn.None: return true;
        case CallbackFcn.Print:
            lastContext.CmdProc.ProcessLine ( $"print \"{EventToStr ( e )}\"" );
            return true;
        case CallbackFcn.Aggregate:
            var list = lastContext.CmdProc.GetVar<List<string>> ( "hookEvents" );
            if ( !list.Any () || list[^1].Length > 90 ) list.Add ( EventToShort ( e ) );
            else list[^1] += ' ' + EventToShort ( e );
            return true;
        default: return false;
        }

        string EventToStr ( HInputEventDataHolder e ) {
            if ( e == null ) return "null";
            else if ( e is HKeyboardEventDataHolder ki ) return $"hook catched {(KeyCode)e.InputCode} ({e.Pressed}) : {e.HookInfo}";
            else if ( e is HMouseEventDataHolder mi ) return $"hook catched {GetDir ( mi.DeltaX, mi.DeltaY )}[{mi.DeltaX}|{mi.DeltaY}] : {e.HookInfo}";
            else return $"Unknown event ({e.GetType ()}): {e}";
        }
        string EventToShort ( HInputEventDataHolder e ) {
            if ( e == null ) return "null";
            else if ( e is HKeyboardEventDataHolder ki ) return $"{(ki.Pressed < 1 ? '↓' : '↑')}{(KeyCode)ki.InputCode}";
            else if ( e is HMouseEventDataHolder mi ) return $"{GetDir ( mi.DeltaX, mi.DeltaY )}[{mi.DeltaX}|{mi.DeltaY}]";
            else return $"Unknown event ({e.GetType ()}): {e}";
        }

        char GetDir ( int x, int y ) {
            if ( x < 0 ) {
                if ( y < 0 ) return '↖';
                else if ( y == 0 ) return '←';
                else return '↙';
            } else if ( x == 0 ) {
                if ( y < 0 ) return '↑';
                else if ( y == 0 ) return '•';
                else return '↓';
            } else {
                if ( y < 0 ) return '↗';
                else if ( y == 0 ) return '→';
                else return '↘';
            }
        }
    }
}