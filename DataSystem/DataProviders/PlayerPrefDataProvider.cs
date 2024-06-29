using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class PlayerPrefDataProvider : IDataProvider
{
    private const string DATA_PATH = "DataSave";

    public void LoadProvider(Action successCallback, Action failCallback)
    {
        Debug.Log("PlayerPrefDataProvider LoadProvider");

        successCallback?.Invoke();
    }

    public GameData LoadData()
    {
        GameData data;
        if (PlayerPrefs.HasKey(DATA_PATH))
        {
            var dataString = PlayerPrefs.GetString(DATA_PATH);
            try
            {
                Debug.Log("try log datastring: " + dataString);

                data = Newtonsoft.Json.JsonConvert.DeserializeObject<GameData>(dataString);
                Debug.Log("Deserialized data: " + data);
            }
            catch (Exception e)
            {
                dataString = Regex.Replace(dataString, ",\\\"Chest\\\":{\\\"Silver\\\":{.*\\d\":\\d*}},", ",\"Chest\":{\"TotalChests\":{\"Silver\":{\"2\":2}},\"LastEarnedChests\":{}},");
                Debug.Log("Deserialized data after regex: " + dataString);
                Debug.Log("catch log: " + e);
                //  data = new GameData();
                try
                {
                    data = Newtonsoft.Json.JsonConvert.DeserializeObject<GameData>(dataString);
                }
                catch (Exception e1)
                {
                    data = new GameData();
                }
            }
        }
        else
            data = new GameData();
        return data;
    }

    public void SyncData(string gameData, bool isForced)
    {
        PlayerPrefs.SetString(DATA_PATH, gameData);
        PlayerPrefs.Save();
    }

    public void SyncData(GameData gameData, bool isForced)
    {
        SaveData(gameData);
    }

    private void SaveData(GameData gameData)
    {
        var dataString = Newtonsoft.Json.JsonConvert.SerializeObject(gameData);
        PlayerPrefs.SetString(DATA_PATH, dataString);
        PlayerPrefs.Save();
    }
}