using Components.Factories;
using Components.Implementations;
using Components.Interfaces;
using Components.Library;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SBld = System.Text.StringBuilder;

namespace InputResender.UserTesting {
	internal class TwoClientsCommsTest : UserTestBase {
		public TwoClientsCommsTest ( SBld sb ) : base ( sb ) {

		}

		protected override void Dispose ( bool disposing ) {

		}

		public IEnumerable<Action> Sender () {
			yield break;
		}
		public IEnumerable<Action> Listener () {
			yield break;
		}
	}
}
