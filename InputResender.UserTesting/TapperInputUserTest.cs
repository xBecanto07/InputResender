using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputResender.UserTesting {
	public class TapperInputUserTest : UserTestBase {
		public TapperInputUserTest ( StringBuilder sb ) : base ( sb ) {
		}

		protected override void Dispose ( bool disposing ) {

		}

		public IEnumerable<Action> WritingTest () {
			Program.WriteLine ( "Press 'T' key to start the test and then type text 'Hello, World!' ... " );
			Program.ClearInput ();
			yield return () => ReserveChar ( "et" );
			if ( ShouldCancel () ) yield break;
			yield break;
		}
	}
}