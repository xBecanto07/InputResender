using System;
using System.Net;
using Components.Library;

namespace Components.Interfaces {
	public abstract class DMainAppControls : ComponentBase<CoreBase> {
		protected DMainAppControls ( DMainAppCore newOwner ) : base ( newOwner ) {
		}
		public DMainAppCore Core { get => (DMainAppCore)Owner; }
		public abstract void ChangePassword (string password );
		public abstract void ChangeTarget ( string EPss );
		public abstract void StartHook ();
		public Action<string> Log { get; set; }
	}

	public class VMainAppControls : DMainAppControls {
		protected IPEndPoint TargetEP;

		public VMainAppControls ( DMainAppCore newOwner ) : base ( newOwner ) {
		}

		public override int ComponentVersion => 1;

		public override void ChangePassword ( string password ) {
			Core.DataSigner.Key = Core.DataSigner.GenerateIV ( System.Text.Encoding.UTF8.GetBytes ( password ) );
			Log ( $"Password changed to {Core.DataSigner.Key.CalcHash ().ToShortCode ()}{Environment.NewLine}" );
		}
		public override void ChangeTarget ( string EPss ) {
			if ( IPEndPoint.TryParse ( EPss, out var newEP ) ) {
				if ( TargetEP != null ) Core.PacketSender.Disconnect ( TargetEP );
				TargetEP = newEP;
				Core.PacketSender.Connect ( TargetEP );
				Log ( $"Changed target to {TargetEP}{Environment.NewLine}" );
			} else Log ( $"Cannot parse {Log} into a valid IP End Point!" );
		}
		public override void StartHook () => throw new NotImplementedException ();
		protected override IReadOnlyList<(string opCode, Type opType)> AddCommands () => throw new NotImplementedException ();
	}
}