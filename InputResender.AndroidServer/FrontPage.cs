using Components.Library;
using InputResender.CLI;
using InputResender.Services;

namespace InputResender.AndroidServer;

public class FrontPage : ContentPage {
	Entry entry;
	Label label;
	public CommandProcessor cmdProcessor;
	public ConsoleManager console;
	LocalhostServer server;
	IDispatcherTimer timer;
	readonly List<string> messages = new ();
	string lastText = string.Empty;

	public FrontPage () {
		entry = new () {
			Placeholder = "Type here...",
			BackgroundColor = Colors.Black,
			TextColor = Colors.LimeGreen,
			HeightRequest = 50,
			FontSize = 20,
			FontAttributes = FontAttributes.Bold,
			FontFamily = "Courier New",
			MaxLength = 1000,
			IsTextPredictionEnabled = false,
			IsSpellCheckEnabled = false,
		};

		label = new () {
			BackgroundColor = Colors.Black,
			TextColor = Colors.LimeGreen,
			FontSize = 20,
			FontAttributes = FontAttributes.Bold,
			FontFamily = "Courier New",
			LineBreakMode = LineBreakMode.WordWrap,
			Text = "Hello, World!",
		};

		Init ();

		Content = new VerticalStackLayout { Children = { entry, new ScrollView { Content = label, }, } };

		entry.TextChanged += Entry_TextChanged;

		timer = Dispatcher.CreateTimer ();
		timer.Interval = TimeSpan.FromMilliseconds ( 250 );
		timer.Tick += OnTick;
		timer.Start ();
	}

	private void OnTick ( object sender, EventArgs e ) {
		if ( messages.Count == 0 ) return;
		lock ( messages ) {
			foreach ( var msg in messages ) {
				switch ( msg[0] ) {
				case 'L': WriteLine ( msg[1..] ); break;
				case 'A': Write ( msg[1..] ); break;
				case 'C': label.Text = string.Empty; break;
				default: WriteLine ( $"Unknown message type: ({msg})" ); break;
				}
			}
		}
		messages.Clear ();
	}

	private void WriteLine ( string s ) => label.Text = lastText = s + "\n" + lastText;
	private void Write (string s) {
		var lines = lastText.Split ( '\n' );
		if ( lines.Length == 0 ) lastText = s;
		else lastText = lines[0] + s + string.Join ( "\n", lines.Skip ( 1 ) );
		label.Text = lastText;
	}

	public void PushLine (string s) {
		lock ( messages ) messages.Add ( 'L' + s );
	}
	public void PushText (string s) {
		lock ( messages ) messages.Add ( 'A' + s );
	}
	public void PushClear () {
		lock ( messages ) messages.Add ( "C" );
	}

	private void Init () {
		console = new ConsoleManager (
			PushLine,
			() => string.Empty, // All reading should be done as even handling, but this will lead to inconsistency between actual text, content of 'console' and what is expected to be displayed
			PushText
			);
		cmdProcessor = new ( PushLine );
		cmdProcessor.AddCommand ( new BasicCommands ( console.WriteLine, PushClear, () => { } ) );
		cmdProcessor.AddCommand ( new FactoryCommandsLoader () );

		cmdProcessor.ProcessLine ( "loadall" );
		cmdProcessor.ProcessLine ( "safemode on" );
		cmdProcessor.ProcessLine ( "core new" );
		//ExecLine ( "core new" );
		ExecLine ( "core own" );
		ExecLine ( "loglevel all" );
		ExecLine ( "network callback recv print" );
		ExecLine ( "network callback newconn print" );
		ExecLine ( "hook manager start" );

		server = new ( this );
	}

	private void Entry_TextChanged (object sender, TextChangedEventArgs e) {
		if ( sender != entry ) return;
		string line	= e.NewTextValue;
		if ( !line.EndsWith ( ";" ) ) return; // Wait for the end of the command
		line = line[..^1];


		if ( string.IsNullOrWhiteSpace ( line ) ) {
			console.WriteLine ( "" );
			return;
		}
		ExecLine ( line );

		entry.Text = string.Empty;
	}

	public string ExecLine (string line ) {
		console.WriteLine ( $"$> {line}" );
		try {
			var res = cmdProcessor.ProcessLine ( line );
			string resTxt = Program.PrintResult ( res, console, Config.PrintFormat.Batch, 80 );
			cmdProcessor?.Owner?.FlushDelayedMsgs ( s => {
				resTxt += $"\u000C{s}";
				console.WriteLine ( s );
			} );
			return resTxt;
		} catch ( Exception ex ) {
			string err = $"Error: {ex.Message}";
			console.WriteLine ( err );
			return err;
		}
	}
}