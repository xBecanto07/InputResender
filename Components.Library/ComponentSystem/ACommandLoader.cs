namespace Components.Library;

public abstract class ACommandLoader<CoreT> : DCommand<CoreT> where CoreT : CoreBase {
	public const string BaseLoadCmdName = "load-cmd";
	private string CmdGroupName { get; init; }
	public override string Description => $"Dynamically add '{CmdGroupName}' commands to the command processor";
	public override string Help => $"{parentCommandHelp} {CallName}";
	public ACommandLoader ( CoreT owner, string cmdGroupName)
		: base ( owner, null, [BaseLoadCmdName + '-' + cmdGroupName], [] ) => CmdGroupName = BaseLoadCmdName + '-' + cmdGroupName;

	protected abstract IReadOnlyCollection<Func<CoreT, DCommand<CoreT>>> NewCommands { get; }
	protected virtual IReadOnlyCollection<(string, Func<DCommand<CoreT>, DCommand<CoreT>>)> NewSubCommands => null;

	// Note that created command might not be actually added to the context
	protected sealed override CommandResult ExecIner ( CommandProcessor<CoreT>.CmdContext context ) {
		string ret = string.Empty;
		Dictionary<string, DCommand<CoreT>> commands = new ();
		Dictionary<string, Func<DCommand<CoreT>, DCommand<CoreT>>> subCommands = new ();
		Dictionary<string, ACommandLoader<CoreT>> cmdLoaders = new ();
		Queue<ACommandLoader<CoreT>> newLoaders = new ();

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
			ret += cmd.Value.CallName;
		}

		foreach ( var subCmd in subCommands ) {
			context.CmdProc.ModifyCommand ( subCmd.Key, subCmd.Value );
		}

		return new CommandResult ( ret );

		void PushCmds ( ACommandLoader<CoreT> loader ) {
			if ( loader.NewCommands != null ) {
				foreach ( var cmdAdder in loader.NewCommands ) {
					DCommand<CoreT> cmd = cmdAdder ( Owner );
					if ( cmd == null ) continue;
					if ( commands.ContainsKey ( cmd.CallName ) ) continue;

					if ( cmd is ACommandLoader<CoreT> loaderCmd ) newLoaders.Enqueue ( loaderCmd );
					else commands.Add ( cmd.CallName, cmd );
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