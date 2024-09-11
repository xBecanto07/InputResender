package com.example.inputresender.ui.notifications;

import androidx.lifecycle.LiveData;
import androidx.lifecycle.MutableLiveData;
import androidx.lifecycle.ViewModel;

import com.example.inputresender.ui.Bus;

public class NotificationsViewModel extends ViewModel {

    private final MutableLiveData<String> mText;

    public NotificationsViewModel() {
        mText = new MutableLiveData<>();
        StringBuilder sb = new StringBuilder();
        sb.append("Testing CLI...");

        sb.append(Bus.Localhost.ProcessLine("hook manager status") + "\n");

        mText.setValue(sb.toString());
    }

    public LiveData<String> getText() {
        return mText;
    }
}