using Components.Interfaces;
using Components.Library;
using InputResender.Commands;

namespace Components.Implementations;
public class HookManagerCommand : ACommand {
    HCallbackHolder<DHookManager.HookCallback> hookCallback;
    public enum CallbackFcn { None, Print, Aggregate }
    CallbackFcn CbFcn = CallbackFcn.None;
    CommandProcessor lastContext;
    List<string> aggregatedEvetns = new ();

    override public string Description => "Input hook manager.";
    // Cmd example: "hook add print keydown mousemove"
    public HookManagerCommand ( string parentDsc = null ) : base ( parentDsc ) {
        commandNames.Add ( "hook" );
        interCommands.Add ( "manager" );
        interCommands.Add ( "add" );
    }

    protected override CommandResult ExecCleanup ( CommandProcessor context, ArgParser args, int argID ) {
        hookCallback?.Unregister ();
        return new CommandResult ( "Hook callback unregistered." );
    }

    override protected CommandResult ExecIner ( CommandProcessor context, ArgParser args, int argID ) {
		DMainAppCore core = context.GetVar<DMainAppCore> ( CoreManagerCommand.ActiveCoreVarName );
		switch ( args.String ( argID, "Action" ) ) {
        case "manager": {
            string act = args.String ( argID + 1, "Sub-action" );
            var manager = core.Fetch<DHookManager> ();
            switch ( act ) {
            case "start":
                if ( manager != null ) return new CommandResult ( "Hook manager already started." );
                manager = new VHookManager ( core );
                return new CommandResult ( "Hook manager started." );
            case "status": return new CommandResult ( manager == null ? "Hook manager not started." : "Hook manager is running." );
            default:
                return new CommandResult ( "Invalid sub-action." );
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

            if ( TryPrintHelp ( args, argID + 1, () => $"hook add <CbAction={{{string.Join ( "|", Enum.GetNames<CallbackFcn> () )}}}> <VKChange1={{{string.Join ( "|", Enum.GetNames<VKChange> () )}}}> [<VKChange2> ...]", out var helpRes ) ) return helpRes;

            CallbackFcn cbFcn = args.EnumC<CallbackFcn> ( argID + 1, "Callback action" );
            lastContext = context;
            CbFcn = cbFcn;
            if ( cbFcn == CallbackFcn.Aggregate ) {
                try { context.GetVar<List<string>> ( "hookEvents" ); } catch ( Exception ) { context.SetVar ( "hookEvents", aggregatedEvetns = new () ); }
            }

            List<VKChange> actionList = new ();
            for ( int i = argID + 2; i < args.ArgC; i++ ) {
                var act = args.EnumC<VKChange> ( i, i == argID + 2 ? "Action" : null );
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
        case "remove": return new CommandResult ( "Removing hooks is not implemented." );
        case "list": return new CommandResult ( "Listing hooks is not implemented." );
        default: return new CommandResult ( $"Invalid action '{args.String ( argID, "Action" )}'." );
        }
    }

    private bool HookCallback ( HInputEventDataHolder e ) {
        switch ( CbFcn ) {
        case CallbackFcn.None: return true;
        case CallbackFcn.Print:
            lastContext.ProcessLine ( $"print \"{EventToStr ( e )}\"" );
            return true;
        case CallbackFcn.Aggregate:
            var list = lastContext.GetVar<List<string>> ( "hookEvents" );
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