package com.example.inputresender.ui.dashboard;

import androidx.lifecycle.LiveData;
import androidx.lifecycle.MutableLiveData;
import androidx.lifecycle.ViewModel;

import com.example.inputresender.AssetFileCopier;
import com.example.inputresender.LocalhostClient;
import com.example.inputresender.ui.Bus;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;

public class DashboardViewModel extends ViewModel {

    private final MutableLiveData<String> mText;

    public DashboardViewModel() {
        Bus.Dashboard = this;
        mText = new MutableLiveData<>();

        try {
            Bus.Localhost = new LocalhostClient();
            Bus.Localhost.Connect();
            mText.setValue("Connected to localhost");
        } catch (Exception e) {
            mText.setValue("Failed to connect to localhost: " + e.getMessage());
        }
    }

    public LiveData<String> getText() {
        return mText;
    }
}