using Components.Library;

namespace Components.Interfaces.Commands;
public class ComponentCommandLoader : ACommandLoader {
	public ComponentCommandLoader () : base ( "dcomponent" ) { }

	private static Dictionary<Type, Func<ACommand>> NewCommandList = new () {
		  { typeof(NetworkManagerCommand ), () => new NetworkManagerCommand () },
		  { typeof(PasswordManagerCommand ), () => new PasswordManagerCommand () },
		  { typeof(TargetManagerCommand ), () => new TargetManagerCommand () },
		  { typeof(HookCallbackManagerCommand ), () => new HookCallbackManagerCommand () },
	};

	protected override IReadOnlyCollection<Func<ACommand>> NewCommands => NewCommandList.Values;
}