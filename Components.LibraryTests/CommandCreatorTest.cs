using System;
using Xunit;
using System.Linq;
using FluentAssertions;
using Components.Library;
using Xunit.Abstractions;
using System.Collections.Generic;

namespace Components.LibraryTests;
internal class CommandAdderA1 : ACommandLoader {
	public const string A1 = "A1";
	public const string A2 = "A2";
	public const string B1 = "B1";
	public const string B1a = "B1a";
	public const string B1b = "B1b";
	public const string CmdName = "adder1A";
	public const string Cmd = BaseLoadCmdName + "-" + CmdName;

	protected override string CmdGroupName => CmdName;
	override protected IReadOnlyCollection<Func<ACommand>> NewCommands => new Func<ACommand>[] {
		() => new CommandA1 (),
		() => new CommandA2 (),
		() => new CommandAdderB1 (),
	};
	protected override IReadOnlyCollection<(string, Func<ACommand, ACommand>)> NewSubCommands => new List<(string, Func<ACommand, ACommand>)> {
		( B1, ( ACommand parent ) => {
			if ( parent is CommandB1 cmdB1 ) RegisterSubCommand ( cmdB1, new CommandB1b (cmdB1) );
			return null;
		})
	};
	//() => ( B1, new CommandB1b ( Help ) ),
}
internal class CommandAdderB1 : ACommandLoader {
	public const string CmdName = "adder2A";
	public const string Cmd = CommandAdderA1.Cmd + "-" + CmdName;

	protected override string CmdGroupName => "adder2A";
	override protected IReadOnlyCollection<Func<ACommand>> NewCommands => new Func<ACommand>[] {
		() => new CommandB1 (),
	};
}

internal abstract class ATestCommand : ACommand {
	public abstract string CommandName { get; }
	override public string Description => $"Test command {CommandName.ToUpper ()}";
	public ATestCommand ( string parentHelp = null ) : base ( parentHelp ) => commandNames.Add ( CommandName );
	override protected CommandResult ExecIner ( CommandProcessor context, ArgParser args, int argID ) => new ( $"Test command {CallName.ToUpper ()} executed." );
}
internal class CommandA1 : ATestCommand {
	override public string CommandName => CommandAdderA1.A1;
}
internal class CommandA2 : ATestCommand {
	override public string CommandName => CommandAdderA1.A2;
}
internal class CommandB1 : ATestCommand {
	CommandB1a subB2;
	override public string CommandName => CommandAdderA1.B1;
	public CommandB1 () {
		subB2 = new CommandB1a ( this );
		subCommands.Add ( subB2.CommandName, subB2 );
	}
}
internal class CommandB1a : ATestCommand {
	override public string CommandName => CommandAdderA1.B1a;
	public CommandB1a ( CommandB1 parent ) : base ( parent.CallName ) { }
}
internal class CommandB1b : ATestCommand {
	override public string CommandName => CommandAdderA1.B1b;
	public CommandB1b ( CommandB1 parent ) : base ( parent.CallName ) { }
}


public class CommandCreatorTest {
	const string A1 = CommandAdderA1.A1;
	const string A2 = CommandAdderA1.A2;
	const string B1 = CommandAdderA1.B1;
	const string B1a = $"{B1} {CommandAdderA1.B1a}";
	const string B1b = $"{B1} {CommandAdderA1.B1b}";
	static string[] AllCmds = new string[] { A1, A2, B1, B1a, B1b };

	private readonly ITestOutputHelper Output;

	public CommandCreatorTest ( ITestOutputHelper output ) {
		Output = output;
	}

	[Fact]
	public void HappyFlowTest () {
		CommandProcessor context = new ( Output.WriteLine );
		context.AddCommand ( new CommandAdderA1 () ); // Adding only the 'root' loader
		context.ProcessLine ( A1 ).Should ().BeOfType<ErrorCommandResult> ().Subject.Message.Should ().ContainAll ( "ommand", A1, "not found" );

		context.ProcessLine ( CommandAdderA1.Cmd ).Should ().BeOfType<CommandResult> ().Subject.Message.Should ().ContainAll ( new[] { A1, A2, B1 } );

		foreach ( var cmd in AllCmds ) {
			var res = context.ProcessLine ( cmd );
			res.Should ().NotBeNull ();
			res.Should ().NotBeOfType<ErrorCommandResult> ();
			res.Message.Should ().Be ( $"Test command {cmd.ToUpper ()} executed." );
		}
	}
}