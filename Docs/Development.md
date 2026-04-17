[Back to main page](../README.md)

# Development Guide

InputResender is designed with simple extesibility and customization as one of the main goals. This guide aims to provide some basic patterns or tutorials for the most important extensability points. I would suggest to first head [down](#menu-navigation) to read through other useful information. Especially [ProjectOverview.md](./ProjectOverview.md) for architecture context, as this document focuses on few practical and specific topics.

## Creating a New Component

**Step 1: Define the interface** — Create a `D*` abstract class in `Components.Interfaces/` inheriting `ComponentBase<CoreBase>` (or `ComponentBase<DMainAppCore>` if it needs core-specific features). Reference `DInterfaceTemplate.cs` as a template.

```csharp
public abstract class DMyComponent : ComponentBase<DMainAppCore> { // Creating a component for a 'DMainAppCore' application.
    protected DMyComponent ( DMainAppCore owner ) : base ( owner ) { } // This will perform automatic registration under 'owner'
    protected override IReadOnlyList<(string opCode, Type opType)> AddCommands () => [
        (nameof(doSomething), typeof(void)) // List of all public methods with their return type. While not implemented yet, this will be used to allow easy and fast Fetch of methods from some component whose type is not known at compile time.
    ];
    public abstract void DoSomething ();
}
```

**Step 2: Create a mock** — Add an `M*` class at the bottom of the `D*` file. This mostly serves for testing, but can be used during runtime. Idea is not to have empty methods in the typical 'mock' style, just to compile it, but rather to have the most basic implementation that will pass generic tests. Good examples can be 'MInputProcessor' which performs simple passthrough of input events, 'MDataSigner' will perform cyclic xor of data and key, 'MLowLevelInput' will reroute request for simulated input to queue of registered hooks, or 'MPacketSernder' will allow comunication between objects in the same process.

```csharp
public class MMyComponent : DMyComponent {
    public override int ComponentVersion => 1;
    public MMyComponent ( DMainAppCore owner ) : base ( owner ) { }
    public override void DoSomething () { }
}
```

**Step 3: Implement the variant** — Create a `V*` class in `Components.Implementations/` implementing the abstract. Reference `ComponentTemplate.cs`.

```csharp
public class VMyComponent : DMyComponent {
    public override int ComponentVersion => 1; // Not implemented yet, but this will allow to provide some backward compatibility
    public VMyComponent ( DMainAppCore owner ) : base ( owner ) { }
    public override void DoSomething () { /* real implementation */ }
}
```

**Step 4: Register** — Add to `CoreCreatorCommand.ExecIner` switch for `core new comp <name>`, or to `DMainAppCoreFactory` for automatic inclusion. Prefer the first option. Automatic inclusion through factory is to be reserved only for components that are critical for the core functionality. If you are creating only new Variant, you can replace the default variant in the factory.

**Step 5: (Optional) Define joiners** — If the component participates in pipelines, register joiners (see below).

Components are expected to provide StateInfo. It is not clear if this will remain, as it could be largly replaced with UI descriptions. Original idea is to capture all information about the component at given time. Many functionalities are too fast to track manually, so capturing series of snapshots for later analysis was useful for development and debugging.

## Creating a New Command

Commands extend `DCommand<DMainAppCore>`. Key elements:
- `CommandNames`: list of console aliases (e.g., `["mycmd"]`)
- `InterCommands`: list of sub-actions with types
- `ExecIner`: implementation using `context.SubAction` switch

```csharp
public class MyCommand : DCommand<DMainAppCore> {
    public override string Description => "Does something useful.";
    private static List<string> CommandNames = ["mycmd"]; // You can also provide multiple aliases here, like ["mycmd", "mc", "doit"]
    private static List<(string, Type)> InterCommands = [("run", null), ("status", null), ("subcmd", typeof(MySubCmd))]; // List of sub-actions. If type is null, you are just letting the user/system know that such sub-action exists. If type is not null, it will be called instead of this main command.

    public MyCommand ( DMainAppCore owner, string parentDsc = null )
        : base ( owner, parentDsc, CommandNames, InterCommands ) {
            RegisterSubCommand ( this, new MySubCmd ( owner, CallName ) );
        }

    protected override CommandResult ExecIner ( CommandProcessor<DMainAppCore>.CmdContext context ) {
        if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
            "run" => CallName + " run: Executes the operation.",
            "status" => CallName + " status: Shows current status.",
            "subcmd" => CallName + " subcmd: Executes the sub-command.",
            _ => null
        }, out var helpRes ) ) return helpRes;

        switch ( context.SubAction ) {
        case "run": return new CommandResult ( "Done." );
        case "status": return new CommandResult ( "OK" );
        default: return new CommandResult ( $"Unknown: '{context.SubAction}'" );
        }
    }
}

public class MySubCmd : DCommand<DMainAppCore> {
    public override string Description => "Sub-command for MyCommand.";
    private static List<string> CommandNames = ["subcmd"];

    public MySubCmd ( DMainAppCore owner, string parentDsc = null )
        : base ( owner, parentDsc, CommandNames, null ) { }

    protected override CommandResult ExecIner ( CommandProcessor<DMainAppCore>.CmdContext context ) {
        return new CommandResult ( "Sub-command executed." );
    }
}
```

Under the 'context' parameter, you have access to multiple useful properties, notably 'Args', which can be used to fetch arguments.
```csharp
// Inform the system that '-i' or '--inline' switch is present. This is not require, but you don't have to check for both versions. Plus, it removes it from the arguments, thus preserving the order of remaining arguments.
context.Args.RegisterSwitch ( 'i', "inline" );

if ( context.Args.Present ("--inline") ) { /* do something */ }
if ( context.Args.HasValue ( "--inline" ) ) {
    // Fetch the data provided with the switch, like '--inline "some content"' => 'some content'
    string inlineContent = context.Args.String ( "-i" );
}
// The .Present or .HasValue methods can be used with long or short switch names. If not registered as switch, they will still work, but you need to use exact name (for example if you have 'inline="some content"').

// Fetch the first argument after the sub-action. Second parameter is for help message. Third parameter is 'required', if true, and the argument is not present, exception will be thrown. Fourth parameter is minimal length of the argument, if provided, and the argument is shorter, exception will be thrown.
string firstArg = context.Args.String ( context.ArgID + 1, "Some first argument", true, 4 );

// You can also automatically parse the argument into int or double. If parsing fails and the argument is required, exception will be thrown. If parsing fails and the argument is not required, null will be returned.
int? number = context.Args.Int ( context.ArgID + 2, "A number argument", false );

// Similarly, parsing into some enum is possible.
KeyCode key = context.Args.Enum<KeyCode> ( context.ArgID + 3, "A key argument", true );

// If you don't want to keep track of the ID of 'first' argument, you can move the context forward. This will also do deep copy of the arguments, so you can keep the original context for some other use if needed. On the other hand, it adds some overhead.
// After reading (hypothetically) a sub-action and 3 arguments, advance the context by 4, so that the next argument to read will be at index 1 again ( context.ArgID + 0 ).
var newContext = context.Sub ( 4 );
```

**Registering:** Add to a loader's `NewCommandList` dictionary. For example in `FactoryCommandsLoader`:
```csharp
{ typeof(MyCommand), ( core ) => new MyCommand ( core ) },
```

## Creating an External DLL

This lets you extend InputResender without modifying original code. See `InputResender.ExternalExtensions/` for an example.

**Step 1:** Create a new .NET 9.0 project referencing `Components.Library` and optionally `Components.Interfaces`.

**Step 2:** Create commands, components, SeClav modules, etc. as needed. Project is not using reflection or doing any dynamic loading, so you need to do your own setup. Best way is to create a command that will create and register your components or perform any other setup you need.

**Step 3:** Create a loader class for all your commands:
```csharp
public class MyExternalLoader : ACommandLoader<DMainAppCore> {
    // For constructor to be usable, it can accept only following parameters: CoreBase, DMainAppCore, DExternalLoader, or ArgParser.
    public MyExternalLoader ( DMainAppCore owner ) : base ( owner, "myExternals" ) { }

    private static readonly Dictionary<Type, Func<DMainAppCore, DCommand<DMainAppCore>>> NewCommandList = new () {
        { typeof( MyCustomCommand ), ( core ) => new MyCustomCommand ( core ) }
    };

    protected override IReadOnlyCollection<Func<DMainAppCore, DCommand<DMainAppCore>>> NewCommands
        => NewCommandList.Values;
}
```

**Step 4:** Build and load at runtime:
```
externals load <path-to-dll> MyExternalLoader
```

For auto-loading, add the command to `config.xml` as an AutoCommand entry. No in-app way to update the list of auto-commands yet, so after adding this manually, you need to restart the app. At the start, integrity check will fail because of the change in `config.xml`. Content of the file will be displayed, so you can review the change, that only your new command is added, and then confirm the change by typing `update` in the console. This will update the hash and allow the app to continue loading.


## Creating Custom Joiners

Joiners connect two pipeline components by converting one's output into the other's input. Signature: `Func<DComponentJoiner, TargetComponent, DataType, (bool success, object result)>`.

```csharp
DComponentJoiner.TryRegisterJoiner<DMySource, DMyTarget, MyDataType> (
    compJoiner, ( joiner, target, data ) => {
        if ( data.IsValid )
            return (true, target.Process ( data ) ); // Joiner is responsible for calling the target component with apropriate data. The result will be returned to the pipeline and can be used as input for the next joiner.
        else
            return (false, null); // If the data cannot be transformed, return false. This will cause the pipeline to skip this joiner and try to find another one, or if there are no more joiners, stop the pipeline execution and return number of successfully processed components up to this point.
    }
);
```

Register joiners in `DMainAppCoreFactory.AddJoiners()`, in a `LoaderCommand` handler, or in the component's constructor. The `load joiners` command triggers `DMainAppCoreFactory.AddJoiners()`.

Once registered, use in a pipeline: `pipeline new MyPipeline DMySource DMyTarget`

## Creating Custom SeClav Modules

SeClav modules bundle data types, commands, macros, and prae-directives. See [SeClav.md](./SeClav.md) for language details.

**Step 1:** Define data types — one `DataTypeDefinition` subclass (type metadata, parsing) and one `IDataType` subclass (holds actual value).
```csharp
public class MyTypeDef : DataTypeDefinition {
	public override string Name => "MyType";
	public override string Description => "Custom data type.";
	public override IReadOnlySet<ICommand> Commands => null; // 'dot' operator not implemented yet

	public override bool TryParse ( ref string line, out IDataType result ) {
        // Implement parsing logic here. If parsing is successful, set 'result' to a new instance of your IDataType and return true. If parsing fails, set 'result' to null and return false.
        result = null;
        return false;
	}
	public override IDataType Default => new MyType ( this, 0 ); // Default value for the type. This is used when declaring variables without initialization.
}

public class MyType : IDataType {
	public DataTypeDefinition Definition { get; }
	public int Value; // Actual data held by this type. Can be anything you need, like a string, a list, etc.
	public MyType ( DataTypeDefinition definition, int value ) {
		Definition = definition;
		Value = value;
	}
	public void Assign ( IDataType value ) {
		if ( value is not MyType v ) // Assigning to correct type should be checked in SCL interperter, but you can also check it here to be sure. If the type is wrong, you can throw an exception or handle it as you see fit.
			throw new InvalidOperationException ( $"Cannot assign value of type '{value.Definition.Name}' to '{Definition.Name}'." );
		Value = v.Value;
	}
    // Following methods are useful to be implemented, but as far as I remember, they are not necessary.
	public override string ToString () => Value.ToString ();
	public override bool Equals ( object? obj ) => obj is MyType v && Value == v.Value;
	public override int GetHashCode () => Value.GetHashCode ();
}
```

**Step 2:** Define commands implementing `ICommand`. Each command needs both `Execute` (release, no guardrails) and `ExecuteSafe` (debug, with logging and type checks).
```csharp
public class MyCommand : ICommand {
	public string CmdCode => "MYCMD"; // Not really needed to be in this assembly-like style, but it is fitting for how it being used.
	public string CommonName => "Add Integers"; // This is used for help messages and such, so it should be human-readable.
	public string Description => "Adds two integer values."; // Description of the command for help messages and documentation.
	public int ArgC => 2; // Number of arguments. Variable count is not supported (yet).
	public IReadOnlyList<(string name, DataTypeDefinition type, string description)> Args => [
		("a", new BasicValueIntDef (), "First integer to add"),
		("b", new BasicValueIntDef (), "Second integer to add")
        // List of arguments. Name, type definition, and description for each argument. This is used for parsing and help messages.
	];
	public DataTypeDefinition ReturnType => new BasicValueIntDef (); // Return type definition. If the command doesn't return anything use 'SCLT_Void' type.
	public IDataType Execute ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args ) {
        // Your 'release' implementation of the command. No checks, just fast execution. Undefined behaviour if something is wrong.
        var (a, b) = runtime.GetVar<BasicValueInt, BasicValueInt> ( args[0], args[1] ); // Fetch arguments as specific type.
		return new BasicValueInt ( a.Definition, a.Value + b.Value );
	}
	public IDataType ExecuteSafe ( ISCLRuntime runtime, IReadOnlyList<SIdVal> args, ref List<string> progress ) {
        // Your 'debug' implementation of the command. This is where you should add checks for argument types, bounds, etc., and log the progress. If something is wrong, you can throw an exception with a descriptive message.

		SIdVal aID = args[0];
		SIdVal bID = args[1];
		IDataType a = runtime.SafeGetVar ( aID ); // Safe version of fetching arguments.
		IDataType b = runtime.SafeGetVar ( bID ); // You can use this version, or the generic as shown in 'Execute'. The generic version is equivalent to doing this with the conversion line below. Generic and this version are both either GetVar or SafeGetVar, depending on which method you are implementing.
		if ( a is not BasicValueInt va )
			throw new InvalidOperationException ( $"Expected integer for argument 'a', got '{a.Definition.Name}'." );
		if ( b is not BasicValueInt vb )
			throw new InvalidOperationException ( $"Expected integer for argument 'b', got '{b.Definition.Name}'." );
		progress.Add ( $" . {va} + {vb} -> {va.Value + vb.Value}" ); // Log the operation performed. This is useful for debugging and understanding how the command works step by step. Also is the biggest drawback in performance. If calling your safe version too often, consider commenting out the logging line.
		return new BasicValueInt ( va.Definition, va.Value + vb.Value );
	}
}
```

**Step 3:** (Optional) Define macros implementing `IMacro`. Macros are text transformations that can be used to create new syntax or simplify complex commands. They are applied before parsing, so they can change the structure of the code. Macros are expected to have some 'guiders', which are specific separators in the code that the macro will look for to identify parts of the input. For example, you could have macro "SUM a + b + c" with guider '+' that will transform it into something like "ADD a b; ADD result c".
```csharp
public class My_Macro : SeClav.IMacro {
    public string CmdCode => "MyMacro";
    public string CommonName => "My Macro";
    public string Description => "Example macro that transforms a line into multiple commands.";
    public bool SelectRight => true; // If true, i-th part is the one after i-th guider. If false, i-th part is what is before i-th guider. For example, with guider '|' and input '| a | b |', if SelectRight is true, parts will be ["a", "b", ""], if SelectRight is false, parts will be ["", "a", "b"].
    public bool UnorderedGuiders => true; // If true, guiders can be in any order, can repeat. Otherwise, guiders must be in the order defined by 'guiders' property, only once, and all must be present.
    public IReadOnlyList<(int after, string split)> guiders => [
        ( 0, "-->" ),
        ( 1, "-e->" ),
        ( 2, "-t->" ),
    ];
    public string[] RewriteByGuiders ( ushort flags, (int guiderID, string arg)[] parts ) {
        // 'flags' is a triplet of 5bit indices for conditional processing. Currently there is no method to convert it back to string representation, but can be done manually. These are the flag requirements that were at the beginning of the original line.
        
        string KeyCode = null;
        string NextE = null;
        string NextT = null;

        foreach ( var part in parts ) {
            switch ( part.guiderID ) {
            case 0: KeyCode = part.arg.Trim (); break;
            case 1: NextE = part.arg.Trim (); break;
            case 2: NextT = part.arg.Trim (); break;
            }
        }

        // Example of transformation. This is taken from macro to simplify writing FSM for Morse code input processing.
        // This macro takes a line like 'E -e-> I -t-> A' and transforms it into multiple commands. First, it defines new state and transitions, then it checks for the input, performs conditional transitions, and finally fires the key event.

        List<string> ret = [ "--> " + KeyCode + " -n-> Init" ];
        if ( !string.IsNullOrWhiteSpace(NextE) ) ret[0] += " -e-> " + NextE;
        if ( !string.IsNullOrWhiteSpace(NextT) ) ret[0] += " -t-> " + NextT;

        ret.Add ( $"NOP \"{ret[0]}\"" );
        ret.Add ( $"COMPARE_INT SettingChanged 0" );
        ret.Add ( "?> emit n" );
        ret.Add ( "COMPARE_KEYS_3 sip_status DotKey DashKey DelimiterKey" );
        if ( !string.IsNullOrWhiteSpace(NextE) ) ret.Add ( "?A emit e; wait" );
        if ( !string.IsNullOrWhiteSpace(NextT) ) ret.Add ( "?B emit t; wait" );
        ret.Add ( $"?C FIRE_KEY sip_status \"{KeyCode}\" 2" );
        ret.Add ( "?C emit n; wait" );

        // The returned array of strings will be injected into the code at the position of the macro, replacing the original line. Can return single line, or multiple. Hypothetically, you could even return empty array to remove the line if you just want to do some processing during compilation, but 'prae' directives are more suitable for that.
        return ret.ToArray ();
    }
}
```

**Step 4:** (Optional) Define prae-directives implementing `PraeDirective` delegate. Prae-directives are executed during compilation, before the actual script is parsed. Access to the compiled data is limited at the moment, but more features will probably be added in the future but some security estimation will be needed first.

```csharp
public void MyPraeDirective ( SCLParsingContext context, ArgParser args ) {
    // This example pre-registers a variable fo given name with some default value. During runtime construction, the provided lambda will be called to get the value. This can be used for example to provide the latest known value of some variable.

    string varName = parser.String ( 0, "Variable Name", shouldThrow: true );
    int varValue = parser.Int ( 1, "Variable Value", shouldThrow: true ).Value;
    ctx.RegisterVariable ( varName, () => new TestValueInt ( testModule.IntTypeDef, varValue ) );
}
```

**Step 5:** Bundle it in `IModuleInfo`:
```csharp
public class MyModule : IModuleInfo {
    public string Name => "MyModule";
    public string Description => "Custom module.";
    public IReadOnlySet<ICommand> Commands => new HashSet<ICommand> { new MyCmd() };
    public IReadOnlySet<IMacro> Macros => new HashSet<IMacro>() { new My_Macro() };
    public IReadOnlySet<DataTypeDefinition> DataTypes => new HashSet<DataTypeDefinition> { new MyTypeDef() };
    public IReadOnlyDictionary<string, PraeDirective> PraeDirectives => new Dictionary<string, PraeDirective>() {
        { "myPrae", MyPraeDirective }
    };
}
```

**Step 4:** Register via `LoaderCommand` in `FactoryCommandsLoader.cs` under the `"sclModules"` case, or from custom command.


## Menu Navigation
- [README.md](../README.md) — Main page and project overview
- [ProjectOverview.md](./ProjectOverview.md) — Architecture and component system
- [UseCases.md](./UseCases.md) — Common usage examples
- [SeClav.md](./SeClav.md) — Scripting language reference
- [Future.md](./Future.md) — Planned changes and known issues
- [Development.md](./Development.md) — Extending the project