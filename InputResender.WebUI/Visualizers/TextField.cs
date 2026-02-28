using System;
using Components.Library;

namespace InputResender.WebUI.Visualizers;
public class TextField (UI_TextField uiTextField) : IBlazorVisualizer (uiTextField) {
	private readonly UI_TextField UiTextField = uiTextField;

	public bool IsReadonly => UiTextField.Validator == null;

	public string Value {
		get => UiTextField.Value;
		set {
			if ( IsReadonly )
				throw new InvalidOperationException ( "This field is read-only." );
			var (isValid, errorMessage) = UiTextField.Validator ( value, UiTextField.Value );
			if ( !isValid )
				throw new ArgumentException ( $"Invalid value: {errorMessage}" );
			UiTextField.ApplyValue ( value );
			//UiTextField.Value = value;
		}
	}

	public override void Apply (object value) {
		if ( IsReadonly ) return;
		throw new NotImplementedException ( "Apply logic not implemented for TextField." );
	}
}