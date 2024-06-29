using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.EssentialKit;
using VoxelBusters.CoreLibrary;
using System;
using System.Text.RegularExpressions;

public class CloudDataProvider : IDataProvider
{
    //private const string SavedGameDataKey = "SavedGameData";
    private const string SavedGameDataKey = "CloudSavedGameData";
    private Action successAction;
    private Action failAction;
    private GameData cloudGameData = null;

    public void LoadProvider(Action successCallback, Action failCallback)
    {
        Debug.Log("Load clodud data provider");
        successAction = successCallback;
        failAction = failCallback;

        Debug.Log("CloudServices.IsAvailable()->" + CloudServices.IsAvailable());
        if (CloudServices.IsAvailable())
        {
            CloudServices.OnSynchronizeComplete += OnSynchronizeComplete;
            Debug.Log("CloudServices.Synchronize->");
            
            CloudServices.Synchronize();
        }
    }

    private void OnSynchronizeComplete(CloudServicesSynchronizeResult result)
    {
        Debug.Log("Received synchronize finish callback.");
        Debug.Log("Status: " + result.Success);
        //If will be false if user deny the authentication or due to network error.
        bool castedData = LoadCloudGameData();
        if(!castedData)
        {
            failAction?.Invoke();
            return;
        }
        if (result.Success && cloudGameData != null)
        {
            Debug.Log("success OnSynchronizeComplete");
            successAction?.Invoke();
        }
        else
        {
            Debug.Log("fail OnSynchronizeComplete");
            failAction?.Invoke();
        }

        // By this time, you have the latest data from cloud and you can start reading.
    }

    public GameData LoadData()
    {
        return cloudGameData;
    }

    private bool LoadCloudGameData()
    {
        var dataString = CloudServices.GetString(SavedGameDataKey);
        if(!string.IsNullOrEmpty(dataString))
        {
            Debug.Log("datastring null degil");
            try
            {
                cloudGameData = Newtonsoft.Json.JsonConvert.DeserializeObject<GameData>(dataString);
                return true;
            }
            catch (Exception e)
            {
                dataString = Regex.Replace(dataString, ",\\\"Chest\\\":{\\\"Silver\\\":{.*\\d\":\\d*}},", ",\"Chest\":{\"TotalChests\":{\"Silver\":{\"2\":2}},\"LastEarnedChests\":{}},");

                try
                {
                    cloudGameData = Newtonsoft.Json.JsonConvert.DeserializeObject<GameData>(dataString);
                    return true;
                }
                catch (Exception e1)
                {
                    cloudGameData = new GameData();
                    return false;
                }
            }
        }
        return false;
    }

    public void SyncData(string gameData, bool isForced)
    {
        CloudServices.SetString(SavedGameDataKey, gameData);
    }

    public void SyncData(GameData gameData, bool isForced)
    {
        SaveData(gameData);
    }

    private void SaveData(GameData gameData)
    {
        var dataString = Newtonsoft.Json.JsonConvert.SerializeObject(gameData);
        CloudServices.SetString(SavedGameDataKey, dataString);

    }
}
