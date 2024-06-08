using System.Net;
using SBld = System.Text.StringBuilder;
using ClientType = InputResender.Services.INetClientService.ClientType;
using Components.Library;

namespace InputResender.Services {
	public class NetClientList : INetClientServiceGroup {
		public bool Active { get => Listening; }
		private readonly BidirectionalSignalerLightweight Signal;
		private string cName;
		public string CommonName { get => cName; set { cName = value; Signal.CommonName = value; } }
		public virtual MessageResult.SignalBackSelector WhenSignalBack { get; set; } = MessageResult.SignalBackSelector.Immediately;

		public NetClientList (string name = "NoName") {
			Signal = new ( CommonName );
			CommonName = name;
		}

		public INetClientService Add ( IPEndPoint ep, ClientType type = ClientType.Unknown ) {
			var ret = INetClientService.Create ( type, ep );
			AddClient ( () => new[] { ret } );
			return ret;
		}
		public bool Remove ( IPEndPoint ep, ClientType type = ClientType.Unknown ) {
			return clients.RemoveWhere ( ( client ) => {
				if ( client.ServiceType != type ) return false;
				IPEndPoint clientEP = client.EP as IPEndPoint;
				if ( clientEP == null ) return false;
				if ( clientEP != ep ) return false;
				return true;
			} ) > 0;
		}

		public MessageResult WaitAny ( System.Threading.CancellationToken cancelToken ) => WaitForRecv ( cancelToken );

		public void Interrupt () => Stop ();
		public void Start () => Listen ();
		public void Direct ( byte[] data, object dest, object src = null ) => Direct ( new MessageResult ( data, src ?? dest, dest, true ) );
		public void Direct ( MessageResult data) => Callback ( data );
		public override string ToString () {
			SBld SB = new ();
			SB.AppendLine ( $"NetClientList '{CommonName}' [{Count}]: {(Active ? "Active" : "Stopped")}" );
			int id = 0;
			foreach ( var client in clients )
				SB.AppendLine ( $"  {id++} : {client}" );
			return SB.ToString ();
		}
		public void Foreach (System.Action<INetClientService> act) { foreach ( var client in clients ) act ( client ); }
	}
}