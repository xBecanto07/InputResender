using System;
using System.Net;
using System.Reflection;
using Components.Library;

namespace Components.Interfaces {
	public abstract class DMainAppControls : ComponentBase<DMainAppCore> {
		protected DMainAppControls ( DMainAppCore newOwner ) : base ( newOwner ) {
		}
		protected sealed override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(ChangePassword), typeof(void)),
				(nameof(ChangeTarget), typeof(void)),
				(nameof(ChangeHookStatus), typeof(bool)),
				(nameof(HookShouldResend), typeof(bool)),
			};
		public abstract void ChangePassword (string password );
		public abstract void ChangeTarget ( string EPss );
		public abstract void ChangeHookStatus ( HHookInfo hookInfo, bool active );
		public abstract bool HookShouldResend { set; protected get; }
		public abstract bool Receiving { get; set; }
		public abstract Action<InputData> ReceiveCallback { set; }
		public Action<string> Log { get; set; }
	}

	public class VMainAppControls : DMainAppControls {
		protected IPEndPoint TargetEP;

		public VMainAppControls ( DMainAppCore newOwner ) : base ( newOwner ) {
		}

		public override int ComponentVersion => 1;
		public override bool HookShouldResend { set; protected get; }

		public override void ChangePassword ( string password ) {
			Owner.DataSigner.Key = Owner.DataSigner.GenerateIV ( System.Text.Encoding.UTF8.GetBytes ( password ) );
			Log ( $"Password changed to {Owner.DataSigner.Key.CalcHash ().ToShortCode ()}{Environment.NewLine}" );
		}
		public override void ChangeTarget ( string EPss ) {
			if ( IPEndPoint.TryParse ( EPss, out var newEP ) ) {
				if ( TargetEP != null ) Owner.PacketSender.Disconnect ( TargetEP );
				TargetEP = newEP;
				Owner.PacketSender.Connect ( TargetEP );
				Log ( $"Changed target to {TargetEP}{Environment.NewLine}" );
			} else Log ( $"Cannot parse {Log} into a valid IP End Point!" );
		}

		public override bool Receiving {
			get => throw new NotImplementedException ();
			set => throw new NotImplementedException ();
		}

		public override Action<InputData> ReceiveCallback { set => throw new NotImplementedException (); }
		public override void ChangeHookStatus ( HHookInfo hookInfo, bool active ) {
				if ( active ) {
					var hookIDs = Owner.InputReader.SetupHook ( hookInfo, HookFastCallback, HookCallback );
					foreach ( var id in hookIDs ) hookInfo.AddHookID ( id );
				} else {
				var hookIDs = hookInfo.HookIDs;
					foreach ( var id in hookIDs ) hookInfo.RemoveHookID ( id );
					hookIDs.Clear ();
					hookIDs = null;
				}
		}
		private bool HookFastCallback ( DictionaryKey key, HInputEventDataHolder inputData ) => HookShouldResend;
		private void HookCallback ( DictionaryKey key, HInputEventDataHolder inputData ) {
			var inputCombination = Owner.InputParser.ProcessInput ( inputData );
			Owner.InputProcessor.ProcessInput ( inputCombination );
		}
	}
}