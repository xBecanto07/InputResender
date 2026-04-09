using Components.Library;
using InputResender.Commands;
using InputResender.Services;

namespace Components.Interfaces.Commands;
public class FileManagerCommand : DCommand<DMainAppCore> {
	public const string HOME_PATH_VAR_NAME = "homePath";
	public static string GetHomePath (CommandProcessor<DMainAppCore> cmdProc )
		=> cmdProc.GetVar<string> ( HOME_PATH_VAR_NAME ) ?? AppDomain.CurrentDomain.BaseDirectory;
	public override string Description => "Access DFileManager and FileAccessService functionalities.";
	protected override bool PrintHelpOnEmpty => true;

	private static List<string> CommandNames = ["filemanager", "file", "fm"];
	private static List<(string, Type)> InterCommands = [
		("find", null),
		("verify", null),
		("whitelist", null),
		("read", null),
		("write", null),
	];

	public FileManagerCommand ( DMainAppCore owner, string parentDsc = null )
		: base ( owner, parentDsc, CommandNames, InterCommands ) { }

	private IFileManager GetFileManager ( CommandProcessor<DMainAppCore>.CmdContext context, bool preferWrapper = false ) {
		var core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand<DMainAppCore>.ActiveCoreVarName );
		var ret = core?.Fetch<DFileManager> ();
		if ( preferWrapper && ret?.FileManagerWrapper != null ) return ret.FileManagerWrapper;

		return ret;
	}

	protected override CommandResult ExecIner ( CommandProcessor<DMainAppCore>.CmdContext context ) {
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"find" => CallName + " find <FileName> [BasePath]\n\tFileName: Name of the file to find"
				+ "\n\tBasePath: Starting directory (default: current directory)",
			"verify" => CallName + " verify <FilePath> [--update] [--show]\n\tFilePath: Path to the file to verify integrity"
				+ "\n\t--update: If integrity check fails, show content and prompt user to store new hash"
				+ "\n\t--show: Show content of the tested file",
			"whitelist" => CallName + " whitelist <FilePath> <Hash>\n\tFilePath: Path to the file\n\tHash: Expected hash (hex or base64)",
			"write" => CallName + " write <FilePath> <Content>\n\tFilePath: Path to the file\n\tContent: Content to write (header is generated automatically)",
			_ => null
		}, out var helpRes ) ) return helpRes;

		switch ( context.SubAction ) {
		case "find": return ExecFind ( context );
		case "verify": return ExecVerify ( context );
		case "whitelist": return ExecWhitelist ( context );
		case "read": return ExecRead ( context );
		case "write": return ExecWrite ( context );
		default: return new ( $"Unknown subcommand '{context.SubAction}'." );
		}
	}

	private CommandResult ExecFind ( CommandProcessor<DMainAppCore>.CmdContext context ) {
		var fm = GetFileManager ( context );
		if ( fm == null ) return new ( "DFileManager not found in active core." );

		string fileName = context.Args.String ( context.ArgID + 1, "FileName", 1, true );
		string basePath = context.Args.String ( context.ArgID + 2, "BasePath", shouldThrow: false );
		if ( string.IsNullOrEmpty ( basePath ) ) basePath = FileManagerCommand.GetHomePath ( context.CmdProc );

		//string optionsStr = context.Args.String ( context.ArgID + 3, "SearchOptions", shouldThrow: false );
		try {
			string result = fm.FileService.GetAssetPath ( basePath, fileName, FileAccessService.SearchOptions.All );
			return new ( $"Found: {result}" );
		} catch ( DirectoryNotFoundException ex ) {
			return new ( $"File not found: {ex.Message}" );
		}
	}

	private CommandResult ExecVerify ( CommandProcessor<DMainAppCore>.CmdContext context ) {
		context.Args.RegisterSwitch ( 'u', "update" );
		var fm = GetFileManager ( context, context.Args.Present ( "--update" ) );
		if ( fm == null ) return new ( "DFileManager not found in active core." );

		context.Args.RegisterSwitch ( 's', "show" );
		context.Args.RegisterSwitch ( 'p', "password" );
		string filePath = context.Args.String ( context.ArgID + 1, "FilePath", 1, true );
		PasswordHolder pass = null;
		if ( context.Args.Present ( "--password" ) )
			pass = new (context.Args.String ( "password", "Password", 4, true ));

		try {
			string content = pass == null ? fm.ReadFile ( filePath ) : fm.ReadFileWithHeader ( filePath, pass );
			if ( context.Args.Present ( "--show" ) )
				return new (
					$"File '{filePath}' integrity verified. Length: {content.Length}\nContent:\n{content.PrefixAllLines ( "  " )}"
				);
			else
				return new ($"File '{filePath}' integrity verified. Length: {content.Length}");
		} catch ( DFileManager.IntegrityException ex ) {
			string msg = $"Integrity check failed for '{filePath}': {ex.Message}";
			if ( ex.Hash != null )
				msg += $"\n - Hash (hex): {Convert.ToHexString ( ex.Hash )}\n - Hash (base64): {Convert.ToBase64String ( ex.Hash )}";
			return new ( msg );
		} catch ( FileNotFoundException ex ) {
			return new ( ex.Message );
		}
	}

	private CommandResult ExecWhitelist ( CommandProcessor<DMainAppCore>.CmdContext context ) {
		var fm = GetFileManager ( context );
		if ( fm == null ) return new ( "DFileManager not found in active core." );

		string filePath = context.Args.String ( context.ArgID + 1, "FilePath", 1, true );
		string hash = context.Args.String ( context.ArgID + 2, "Hash", 1, true );

		try {
			fm.WhitelistHash ( filePath, hash );
			return new ( $"File '{filePath}' whitelisted." );
		} catch ( Exception ex ) {
			return new ( $"Whitelist failed: {ex.Message}" );
		}
	}

	private CommandResult ExecRead ( CommandProcessor<DMainAppCore>.CmdContext context ) {
		context.Args.RegisterSwitch ( 'u', "update" );
		var fm = GetFileManager ( context, context.Args.Present ( "--update" ) );
		if ( fm == null ) return new ( "DFileManager not found in active core." );

		context.Args.RegisterSwitch ( 'p', "password" );
		PasswordHolder pass = new (context.Args.String ( "password", "Password", 4, true ));
		string filePath = context.Args.String ( context.ArgID + 1, "FilePath", 1, true );

		try {
			string content = fm.ReadFileWithHeader ( filePath, pass );
			return new ( $"Content of '{filePath}':\n{content}" );
		} catch ( DFileManager.IntegrityException ex ) {
			string msg = $"Integrity check failed for '{filePath}': {ex.Message}";
			if ( ex.Hash != null )
				msg += $"\n - Hash (hex): {Convert.ToHexString ( ex.Hash )}\n - Hash (base64): {Convert.ToBase64String ( ex.Hash )}";
			return new ( msg );
		} catch ( FileNotFoundException ex ) {
			return new ( ex.Message );
		}
	}

	private CommandResult ExecWrite ( CommandProcessor<DMainAppCore>.CmdContext context ) {
		var fm = GetFileManager ( context );
		if ( fm == null ) return new ( "DFileManager not found in active core." );

		context.Args.RegisterSwitch ( 'p', "password" );
		PasswordHolder pass = new (context.Args.String ( "password", "Password", 4, true ));
		string filePath = context.Args.String ( context.ArgID + 1, "FilePath", 1, true );
		string content = context.Args.String ( context.ArgID + 2, "Content", 0, true );

		try {
			fm.WriteFileWithHeader ( filePath, content, pass );
			return new ( $"File '{filePath}' written with header." );
		} catch ( Exception ex ) {
			return new ( $"Write failed: {ex.Message}" );
		}
	}
}