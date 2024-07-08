using Components.Implementations;
using Components.ImplementationTests;
using Components.Interfaces;
using Components.Library;
using InputResender.Services;
using InputResender.Services.NetClientService.InMemNet;
using InputResender.ServiceTests.NetServices;
using System;
using Xunit.Abstractions;


namespace InputResender.Cmd;
public class Program {
	static CoreBase core;
	static VPacketSender packetSender;

	static void Main ( string[] args ) {
		core = new CoreBaseMock ();
		VLogger logger = new ( core );
		logger.OnLog += ( msg ) => Console.WriteLine ( msg );

		Console.WriteLine ( "Program started ..." );
		while (true) {
			if ( !ProcessLine () ) break;
		}

		Console.WriteLine ( "Press any key to exit" );
		Console.ReadKey ();
	}

	private static bool ProcessLine () {
		string[] args = new string[16];
		for ( int i = 0; i < 16; i++ ) args[i] = string.Empty;
		string[] strings = Console.ReadLine ().Split ( ' ' );
		Array.Copy ( strings, args, strings.Length );
		if ( args[0] == "" ) return true;

		switch ( args[0] ) {
		case "start": try {
				packetSender ??= new ( core, -1, ( msg, e ) => Console.WriteLine ( $"{msg}{Environment.NewLine}{e.Message}{Environment.NewLine}{e.StackTrace}" ) );
				PrintNetworks ( packetSender );
				Console.WriteLine ( "Started PacketSender with following networks ..." );
			} catch ( Exception e ) {
				Console.WriteLine ( $"Error when starting: {e.Message}{Environment.NewLine}{e.StackTrace}" );
				if ( packetSender != null ) packetSender.Destroy ();
				packetSender = null;
			}
			return true;
		case "exit": return false;
		case "networks": PrintNetworks ( packetSender ); return true;
		case "connect":
			if ( args[1].Length < 4 ) { Console.WriteLine ( "Missing argument" ); return true; } else {
				try {
					packetSender.Connect ( args[1] );
					Console.WriteLine ( "Connected" );
					return true;
				} catch ( Exception e ) {
					Console.WriteLine ( $"Error when connecting: {e.Message}{Environment.NewLine}{e.StackTrace}" );
					return true;
				}
			}
		case "recvStart":
			packetSender.ReceiveAsync ( ( data ) => {
				Console.WriteLine ( $"Received {data.Length} bytes ({System.Text.Encoding.UTF8.GetString ( data )}" );
				return true;
			} );
			Console.WriteLine ( "Started receiving" );
			return true;
		case "recvStop":
			packetSender.ReceiveAsync ( null );
			Console.WriteLine ( "Stopped receiving" );
			return true;
		case "send":
			if ( args[1].Length < 4 ) { Console.WriteLine ( "Missing argument" ); return true; } else {
				try {
					packetSender.Send ( System.Text.Encoding.UTF8.GetBytes ( args[1] ) );
					Console.WriteLine ( "Sent" );
					return true;
				} catch ( Exception e ) {
					Console.WriteLine ( $"Error when sending: {e.Message}{Environment.NewLine}{e.StackTrace}" );
					return true;
				}
			}
		default: Console.WriteLine ( "Unknown command" ); return true;
		}
	}

	static void PrintNetworks ( VPacketSender packetSender ) {
		for (int i = 0; i < packetSender.EPList.Count; i++ ) {
			Console.WriteLine ( $"Network {i}:" );
			var network = packetSender.EPList[i];
			for (int j = 0; j < network.Count; j++ )
				Console.WriteLine ( $"  TTL {j} = {network[j].Address}:{network[j].Port}" );
		}
	}
}

class Outputer : ITestOutputHelper {
	public void WriteLine ( string message ) {
		System.Console.WriteLine ( message );
	}
	public void WriteLine ( string format, params object[] args ) {
		System.Console.WriteLine ( format, args );
	}
}