using Components.Library;

namespace Components.Interfaces.Commands;
public class ComponentCommandLoader : ACommandLoader<DMainAppCore> {
	public ComponentCommandLoader ( DMainAppCore owner ) : base ( owner, "dcomponent" ) { }

	private static Dictionary<Type, Func<DMainAppCore, DCommand<DMainAppCore>>> NewCommandList = new () {
		{ typeof( NetworkManagerCommand ), ( core ) => new NetworkManagerCommand ( core ) },
		{ typeof( PasswordManagerCommand ), ( core ) => new PasswordManagerCommand ( core ) },
		{ typeof( TargetManagerCommand ), ( core ) => new TargetManagerCommand ( core ) },
		{ typeof( HookCallbackManagerCommand ), ( core ) => new HookCallbackManagerCommand ( core ) },
		{ typeof( PipelineCommand ), ( core ) => new PipelineCommand ( core ) },
	};

	protected override IReadOnlyCollection<Func<DMainAppCore, DCommand<DMainAppCore>>> NewCommands
		=> NewCommandList.Values.Select<Func<DMainAppCore, DCommand<DMainAppCore>>, Func<DMainAppCore, DCommand<DMainAppCore>>>( f => core => f((DMainAppCore)core) ).ToList();
}