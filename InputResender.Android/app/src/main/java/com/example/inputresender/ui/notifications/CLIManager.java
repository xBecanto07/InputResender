package com.example.inputresender.ui.notifications;

import com.example.inputresender.ui.Bus;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.io.IOException;

public class CLIManager {
    public StringBuilder Log = new StringBuilder();
    private  Process process;
    private BufferedReader reader;
    private BufferedWriter writer;

    public boolean StartCLI() {
        try {
            //process = Runtime.getRuntime().exec(Bus.CLIPath);
            process = Runtime.getRuntime().exec(new String[]{"su", "-c", Bus.CLIPath});
            reader = new BufferedReader(new InputStreamReader(process.getInputStream()));
            writer = new BufferedWriter(new OutputStreamWriter(process.getOutputStream()));
            return true;
        } catch (IOException e) {
            Log.append("Error starting CLI: ").append(e.getMessage()).append("\n");
            return false;
        }
    }

    public CLIResponse SendCommand(String command) {
        try {
            writer.write(command);
            writer.newLine();
            writer.flush();
            return new CLIResponse(command, reader);
        } catch (IOException e) {
            Log.append("Error sending command to CLI: ").append(e.getMessage()).append("\n");
            return null;
        }
    }
}

