using System;
using System.Collections.Generic;
using lib.Managers.AnalyticsSystem;
using UnityEngine;
using UnityEngine.Events;

public class DataService : Singleton<DataService>
{
	public bool IsReady { get; private set; }

	private const TutorialType SaveStartTutorial = TutorialType.OpenFirstTileTutorial;

	private bool isSuspicious = false;
	private IDataProvider dataProvider;
	private IDataProvider backupDataProvider;
	private GameData gameData = new GameData();
	private int lastGameDataSaveTime = 0;

	public UnityEvent DataServiceReadyEvent = new UnityEvent();
	
	private Dictionary<DataProviderType, IDataProvider> DataProviderDic = new Dictionary<DataProviderType, IDataProvider>()
	{
		{DataProviderType.PlayerPref, new  PlayerPrefDataProvider()},
		{DataProviderType.Cloud, new  CloudDataProvider()},
	};
   
	public bool IsDataServiceReady()
    {
		return !IsReady ;
    }

	public void LoadProviders()
    {
		Debug.Log("Data service load providers");
		dataProvider = DataProviderDic[ConfigurationService.Configurations.DataProviderType];
		Debug.Log("dataProvidername");

		backupDataProvider = DataProviderDic[ConfigurationService.Configurations.DataBackupProviderType];
		Debug.Log("dataProvidername1");

		AnalyticsManager.Instance.LoadDataEvent("start", PlayerPrefs.HasKey("PlayerId") ? PlayerPrefs.GetString("PlayerId") : "", false);
		dataProvider.LoadProvider(()=>LoadDataService(dataProvider), ()=>LoadDataService());
	}

	private void LoadDataService(IDataProvider provider = null)
    {
		GameData data = new GameData();
		bool isProvider;

		if(provider != null && ConfigurationService.Configurations.DataBackupProviderType != ConfigurationService.Configurations.DataProviderType)
        {
			var providerData = provider.LoadData();
			var backupProviderData = backupDataProvider.LoadData();
			isProvider = providerData.LastDataSaveTime >= backupProviderData.LastDataSaveTime;
			var mainProviderData = isProvider ? providerData : backupProviderData;
			//Debug.Log("provider dataversion: " + providerData.DataVersion);
			//Debug.Log("backup dataversion: " + backupProviderData.DataVersion);
			data = mainProviderData;
		}
		else
		{
			isProvider = false;
			data = backupDataProvider.LoadData();
		}

		gameData = data;
		lastGameDataSaveTime = gameData.LastDataSaveTime;
		//Debug.Log("dataVersion: " + dataVersion);

		if (!gameData.State.ContainsKey(StateType.CreateTime) || gameData.State[StateType.CreateTime] == 0)
		{
			gameData.State[StateType.CreateTime] = Timestamp.Now();
		}
		SetPlayerId();
		//SetCheaterState();

		try
		{
			Debug.Log("Try version updater");
			VersionUpdater.Update(ref gameData);
			Debug.Log("After version updater");
		}
		catch (Exception e)
        {
			Debug.Log("catch version updater exception: " + e);
		}

		IsReady = true;
		DataServiceReadyEvent?.Invoke();
		
		//GameStarter.instance.LoadGame();
		
		// Şimdilik yapılmamasına karar verildi!!!
		// TODO Emiran Datadan, Timestamp.Now() Event ve Shop için LastClaimTime ve InEvent gönderilecek. Event için yoksa 0 dönsün
		// Dictionary <string, int>
		// Bu parametre her seferinde eklenecek.
		AnalyticsManager.Instance.LoadDataEvent("end", (PlayerPrefs.HasKey("PlayerId") ? PlayerPrefs.GetString("PlayerId") : ""), isProvider);

		//Register Unity Focus and quit events
		Application.focusChanged += (focused) => { if (!focused) Save(); };
		Application.quitting += () => Save();
	}
	
	private void SetPlayerId()
    {
		if (!gameData.MetaData.ContainsKey(MetaDataType.PlayerId) || gameData.MetaData[MetaDataType.PlayerId] == "")
		{
#if !UNITY_EDITOR
			string gameName = "NiceFarm";
			int random6Number = UnityEngine.Random.Range(100000, 10000000);
			int creationTime = gameData.State[StateType.CreateTime];
			int random4Number = UnityEngine.Random.Range(1000, 10000);

			var playerId = gameName + "_" + random6Number.ToString() + "_" +
				creationTime.ToString() + "_" + random4Number.ToString();

			gameData.MetaData[MetaDataType.PlayerId] = playerId;

			var playerPrefId = PlayerPrefs.GetString("PlayerId");
			if (string.IsNullOrEmpty(playerPrefId))
			{
				PlayerPrefs.SetString("PlayerId", playerId);
				PlayerPrefs.Save();
			}
#else
            string playerId = SystemInfo.deviceUniqueIdentifier;
            gameData.MetaData[MetaDataType.PlayerId] = playerId;

            var playerPrefId = PlayerPrefs.GetString("PlayerId");
            if (string.IsNullOrEmpty(playerPrefId))
            {
                PlayerPrefs.SetString("PlayerId", playerId);
                PlayerPrefs.Save();
            }

            //Debug.Log("Clear all playerpref device id: " + gameData.MetaData[MetaDataType.PlayerId]);
#endif
        }
	}
	
	private void SetCheaterState()
	{
		if (!gameData.MetaData.ContainsKey(MetaDataType.CheaterState))
		{
			gameData.MetaData[MetaDataType.CheaterState] = CheaterState.Innocent.ToString();
			Debug.Log("Cheater State : " + CheaterState.Innocent.ToString());
		}
	}

    public void ResetData()
    {
        var playerId = "NiceFarm_Developer_Account";
        if (gameData.MetaData.ContainsKey(MetaDataType.PlayerId) && !(gameData.MetaData[MetaDataType.PlayerId] == ""))
        {
            playerId = gameData.MetaData[MetaDataType.PlayerId];
        }

        gameData = new GameData();
        gameData.MetaData = new Dictionary<MetaDataType, string>();
        gameData.MetaData[MetaDataType.PlayerId] = playerId;

        Save();
		IsReady = false;
    }

    private bool dontSaveActive;
    public void ResetDataAndDontSave()
    {
	    var playerId = "NiceFarm_Developer_Account";
	    if (gameData.MetaData.ContainsKey(MetaDataType.PlayerId) && !(gameData.MetaData[MetaDataType.PlayerId] == ""))
	    {
		    playerId = gameData.MetaData[MetaDataType.PlayerId];
	    }

	    gameData = new GameData();
	    gameData.MetaData = new Dictionary<MetaDataType, string>();
	    gameData.MetaData[MetaDataType.PlayerId] = playerId;
	    
	    
	    Save();
	    dontSaveActive = true;
	    IsReady = false;
    }

    public GameData GetData()
	{
		if (!IsReady)
		{
			isSuspicious = true;
			Debug.LogError("DataManager: GetData,  Reaching Data too Early");
		}
		return gameData;
	}

	public T GetData<T>(DataType DataName) where T : class, new()
	{
		if (!IsReady)
		{
			isSuspicious = true;
			Debug.LogError("DataManager: GetData,  Reaching Data too Early");
		}
		return gameData.GetData<T>(DataName);
	}

	private bool isSaving;
	public void SetData<T>(DataType DataName, T Value, bool syncImmediately = false) where T : class, new()
	{
		if (!IsReady)
		{
			Debug.LogError("DataManager: SetData,  Reaching Data too Early");
		}
		if (isSuspicious)
		{
			Debug.LogError("DataManager: SetData,  Writing suspicious data DataName; OldData: "
				+ Newtonsoft.Json.JsonConvert.SerializeObject(gameData.GetData<T>(DataName)) + ", NewData"
				+ Newtonsoft.Json.JsonConvert.SerializeObject(Value)
				);
		}
		gameData.SetData(DataName, Value);
		if (syncImmediately && !isSaving)
		{
			isSaving = true;
			CoroutineDispatcher.ExecuteWithDelay(1f,() =>
			{
				Save();
				isSaving = false;
			});
		}
	}

	private void Save()
	{
		if (!TutorialManager.Instance.IsTutorialCompleted(SaveStartTutorial)) return;
		if(dontSaveActive) return;
		if(BotManager.Instance.botStarted) return;

		lastGameDataSaveTime = Timestamp.Now();
		gameData.LastDataSaveTime = lastGameDataSaveTime;

		var dataString = Newtonsoft.Json.JsonConvert.SerializeObject(gameData);

		dataProvider.SyncData(dataString, true);
		if(ConfigurationService.Configurations.DataBackupProviderType != ConfigurationService.Configurations.DataProviderType)
			backupDataProvider.SyncData(dataString, true);
	}

}