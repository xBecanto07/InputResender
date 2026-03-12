using Components.Library;
using InputResender.Services.NetClientService;
using InputResender.Services;
using Components.Library.ComponentSystem;

namespace Components.Interfaces.UI;
public abstract class DUIFactory : ComponentBase<CoreBase> {
	/* Diary isn't part of the git repo, so I'll write the entry here. 😉
	 * ### Wednesday, 2026-02-18
	 * Implementing the UI. As always, the future-proofing is the hardest part.
	 * Paradoxically, the fact that Blazor is on C# and that I can directly access everything in the system makes it even harder.
	 * Before it was limited to only the CLI and what that offered. Now? Everything is permitted. And with great power comes great responsibility.
	 * Ok, joking aside, I'd rather have a system to tell WHAT to show rather than HOW to show it. My main inspiration now is the way Unity's Editor system works.
	 * Component or script developer shouldn't care where is a button, if this is textbox or dropdown, or when or how to update it.
	 * Instead, they should just tell, "Hey, I have a bool, int and two strings here. Make them public!"
	 * My current idea is to have a very generic structure, pretty much a Type-Value pair.
	 * UI constructor would take a look and based on the type would construct some UI. Like checkbox for bool, textbox for string, button for action.
	 * The functionality behind though is a different story. What and when to update? Based on what?
	 * Currently, I have a bunch of Actions there, and we'll see how it goes. Probably will change it later on as the project progresses.
	 * A big thing that I'm scared now is how permissive such system would be. A component also provides UI to anything there? What to use the CLI now for?
	 * One idea I'm playing with is to go all in with it. Let ACommand exist for the more complex stuff and create CLI commands dynamically based on the UI.
	 * UI elements could then be organized into some groups with option for 'batch update/apply' using that complex command.
	 * Where's my imagination lacking right now, is how exactly present those complex commands. 🫤
	 */

	public DUIFactory ( CoreBase owner ) : base ( owner ) { }
	public override int ComponentVersion => 1;

	protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
		(nameof ( RegisterComponentUI ), typeof( void )),
		(nameof ( RegisterCommandUI ), typeof( void )),
		(nameof ( UnregisterComponentUI ), typeof( void )),
		(nameof ( UnregisterCommandUI ), typeof( void ))
	};

	public abstract void RegisterComponentUI ( ComponentBase owner, ComponentUIParametersInfo info );
	public abstract void RegisterCommandUI ( DCommand<DMainAppCore> cmd, ComponentUIParametersInfo info );
	public abstract void UnregisterComponentUI ( ComponentBase owner );
	public abstract void UnregisterCommandUI ( ComponentBase owner );
}