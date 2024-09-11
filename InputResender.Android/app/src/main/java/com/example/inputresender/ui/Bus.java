package com.example.inputresender.ui;

import android.content.pm.PackageManager;
import android.content.res.AssetManager;

import com.example.inputresender.LocalhostClient;
import com.example.inputresender.MainActivity;
import com.example.inputresender.ui.dashboard.DashboardViewModel;
import com.example.inputresender.ui.home.HomeViewModel;
import com.example.inputresender.ui.notifications.NotificationsViewModel;

public class Bus {
    public static String CLIPath;
    public static String LocalStoragePath;
    public static String LocalTmpPath;
    public static AssetManager Assets;
    public static DashboardViewModel Dashboard;
    public static HomeViewModel Home;
    public static MainActivity Main;
    public static NotificationsViewModel Notifications;
    public static LocalhostClient Localhost;
    public static PackageManager PackageManager;
}
