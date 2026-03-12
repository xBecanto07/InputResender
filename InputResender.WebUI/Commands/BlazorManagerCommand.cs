using System.Collections.Generic;

using InputResender.WebUI;
using Components.Library;
using Components.Interfaces;
using Components.Interfaces.UI;
using InputResender.Commands;
using InputResender.Services;
using InputResender.Services.NetClientService;
using System.Net;

namespace InputResender.WebUI.Commands;
public class BlazorManagerCommand : DCommand<DMainAppCore> {
	public override string Description => "Command to manage the Blazor server.";

	private static List<string> CommandNames = ["Blazor", "blazor"];
	private static List<(string, System.Type)> InterCommands = [
		("start", null)
		, ("stop", null)
		];

	public BlazorManagerCommand ( DMainAppCore owner, string parentDsc = null )
		: base ( owner, parentDsc, CommandNames, InterCommands ) {
	}

	protected override CommandResult ExecIner ( CommandProcessor<DMainAppCore>.CmdContext context ) {
		if ( TryPrintHelp ( context.Args, context.ArgID + 1, () => context.SubAction switch {
			"start" => CallName + " start [Address] [Port]: Start the Blazor server at the specified address and port.",
			"stop" => CallName + " stop: Stop the Blazor server.",
			_ => null
		}, out var helpRes ) ) return helpRes;

		DMainAppCore core = context.CmdProc.GetVar<DMainAppCore> ( CoreManagerCommand<DMainAppCore>.ActiveCoreVarName );
		if ( core == null ) return new CommandResult ( "No active core found." );
		var webFactory = core.Fetch<VUIFactoryBlazor> ();
		webFactory ??= new VUIFactoryBlazor ( core );

		var webServer = core.Fetch<DWebServer> ();
		webServer ??= new VWebServerBlazor ( core );

		switch ( context.SubAction ) {
		case "start": {
			context.Args.RegisterSwitch ( 'a', "Address", "Address to bind the Blazor server to. Default is localhost." );
			context.Args.RegisterSwitch ( 'p', "Port", "Port to bind the Blazor server to. Default is 1648." );
			// string address = context.Args.String ( context.ArgID + 1, "Address" );
			// int port = context.Args.Int ( context.ArgID + 2, "Port" );
			if (!IPAddress.TryParse ( context.Args.String ( "--Address", "Address to bind to", 4, false), out var ipAddress ))
				ipAddress = IPAddress.Loopback;
			int port = context.Args.Int ( "--Port", "Port to bind to", false ).GetValueOrDefault ( 1648 );


			webServer.StartServer ( new IPNetPoint ( ipAddress, port ) );
			return new CommandResult ( $"Blazor server started at {ipAddress}:{port}." );
		}
		case "stop": {
			webServer.StopServer ();
			return new CommandResult ( "Blazor server stopped." );
		}
		default: return new CommandResult ( $"Unknown sub-action '{context.SubAction}'." );
		}
	}
}