namespace Components.Library;

public abstract class ACommandLoader : ACommand {
	public const string BaseLoadCmdName = "load-cmd";
	protected abstract string CmdGroupName { get; }
	public override string Description => $"Dynamically add '{CmdGroupName}' commands to the command processor";
	public override string Help => $"{parentCommandHelp} {commandNames.First ()}";
	public ACommandLoader () : base ( null ) => commandNames.Add ( BaseLoadCmdName + '-' + CmdGroupName );

	protected abstract IReadOnlyCollection<Func<ACommand>> NewCommands { get; }
	protected virtual IReadOnlyCollection<(string, Func<ACommand, ACommand>)> NewSubCommands => null;

	// Note that created command might not be actually added to the context
	protected sealed override CommandResult ExecIner ( CommandProcessor.CmdContext context ) {
		string ret = string.Empty;
		Dictionary<string, ACommand> commands = new ();
		Dictionary<string, Func<ACommand, ACommand>> subCommands = new ();
		Dictionary<string, ACommandLoader> cmdLoaders = new ();
		Queue<ACommandLoader> newLoaders = new ();

		PushCmds ( this );

		while ( newLoaders.Any () ) {
			var cmdLoader = newLoaders.Dequeue ();
			if ( cmdLoader == null ) continue;
			if ( cmdLoaders.ContainsKey ( cmdLoader.CmdGroupName ) ) continue;
			cmdLoaders.Add ( cmdLoader.CmdGroupName, cmdLoader );
			PushCmds ( cmdLoader );
		}

		foreach ( var cmd in commands ) {
			context.CmdProc.AddCommand ( cmd.Value );
			if ( !string.IsNullOrEmpty ( ret ) ) ret += Environment.NewLine;
			ret += cmd.Value.Help;
		}

		foreach ( var subCmd in subCommands ) {
			context.CmdProc.ModifyCommand ( subCmd.Key, subCmd.Value );
		}

		return new CommandResult ( ret );

		void PushCmds ( ACommandLoader loader ) {
			if ( loader.NewCommands != null ) {
				foreach ( var cmdAdder in loader.NewCommands ) {
					ACommand cmd = cmdAdder ();
					if ( cmd == null ) continue;
					if ( commands.ContainsKey ( cmd.Help ) ) continue;

					if ( cmd is ACommandLoader loaderCmd ) newLoaders.Enqueue ( loaderCmd );
					else commands.Add ( cmd.Help, cmd );
				}
			}
			if ( loader.NewSubCommands != null ) {
				foreach ( var subCmdAdder in loader.NewSubCommands ) {
					if ( subCmdAdder.Item2 == null ) continue;
					if ( !subCommands.ContainsKey ( subCmdAdder.Item1 ) )
						subCommands.Add ( subCmdAdder.Item1, subCmdAdder.Item2 );
				}
			}
		}
	}
}