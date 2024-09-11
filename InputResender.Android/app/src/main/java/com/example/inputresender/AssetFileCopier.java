package com.example.inputresender;

import com.example.inputresender.ui.Bus;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.file.Files;
import java.util.Vector;
import java.io.File;
import java.util.List;

public class AssetFileCopier {
    public Vector<String> Log = new Vector<String>();
    public StringBuilder SB = new StringBuilder();
    public boolean Success;
    public Vector<File> Execs = new Vector<File>();

    public AssetFileCopier(String srcPath, String dstPath) {
        // srcPath: "CLI_x64" or "CLI_ARM64"
        // dstPath: "/data/user/0/com.example.inputresender/files"
        String rootFolder = dstPath + "/" + srcPath;
        Success = true;
        try {
            // If destination directory already exists, delete it
            if (new File(rootFolder).exists()) {
                List<File> undeleted = Extensions.DeleteDir(rootFolder);
                if (!undeleted.isEmpty()) {
                    SB.append("Failed to delete destination directory: ").append(rootFolder).append("\n");
                    for (File file : undeleted) {
                        SB.append("Failed to delete file: ").append(file.getAbsolutePath()).append("\n");
                    }
                    Success = false;
                    return;
                }
            }

            /*// Create destination directory
            if (!new File(dstPath).mkdirs()) {
                SB.append("Failed to create destination directory: ").append(dstPath).append("\n");
                Success = false;
                return;
            }*/

            // Copy files from source to destination
            Vector<String> dirs = new Vector<String>();
            dirs.add(srcPath);
            while (!dirs.isEmpty()) {
                //String assetSrcPath = Bus.LocalStoragePath + "/" + srcPath;
                String dir = dirs.remove(0);
                String[] paths = Bus.Assets.list(dir);
                if (paths == null) {
                    SB.append("Failed to list files in directory: ").append(dir).append("\n");
                    Success = false;
                    continue;
                }

                File parentDir = new File(dstPath + "/" + dir);
                if (parentDir.exists()) {
                    SB.append("Parent directory already exists: ").append(parentDir.getAbsolutePath()).append("\n");
                    Success = false;
                    return;
                } else {
                    if (!parentDir.mkdir()) {
                        SB.append("Failed to create parent directory: ").append(parentDir.getAbsolutePath()).append("\n");
                        Success = false;
                        return;
                    }
                }

                for (String path : paths) {
                    try {
                        String src = dir + "/" + path; // Since path was retuned by listing 'dir', this should be guaranteed to exist
                        InputStream inStream;
                        try {
                            inStream = Bus.Assets.open(src);
                        } catch (IOException e) {
                            // Probably a directory
                            dirs.add(src);
                            continue;
                        }

                        File dstFile = new File(dstPath + "/" + src);

                        Log.add("Copying file from =" + src + "= to =" + dstFile.getAbsolutePath() + "=");

                        try (OutputStream outStream = Files.newOutputStream(dstFile.toPath())) {
                            byte[] buffer = new byte[4096];
                            int read;
                            while ((read = inStream.read(buffer)) != -1) outStream.write(buffer, 0, read);

                            if (path.endsWith(".exe")) Execs.add(dstFile);
                        } catch (Exception e) {
                            SB.append("Failed to copy file from =").append(src).append("= to =").append(dstFile.getAbsolutePath()).append("=:\n - ").append(e.getMessage()).append("\n");
                            Success = false;
                            if (src.endsWith(".exe") || src.endsWith(".dll")) return; // Critical files, stop copying
                            continue;
                        }
                    } catch (Exception e) {
                        PrintException(e);
                        Success = false;
                    }
                }
            }
        } catch (Exception e) {
            PrintException(e);
            Success = false;
        }
    }

    private void PrintException (Exception e) {
        SB.append("\nError during copying CLI files: ").append(e.getMessage());
        SB.append("\n  Error: ").append((e.toString()));
        SB.append("\n  Cause: ").append((e.getCause()));
        SB.append("\n  StackTrace: ");
        for (StackTraceElement el : e.getStackTrace()) SB.append("\n    ").append(el.toString());
        SB.append("\n Last known lines:\n");
        for (int i = 0; i < 5; i++) {
            SB.append(Log.get(Log.size() - 5 + i)).append("\n");
        }
    }
}
