using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TestInfo = System.ValueTuple<string, bool, string>;
using SBld = System.Text.StringBuilder;

namespace InputResender.UserTesting {
	public abstract class UserTestBase : IDisposable {
		[Flags]
		public enum ClientState { Unknown = 0, Master = 1, Slave = 2 }

		protected SBld SB;

		public UserTestBase (SBld sb) {
			SB = sb ?? new SBld ();
		}

		public static IEnumerable<TestInfo[]> RunAll () {
			Type[] childs = FindTestClasses ();
			List<TestInfo> ret = new List<TestInfo> ();

			foreach ( var classInfo in childs ) {
				var supportedStates = classInfo.GetProperty ( "SupportedState", BindingFlags.Static | BindingFlags.Public );
				if ( supportedStates != null && supportedStates.CanRead && supportedStates.PropertyType == typeof ( ClientState ) )
					if ( ((ClientState)supportedStates.GetValue ( null, null ) & UserTestApp.ClientState) == 0 ) continue;

				var constructor = classInfo.GetConstructor ( new[] { typeof ( SBld ) } );
				if ( constructor == null ) {
					ret.Add ( (classInfo.Name, false, $"No parameter-less constructor found for type {classInfo.Name}!") );
					continue;
				}

				var testMethods = classInfo.GetMethods ();

				foreach ( var testMethod in testMethods ) {
					if ( testMethod.GetParameters ().Length != 0 ) continue;
					if ( testMethod.ReturnParameter.ParameterType != typeof ( IEnumerable<bool?> ) ) continue;

					SBld SB = new SBld ();
					bool res = false;
					UserTestBase testClass = null;

					Program.ClearInput ();
					if ( UserTestApp.LogLevel >= 2 ) Program.WriteLine ( $"Starting test '{classInfo.Name}.{testMethod.Name}' ..." );
					testClass = (UserTestBase)constructor.Invoke ( new[] { SB } );
					if ( UserTestApp.LogLevel >= 3 ) Program.WriteLine ( $"Test initialized, starting execution." );
					var resEnu = ((IEnumerable<bool?>)testMethod.Invoke ( testClass, null ));
					foreach ( var resSub in resEnu ) {
						if ( resSub == null ) yield return null;
						else { res = resSub.Value; break; }
					}
					testClass.Dispose ();
					testClass = null;
					if ( UserTestApp.LogLevel >= 4 ) Program.WriteLine ( $"Test execution finished" );

					if ( testClass != null ) testClass.Dispose ();
					Program.ClearInput ();

					ret.Add ( ($"{classInfo.Name}.{testMethod.Name}", res, SB.ToString ()) );
				}
			}
			yield return ret.ToArray ();
			yield break;
		}

		// Dispose pattern
		~UserTestBase () => Dispose ( false );
		public void Dispose () => Dispose ( true );
		protected abstract void Dispose ( bool disposing );

		private static Type[] FindTestClasses () {
			List<Type> objects = new List<Type> ();
			foreach ( Type type in
				Assembly.GetAssembly ( typeof ( UserTestBase ) ).GetTypes ()
				.Where ( myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf ( typeof ( UserTestBase ) ) ) ) {
				objects.Add ( type );
			}
			return objects.ToArray ();
		}
	}
}
