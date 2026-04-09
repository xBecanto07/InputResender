using Components.Library;

namespace Components.Interfaces;
public abstract class DExternalLoader : ComponentBase<DMainAppCore> {
	public DExternalLoader ( DMainAppCore owner ) : base ( owner ) { }

	protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
			(nameof(LoadExternal), typeof(ACommandLoader<DMainAppCore>))
		};

	public abstract ACommandLoader<DMainAppCore> LoadExternal ( string path, string loaderName, ArgParser args );
}

public class MExternalLoader : DExternalLoader {
	public MExternalLoader ( DMainAppCore owner ) : base ( owner ) { }
	public override int ComponentVersion => 1;

	public override ACommandLoader<DMainAppCore> LoadExternal ( string path, string loaderName, ArgParser args ) {
		ArgumentException.ThrowIfNullOrWhiteSpace ( path, nameof(path) );
		ArgumentException.ThrowIfNullOrWhiteSpace ( loaderName, nameof(loaderName) );

		if ( !System.IO.File.Exists ( path ) )
			throw new System.IO.FileNotFoundException ( $"File {path} not found" );

		byte[] binary = Owner.FileManager.GetWrapperOrSelf ().ReadBinary ( path );
		var assembly = System.Reflection.Assembly.Load ( binary );
		if ( assembly == null )
			throw new Exception ( $"Failed to load assembly from {path}" );

		var loaderTypes = assembly.GetTypes ().Where ( t => t.Name == loaderName );
		if ( !loaderTypes.Any () )
			throw new Exception ( $"No type named {loaderName} found in assembly {path}" );
		if ( loaderTypes.Count () > 1 )
			throw new Exception ( $"Multiple types named {loaderName} found in assembly {path}" );

		var loaderType = loaderTypes.First ();
		if ( !typeof(ACommandLoader<DMainAppCore>).IsAssignableFrom ( loaderType ) )
			throw new Exception ( $"Type {loaderType.FullName} does not inherit from ACommandLoader" );

		ACommandLoader<DMainAppCore> loaderInstance = null;
		try {
			// Look what constructors are available and try to provide the right arguments (supported are CoreBase, DExternalLoader, ArgParser args)
			Type[] supportedCtorArgs = new Type[] { typeof(CoreBase), typeof(DMainAppCore), typeof(DExternalLoader), typeof(ArgParser) };
			List<object> ctorArgs = [];
			var ctor = loaderType.GetConstructors ().FirstOrDefault ( c => {
				foreach ( var par in c.GetParameters () ) {
					int idx = Array.IndexOf ( supportedCtorArgs, par.ParameterType );
					if ( idx == -1 ) return false; // Unsupported parameter type
					object arg = par.ParameterType switch {
						Type t when t == typeof(CoreBase) => Owner,
						Type t when t == typeof(DMainAppCore) => Owner,
						Type t when t == typeof(DExternalLoader) => this,
						Type t when t == typeof(ArgParser) => args,
						_ => null
					};
					if ( arg == null ) return false; // Should not happen, but just in case
					ctorArgs.Add ( arg );
				}
				return true;
			} );
			if ( ctor == null )
				throw new Exception ( $"No suitable constructor found for type {loaderType.FullName}. Supported parameter types are: {string.Join ( ", ", supportedCtorArgs.Select ( t => t.Name ) )}" );
			return loaderInstance = (ACommandLoader<DMainAppCore>)ctor.Invoke ( ctorArgs.ToArray () );
		} catch ( Exception ex ) {
			throw new Exception ( $"Failed to create instance of type {loaderType.FullName} from assembly {path}: {ex.Message}" );
		}
	}

	public override StateInfo Info => new VStateInfo ( this );
	public class VStateInfo : StateInfo {
		public VStateInfo ( MExternalLoader owner ) : base ( owner ) { }
	}
}