using Components.Interfaces;
using System;
using System.Net;

namespace InputResender.Cmd; 
public class MainSandboxApp {
	private DMainAppCore Core;
	private IPEndPoint TargetEP;

	public event Action<string> Log;
	private void WriteLine ( string line ) => Log?.Invoke ( line );

	public MainSandboxApp ( DMainAppCore core, Action<string> output ) {
		Core = core;
		Log += output;
		Core.MainAppControls.Log = WriteLine;
	}

	public void ChangeTarget ( string targ ) => throw new NotImplementedException ();
	//CmdProcessor.ProcessLine ( $"target set {res.Text}" );
}
