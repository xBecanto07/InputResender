using System;
using Components.Library;

namespace InputResender.WebUI.Visualizers;
public class Separator (UI_Separator ui_separator) : IBlazorVisualizer (ui_separator) {
	public override void Apply (object value) {
		throw new NotImplementedException ( "Apply logic not implemented for Separator." );
	}
}