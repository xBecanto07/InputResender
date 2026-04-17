[Back to main page](../README.md)

# Documentation for the SeClav language
SeClav is a simple scripting language designed for the InputResender project. It is used to write scripts that can be dynamically loaded and executed by the project. The language is designed to be fast to interpret, expandable, and easy to check integrity of the scripts.

The term 'language' might might not be the most accurate, as language usually includes some vocabulary. SeClav Language (SCL) provides mainly syntax, the 'vocabulary', i.e. commands, datatypes, etc. is provided by external modules. This allows to both simply expand the language, and to offer some security by restricting the commands that can be used in a script.

## Execution
As with any other functionality in this project, custom component providing alternative functionality can be implemented and used. The default implementation is operating on parsed data to increase performance, but in 'intermediate' format rather than actual binary. This also serves the purpose of analyzing the script before execution. The process is as follows:
1. Script is loaded from file or string
2. Script is parsed line by line into data structures friendly for interpretation.
3. \* Script is presented to user for confirmation. _(Currently not implemented, but planned for future versions)_
4. SCL runtime is constructed, script is assigned, memory is allocated, etc.
5. Initial 0 is pushed into active PC (program counter). _Note that this is already operating on instructions, not lines of code. Each command can be parsed into one or more instructions, like executing one command as one instruction, assigning the result to variable as another instruction._
6. Interpreter will pick first active PC and continue execution.
   1. Test on flags. If some flags are required but such are not set, skip the instruction and move to next one.
   2. Switch on instruction type. If it is some FSM operation, push new PC into active or inactive queue. Potentially terminate current execution loop and trying to pull another active PC.
   3. If opCode is command, fetch arguments, execute the command and store the result if is provided.
   4. Move to next instruction. If there are no more instructions, terminate the current execution loop and try to pull another active PC.

This process might resamble simple CPU architecture, which is by design, as this offers simplicity and performance. SeClav itself looks more like assembly language in its basic form, again by design, as it allows very simple syntax and line by line parsing. But it is still an interpreted script and thus expects that most of the performance heavy operations are done inside of the commands.

SCL does not support typical jump instructions. This is replaced by FSM (Finite State Machine) emulation, although not in any strict way. If no state is defined, the script is executed as one single big script. Otherwise, anything before first state is executed during first run and then the script is controlled by emitting tokens/signals for state transitions. As long as some active state is in queue, the script continues with execution. When script is restarted, any inactive state is moved to active. If no active state is present, the script is restarted from the beginning. This allows surprisingly simple way to do most traditional flow control operations, like loops, ifs, or even parallel execution, critical sections (parallelism not currently implemented though), etc. Thing to keep in mind is that memory doesn't change between states. No scopes are currently implemented nor planned, although eventually blocks/functions/scopes will probably be somehow added. All flags are reset when transition to new state is done.

SCL scripts can be executed with or without safe mode, similar to Debug/Release modes. In safe mode, many checks are added to ensure that the scripts work as expected. Examples of such are checks for index bounds, correct types of arguments and assignments. To be more specific, the interpreter 'engine' itself has very few changes as it doesn't now what commands, datatypes, etc. are. The checks are done either in the runtime (with things like memory management) or inside the commands themselves. Additionally, in safe mode, logging is enabled, so after execution, the caller recieves a string array with all the information that was provided by interpreter, runtime, and commands.

## Syntax
First construct of SCL are prae directives. Resembling directives in C language, they are used to execute code during compilation. Starting with '@' sign. They will mostly be defined in external modules, with only following 5 being built-in:
```SeClav
@using ModuleName
	# Load all datatypes, methods, prae directives and others from the module
@in Type VarName
	# Marks existance of external variable of given type and name. Value must be provided by caller of the script. Like an argument of a function. Value doesn't need to be accessed. If caller sets variable which wasn't marked as @in, it will be ignored without exception. All @in variables must be set by caller, otherwise an exception will be thrown. If caller sets variable which was marked as @in, but with wrong type, an exception will be thrown.
@out Type VarName
	# Defines an output variable. Value of all @out variables will be assigned by the script.
@mapper TypeOut Name : TypeIn
	# Define existance of external dictionary. Effectively function mapping InT=>OutT. Same as @in, all @mappers must be assigned before script is started
@extFcn TypeOut Name : <Argument Types>
	# Defines existance of external function. Expanded @mapper with simillar functionality, but allowing multiple input arguments
```

Following constructs are supported in a SCL scripts:
```
	# Comment
	@Prae Directive
	DataType name [= value]
	name = value
	Command <arguments>
	Macro <code|separators>
```

Macro might not be distingueshed from a command call by the programmer. It changes a given line into any code as pure text modification. Macros can require a 'separators' to define group for the text transformations needs. Example of macro could be:
```
SUM a + b + c + d...
```
The 'SUM' name will initiate the transformation and the plus signs are only separators. This would transform the code into multiple 'ADD_INT' commands.

Conditional execution can be done by prefixing a line with question mark followed by mask bit identifier. Those can be either uppercase hexadecimal digit or one of the alternatives:
```
?N   # No mask is set
?!   # Last 'if' result
?~   # Complement of last 'if' result
?=   # Equals
?>   # Larger
?<   # Smaller
```

## Available modules
Please note that the list here about all modules that are either used or defined in project. For other modules, please search the web or ask friends 😉 Also not all modules can be directly loaded via a command call. For example modules used only for unit testing are also present, mostly to provide an example of what can be used, added, etc. These modules will be marked with asterisk mark.

Module contents are marked as follows:
- ⊕ - Command (argumetns) [→ mask(effected bits)]
- & - Datatype class → Type name
- ∂ - Macro := Example usage
- @ - Prae (arguments)

**\*Internal**
- ⊕ set (Any target, Same value)
- ⊕ throw ()
- & SCLT_Any -> N/A (similar to generic type or 'object' in C#)
- & SCLT_Same -> N/A (any type, must be the same as previous 'SCLT_Any' in the same command)
- & SCLT_Void -> VoidData (no type, used for functions that don't return anything)
- ∂ EmitMacro := emit tokenForNextState

**\*SCL_TestModule**
- ⊕ ASSERT_EQ (Any var1, Same var2)
- ⊕ APPEND_INT_TO_STR (TestString str, TestInt i)
- ⊕ ADD_INT (TestInt a, TestInt b)
- ⊕ CONCAT_STR (TestString s1, TestString s2)
- ⊕ AppendIntToString
- ⊕ SET_FLAG (Int flag)
- ⊕ RESET_FLAG (Int flag)
- ⊕ READ_FLAGS ()
- ⊕ COMPARE_INT (TestInt a, TestInt b) → mask(= > <)
- & TestValueIntDef → TestInt
- & TestValueStringDef → TestString
- ∂ SetResetMultipleFlags := SET_RESET_FLAGS - 1 - 6 + 5 + 7
- ∂ JoinStrings := JOIN_STRINGS concatResult = Flags: | 1|,2|,6|
- ∂ AddOrAppend := ADD_OR_APPEND -> result a . s . d . 40 + 2

***SCL_BasicModule**
- ⊕ COMPARE_INT (Int a, Int b) → mask(= > <)
- ⊕ APPEND_INT_TO_STRING (String str, Int i)
- ⊕ ADD_INT (Int a, Int b)
- ⊕ CONCAT_STR (String s1, String s2)
- ⊕ READ_FLAGS ()
- & BasicValueIntDef → Int
- & BasicValueStringDef → String

**ScriptedInputProcessor**
- & SCL_StatusTypeDef → SIP_Status_t
- ⊕ PRINT_SIP_STATUS (SCL_StatusTypeDef status)
- ⊕ GET_SIP_KEY_NAME (SCL_StatusTypeDef status, Int id)
- ⊕ GET_SIP_KEY_STATUS (SCL_StatusTypeDef status, String name)
- ⊕ FIRE_KEY (SCL_StatusTypeDef status, String key, Int pressed)

## Menu Navigation
- [README.md](../README.md) — Main page and project overview
- [ProjectOverview.md](./ProjectOverview.md) — Architecture and component system
- [UseCases.md](./UseCases.md) — Common usage examples
- [SeClav.md](./SeClav.md) — Scripting language reference
- [Future.md](./Future.md) — Planned changes and known issues
- [Development.md](./Development.md) — Extending the project