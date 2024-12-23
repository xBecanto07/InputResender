﻿using System.Collections;

namespace Components.Library {
	public abstract class ComponentBase {
		public CoreBase Owner { get; private set; }
		public abstract int ComponentVersion { get; }
		public readonly DateTime CreationTime;
		public readonly IReadOnlyList<(string opCode, Type opType)> SupportedCommands;
		private static int NextID = Random.Shared.Next ();
		public readonly int ID;
		public string Name { get => ID.ToShortCode (); }
		public virtual string VariantName { get => null; }
		public virtual Type[] AcceptedTypes { get => null; }

		protected ComponentBase() {
			CreationTime = DateTime.Now;
			ID = NextID++;
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

		public abstract StateInfo Info { get; }
		public abstract class StateInfo {
			protected const string BR = "\r\n";
			public readonly ComponentBase Owner;
			public StateInfo ( ComponentBase owner ) {
				Owner = owner;
				GeneralInfo = $"Version: {Owner.ComponentVersion}\r\nCreation time: {Owner.CreationTime}\r\nName: {Owner.Name}\r\nVariant: {Owner.GetType ().Name}{BR}";
			}
			public readonly string GeneralInfo;
			public virtual string AllInfo () => GeneralInfo;
		}
	}
	public abstract class ComponentBase<CoreType> : ComponentBase where CoreType : CoreBase {
		public ComponentBase (CoreType newOwner) { ChangeOwner ( newOwner ); }
		public new virtual CoreType Owner { get; protected set; }
		protected override void ChangeOwner ( CoreBase newOwner ) { Owner = (CoreType)newOwner; base.ChangeOwner ( newOwner ); }
	}

	public class ComponentMock : ComponentBase<CoreBase> {
		public ComponentMock ( CoreBase newOwner ) : base ( newOwner ) {}

		public override int ComponentVersion => 1;

		protected override IReadOnlyList<(string opCode, Type opType)> AddCommands () => new List<(string opCode, Type opType)> () {
				(nameof(MockMethod), typeof(void))
			};

		public virtual void MockMethod () { }

		public override StateInfo Info => new MockStateInfo ( this );
		public class MockStateInfo : StateInfo {
			public MockStateInfo ( ComponentBase owner ) : base ( owner ) { }
			public override string AllInfo () => $"{GeneralInfo}";
		}
	}
}
