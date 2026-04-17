[Back to main page](../README.md)

# Planned changes
In the past, I tried to use the GitHub Projects feature, I have local Kanban board, I tried many ways to keep track of the planned changes and active development. It works fine until there is some bug that requires change all over the codebase, takes a lot of time, and feature requests are piling up. These more official systems might be updated in the future, but for now, I'm providing basic list of planned changes here.

## New features
- Add support for defining custom macros in SeClav scripts. _Currently macros must be provided by modules._
- Add support for defining custom data structures in SeClav.
- Allow overloading of SCL commands.
- Support parallel execution of multiple states in SCL.
- Allow command to be between its arguments. _There are no real restrictions on command names, allowing to read an operand first would allow simple emulation of traditional infix notation. At this moment, it is planned for parser to remain as a regular language. Support for real infix, with operator precedence, etc. would require much more complex parser and the benefits don't seem to be worth it to me. An option is to allow some 'inline' mode (inspired by LaTeX math mode), where you'd be able to write some filtered C# code (restrictions like banning 'using', 'System.', etc.). This would be parsed and executed as single Func call, so it would both provide complex syntax and good performance._
- More basic datatypes and commands for basic SCL module.
- Better SCL integration into SIP, like allowing to run SCL script after N milliseconds pass after most recent input event.
- Allow calling commands from SCL. _This might be included in bigger overall involving UI automation._


- Support Windows Input API.
- Reading RAW input.
- Expand basic UI descriptions.
- Allow automatic command/SCL modules from UI descriptions. _This would unify the 'UI methods' where as long as the UI description is provided, the commands and SCL modules for interacting with it are automatically generated. Some functionality might remain command or script specific, but all the basic controls would be covered by this._
- Add user groups. _Same idea as is in Linux. Each command or UI element is assigned to some group, and users can be assigned to groups. Main goal isn't security (as in protection from hacker attack) here, but rather to hide complexity from users. Imagine having 'Basic', 'Advanced', and 'Superuser' groups, where for example, 'Basic' users should not be able to whitelist file hashes as they might not understand content of the files and therefore might introduce security risk by 'auto-allowing' files._
- Add UI descriptions for more components/commands. _UI descriptions work nice so far, but only very few things support it._
- Add option for UI description generation from SeClav scripts. _Especially @in variables or maybe even @mapper should be controllable from UI._
- Support parameters for auto-command groups.

## Breaking changes
- Separate InputResender specific code from general purpose code. _This was supposed to be done from the start, but the separation was done at bad spot because the general architecture code was expected to be quite small. So in the future there will be project/project group for component related stuff, command execution etc. Than probably multiple separate projects for specific topics, like SCL, input processing, networking, etc. To improve organization of the codebase, many things should also be renamed (mostly that whole Components.* thing)._

## Known issues
- Using multiple hook probes captures the same input event multiple times. _Probes are not and probably will not be used in real usages, but testing with fully functional probes would be soooo much easier._
- Setting up new hooks requires running AutoHotKey to be restarted. _AHK seems to be working fine together with InputResender, just the hook instalation breaks it until AHK is reloaded. I'm not sure what could be causing this, but I encountered the same to be caused by Red Dead Redemption 2, where launching the game causes AHK to stop working until it is reloaded._
- Sometimes the VTapperInputProcessor sends phantom key press after tap combinations of more than one fingers. Seems to me to be at random._

## Menu Navigation
- [README.md](../README.md) — Main page and project overview
- [ProjectOverview.md](./ProjectOverview.md) — Architecture and component system
- [UseCases.md](./UseCases.md) — Common usage examples
- [SeClav.md](./SeClav.md) — Scripting language reference
- [Future.md](./Future.md) — Planned changes and known issues
- [Development.md](./Development.md) — Extending the project