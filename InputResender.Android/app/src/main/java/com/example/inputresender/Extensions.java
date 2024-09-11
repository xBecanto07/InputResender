package com.example.inputresender;

import com.example.inputresender.ui.Bus;

import java.io.File;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.file.Files;
import java.util.List;
import java.util.Vector;

public class Extensions {
    public static List<File> DeleteDir (String path){
        List<File> undeleted = new Vector<File>();
        // Recursively call this method to delete all files and the given folder
        File dir = new File(path);
        if (!dir.exists()) return undeleted;

        File[] files = dir.listFiles();
        if (files == null) return undeleted;

        for (File file : files) {
            if (file.isDirectory()) {
                DeleteDir(file.getAbsolutePath());
            } else {
                if (!file.delete()) undeleted.add(file);
            }
        }

        if (!dir.delete()) undeleted.add(dir);
        return undeleted;
    }

    public static Exception CopyAssetsFile(String srcPath, String dstPath) {
        try (InputStream in = Bus.Assets.open(srcPath);
             OutputStream out = Files.newOutputStream(new File(dstPath).toPath())) {
            byte[] buffer = new byte[4096];
            int read;
            while ((read = in.read(buffer)) != -1) out.write(buffer, 0, read);
            return null;
        } catch (Exception e) {
            return e;
        }
    }
}
