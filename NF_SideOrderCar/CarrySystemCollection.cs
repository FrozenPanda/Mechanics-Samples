using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CarrySystemCollection", menuName = "lib/CarrySystemCollection")]
public class CarrySystemCollection : ScriptableObject
{
    [Header("---- Side Order Car Options ----")]
    public List<int> SideOrderCarSkins = new List<int>();
    public List<Sprite> SideOrderCarSprites = new List<Sprite>();
    public List<LevelUpConditions> SideOrderCarLevelUpCosts = new List<LevelUpConditions>();
    public List<CarryCapacity> SideOrderCarryCapacities = new List<CarryCapacity>();

    [Space] [Header("---- Inventory House Options ----")]
    public List<LevelUpConditions> InventoryLevelUpCosts = new List<LevelUpConditions>();
    public List<CarryCapacity> InventoryCapacities = new List<CarryCapacity>();

    [Space] [Header("---- Manager Options -----")]
    public List<ManagerLevelByCity> ManagerAutomateStates = new List<ManagerLevelByCity>();

    [Space] [Header("---- Carrier PATH Options")]
    public List<CarrierPathInfo> CarrierPathInfos = new List<CarrierPathInfo>();
    
    public static CarrySystemCollection LoadCollection(string collectionPath)
    {
        return Resources.Load<CarrySystemCollection>(collectionPath);
    }
}

[Serializable]
public struct LevelUpConditions
{
    public int StartLevel;
    public IdleNumber startNumber;
    public float IncreasePerLevel;
}

[Serializable]
public struct CarryCapacity
{
    public int StartLevel;
    public IdleNumber startNumber;
    public float IncreasePerLevel;
}

[Serializable]
public struct ManagerLevelByCity
{
    public int cityID;
    public List<int> byLevel;
}

[Serializable]
public struct CarrierPathInfo
{
    public string marketID;
    public int pathConnectIndex;
}

