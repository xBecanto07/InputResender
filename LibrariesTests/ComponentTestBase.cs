using Components.Library;
using FluentAssertions;
using Xunit;

namespace LibrariesTests {
	public abstract class ComponentTestBase<T> where T : ComponentBase {
		protected readonly CoreBase Owner;
		protected readonly T TestObject;

		public ComponentTestBase () {
			Owner = CreateCoreBase ();
			TestObject = GenerateTestObject ();
			if ( TestObject == null ) throw new ArgumentNullException ( "Tested componant instance cannot be null! Please provide your tested component instance (try to use 'this')." );
		}

		public abstract CoreBase CreateCoreBase ();
		public abstract T GenerateTestObject ();

		[Fact]
		public void TestCommandAvailability () {
			var commands = TestObject.SupportedCommands;
			commands.Should ().NotBeNull ().And.NotBeEmpty ();
			foreach ( var commandInfo in commands ) {
				try {
					var fetchedObject = TestObject.Fetch ( commandInfo.opCode, commandInfo.opType );
					fetchedObject.Should ().NotBeNull ();
				} catch ( Exception e ) {
					Assert.Fail ( $"Exception accoured while testing accessibility of a command '{commandInfo.opCode}' with expected return type '{commandInfo.opType}'{Environment.NewLine}{e.Message}{Environment.NewLine}{e.StackTrace}" );
				}
			}
		}

		[Fact]
		public void ValidVersionNumber () {
			TestObject.ComponentVersion.Should ().BeGreaterThan ( 0 );
		}

		[Fact]
		public void AllCommandsRegistered () {
			Type ComponentDef = TestObject.GetType ().BaseType;
			int DefinitionMethodCount = ComponentDef.Methods ().Count ();
			int BaseMethodCount = ComponentDef.BaseType.Methods ().Count ();
			TestObject.SupportedCommands.Should ().HaveCount ( DefinitionMethodCount - BaseMethodCount );
		}
	}
}
