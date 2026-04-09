using Components.Interfaces;
using Components.Library;
using InputResender.Services;
using System;

namespace InputResender.CLI;
public class FileManagerCLIWrapper (CliWrapper cliWrapper) : IFileManager {
	/* While I currently don't see other uses for the idea of 'wrappers' around components,
	 I can imagine it being a useful feature in the future.
	 Probably best idea would be to register those under Core.
	 Probably extract interfaces out of Definitions.
	 That would allow to seamlessly incorporate it in the Fetch<> function.
	 If you want the component itself, just request the Definition.
	 If you allow some UI wrapping, request an interface.
	 I'd say this feature should be queued up for all the other 'overall' changes.

	 For now, I'll just include it as a member of few specific methods.
	 Or better, I'll implement some compromise inside of the CmdProc.
	 */

	private readonly CliWrapper CliWrapper = cliWrapper;
	private const string UpdatePrompt
		= "If you want to update the expected hash to match the file content, please type 'update'. Type 'no' or 'reject' to reject this file";

	private DFileManager FileManager
		=> CliWrapper.CmdProc.Owner.Fetch<DFileManager> ()
			?? throw new Exception ( "DFileManager not found in active core." );

	public FileAccessService FileService {
		get => FileManager.FileService;
		set => FileManager.FileService = value;
	}

	public void WhitelistHash ( string path, string hash ) => FileManager.WhitelistHash ( path, hash );

	public void WriteFileWithHeader ( string path, string content, PasswordHolder password )
		=> FileManager.WriteFileWithHeader ( path, content, password );

	public string ReadFileWithHeader ( string path, PasswordHolder password )
		=> Process ( path
			, () => FileManager.ReadFileWithHeader ( path, password )
			, ( ex ) => FileManager.WriteFileWithHeader ( path, ex.Content, password )
		);

	public string ReadFile ( string path )
		=> Process ( path
			, () => FileManager.ReadFile ( path )
			, ( ex ) => FileManager.WhitelistHash ( path, Convert.ToHexString ( ex.Hash ) )
		);


	private string Process ( string path, Func<string> action
		, Action<DFileManager.IntegrityException> overrideContent ) {
		try { return action (); }
		catch ( DFileManager.IntegrityException ex ) {
			if ( ex.Hash == null )
				CliWrapper.Console.WriteLine ( $"File integrity check failed for {path} due to '{ex.Message}'." );
			else {
				CliWrapper.Console.WriteLine ( $"File integrity check failed for {path} due to '{ex.Message}'." );
				CliWrapper.Console.WriteLine ( $" - Expected hash (hexa): {Convert.ToHexString ( ex.Hash )}" );
				CliWrapper.Console.WriteLine ( $" - Expected hash (base64): {Convert.ToBase64String ( ex.Hash )}" );
			}

			CliWrapper.Console.WriteLine ( $" - File content:" );
			foreach ( var line in ex.Content.Split ( '\n' ) ) CliWrapper.Console.WriteLine ( $"   {line}" );
			CliWrapper.Console.WriteLine ( "" );
			while ( true ) {
				CliWrapper.Console.WriteLine ( UpdatePrompt );
				string response = CliWrapper.Console.ReadLineBlocking ().Trim ().ToLowerInvariant ();
				switch ( response ) {
				case "no":
				case "reject":
				case "deny":
					throw;
				case "update":
					//WriteFileWithHeader ( path, ex.Content, password );
					overrideContent ( ex );
					CliWrapper.Console.WriteLine ( $"File {path} updated with new hash." );
					return ex.Content;
				default: continue;
				}
			}
		}
	}
}