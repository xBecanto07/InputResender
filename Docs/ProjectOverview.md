[Back to main page](../README.md)

# Project Overview

The InputResender consists of a modular Core/Component 'framework' and a set of components for input capture, simulation, and processing, network communication, scripting, and more. Aim is to provide a flexible and easy-to-expand system for user input processing. Solution is organized into multiple layers, where individual elements should rely mostly on the layer below, keeping high level of independence across the same layer.

This document is meant to provide some high-level overview of the project to allow easier understanding and navigation of the codebase. Not an official documentation, but this should give enough context for a skilled developer to quickly utilize this code for their own needs.

> **Note:** This project is under active development with many bigger changes planned ahead. Overall architecture should not change drastically, but some reorganization and renaming is expected, especially in the lower layers (Components, Services). See [Future.md](./Future.md) for planned breaking changes.

## Architectural Layers
Higher layers depend on lower layers. Communication and dependency across one layer should be kept to minimum.

```
   InputResender.WindowsGUI           (top-layer, Windows entry point)
          │
   InputResender.OSDependent.Windows  (platform-specific: hooks, Win32 API)
   InputResender.WebUI                (Blazor GUI)
          │
   InputResender.CLI                  (OS-independent API/CLI)
          │
   Components.Implementations         (V* concrete variants)
          │
   Components.Interfaces              (D* abstract definitions, M* mocks, SeClav engine)
          │
   InputResender.Services             (various useful services like networking, file access, ...)
          │
   Components.Library                 (CoreBase, ComponentBase, CommandProcessor, data structures)
```

## Component-Core Architecture

A **Core** (`CoreBase`) is agregator and container of components. Those are register into one specific core instance and can be fetched by type.

Fetching: `core.Fetch<DInputReader>()` finds the highest-priority component whose `TypeTree` contains `DInputReader`.

Migration: `this.ChangeOwner(newCore)` or `originalCore.PassComponentTo(component, newCore)` moves a component between cores. This changes stored owner reference and updates component sets in both cores. Components should never be cloned or registered into multiple cores.

## D/V/M Naming Convention

| Prefix | Meaning                            | Location | Example |
|--------|------------------------------------|----------|---------|
| `D` | Definition (abstract interface)    | `Components.Interfaces` | `DInputProcessor` |
| `V` | Variant (concrete implementation)  | `Components.Implementations` | `VInputProcessor` |
| `M` | Mock (trivial functioning version) | Bottom of `D*` files | `MInputProcessor` |

It is by design that Definitions are abstract classes, not interfaces. It allows to provide some shared functionality, store some common properties, but most importantly, it serves as a great restriction for derived Variants. While everything could be done with interfaces, and it would be more flexible, it would also be more tempting to proclaim classes as components left and right, cluttering the system with many classes that don't really fit the role. By using abstract classes, developer is forced to think how to implement the component and how to connect it with the rest of the system.

Another restriction is introduced with the generic version of ComponentBase. Provided generic type is some specific Core that should be specific to a given application. Currently, it is `DMainAppCore`, which in the future should be renamed to something like `DInputResenderCore`, or at least create such derived class. This way, you can restrict the component to be used only in a specific core, which now can provide some application-specific functionality. Simple example can be required components, created during construction of the core, that are (somewhat) guaranteed to be present in the core, accessible by premade getter. This generic ComponentBase also requires reference to new owner, under which it is automatically registered when created. This guarantees that the Fetch method (and all the core related functionality) is always available.

## Data Flow
There are two main routes for data flow: Fetch/Call and Pipelines. All the components should be really treated as dynamic modules that can be easily added, removed, and replaced, during runtime. Storing direct references to other components should be avoided as much as possible. Instead, you should always perform new fetch of a definition you want to interact with. Avoid fetching variants directly, unless you depend on that specific implementation. Since components can be migrated between cores or replaced by another variant, storing references can easily lead to hard-to-debug issues. To be clear, the Fetch method is not thread-safe, but compoents should not be changed that often and that randomly for this to be a problem.

Best option is to use the generic `Fetch<T>()` method, which will find the highest-priority component that can be used as `T`. If you don't have reference to the desired type (e.g., you rely on another DLL that will be loaded at runtime), you can also fetch a ComponentBase based on common name or name of the variant, and other properties. This is more error-prone and harder to maintain, but can be useful in some cases.

**Pipelines**

The hard-coded link, resulting from fetching a component, is not always desirable. To provide flexible data flow, there are pipelines. The simple idea is: when you create some data that you don't want to dictate yourself how to process, you call `DComponentJoiner.TrySend(this, (potential target definition), data);`. This will attempt to execute registered pipeline from the calling component to target component, or anyone if not specified. Individual hops of the pipeline are called **joiners**. Separately registered little functions that know how to use or convert some incoming data and pass them to one specific component. This way, user or a higher level code can simply define sequence of component definitions, without worrying about how to connect them, during runtime.

While more sophisticated system is planned for the future, currently component only needs to manually start he pipeline (via `TrySend`) and optionally register joiners to allow receiving data. Joiners can be registered anywhere in the project, the 'proper' place is an open question at this point. The clean way could be to offer command to register joiners on call, or do it during component construction and hope that `DComponentJoiner` is already available. Joiners should be kept as simple as possible. You don't have to worry about exceptions, as execution of pipeline is encapsulated in try-catch block. Here is a template for a joiner function:
```csharp
DComponentJoiner.TryRegisterJoiner<DInputComponent, DOutputComponent, DInputType>
  ( dComponentJoiner, ( executor, outputComp, msg ) => {
    if (bad data in msg) return (false, null);
    else return (true, outputComp(msg));
  } );
```

Normally, you shouldn't need to interact with the data directly from inside a joiner, just accept it and pass to the target component. Some processing is although needed for some components. Especially for input processing, there are multiple data types that represent different stages of the processing. To avoid confusion, here is a quick overview of the data flow in the structures related to input processing pipeline:

1. **`HInputData`** — Platform-specific raw data (e.g., `WinLLInputData`). Created by `DLowLevelInput`.
2. **`HInputEventDataHolder`** — Platform-independent event data (InputCode, pressure values, deltas). Created by `DInputReader`.
3. **`HInputEventDataHolder[]`** - Group of events that are related to first one (e.g. already held down keys). Created by `DInputMerger`.
4. **`InputData`** — High-level command representation. Created by `DInputProcessor`.
5. **`HMessageHolder`** — Network envelope around binary data. Created by `DDataSigner`.

Example of pipeline to send input over network: `DLowLevelInput` → `DInputReader` → `DInputMerger` → `DInputProcessor` → `DDataSigner` → `DPacketSender`

Example of pipeline to process input locally: `DLowLevelInput` → `DInputReader` → `DInputMerger` → `DInputProcessor` → `DInputSimulator` → `DInputSimulator`

Joiners are registered in `DMainAppCoreFactory.AddJoiners()` via `DComponentJoiner.TryRegisterJoiner<Source, Target, DataType>()`. They convert one component's output into another's input.

## Command System

Commands extend `DCommand<CoreT>` and are grouped in **loaders** (`ACommandLoader<CoreT>`). The loader hierarchy:

```
TopLevelLoader (WindowsGUI)
 ├─ FactoryCommandsLoader (CLI)
 │   ├─ CoreManagerCommand (sub-command under "core")
 │   ├─ ConnectionManagerCommand
 │   ├─ AutoCmdsCommand
 │   ├─ LoaderCommand ("load sclModules", "load joiners")
 │   └─ ...
 ├─ InputCommandsLoader
 │   ├─ HookManagerCommand
 │   ├─ InputSimulatorCommand
 │   ├─ ScriptedInputProcessorCommand
 │   └─ VTapperInputCommand
 ├─ SeClavCommandLoader
 │   └─ SeClavRunnerCommand
 └─ WindowsCommands (OS-specific)
```

Commands are loaded at runtime via `loadall` or individually via `load-cmd-<group>` (e.g., `load-cmd-generalCmds`).

Commands are used for basic console UI and for a shared (external and internal) API. Idea here is that whatever some high level code can do, user can also do via command line, and vice versa. Therefore it is recommended to rather implement some functionality as a new command and then call it via CommandProcessor, instead of executing it directly.

Command are usually located in the same layer as the functionality they provide, under a `Commands` subfolder. They should be designed as generic as possible, operating either on a Definition or very specific functionality of a Variant. All commands have static lists `CommandNames` and `InterCommands` that define their callnames and any potential sub-commands. This allows automatic testing. All commands offer help text when called with one of [`-h`, `?`, `-?`, `--help`].

Every command is also a component, so it can be fetched, is registered into a core, and has 'Owner' property, allowing to easily access other components. This also means that commands can be part of pipelines.

CommandProcessor is a component responsible for managing commands and their execution. This is the only exception to "don't store component references" rule. This should change in the future, but such change is not planned in near future. Since commands are one single Variant (at least so far), changing them is not expected and storing their reference in CommandProcessor is not expected to cause issues. CommandProcessor is capable of parsing text input, finding the right command instance, and executing it with provided arguments.

CommandProcessor also allows for dictionary-based storage for data, that can be used throughout the project. Most important use is `CoreManagerCommand.ActiveCoreVarName`, which stores reference to the currently active core, allowing interaction with a different core, other than the one a given command is registered to. While not currently used, this option highlights the flexibility of the architecture, focus on thorough separation of component groups, and dynamic existence of components.

## UI
Since the project is meant to be as platform-independent and customizable as possible, there are only UI descriptions. Those are simple data structures that hold necessary information to create working UI element. You can imagine it like very basic HTML. There are multiple basic UI elements provided, like button, text input, dropdown, etc. These are created by components when they want to provide some UI. 'DUIFactory' component will be either subscribed to ask new registered components for their UI descriptions, or will be crawling through own core to find them. This aggregated list can be used in other component (like VWebServerBlazor) to create actual UI. Blazor is currently the official UI solution, but with the generic nature of the UI descriptions, other solutions can be easily implemented and used instead.

At this moment, UI is purely functional, without almost any styling options. UI elements can be grouped into 'ComponentUIParametersInfo', that can also contain itself, allowing for nested UI. Besides that, all the styling is desided by the component responsible for presenting the UI, which is currently VWebServerBlazor. As this is recently added feature, current phase is pushing the limits of this simple system, how nice and functioning UI can be infered from the basic descriptions. Some styling options will probably be added in the future, but it still is not clear how to do it without sacrificing simplicity and portability.

Please note, that 'UI' is being used here, not a 'GUI'. While various forms of graphical UI are probably main reason for this system, the UI descriptions are not limited to graphical interfaces. For example, it is planned to expand the CLI to make use of the UI descriptions, reducing need for separate commands and unifying some interactions. Point is, if the sole goal would be graphical UI, components would be providing their Blazor code directly, instead of describing the UI in a generic way. This way, the UI can be used in various ways, and the components don't need to care about how their UI is presented.

## Menu Navigation
- [README.md](../README.md) — Main page and project overview
- [ProjectOverview.md](./ProjectOverview.md) — Architecture and component system
- [UseCases.md](./UseCases.md) — Common usage examples
- [SeClav.md](./SeClav.md) — Scripting language reference
- [Future.md](./Future.md) — Planned changes and known issues
- [Development.md](./Development.md) — Extending the project