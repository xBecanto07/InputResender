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
	public bool IsScriptIntegratable => statusID.HasValue;
	SIdVal? statusID;
	const string StatusVarName = "sip_status";

	public override int ComponentVersion => 1;

	public VScriptedInputProcessor ( CoreBase owner ) : base ( owner ) { }


	public override StateInfo Info => throw new NotImplementedException ();

	public override void ProcessInput ( DataHolder[] inputCombination ) {
		var runtime = new SCLRuntimeHolder ( Script );
		runtime.SetExternVar ( StatusVarName, new SCL_StatusType ( this, inputCombination, runtime ), Owner );
		if ( statusID.HasValue ) {
			runtime.SafeSetVar ( statusID.Value, new SCL_StatusType ( this, inputCombination, runtime ) );
		}
		var runner = new SCLRunner ( Script );
		List<string> progress = [];
		runner.ExecuteSafe ( runtime, [], ref progress );
	}

	public void AssignScript ( SCLScriptHolder script ) {
		if ( script == null ) throw new ArgumentNullException ( nameof ( script ) );
		Script = script;
		if (!Script.IsUsingModule (Components.Interfaces.SeClav.SCL_BasicModule.ModuleName)) {
			Owner.PushDelayedMsg ( "ScriptedInputProcessor: Assigned script does not use SeClav.Basic module. Cannot use any SIP integration. " );
			return;
		}

		try {
			statusID = Script.GetVariableID ( StatusVarName );
		} catch ( KeyNotFoundException ) {
			Owner.PushDelayedMsg ( $"ScriptedInputProcessor: '{StatusVarName}' variable not found in script. Cannot access status from inside the script." );
		}
	}



	public class SCL_Module : IModuleInfo {
		public string Name => "ScriptedInputProcessor";
		public string Description => "A module for scripted processing of input.";

		public IReadOnlySet<ICommand> Commands => new HashSet<ICommand> () {
			new SCL_PrintStatusCmd (),
			new SCL_GetKeyName (),
			new SCL_GetKeyStatus (),
			new SCL_FireKeyCmd (),
		};

		public IReadOnlySet<IMacro> Macros => new HashSet<IMacro> () { };

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
			( "pressed", new BasicValueIntDef (), "1 to press the key, 0 to release." )
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

			bool pressed = pressedVal.Value != 0;
			status.Processor.Owner.PushDelayedMsg ( $"Firing key '{keyVal.Value}' - Pressed: {pressed}" );

			KeyCode key = KeyCode.None;
			try {
				key = (KeyCode)Enum.Parse ( typeof ( KeyCode ), keyVal.Value, ignoreCase: true );
			} catch ( Exception ) {
				throw new InvalidOperationException ( $"Key '{keyVal.Value}' is not a valid KeyCode." );
			}

			InputData ret = new ( status.Processor, key, pressed );
			status.Processor.FireCallback ( ret );
			return voidType.Default;
		}
	}
}