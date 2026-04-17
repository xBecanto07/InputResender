# InputResender
InputResender is a tool that offers a configurable and expandable system for capturing user input events, processing them, and sending them over the network. The underlying framework is designed to be adaptable to various use cases, related to input processing or not, and to be easily expandable by users with different levels of technical knowledge. The original idea is to provide a tool that will offer conditional complex processing of user input, not only as isolated events, but also as combinations, sequences, or other context. Result of the processing can be used to control this system, be given back to the operating system as new input events, or be sent over the network to control remote machine.

Original inspiration, that can serve as a good use case example, was to be able to write (programming or chatting) on second PC while playing video games on first PC, without the need to switch between them. The project is currently in early stages of development, but this use case is already supported. Also, the infrastructure that supports this use case is sufficiently developed to be expanded by skilled users to almost any other use case.

## Setup and First Steps
Current version supports only Windows out-of-the-box. Custom expansions on other platforms are discussed in the [Development](Docs/Development.md) section.
To get started, head to the **Releases** tab and download the latest release. `Targeted` version requires installed .NET 9.0 runtime, but is smaller in size. `Bundled` version is 'self-contained', meaning it includes .NET runtime, so it should work without any additional setup, but is larger in size. After downloading, simply extract the archive (files are placed under 'InputResender' subfolder) and run the `InputResender.WindowsGUI.exe` file.
Simplest way to get started is to clone the repository, load it in Visual Studio 2022 and run the "InputResender.WindowsGUI" project.

Program accepts two arguments: cfg=&lt;path to config file&gt; and pass=&lt;password&gt;. If path is not provided, it will look for "config.json" in the same directory as the executable (and in solution/projects directories if such structure is detected). If password is not provided, it will be prompted in the console. This password will not encrypt any data, rather it will be used to verify integrity of the configuration file. The default configuration file in this repository is provided with correct hash, under the old trusted "asdf" password.

All written text in the console is visible by default. If you want to hide any sensitive information, end your line with a dollar sign. Following text is masked with asterisks in the console, until you either press number sign (#) to send the input or press again dollar sign to continue writing in plain text. This technique can be used not only when writing password, but any time when using the console.

If you want to see the project in action, check the [Use Cases](Docs/UseCases.md) page for some examples. As a simple demonstration, here is how to set up the project for the original use case of controlling second PC while playing games on first PC (if you're using arrows instead of WASD, as ERT will be captured, otherwise modify the config file accordingly):
1. Download the release 'Bundled' version and extract it on both PCs. (If you are 100% sure that .NET 9.0 runtime is installed on both PCs, you can download 'Targeted' version, but 'Bundled' should work just fine.)
2. Run the `InputResender.WindowsGUI.exe` file on both PCs.
3. Enter 'asdf' as password on both PCs. (To pass the config integrity check. If you modified it, review the printed content and either exit program or type 'update'.)
4. On the second (remote) PC, enter `auto run Client`. (This will execute bunch of commands and shows list of IPs to connect to. If you're in same network, there will probably be something like 192.168.x.x:45256.)
5. On the first (gaming) PC, enter `auto run ServerPrep`. (This is just a preparation for the server as arguments for 'auto run' are not supported yet.)
6. On the first PC, enter `password add XYZ`, where instead of XYZ you enter some password you want. If you want to hide the password type `password add $` + Enter + `XYZ#`. (This will use this password to encrypt the connection between PCs. Network messages are process only when they can be decrypted with the correct password.)
7. On the first PC, enter `target set 192.168.x.x:45256`, where instead of 192.168.x.x:45256 you enter the IP and port of the second PC that was printed in step 4. (This will connect the first PC to the second one.)
8. On the first PC, enter `hook add fast Pipeline Keydown KeyUp`. This will set up the hooks for keyboard input. Now pressing ERT keys will be captured, processed, and sent to the second PC. Other keys should work as normal.
9. On the first PC, switch to the game and try pressing following sequence: E E E E R E R E T E E R E T E E R T T T R. This should result in "hello" being typed on the second PC. (Standard international Morse code is used here, where E is dot, T is dash, and R is letter separator.)

## LLM use in development
First of all, first half of the development (i. e. the overall architecture design, Windows raw input hooks, networking, command processor and few first commands) were done without any LLM assistance. Second half was done with increasing use of LLMs (ChatGPT/LeChat, GitHub Copilot, later on Claude Opus 4.6 agents). This was not due to increased availability of LLMs or complexity of the project. Quite the contrary, many patterns emerged during the development which made it very easy to isolate new task, describe it, and check the result, all that while LLM having many examples of the codebase to learn from.

For peole sceptical about using LLMs, let me describe how I used it in more detail: First, I thought of overall plan for some changes, without typing anything, no LLM involved. If simple enough task, that would not change more than 3 files or multiple files each max 5 lines of changes, and if can be described with sufficient detail within ~2 paragraphs, that would be task for agent. Generated code is checked line by line. Everything, that is not how I would have written it, is rewritten manualy (worth noting that this is less and less common as the number of examples, i.e. other implementation of same pattern, increases). More complex changes are done 'semi-manually', meaning I'm not requesting or reading Copilot suggestion until I have clear picture of how the code should look like (in detail). Keep accepting word by word that fits the picture, write the rest myself (pretty much just a smarter autocomplete). While this is not the fastest way to write code in modern days, I belive that this is a good compromise between the potential of LLMs while not losing touch with both overall picture and the specific details of the codebase, as a developer. I am sometimes wondering, if the changes that I rejected and replaced with my own code, would be better as a lot of those were not necessarily wrong, just not what I would write. But doing such experiments is not worth the time and effort.

I can recommend this approach, and LLMs usage in general, for creating new commands, UI descriptions, SeClav modules, and potentially for new components. By this I mean that with these tasks, the quality of generated code was high enough in my experience. For other tasks, I would not recommend it exactly for this reason, meaning that I would recommend to check it more carefully. But of course, in the end, you do you, good code is a good code.

## License
See [LICENSE](./LICENSE).

Project is for licensed under AGPL-3.0. I believe that code should be publicly available by default (with only few exceptions, like security) and open source contributors should be fairly compensated for their work by anyone who materially benefits from it. My very simple interpretation of AGPL-3.0 is: "If you use this project as part of your code, make that code public! (And reference the authors). If you use this project as user for any commercial purpose (or shipping it as part of a commercial product), pay something to the authors." Not asking for money here for myself, rather for all the people who like to participate in any open source project. There sure are bigger and better projects that sure deserve more support and recognition.

## Menu Navigation
- [README.md](./README.md) — Main page and project overview
- [ProjectOverview.md](Docs/ProjectOverview.md) — Architecture and component system
- [UseCases.md](Docs/UseCases.md) — Common usage examples
- [SeClav.md](Docs/SeClav.md) — Scripting language reference
- [Future.md](Docs/Future.md) — Planned changes and known issues
- [Development.md](Docs/Development.md) — Extending the project
- [Related Thesis](Docs/Thesis.pdf) - Master's thesis related to the project
