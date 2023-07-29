using Components.Interfaces;
using Components.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SBld = System.Text.StringBuilder;

namespace InputResender.UserTesting {
	public abstract class UserTestBase : IDisposable {
		[Flags]
		public enum ClientState { Unknown = 0, Master = 1, Slave = 2 }
		public struct ResultInfo {
			public string Name;
			public bool Passed;
			public string Msg;

			public ResultInfo ( string name, bool pass, string msg ) { Name = name; Passed = pass; Msg = msg; }
		}

		public readonly SBld SB;
		public ResultInfo Result;
		public static ResultInfo[] TestResults { get; private set; } = null;
		protected char? ReservedChar;

		public UserTestBase (SBld sb) {
			SB = sb ?? new SBld ();
		}

		public static IEnumerable<Action> RunAll () {
			TestResults = null;
			Type[] childs = FindTestClasses ();
			List<ResultInfo> ret = new List<ResultInfo> ();

			foreach ( var classInfo in childs ) {
				if ( !IsUsableTestClass ( classInfo ) ) continue;

				var constructor = classInfo.GetConstructor ( new[] { typeof ( SBld ) } );
				if ( constructor == null ) {
					ret.Add ( new ResultInfo (classInfo.Name, false, $"No parameter-less constructor found for type {classInfo.Name}!") );
					continue;
				}

				var testMethods = classInfo.GetMethods ();

				foreach ( var testMethod in testMethods ) {
					string testName = IsUsableTestMethod ( testMethod );
					if ( testName == null ) continue;

					var testClass = SetupTestEnv ( constructor, testName, out SBld SB );

					var resEnu = (IEnumerable<Action>)testMethod.Invoke ( testClass, null );
					foreach ( var testSubTask in resEnu ) yield return testSubTask;
					
					var result = testClass.Result;
					CleanTestEnv ( testClass );

					result.Msg = SB.ToString ();
					ret.Add ( result );
				}
			}
			TestResults = ret.ToArray ();
			yield break;
		}

		private static bool IsUsableTestClass (Type classInfo ) {
			var supportedStates = classInfo.GetProperty ( "SupportedState", BindingFlags.Static | BindingFlags.Public );
			if ( supportedStates != null && supportedStates.CanRead && supportedStates.PropertyType == typeof ( ClientState ) )
				if ( ((ClientState)supportedStates.GetValue ( null, null ) & UserTestApp.ClientState) == 0 ) return false;
			return true;
		}
		private static string IsUsableTestMethod (MethodInfo method) {
			if ( method.GetParameters ().Length != 0 ) return null;
			if ( method.ReturnParameter.ParameterType != typeof ( IEnumerable<Action> ) ) return null;

			return $"{method.DeclaringType.Name}.{method.Name}";
		}

		private static UserTestBase SetupTestEnv (ConstructorInfo constructor, string testName, out SBld SB) {
			SB = new SBld ();
			Program.ClearInput ();
			UserTestApp.Log ( 2, $"Starting test '{testName}' ..." );
			var ret = (UserTestBase)constructor.Invoke ( new[] { SB } );
			UserTestApp.Log ( 3, $"Test initialized, starting execution." );
			ret.Result.Name = testName;
			ret.Result.Passed = false;
			return ret;
		}
		private static void CleanTestEnv (UserTestBase testClass) {
			testClass.Dispose ();
			testClass = null;
			UserTestApp.Log ( 4, $"Test execution finished" );

			if ( testClass != null ) testClass.Dispose ();
			Program.ClearInput ();
			Program.WriteLine ();
		}

		// Dispose pattern
		~UserTestBase () => Dispose ( false );
		public void Dispose () => Dispose ( true );
		protected abstract void Dispose ( bool disposing );

		protected void ReserveChar ( string accepted ) {
			char C = '\0';
			while (!accepted.Contains(C))
				C = Program.Read ();
			ReservedChar = C;
		}
		protected bool ShouldCancel () {
			char C;
			if ( ReservedChar == null ) {
				C = Program.Read ( false );
				if ( C != '\0' ) throw new DataMisalignedException ( "Cannot access character before it's registration!" );
				else ReservedChar = C;
			}
			C = ReservedChar.Value;
			if ( C != 'e' ) return false;
			SB.AppendLine ( "Canceling the test." );
			return true;
		}

		private static Type[] FindTestClasses () {
			List<Type> objects = new List<Type> ();
			foreach ( Type type in
				Assembly.GetAssembly ( typeof ( UserTestBase ) ).GetTypes ()
				.Where ( myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf ( typeof ( UserTestBase ) ) ) ) {
				objects.Add ( type );
			}
			return objects.ToArray ();
		}

		protected bool ShouldCancel ( ClientState wantedState) {
			if ( UserTestApp.ClientState != wantedState ) {
				SB.AppendLine ( "Skipped (client state)" );
				Result.Passed = true;
				return true;
			}
			return false;
		}
	}
}
