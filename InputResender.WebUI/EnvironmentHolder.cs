using Components.Library;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InputResender.WebUI;
public class EnvironmentHolder {
	public readonly VWebServerBlazor ServerComponent;
	public readonly CoreBase OwnerCore;
	private readonly Dictionary<string, ComponentUIParametersInfo> _componentUIs;
	public IReadOnlyDictionary<string, ComponentUIParametersInfo> ComponentUIs;
	public readonly List<string> Errors = [];

	public EnvironmentHolder ( VWebServerBlazor owner ) {
		ArgumentNullException.ThrowIfNull ( owner, nameof ( owner ) );
		ServerComponent = owner;
		OwnerCore = owner.Owner;
		_componentUIs = new ();
		ComponentUIs = _componentUIs.AsReadOnly ();

		UpdateGUI ();
	}

	public void UpdateGUI () {
		_componentUIs.Clear ();
		var blazorFactory = OwnerCore.Fetch<VUIFactoryBlazor> ();
		if ( blazorFactory == null )
			return;
		var components = blazorFactory.GetAllUiGroups ();
		if ( components == null )
			return;

		//return components.Select (c => (GetNavLinkHref(c.ComponentName), c)).ToList ();
		foreach ( var compParamGroup in components ) {
			_componentUIs[compParamGroup.ComponentName] = compParamGroup;
		}
	}
}