package com.example.inputresender;

import android.os.StrictMode;

import java.net.DatagramSocket;
import java.net.DatagramPacket;
import java.net.InetAddress;
import java.nio.charset.StandardCharsets;
import java.util.Queue;

public class LocalhostClient {
    private DatagramSocket udpClient;
    private InetAddress localAddress;
    private InetAddress serverAddress;
    private int localPort;
    private int serverPort;
    int BUFFER_SIZE = 4096;
    private byte[] buffer = new byte[BUFFER_SIZE];

    public String ProcessLine (String line) {
        byte[] data = line.getBytes(StandardCharsets.UTF_8);
        DatagramPacket sendPacket = new DatagramPacket(data, data.length, serverAddress, serverPort);
        try {
            udpClient.send(sendPacket);
        } catch (Exception e) {
            throw new RuntimeException("Failed to send data: " + e.getMessage());
        }

        RecvACK();

        DatagramPacket recvPacket = new DatagramPacket(buffer, buffer.length);
        try {
            udpClient.setSoTimeout(20000);
            udpClient.receive(recvPacket);
            return new String(recvPacket.getData(), 0, recvPacket.getLength(), StandardCharsets.UTF_8);
        } catch (Exception e) {
            throw new RuntimeException("Failed to receive data: " + e.getMessage());
        }
    }

    public void Connect () {
        try {
            StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().permitAll().build();
            StrictMode.setThreadPolicy(policy);
            localAddress = InetAddress.getByName("127.0.0.1");
            serverAddress = InetAddress.getByName("127.0.0.1");
        } catch (Exception e) {
            throw new RuntimeException("Failed to prepare addresses: " + e.getMessage());
        }

        for (int port = 40300; port <= 40400; port += 10) {
            try {
                udpClient = new DatagramSocket(port);
                localPort = port;
                break;
            } catch (Exception e) {
                if (port == 40400) throw new RuntimeException("Failed to prepare client: " + e.getMessage());
            }
        }

        byte[] connReq = new byte[]{0x05};
        for (int port = 40500; port <= 40600; port += 10) {
            try {
                serverPort = port;
                DatagramPacket sendPacket = new DatagramPacket(connReq, connReq.length, serverAddress, serverPort);
                udpClient.send(sendPacket);

                RecvACK();
                return;
            } catch (Exception e) {
                if (port == 40600) throw new RuntimeException("Failed to connect to server: " + e.getMessage());
            }
        }
    }

    private void RecvACK () {
        DatagramPacket recvPacket = new DatagramPacket(buffer, buffer.length);
        try {
            udpClient.setSoTimeout(200);
            udpClient.receive(recvPacket);
            int len = recvPacket.getLength();
            if (len != 1 || recvPacket.getData()[0] != 0x06)
                throw new RuntimeException("Wrong ACK received");
        } catch (Exception e) {
            throw new RuntimeException("Failed to receive ACK: " + e.getMessage());
        }
    }
}
