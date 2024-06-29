using System;
using System.Collections.Generic;

public interface IDataProvider
{
    void LoadProvider(Action successCallback, Action failCallback);
    GameData LoadData();
    void SyncData(GameData gameData, bool isForced);
    void SyncData(string gameData, bool isForced);
}