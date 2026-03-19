using Components.Interfaces;
using Components.Library;
using DataHolder = Components.Interfaces.HInputEventDataHolder;
using System.Xml;
using SeClav;
using SeClav.DataTypes;
using Components.Interfaces.SeClav;

namespace Components.Implementations;
public class VScriptedInputProcessor : DInputProcessor {
	public SeClav.SCLScriptHolder Script { get; private set; }
	const string StatusVarName = "sip_status";
	SCLRuntimeHolder ScriptRuntime;
	//SCLRunner ScriptRunner;
	object PersistentScriptData = null;
	List<string> progress = [];
	readonly List<KeyCode> ListeningKeys = [];
	VKChangeMask ListeningActionMask = 0;
	int ProcessTimeout = 0;
	bool WasFired = false;
	SCLDebugger Debugger;
	readonly List<string> DebuggerLog = [];

	public override int ComponentVersion => 1;

	public VScriptedInputProcessor ( CoreBase owner ) : base ( owner ) { }


	public override StateInfo Info => throw new NotImplementedException ();

	public override void ProcessInput ( DataHolder[] inputCombination ) {
		DebuggerLog.Add ( $" ... Captured {inputCombination.Select ( ( d ) => d.ToString () ).Aggregate ( ( a, b ) => a + "+" + b )} ..." );
		if ( ListeningKeys.Count != 0 ) {
			bool passingMask = false;
			foreach ( var data in inputCombination[..1] ) {
				// Look only at the actual event now. Rest is expected to be 'status' data. Ignore that for now until better system for that is implemented.
				KeyCode key = (KeyCode)data.InputCode;
				if ( !ListeningKeys.Contains ( key ) ) continue;
				bool isBeingPressed = data.BeingPressedX ^ data.BeingPressedY ^ data.BeingPressedZ;
				bool isBeingReleased = data.BeingReleasedX ^ data.BeingReleasedY ^ data.BeingReleasedZ;
				VKChangeMask action = VKChangeMask.None;
				if ( isBeingPressed ) action |= VKChangeMask.KeyDown;
				if ( isBeingReleased ) action |= VKChangeMask.KeyUp;
				if ( (action & ListeningActionMask) != 0 ) {
					passingMask = true;
					break;
				}
			}
			if ( !passingMask ) return;
		}


		try {
			//if ( PersistentScriptData != null )
			//	ScriptRunner.PersistantStatus = PersistentScriptData;
			ScriptRuntime.SetExternVar ( StatusVarName, new SCL_StatusType ( this, inputCombination, ScriptRuntime )
				, Owner
			);
			ScriptRuntime.SetExternVar<BasicValueIntDef> ( "SettingChanged", intDefinition => new BasicValueInt ( intDefinition, 0 ), Owner );
			WasFired = false;
			lock (ScriptRuntime) {
				ScriptRuntime.Execute ( true );
				//ScriptRunner.ExecuteSafe ( ScriptRuntime, [], ref progress );
			}
		} catch ( Exception ex ) {
			Owner.PushDelayedError ( "Error during execution of input processing script!", ex );
		}

		DebuggerLog.Add ( $"= = = = = Script Execution Log after {inputCombination.Select ( ( d ) => d.ToString () ).Aggregate ( ( a, b ) => a + "+" + b )} = = = = =" );
		foreach ( var log in Debugger.ExecutionLog )
			DebuggerLog.Add ( log.ToString () );
		Debugger.ClearLog ();
		//PersistentScriptData = ScriptRunner.PersistantStatus;
		//Task.Delay ( ProcessTimeout ).ContinueWith ( _ => {
		//	if ( !WasFired ) {
		//		lock ( ScriptRunner ) {
		//			ScriptRunner.ExecuteSafe ( ScriptRuntime, [], ref progress );
		//		}
		//	}
		//} );
	}

	public void AssignScript ( SCLScriptHolder script ) {
		if ( script == null ) throw new ArgumentNullException ( nameof ( script ) );
		Script = script;
		//if (!Script.IsUsingModule (Components.Interfaces.SeClav.SCL_BasicModule.ModuleName)) {
		//	Owner.PushDelayedMsg ( "ScriptedInputProcessor: Assigned script does not use SeClav.Basic module. Cannot use any SIP integration. " );
		//	return;
		//}
		ScriptRuntime = new SCLRuntimeHolder ( Script );
		//ScriptRunner = new SCLRunner ( Script );
		Debugger = ScriptRuntime.SetupDebugger ();
		progress.EnsureCapacity ( 1024 );
	}



	public class SCL_Module : IModuleInfo {
		public const string ModuleName = "ScriptedInputProcessor";
		public string Name => ModuleName;
		public string Description => "A module for scripted processing of input.";

		public IReadOnlySet<ICommand> Commands => new HashSet<ICommand> () {
			new SCL_PrintStatusCmd (),
			new SCL_GetKeyName (),
			new SCL_GetKeyStatus (),
			new SCL_FireKeyCmd (),
			new SCL_CompareKeys4 (),
			new SCL_Setup (),
			new SCL_ResetListens (),
			new SCL_AddListen (),
		};

		public IReadOnlySet<IMacro> Macros => new HashSet<IMacro> () {
			new SCL_FSM_Ternary_Tree_Macro (),
		};

		public IReadOnlySet<DataTypeDefinition> DataTypes => new HashSet<DataTypeDefinition> () {
			new SCL_StatusTypeDef (),
		};

		public IReadOnlyDictionary<string, PraeDirective> PraeDirectives => new Dictionary<string, PraeDirective> () { };
	}

	public class SCL_StatusTypeDef : SeClav.DataTypeDefinition {
		public override string Name => "SIP_Status_t";
		public override string Description => "Container for status of ScriptedInputProcessor execution.";
		public override IReadOnlySet<ICommand> Commands => null;
		public override bool TryParse ( ref string line, out IDataType result ) {
			result = null;
			return false; //Parsing not implemented for SCL_StatusType
		}
		public override IDataType Default => new SCL_StatusType ( null, [], null );

		public override bool Equals ( object obj ) => obj is SCL_StatusTypeDef;
	}

	public class SCL_StatusType : SeClav.IDataType {
		public VScriptedInputProcessor Processor;
		public DataHolder[] InputCombination;
		public DataTypeDefinition Definition { get; init; }

		public SCL_StatusType ( VScriptedInputProcessor comp, DataHolder[] inputCombination, SCLRuntimeHolder SCLruntime ) {
			Processor = comp;
			InputCombination = inputCombination;
			Definition = SCLruntime == null ? new SCL_StatusTypeDef () : SCLruntime.GetDefinition<SCL_StatusTypeDef> ();
		}

		public void Assign ( IDataType value ) {
			if ( value is not SCL_StatusType val )
				throw new InvalidOperationException ( "Cannot assign non-SCL_StatusType to SCL_StatusType." );
			Processor = val.Processor;
			InputCombination = val.InputCombination;
		}
	}

	public class SCL_PrintStatusCmd : SeClav.ICommand {
		private readonly SeClav.DataTypes.SCLT_Void voidType = new ();

		public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [
			( "status", new SCL_StatusTypeDef (), "The status to print." )
			];

		public int ArgC => 1;
		public DataTypeDefinition ReturnType => voidType;
		public string CmdCode => "PRINT_SIP_STATUS";
		public string CommonName => "Print SIP Status";
		public string Description => "Prints the status of the ScriptedInputProcessor.";

		public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
			List<string> progress = [];
			return ExecuteSafe ( runtime, args, ref progress );
		}
		public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
			var arg0 = runtime.SafeGetVar ( args[0] );
			if ( arg0 is not SCL_StatusType status ) {
				throw new InvalidOperationException ( "Argument is not of type SCL_StatusType." );
			}

			status.Processor.Owner.PushDelayedMsg ( $"\n = = = = = = = = = = = =\nScriptedInputProcessor Status:\nInputCombination Count: {(status.InputCombination == null ? 0 : status.InputCombination.Length)}\n= = = = = = = = = = = = = = = =\n" );
			return voidType.Default;
		}
	}

	public class SCL_GetKeyName : SeClav.ICommand {
		private readonly BasicValueStringDef stringType = new ();
		private readonly SCLT_Void voidType = new ();

		public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [
			( "status", new SCL_StatusTypeDef (), "The status containing data" ),
			( "id", new BasicValueIntDef(), "Input key ID" )
			];

		public int ArgC => 2;
		public DataTypeDefinition ReturnType => stringType;
		public string CmdCode => "GET_SIP_KEY_NAME";
		public string CommonName => "Get Key Name";
		public string Description => "Get name of i-th key in given input event";

		public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
			List<string> progress = [];
			return ExecuteSafe ( runtime, args, ref progress );
		}
		public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
			var (status, idVal) = runtime.SafeGetVar<SCL_StatusType, BasicValueInt> ( args[0], args[1] );

			if (idVal.Value >= status.InputCombination.Length)
				throw new InvalidOperationException ( $"Input ID {idVal.Value} is out of range for the current input combination." );

			return new BasicValueString ( stringType, ((KeyCode)status.InputCombination[idVal.Value].InputCode).ToString () );
		}
	}

	public class SCL_GetKeyStatus : SeClav.ICommand {
		private readonly BasicValueIntDef intType = new ();
		private readonly BasicValueStringDef stringType = new ();
		private readonly SCLT_Void voidType = new ();

		public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [
			( "status", new SCL_StatusTypeDef (), "The status containing data" ),
			( "name", stringType, "Input key name" )
			];

		public int ArgC => 2;
		public DataTypeDefinition ReturnType => intType;
		public string CmdCode => "GET_SIP_KEY_STATUS";
		public string CommonName => "Get Key Status";
		public string Description => "Get press status of given key in given input event";

		public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
			List<string> progress = [];
			return ExecuteSafe ( runtime, args, ref progress );
		}
		public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
			var (status, key) = runtime.SafeGetVar<SCL_StatusType, BasicValueString> ( args[0], args[1] );

			//return new BasicValueString ( stringType, ((KeyCode)status.InputCombination[idVal.Value].InputCode).ToString () );

			foreach ( var data in status.InputCombination ) {
				if ( ((KeyCode)data.InputCode).ToString () == key.Value ) {
					return new BasicValueInt ( intType, data.ValueX );
				}
			}
			return new BasicValueInt ( intType, -1 );
		}
	}

	public class SCL_FireKeyCmd : SeClav.ICommand {
		private readonly SeClav.DataTypes.SCLT_Void voidType = new ();

		public string CmdCode => "FIRE_KEY";
		public string CommonName => "Fire Key";
		public string Description => "Triggers a key event.";
		public int ArgC => 3;
		public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [
			( "status", new SCL_StatusTypeDef (), "The status to use." ),
			( "key", new BasicValueStringDef (), "The key to fire." ),
			( "pressed", new BasicValueIntDef (), "1 to press the key, 0 to release, 2 to press and release" )
		];
		public DataTypeDefinition ReturnType => voidType;
		public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
			List<string> progress = [];
			return ExecuteSafe ( runtime, args, ref progress );
		}
		public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
			var (status, keyVal, pressedVal) = runtime.SafeGetVar<
				SCL_StatusType, BasicValueString, BasicValueInt
				> ( args[0], args[1], args[2] );

			KeyCode key = KeyCode.None;
			try {
				key = (KeyCode)Enum.Parse ( typeof ( KeyCode ), keyVal.Value, ignoreCase: true );
			} catch ( Exception ) {
				throw new InvalidOperationException ( $"Key '{keyVal.Value}' is not a valid KeyCode." );
			}

			List<InputData> eventsToFire = [];
			switch(pressedVal.Value) {
			case 0: eventsToFire.Add ( new InputData ( status.Processor, key, false ) ); break;
			case 1: eventsToFire.Add ( new InputData ( status.Processor, key, true ) ); break;
			case 2:
				eventsToFire.Add ( new InputData ( status.Processor, key, true ) );
				eventsToFire.Add ( new InputData ( status.Processor, key, false ) );
				break;
			default: throw new InvalidOperationException ( $"Key '{keyVal.Value}' is not a valid key action code.");
			}

			if (eventsToFire.Count > 0)
				status.Processor.WasFired = true;
			for (int i = 0; i < eventsToFire.Count; i++) {
				var ev = eventsToFire[i];
				status.Processor.DebuggerLog.Add ( $"Firing key event from script: {ev}" );
				status.Processor.Owner.PushDelayedMsg ( $"Firing key '{ev.Key}' - Pressed: {ev.Pressed}" );
				status.Processor.FireCallback ( ev );
				if (i < eventsToFire.Count - 1) {
					// If there are multiple events (i.e. press and release), add a small delay between them to ensure they are processed in the correct order.
					System.Threading.Thread.Sleep ( 10 );
				}
			}
			return voidType.Default;
		}
	}

	public class SCL_CompareKeys4 : SeClav.ICommand {
		private readonly SeClav.DataTypes.SCLT_Void voidType = new ();
		private readonly BasicValueStringDef stringType = new ();

		public string CmdCode => "COMPARE_KEYS_3";
		public string CommonName => "Compare Keys (3)";
		public string Description => "Compares the status of 3 keys. Sets flags (A-C) based on their states.";
		public int ArgC => 4;
		public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [
			( "status", new SCL_StatusTypeDef (), "The status to use." ),
			( "key1", stringType, "The first key to compare." ),
			( "key2", stringType, "The second key to compare." ),
			( "key3", stringType, "The third key to compare." ),
		];
		public DataTypeDefinition ReturnType => voidType;
		public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
			List<string> progress = [];
			return ExecuteSafe ( runtime, args, ref progress );
		}
		public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
			var (status, keyVal1, keyVal2, keyVal3) = runtime.SafeGetVar<
				SCL_StatusType, BasicValueString, BasicValueString, BasicValueString
				> ( args[0], args[1], args[2], args[3] );
			string[] keys = [ keyVal1.Value, keyVal2.Value, keyVal3.Value ];
			KeyCode[] keyCodes = new KeyCode[3];
			bool[] pressed = new bool[3];
			ISCLRuntime.SCLFlags[] flags = [
				ISCLRuntime.SCLFlags._A,
				ISCLRuntime.SCLFlags._B,
				ISCLRuntime.SCLFlags._C,
				];



			for (int i = 0; i < 3; i++) {
				try {
					keyCodes[i] = (KeyCode)Enum.Parse ( typeof ( KeyCode ), keys[i], ignoreCase: true );
				} catch ( Exception ) {
					throw new InvalidOperationException ( $"Key '{keys[i]}' is not a valid KeyCode." );
				}

				foreach ( var data in status.InputCombination ) {
					if ( (KeyCode)data.InputCode == keyCodes[i] ) {
						pressed[i] = data.ValueX != 0;
						break;
					}
				}

				if (pressed[i] ) runtime.SetFlag ( flags[i] );
				else runtime.ResetFlag ( flags[i] );
			}

			return voidType.Default;
		}
	}

	public class SCL_Setup : SeClav.ICommand {
		private readonly SeClav.DataTypes.SCLT_Void voidType = new ();
		private readonly BasicValueIntDef intType = new ();

		public string CmdCode => "SIP_SETUP";
		public string CommonName => "SIP Setup";
		public string Description => "Setup SIP interaction.";
		public int ArgC => 3;
		public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [
			( "status", new SCL_StatusTypeDef (), "The status to use." ),
			( "ActionTypeMask", intType, "The key input action type mask." ),
			( "ProcessTimeout", intType, "Timeout in ms to restart processing script if no key was fired" ),
		];
		public DataTypeDefinition ReturnType => voidType;
		public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
			List<string> progress = [];
			return ExecuteSafe ( runtime, args, ref progress );
		}
		public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
			var (status, actType, procTimeout) = runtime.SafeGetVar<
				SCL_StatusType, BasicValueInt, BasicValueInt
				> ( args[0], args[1], args[2] );

			VKChangeMask actionMask = VKChangeMask.None;
			if ( (actType.Value & 1) != 0 ) actionMask |= VKChangeMask.KeyDown;
			if ( (actType.Value & 2) != 0 ) actionMask |= VKChangeMask.KeyUp;
			status.Processor.ListeningActionMask = actionMask;
			status.Processor.ProcessTimeout = procTimeout.Value;
			return voidType.Default;
		}
	}

	public class SCL_ResetListens : SeClav.ICommand {
		private readonly SeClav.DataTypes.SCLT_Void voidType = new ();

		public string CmdCode => "SIP_RESET_LISTENS";
		public string CommonName => "SIP Reset Listens";
		public string Description => "Reset SIP interaction listenings.";
		public int ArgC => 1;
		public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [
			( "status", new SCL_StatusTypeDef (), "The status to use." )
		];
		public DataTypeDefinition ReturnType => voidType;
		public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
			List<string> progress = [];
			return ExecuteSafe ( runtime, args, ref progress );
		}
		public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
			var status = runtime.SafeGetVar<SCL_StatusType> ( args[0] );
			status.Processor.ListeningKeys.Clear ();
			return voidType.Default;
		}
	}

	public class SCL_AddListen : SeClav.ICommand {
		private readonly SeClav.DataTypes.SCLT_Void voidType = new ();
		private readonly BasicValueStringDef stringType = new ();

		public string CmdCode => "SIP_ADD_LISTEN";
		public string CommonName => "SIP Add Listen";
		public string Description => "Add SIP interaction listening.";
		public int ArgC => 2;
		public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [
			( "status", new SCL_StatusTypeDef (), "The status to use." ),
			( "key", stringType, "The key to listen for." )
		];
		public DataTypeDefinition ReturnType => voidType;
		public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
			List<string> progress = [];
			return ExecuteSafe ( runtime, args, ref progress );
		}
		public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
			var (status, key) = runtime.SafeGetVar<SCL_StatusType, BasicValueString> ( args[0], args[1] );
			var keyCode = (KeyCode)Enum.Parse ( typeof ( KeyCode ), key.Value, ignoreCase: true );
			status.Processor.ListeningKeys.Add ( keyCode );
			return voidType.Default;
		}
	}

	public class SCL_FSM_Ternary_Tree_Macro : SeClav.IMacro {
		// It would be nice to be able to define macro inside of SeClav itself. Until that's implemented, we'll predefine some useful macros here.
		public string CmdCode => "SIP_3Tree";
		public string CommonName => "SIP FSM Ternary Tree";
		public string Description => "A macro for defining a simple FSM state with ternary branching (go left / go right / execute / cancel). Mostly usable for Morse-like code transformings";
		public bool SelectRight => true;
		public bool UnorderedGuiders => true;
		public IReadOnlyList<(int after, string split)> guiders => [
			( 0, "-->" ),
			( 1, "-e->" ),
			( 2, "-t->" ),
		];
		public string[] RewriteByGuiders ( ushort flags, (int guiderID, string arg)[] parts ) {
			string KeyCode = null;
			string NextE = null;
			string NextT = null;

			foreach ( var part in parts ) {
				switch ( part.guiderID ) {
				// Apply only last one. Throwing on duplication would be better but who cares. 😑
				case 0: KeyCode = part.arg.Trim (); break;
				case 1: NextE = part.arg.Trim (); break;
				case 2: NextT = part.arg.Trim (); break;
				}
			}

			List<string> ret = [ "--> " + KeyCode + " -n-> Init" ];
			if ( !string.IsNullOrWhiteSpace(NextE) ) ret[0] += " -e-> " + NextE;
			if ( !string.IsNullOrWhiteSpace(NextT) ) ret[0] += " -t-> " + NextT;

			ret.Add ( $"NOP \"{ret[0]}\"" );
			ret.Add ( $"COMPARE_INT SettingChanged 0" );
			ret.Add ( "?> emit n" );
			ret.Add ( "COMPARE_KEYS_3 sip_status DotKey DashKey DelimiterKey" );
			if ( !string.IsNullOrWhiteSpace(NextE) ) ret.Add ( "?A emit e; wait" );
			if ( !string.IsNullOrWhiteSpace(NextT) ) ret.Add ( "?B emit t; wait" );
			ret.Add ( $"?C FIRE_KEY sip_status \"{KeyCode}\" 2" ); // Delimiter, i.e. finish processing
			ret.Add ( "?C emit n; wait" ); // Key fired, finish and jump to start
			return ret.ToArray ();
		}
	}
}