﻿using Components.Implementations;
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
			if ( !ProcessLine ( Console.ReadLine (), Console.WriteLine ) ) break;
		}

		Console.WriteLine ( "Press any key to exit" );
		Console.ReadKey ();
	}

	private static bool ProcessLine (string line, Action<string> WriteLine) {
		ArgParser parser = new ( line, WriteLine );
		if ( parser.ArgC == 0 ) return true;

		switch (parser.String(0, "Command") ) {
		case "start": try {
				packetSender ??= new ( core, -1, ( msg, e ) => WriteLine ( $"{msg}{Environment.NewLine}{e.Message}{Environment.NewLine}{e.StackTrace}" ) );
				PrintNetworks ( packetSender );
				WriteLine ( "Started PacketSender with following networks ..." );
			} catch ( Exception e ) {
				WriteLine ( $"Error when starting: {e.Message}{Environment.NewLine}{e.StackTrace}" );
				if ( packetSender != null ) packetSender.Destroy ();
				packetSender = null;
			}
			return true;
		case "exit": return false;
		case "networks": PrintNetworks ( packetSender ); return true;
		case "connect":
			if ( parser.String ( 1, "Target EP", 4 ) == null ) return true;
			else {
				try {
					packetSender.Connect ( parser.String ( 1, null ) );
					WriteLine ( "Connected" );
					return true;
				} catch ( Exception e ) {
					WriteLine ( $"Error when connecting: {e.Message}{Environment.NewLine}{e.StackTrace}" );
					return true;
				}
			}
		case "recvStart":
			packetSender.ReceiveAsync ( ( data ) => {
				WriteLine ( $"Received {data.Length} bytes ({System.Text.Encoding.UTF8.GetString ( data )}" );
				return true;
			} );
			WriteLine ( "Started receiving" );
			return true;
		case "recvStop":
			packetSender.ReceiveAsync ( null );
			WriteLine ( "Stopped receiving" );
			return true;
		case "send":
			if ( parser.String (1, "Message", 3) == null ) return true;
			else {
				try {
					packetSender.Send ( System.Text.Encoding.UTF8.GetBytes ( parser.String ( 1, null ) ) );
					WriteLine ( "Sent" );
					return true;
				} catch ( Exception e ) {
					WriteLine ( $"Error when sending: {e.Message}{Environment.NewLine}{e.StackTrace}" );
					return true;
				}
			}
		default: WriteLine ( "Unknown command" ); return true;
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