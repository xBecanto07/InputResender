# Input resender components


1) **<u>DLowLevelInput</u>** - Contains actual low level access to input devices, both input simulation and input hooks.
2) a) **<u>DInputReader</u>** - Provides high level access to input hooks. Abstracts specific system implementation, doesn't hide different input types (like keyboard, mouse, gamepad, etc.) but provides unified interface to access them. (e.g. VKeyboardInputReader, VMouseInputReader))
   b) **<u>DInputSimulater</u>** - Compomentary component for DInputReader to programmatically simulate input events.
3) **<u>DInputParser</u>** - Provides Input event handler that takes the HInputEventDataHolder and combines it based on defined rules (e.g. VTapperInput)
4) **<u>DInputProcessor</u>** - High level input management. Provides callback for one or more correlating input events to execute high level actions (don't see proper example yet)
5) **<u>DTextWriter</u>** - Simple text editor that is controled by InputData (i.e. output of DInputProcessor).