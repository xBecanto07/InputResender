using Xunit;
using FluentAssertions;

namespace Components.Library {
	public abstract class ComponentBase {
		private CoreBase Owner;
		public abstract int ComponentVersion { get; }
		public readonly IReadOnlyList<(string opCode, Type opType)> SupportedCommands;

		protected ComponentBase() {
			List<(string opCode, Type opType)> commands = new List<(string opCode, Type opType)>();
			var cmds = AddCommands ();
			commands.AddRange ( cmds );
			SupportedCommands = commands;
		}
		protected abstract IReadOnlyList<(string opCode, Type opType)> AddCommands ();
		protected virtual void ChangeOwner ( CoreBase newOwner ) {
			Owner?.Unregister ( this );
			(Owner = newOwner).Register ( this );
		}

		public object Fetch ( string opCode, Type type ) {
			// Can be overriden to provide own Fetch functionality, but not reccomended.
			foreach (var cmd in SupportedCommands ) {
				if ( cmd.opCode == opCode & cmd.opType == type ) return cmd;
			}
			return null;
		}
		public T Fetch<T> ( string opCode ) => (T)Fetch ( opCode, typeof ( T ) );
	}
	public abstract class ComponentBase<CoreType> : ComponentBase where CoreType : CoreBase {
		public ComponentBase (CoreType newOwner) { ChangeOwner ( newOwner ); }
		public virtual CoreType Owner { get; protected set; }
		protected override void ChangeOwner ( CoreBase newOwner ) { Owner = (CoreType)newOwner; base.ChangeOwner ( newOwner ); }
	}

	public class ComponentMock : ComponentBase<CoreBase> {
		public ComponentMock ( CoreBase newOwner ) : base ( newOwner ) {}

		public override int ComponentVersion => 1;

		protected override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(MockMethod), typeof(void))
			};

		public virtual void MockMethod () { }
	}
	public abstract class ComponentTestBase<T> where T : ComponentBase<CoreBase> {
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
			foreach (var commandInfo in commands) {
				try {
					var fetchedObject = TestObject.Fetch ( commandInfo.opCode, commandInfo.opType );
					fetchedObject.Should ().NotBeNull ();
				} catch (Exception e) {
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
			var DefinitionMethods = GetMethods ( ComponentDef );
			int DefinitionMethodCount = DefinitionMethods.Count ();
			var BaseMethods = GetMethods ( ComponentDef.BaseType );
			int BaseMethodCount = BaseMethods.Count ();
			var DiffMethods = DefinitionMethods.Where ( x => !BaseMethods.Contains ( x ) ).ToArray ();
			int DiffMethodsCount = DiffMethods.Count ();
			TestObject.SupportedCommands.Should ().HaveCount ( DefinitionMethodCount - BaseMethodCount );

			string[] GetMethods ( Type type ) => type.GetMethods ().Select ( x => $"{x.ReturnType.Name} {x.Name}" ).ToArray ();
		}
	}
}
