using System;
using System.Text.Json;
using System.Collections.Generic;
using Components.Library;
using Components.Interfaces.UI;
using InputResender.Services;
using InputResender.WebUI.BlazorComponents;
using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Components.Web;
//using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
//using System.Net.Http;
using System.Threading.Tasks;

namespace InputResender.WebUI;
public class VWebServerBlazor : DWebServer {
	public const string RootDirectory = "wwwroot";
	public const string RootDirIdentifier = "BlazorConfig.txt";
	private IPNetPoint EP;
	private IHost host;
	private Task hostTask;
	//private WebAssemblyHost host;

	public VWebServerBlazor ( CoreBase owner ) : base ( owner ) { }

	public override void StartServer ( INetPoint ep ) {
		if ( ep is not IPNetPoint ipEp )
			throw new ArgumentException ( "Only IPNetPoint is supported.", nameof ( ep ) );
		if ( host != null )
			throw new InvalidOperationException ( "Server already running." );
		EP = ipEp;
		
		host = Host.CreateDefaultBuilder ()
			.ConfigureWebHostDefaults ( PrepareBuilder )
			.Build ();
		hostTask = host.StartAsync ();
	}

	private void PrepareBuilder ( IWebHostBuilder webBuilder ) {
		EnvironmentHolder envHolder = new ( this );
		string wwwRootPath = FindRootDir ();
		Console.WriteLine($"Using wwwroot path: {wwwRootPath}");
		
		webBuilder.UseUrls ( $"http://{EP.Address}:{EP.Port}" );
		webBuilder.UseWebRoot ( Path.Combine ( wwwRootPath, "wwwroot" ) );
		webBuilder.UseContentRoot ( Path.Combine ( wwwRootPath, "wwwroot" ) );
		webBuilder.ConfigureServices ( services => {
			// services.AddRazorPages ();
			// services.AddServerSideBlazor ();
			services.AddCascadingValue ( "EnviHolder", sp => envHolder );
			services.AddHttpClient ();
			services.AddRazorComponents ()
				.AddInteractiveServerComponents ();
		} );
		webBuilder.Configure ( app => {
			app.UseStaticFiles ();
			app.UseRouting ();
			app.UseAntiforgery ();
			app.UseEndpoints ( endpoints => {
				//endpoints.MapBlazorHub ();
				endpoints.MapRazorComponents<App> ()
					.AddInteractiveServerRenderMode ();
				endpoints.MapGet ( "/AutoCmd/list", GetAutoCmdList );
				endpoints.MapGet ( "/AutoCmd/status", GetAutoCmdStatus );
			} );
		} );
	}

	public override void StopServer () {
		if (hostTask == null) return;
		host.StopAsync ().Wait ();
		host.Dispose ();
		host = null;
		hostTask.Dispose ();
		hostTask = null;
		// app?.StopAsync ().Wait ();
		// app?.DisposeAsync ();
	}
	
	
	private record struct CmdGroupInfo ( int Id, string Name );
	private List<CmdGroupInfo> GetAutoCmdList () {
		var res = Owner.Fetch<CommandProcessor> ()?.ProcessLine ( "autocmd list" );
		if ( res == null ) return null;
		List<CmdGroupInfo> groups = [];
		foreach ( var line in res.Message.Split ( ['\r', '\n'], StringSplitOptions.RemoveEmptyEntries ) ) {
			string s = line.Trim ();
			if ( !s.StartsWith ( '[' ) || !s.EndsWith ( ':' ) ) continue;
			
			int endIdx = s.IndexOf ( ']' );
			int itemID = int.Parse ( s[1..endIdx] );
			groups.Add ( new (itemID, s[(endIdx + 2)..^2]) );
		}
		return groups;
		//string json = JsonSerializer.Serialize ( groups );
		//return json;
	}
	private string GetAutoCmdStatus () {
		var res = Owner.Fetch<CommandProcessor> ()?.ProcessLine ( "autocmd list" );
		return res?.Message;
		/*
Automatic command groups:
[0] initCmds:
	# 0 |> loadall
	# 1 |> clear
	# 2 |> safemode on
	# 3 |> core new
	# 4 |> core own
	# 5 |> conns force init
	# 6 |> loglevel all
	# 7 |> network callback recv print
	# 8 |> network callback newconn print
[1] setupPipes:
	# 0 |> hook manager start
	# 1 |> pipeline new Sender DInputProcessor DDataSigner DPacketSender
	# 2 |> pipeline new Recver DPacketSender DDataSigner DInputSimulator
	# 3 |> pipeline new InputProcess DInputReader DInputMerger DInputProcessor
	# 4 |> pipeline new HookManagerInputProcess SHookManager DInputMerger DInputProcessor
[2] testSimKey:
	# 0 |> hook add Print Keydown KeyUp
	# 1 |> sim keypress R
[3] printWinKey:
	# 0 |> windows load --force
	# 1 |> windows msgs start
	# 2 |> hook add Print Keydown KeyUp
[4] startListener:
	# 0 |> help
[5] TestSclAutorun:
	# 0 |> load sclModules
	# 1 |> load joiners
	# 2 |> seclav parse SIPtest.scl
	# 3 |> SIP force
	# 4 |> SIP assign SIPtest.scl
	# 5 |> auto run setupPipes
	# 6 |> hook add Pipeline Keydown KeyUp
	# 7 |> sim keydown R
		 */
	}
	
	private string FindRootDir () {
		System.IO.DirectoryInfo currentDir = new System.IO.DirectoryInfo ( AppDomain.CurrentDomain.BaseDirectory );
		while ( currentDir != null ) {
			string potentialPath = System.IO.Path.Combine ( currentDir.FullName, RootDirectory );
			if ( System.IO.Directory.Exists ( potentialPath ) && System.IO.File.Exists ( System.IO.Path.Combine ( potentialPath, RootDirIdentifier ) ) )
				return currentDir.FullName;
			
			potentialPath = System.IO.Path.Combine ( currentDir.FullName, "InputResender.sln" );
			if ( System.IO.File.Exists ( potentialPath ) )
				break;
			currentDir = currentDir.Parent;
		}
		if ( currentDir != null ) {
			// We're in solution base directory, search for root in all subdirectories (non-recursive)
			foreach ( var subDir in currentDir.GetDirectories () ) {
				string potentialPath = System.IO.Path.Combine ( subDir.FullName, RootDirectory );
				if ( System.IO.Directory.Exists ( potentialPath ) && System.IO.File.Exists ( System.IO.Path.Combine ( potentialPath, RootDirIdentifier ) ) )
					return subDir.FullName;
			}
		}
		throw new Exception ( $"Could not find '{RootDirectory}' directory in any parent of the application base directory." );
	}
}