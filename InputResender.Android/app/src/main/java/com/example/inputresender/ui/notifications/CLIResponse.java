package com.example.inputresender.ui.notifications;

import java.io.BufferedReader;

public class CLIResponse {
    public String Cmd;
    public String Header;
    public String Body;
    
    public CLIResponse(String Cmd, BufferedReader reader) {
        StringBuilder sb = new StringBuilder();
        this.Cmd = Cmd;
        try {
            while (true) {
                int nextChar = reader.read();

                if (nextChar < -1) {
                    Header = "CLI closed";
                    Body = sb.toString();
                    return;
                }

                switch (nextChar) {
                    case 1: // SOH: Start reading header
                        sb.setLength(0);
                    case 2: // STX: Start reading body
                        Header = sb.toString();
                        sb.setLength(0);
                        break;
                    case 3: // ETX: End of response
                        Body = sb.toString();
                        return;
                    default:
                        sb.append((char) nextChar);
                }
            }
        } catch (Exception e) {
            Header = "Critical error";
            Body = "Failed to read response from CLI: " + e.getMessage() + "\nWas able to read: " + sb.toString();
        }
    }
}
