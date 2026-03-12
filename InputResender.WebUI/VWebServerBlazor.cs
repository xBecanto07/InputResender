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
using Components.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
		webBuilder.ConfigureLogging ( logging => {
			logging.ClearProviders ();
			logging.SetMinimumLevel ( LogLevel.Warning );
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
		var res = Owner.Fetch<CommandProcessor<DMainAppCore>> ()?.ProcessLine ( "autocmd list" );
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
		var res = Owner.Fetch<CommandProcessor<DMainAppCore>> ()?.ProcessLine ( "autocmd list" );
		return res?.Message;
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

