[Back to main page](../README.md)

# Use Cases

Practical examples of common tasks with InputResender. Each section provides either console commands or config.xml snippets. For architecture context, see [ProjectOverview.md](./ProjectOverview.md). For extending the system, see [Development.md](./Development.md). Or examples from this page are already defined in the default `config.xml` so you don't need to edit it and deal with integrity failures, just run the corresponding auto-command group with `auto run <GroupName>`.

### Getting Started
On first launch, the program looks for a configuration file (default: `config.xml`). If not found, it will print error and exit. In such case, please start the program with `cfg=path/to/config.xml` argument, or place a config file in the same directory as the executable. Integrity check is performed on the config file using a hash and password. The default config file uses "asdf" as password. If you did not change the config file, integrity should be verified successfully.

While you can use the default password, you can change it to prevent others from starting the program without your permission. 

### Initial Setup
> $> auto run initCmds

Every session starts with loading components and setting up a core. The `initCmds` auto-command group handles this:

```
loadall                         # Load all command loaders recursively
clear                           # Clear screen
safemode on                     # Enable try-catch wrapper for commands
core new                        # Create a fully featured DMainAppCore
core migrate act own skipSame   # Move unique components from default core to new core
core destroy                    # Destroy the now-empty default core
core activate                   # Set the new core as active
loglevel all                    # Enable all log messages
```

In `config.xml`, set `<AutostartName>initCmds</AutostartName>` to run these automatically at launch.

Note: You can load individual commands one by one as needed, and add specific components instead of migrating the whole default core. The above is the convenient "get it all at once" approach. But commands like 'help' or 'core list' to print all commands or components might become quite cluttered with too many loaded commands, so feel free to load only what you need.

### Capturing and Printing Input Events (Windows)
> $> auto run printWinKey

Capture keyboard events using Windows low-level hooks and print them to console:

```
#auto run initCmds              # Standard setup (if not auto-started)
windows load --force            # Load Windows-specific DLowLevelInput (overrides mock)
windows msgs start              # Start Win32 message loop (required for hooks)
hook manager start              # Start the hook manager
hook add Print Keydown KeyUp    # Register a hook that prints KeyDown and KeyUp events
```

Config XML equivalent:
```xml
<printWinKey>
    <W0>windows load --force</W0>
    <W1>windows msgs start</W1>
    <W2>hook manager start</W2>
    <W3>hook add delayed Print Keydown KeyUp</W3>
</printWinKey>
```

### Simulating Input
> $> auto run HelloWorld

Send simulated keyboard events to the OS:
This will execute the command 'print hello' that should print the text "Hello" into a console.

```
windows load --force
hook manager start
sim keypress P R I N T   # Simulate writing "print" (key down + up for each key, short delay between)
sim keydown Space        # Simulate pressing Space key down
sim keyup Space          # Simulate releasing Space key
sim keypress H E L L O   # Simulate writing "hello"
sim keypress Enter       # Simulate pressing Enter key

```

### Server-Client Architecture

Send keyboard input from one machine to another over the network.

**Shared setup** (used by both server and client, called automatically by their respective auto-command groups):
```
windows load --force                # Load Windows-specific components
windows msgs start                  # Start Windows message loop (required for hooks)
core new comp packetsender          # Create new DPacketSender component under active core 
network callback recv print         # Print new network connections to console
hook manager start                  # Start the hook manager, this is required not only for hooks, but also for simulated input
load sclModules                     # Load SeClav modules for scripting support
load joiners                        # Load pipeline joiners
SIP force                           # Override any DInputProcessor with VScriptedInputProcessor to call SeClav scripts for input processing
```

**Client** — Receives encrypted data, decrypts, and simulates input locally:

> $> auto run Client

Client should be started first to listen for incoming connections. Remember to use custom password with `password add <YourPassword>` command to replace the out-of-the-blue picked default one. Auto-command ens with printing all connectable endpoints to console, so you don't need to search for IP and port yourself.

There could be problem with firewall. This needs to be fixed manually at the moment by allowing the program to listen for incoming connections on the port specified in server's target (default: 45256).

```
auto run Shared                     # Run shared setup
pipeline new Recver NetworkCallbacks DDataSigner DInputSimulator
                                    # Create pipeline that receives network data, decrypts them, and simulates input
network callback recv pipeline      # Instruct network manager to call pipeline on receiving new data
password add blue                   # Set password (must match server)
network hostlist                    # Print all connectable endpoints to console
```

**Server** — Captures input, processes via SeClav script (Morse code), encrypts, and sends:

> $> auto run ServerPrep

Passing arguments to auto-commands is not supported yet, so either modify the config file with your desired target IP and password and then run `auto run Server`. If you do not want to modify the config file, you can run the 'ServerPrep' and enter the last three commands manually in console with your desired target IP and password (must match client).
```
auto run Shared                       # Run shared setup
pipeline new ReadInputAndProcess DInputReader DInputMerger VScriptedInputProcessor
                                            # Create pipeline that captures input and process it for a SeClav script
pipeline new Sender DInputProcessor DDataSigner DPacketSender
                                            # Create pipeline that encrypts processed input and sends to client over network
fm whitelist MorseCode.scl XAM5z0P9X1MwRmYLHQI/yP6MM5lB2StteLqaCWAAfwA=
                                            # Whitelist the SeClav script for integrity check (hash must match the file content and password)
seclav parse MorseCode.scl            # Parse a SeClav script that processes input events and interpret them as Morse code, outputting letters (E=dot, T=dash, R=letter separator)
SIP assign MorseCode.scl              # Assign the parsed script to the processor, so it will be called for every input event

password add blue                     # Set password (must match client)
target set 192.168.72.12:45256        # Connect to the client (replace with your client's IP and port)
hook add fast Pipeline Keydown KeyUp  # Capture input and send to pipeline for processing and sending to client
```

Both sides must use the same password (`password add blue`). The server sets a target IP; the client listens for incoming connections.

There are 'fast' and 'delayed' versions of hooks. Fast hook is processed immediately and can consume the event (prevent it from being passed to other applications). Delayed hook is processed in separate thread, cannot consume the event, but will not cause hook to disconnect if processing takes too long. Unless you're doing some heavy processing or waiting, fast hook should be fine. Otherwise, you can use delayed hook together with `hook add fast filter` to consume some events if you want to. 

### Tapper Input (Chorded Keyboard)
> $> auto run tapperRun

Component VTapperInput was inspired by the TapStrap device. This allows you to use only 5 keys to produce a full keyboard by mapping simultaneous key presses (chords) to output keys. It serves as an example of more complex processing of input via dedicated component.

You can send this processed input to remote machine over network as in the previous example, or you can use it locally as will be shown here. Although keys (A S D F Space) are used in this example, idea is that if you have extra 'programmable' keyboard, you can setup 5 of its keys to F13-F17 and use those. This will leave your regular keyboard free for normal use, and you can use the tapper keys for special functions, pair programming, different language input, etc.

Please note that at this moment, only 'normal' keys are supported, so no modifiers (Ctrl, Alt, WinKey, ...) or non-button characters (like 3/#, ě, ß, 😀, ...). Currently it emulates a '[low level keys](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes)', as if you press that button on keyboard. This also means it is subject to keyboard layout and language settings, modifiers like CapsLock, etc. Other means of input are planned for the future.

**Setup** — Assign 5 trigger keys + 2 modifier keys, then define mappings:
```xml
<tapper>
    <ta00>tapper force A S D F Space CapsLock Scroll</ta00>
    <ta01>tapper wait 600</ta01>
    <ta02>tapper conditioon None</ta02>
    <ta03>tapper mapping Single X---- T</ta03>   <!-- A only → T -->
    <ta04>tapper mapping Single -X--- A</ta04>   <!-- S only → A -->
    <ta05>tapper mapping Single --X-- E</ta05>   <!-- D only → E -->
    <ta06>tapper mapping Single ---X- I</ta06>   <!-- F only → I -->
    <ta07>tapper mapping Single ----X O</ta07>   <!-- Space only → O -->
    <ta08>tapper mapping Single XX--- D</ta08>   <!-- A+S → D -->
    <ta13>tapper mapping Single -X-X- R</ta13>   <!-- S+F → R -->
    <ta16>tapper mapping Single --X-X Space</ta16> <!-- D+Space → Space -->
    <ta22>tapper mapping Single XXX-X Enter</ta22> <!-- A+S+D+Space → Enter -->
    <!-- ... more mappings ... -->
</tapper>
```

The `tapper force` will again replace any DInputProcessor with VTapperInput component. The 5 trigger keys (A S D F Space) will be monitored and their combinations will be mapped to an output key. The modifiers (CapsLock and Scroll) will be sent when switching between special Shift/Switch layers, allowing you to have more mappings without needing more trigger keys. In total, there are 5 layers (Single press, Double press, Triple press, Shift, and Switch), so you can have up to 5 different outputs for each combination of trigger keys. Other means of combinations are planned for the future as well, like mapping pairs of subsequent key presses (e.g. ASCII/UTF8 writer: -X-X- + -XXX- = 0x57 = 'W').

Mapping format: `tapper mapping <Layer> <Pattern> <OutputKey> [Modifier]`
- Pattern: 5 characters, `X` = pressed, `-` = not pressed, matching the 5 trigger keys
- Layer: `Single`, `Double`, `Triple`, `Shift`, `Switch`
- OutputKey: Any key that can be simulated by the system (currently low-level keys, but more are planned, see [KeyCodes enum](../Components.Library/KeyCodes.cs))
- Modifier: Mask of either real modifiers (like Ctrl, Alt, ...) or virtual modifiers (0-27). This modifier mask is sent together with the output key, but is not currently used anywhere (as far as I remember). But it is one of the planned featers, or you sure can use it in your own processing.

**Running the tapper:**
```xml
<tapperRun>
    <tr0>auto run Shared</tr0>
    <tr1>auto run tapper</tr1>
    <tr2>hook manager verbosity 0</tr2>
    <tr3>tapper verbosity 0</tr3>
    <tr4>pipeline new LocalCallback DInputProcessor DInputSimulator DInputSimulator</tr4>
    <tr5>pipeline new ReadInputAndProcess exact=SHookManager DInputMerger VTapperInput origin</tr5>
    <tr6>hook add Fast Pipeline Keydown KeyUp</tr6>
</tapperRun>
```

Note the 'exact=SHookManager' argument. When creating a pipeline, it automatically checks that such component exists and is available. But 'SHookManager' does not exist until a hook is created. We could first create the hook, then the pipeline, but when entered manually, writing the pipeline command would already be picked up by the hook. So we can use 'exact' specifier to tell the pipeline we guarantee that such Variant will be available by the time the pipeline is executed. At the end there is also 'origin', which forces the pipeline to use the exact instance of a component that started the pipeline (updated each time the pipeline is started).

These are the available specifiers for component arguments in pipelines:
- origin — the component that started the pipeline
- exact=<variant> - the component with the exact variant name (e.g. 'exact=SHookManager'), without validity check
- id=<component id> - the component with the specified id (for example what you get from 'core list' command), without validity check
- def/definition=<component definition> - the component with the specified definition (e.g. 'def=SHookManager' or 'definition=SHookManager'), with validity check (slightly more strict than default option without specifier)
- reflection=<type name> - find type by name via reflection, no checks, attempt to fetch valid component only when pipeline is executed

### Loading External Library
> $> auto run externClipboard

Load an external library (DLL), call command loader in it, and test the loaded command by printing help message for it. In this example, the library provides command for interacting with Windows clipboard.

```
fm whitelist InputResender.ExternalExtensions.dll 3NVdPEy9AHI/R+ATG9aLtdXak13KgW8FiElkXLzq81k=
                                    # Whitelist the external library for integrity check
externals load InputResender.ExternalExtensions.dll ExternalClipboardLoader
                                    # Load the library and create new instance of main loader class in it
load-cmd-extClip                    # Execute external command loader to load its commands into the system
clipboard -h                        # Print help message for the loaded command to verify it works

clipboard getText                   # Get current clipboard text and print it to console
clipboard setText "Hello, World!"   # Set clipboard text to "Hello, World!"
clipboard getText                   # Get update clipboard content, should print "Hello, World!"
```

### Config XML Structure

The configuration file (`config.xml`) has the following structure. First line is a hash for integrity verification.

```xml
hash-of-file-content
<Config>
    <AutostartName>initCmds</AutostartName>
    <PrintAutoCommands>T</PrintAutoCommands>
    <MaxOnelinerLength>125</MaxOnelinerLength>
    <ResponsePrintFormat>Normal</ResponsePrintFormat>
    <AutoCommands>
        <GroupName>
            <C0>first command</C0>
            <C1>second command</C1>
        </GroupName>
    </AutoCommands>
</Config>
```

| Field | Values | Description |
|-------|--------|-------------|
| `AutostartName` | Any group name | Auto-command group executed at launch |
| `PrintAutoCommands` | `T` / `F` | Show individual auto-commands in console |
| `MaxOnelinerLength` | Integer | Max chars for merging command+result on one line |
| `ResponsePrintFormat` | `None`, `Batch`, `ErrOnly`, `Normal`, `Full` | Console output verbosity |

Tags within a group are sorted **lexically**, so use `C0`, `C1`, ... or `a0`, `a1`, ... to control execution order. If you use same tag name for all the commands in a group, it should follow the order in the file.

Run any group manually: `auto run <GroupName>`. List all groups: `auto list`.

### Other Useful Commands
- `help` - shows list of available commands
- `argParse <text>` - debugging tool, shows how the provided text would be parsed into arguments for a command
- `core list` - shows all components in the active core
- `pwd` - print current 'home path', i.e. the root directory for file operations
- `core typeof <component definition>` - shows of what specific variant is provided component
- `Blazor start` - starts a Blazor web server for GUI (in early stages of development)
- `conns list` - shows all active network connections
- `conns send #0 Hello` - sends "Hello" message to the first connection in the list (index starts at 0)
- `conns close #0` - closes the first connection in the list

## Menu Navigation
- [README.md](../README.md) — Main page and project overview
- [ProjectOverview.md](./ProjectOverview.md) — Architecture and component system
- [UseCases.md](./UseCases.md) — Common usage examples
- [SeClav.md](./SeClav.md) — Scripting language reference
- [Future.md](./Future.md) — Planned changes and known issues
- [Development.md](./Development.md) — Extending the project