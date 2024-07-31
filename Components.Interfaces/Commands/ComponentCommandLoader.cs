using Components.Library;

namespace Components.Interfaces.Commands;
public class ComponentCommandLoader : ACommandLoader {
	protected override string CmdGroupName => "dcomponent";

	protected override IReadOnlyCollection<Func<ACommand>> NewCommands => new Func<ACommand>[] {
		() => new NetworkManagerCommand (),
	};
}