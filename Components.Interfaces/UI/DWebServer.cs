using Components.Library;
using InputResender.Services.NetClientService;
using InputResender.Services;

namespace Components.Interfaces.UI;
public abstract class DWebServer : ComponentBase<CoreBase> {
	public DWebServer ( CoreBase owner ) : base ( owner ) { }

	public override int ComponentVersion => 1;

	protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
			(nameof(StartServer), typeof(void)),
			(nameof(StopServer), typeof(void))
		};

	public abstract void StartServer ( INetPoint ep );
	public abstract void StopServer ();

	public override void Clear () {
		StopServer ();
		base.Clear ();
	}



	public override StateInfo Info => new DStateInfo ( this );
	public class DStateInfo : StateInfo {
		public DStateInfo ( DWebServer owner ) : base ( owner ) {
		}
		public override string AllInfo () => $"{base.AllInfo ()}{BR}No more info :(";
	}
}

public class MWebServer : DWebServer {
	public MWebServer ( CoreBase owner ) : base ( owner ) { }
	public override void StartServer(INetPoint ep)
	{
		throw new NotImplementedException();
	}
	public override void StopServer() {
		throw new NotImplementedException();
	}
}