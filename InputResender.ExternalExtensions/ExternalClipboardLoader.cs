using System;
using System.Collections.Generic;
using Components.Interfaces;
using Components.Library;

namespace InputResender.ExternalExtensions;
public class ExternalClipboardLoader : ACommandLoader<DMainAppCore> {
	public ExternalClipboardLoader ( DMainAppCore owner ) : base ( owner, "extClip" ) { }
	private static readonly Dictionary<Type, Func<DMainAppCore, DCommand<DMainAppCore>>> NewCommandList = new () {
		{ typeof( ClipboardManagerCommand ), ( core ) => new ClipboardManagerCommand ( core ) }
	};
	protected override IReadOnlyCollection<Func<DMainAppCore, DCommand<DMainAppCore>>> NewCommands => NewCommandList.Values;
}