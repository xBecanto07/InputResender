namespace Components.Library;
public abstract class ComponentUIParameter (string name, string label, string description, Type type ) {
	public readonly string Name = name;
	public readonly string Label = label;
	public readonly string Description = description;
	public readonly Type Type = type;
	public ComponentUIParametersInfo Owner;
	public event Action OnDataChanged; // Notify upstream (UI) about change in data source
	public event Action OnUpdateRequested; // Notify downstream (data source) about request to fetch new value from data source

	//protected bool Dirty = true;
	protected abstract void RequestUpdateValue ();

	///<summary>Use this at data source to mark the value as dirty and notify the UI to fetch the new value</summary>
	public void NotifyDataChanged () {
		RequestUpdateValue (); // Fetch the new value
	}
	protected void NotifyFinishedUpdate () {
		OnDataChanged?.Invoke ();
	}

	///<summary>Use this in the UI to request the latest value from the data source and update the UI accordingly</summary>
	public void RequestUpdate () {
		RequestUpdateValue (); // Fetch the new value
		OnUpdateRequested?.Invoke ();
	}
}

public abstract class ComponentUIParameter<T> : ComponentUIParameter {
	public T Value { get; private set; }
	public bool IsReadOnly => Validator == null;
	private readonly Func<T, T> _UpdateValue;
	private readonly Func<T, T, (bool, string)> Validator;
	private readonly Action<T> Apply;

	protected ComponentUIParameter (string name, string label, string description, T initValue, Func<T, T> updateValue)
		: base (name, label, description, typeof( T )) {
		Value = initValue;
		_UpdateValue = updateValue;
		Validator = null;
		Apply = null;
	}
	protected ComponentUIParameter (string name, string label, string description, T initValue, Func<T, T> updateValue, Func<T, T, (bool, string)> validator, Action<T> apply )
		: base (name, label, description, typeof( T )) {
		ArgumentNullException.ThrowIfNull ( validator, nameof ( validator ) );
		ArgumentNullException.ThrowIfNull ( apply, nameof ( apply ) );
		Value = initValue;
		_UpdateValue = updateValue;
		Validator = validator;
		Apply = apply;
	}

	protected override void RequestUpdateValue () {
		//UpdateValue ();
		if ( _UpdateValue == null ) return;
		T newVal = _UpdateValue ( Value );
		if ( !Equals ( newVal, Value ) ) {
			Value = newVal;
			NotifyFinishedUpdate ();
		}
	}

	public (bool, string) Validate ( T newVal ) {
		if ( Validator == null )
			return ( false, "This parameter is read-only." );
		return Validator ( newVal, Value );
	}
	public void ApplyValue ( T newVal ) {
		if (Validator == null)
			throw new InvalidOperationException ( "This parameter is read-only." );
		var (isValid, errorMessage) = Validator ( newVal, Value );
		if ( !isValid )
			throw new ArgumentException ( $"Invalid value: {errorMessage}" );
		Apply?.Invoke ( newVal );
		RequestUpdateValue ();
	}
}

public class UI_IntField ( string name, string label, string description, Func<int> updateValue, Func<int, int, (bool, string)> validator = null )
	: ComponentUIParameter<int> ( name, label, description, 0, ( _ ) => updateValue () ) {
}

public class UI_ListView ( string name, string label, string description, Func<List<string>> updateValue )
	: ComponentUIParameter<List<string>> ( name, label, description, [], ( _ ) => updateValue () ) {
}

public class UI_DropDown : ComponentUIParameter<(int selID, List<string> options)> {
	/// <summary>Use this when data change selection options, but only UI changes the selected index.</summary>
	public UI_DropDown (string name, string label, string description, Func<List<string>> updateValue)
		: base (name, label, description, (0, []), (old) => (old.selID, updateValue ())) {}
	/// <summary>Use this when selection options are constant and data change the selected index.</summary>
	public UI_DropDown (string name, string label, string description, List<string> options, Func<int> updateValue)
		: base (name, label, description, (0, []), (old) => (updateValue (), options)) {}

	/// <summary>Use this when data change selection options, but only UI changes the selected index.</summary>
	public UI_DropDown ( string name, string label, string description, Func<List<string>> updateValue, Func<int, int, (bool, string)> validator, Action<int> apply )
		: base ( name, label, description, (0, []),
			updateValue: ( old ) => (old.selID, updateValue ()),
			validator: (oldV, newV ) => validator ( oldV.selID, newV.selID ),
			apply: (newVal) => apply(newVal.selID) ) {}
	/// <summary>Use this when selection options are constant and data change the selected index.</summary>
	public UI_DropDown ( string name, string label, string description, List<string> options, Func<int> updateValue, Func<int, int, (bool, string)> validator, Action<int> apply )
		: base ( name, label, description, (0, []),
			updateValue: ( old ) => (updateValue (), options),
			validator: ( oldV, newV ) => validator ( oldV.selID, newV.selID ),
			apply: ( newVal ) => apply ( newVal.selID ) ) { }

	public (bool, string) Validate ( int newSelID ) => Validate ( (newSelID, Value.options) );
	public void ApplyValue ( int newSelID ) => ApplyValue ( (newSelID, Value.options) );
}

public class UI_TextField ( string name, string label, string description, Func<string> updateValue, Func<string, string, (bool, string)> validator = null )
	: ComponentUIParameter<string> ( name, label, description, string.Empty, ( _ ) => updateValue () ) {
	public readonly Func<string, string, (bool, string)> Validator = validator;
}

public class UI_ActionButton ( string name, string label, string description, Action onClick )
	: ComponentUIParameter<bool> ( name, label, description, false, ( _ ) => false ) {
	public readonly Action OnClick = onClick;
}

public class UI_Separator ( string name, string label, string description )
	: ComponentUIParameter<bool> ( name, label, description, false, ( _ ) => false ) {
}

public class ComponentUIParametersInfo {
	public readonly string ComponentName, ComponentDescription;
	public readonly string ComponentID;
	public readonly Type ComponentType;
	public readonly IReadOnlyList<ComponentUIParameter> Parameters;
	public readonly Action OnDestroy;
	public readonly Action<object[]> OnApply;
	public readonly Func<string[], object[], object[]> OnBatchUpdate;
	public event Action OnDataChanged, OnUpdateRequested;

	public ComponentUIParametersInfo (
		string componentName
		, string componentID
		, string componentDescription
		, Type componentType
		, IReadOnlyList<ComponentUIParameter> parameters
		, Action onDestroy
		, Action<object[]> onApply
		, Func<string[], object[], object[]> onBatchUpdate = null
		) {
		ComponentName = componentName;
		ComponentID = componentID;
		ComponentDescription = componentDescription;
		ComponentType = componentType;
		Parameters = parameters;
		OnDestroy = onDestroy;
		OnApply = onApply;
		OnBatchUpdate = onBatchUpdate;

		foreach ( var param in Parameters ) {
			param.Owner = this;
			//param.OnDataChanged += NotifyDataChanged;
		}
	}

	public void NotifyDataChanged () {
		foreach ( var param in Parameters )
			param.NotifyDataChanged ();
		OnDataChanged?.Invoke ();
	}

	public void RequestUpdate () {
		foreach ( var param in Parameters )
			param.RequestUpdate ();
		OnUpdateRequested?.Invoke ();
	}
}