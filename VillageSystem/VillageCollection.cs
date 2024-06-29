using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VillageCollection", menuName = "lib/VillageCollection")]
public class VillageCollection : ScriptableObject
{
    public List<VillageHomeData> VillageHomeDatas = new List<VillageHomeData>();

    public List<VillagersConversationData> VillagersConversationDatas = new List<VillagersConversationData>();

    public static VillageCollection LoadCollection(string collectionPath)
    {
        return Resources.Load<VillageCollection>(collectionPath);
    }
}

[Serializable]
public class VillageHomeData
{
    public string HomeName = "";
    public int HomeID;
    public List<GameObject> HomeUpgradeBuildings;
    public List<IdleNumber> HomeUpgradesRequirements;
#if UNITY_EDITOR
    [Searchable]
#endif
    public PoolType[] ReleatedHomeSource;
    public VillageUpgradeData[] UpgradeDatas;
    public string HomeUpgradeSkillName;
    public string HomeUpgradeSkillInfo;
}

[Serializable]
public class VillageUpgradeData
{
    public int EffectLevel;
    public IdleUpgradeType UpgradeType;
    public GeneralUpgradeType GeneralUpgradeType;
    public ObjectDataType ObjectDataType = ObjectDataType.ProductCount;
    public float Multiply;
    public float Additon;
}

[Serializable]
public class VillagersConversationData
{
    public bool allHousesUnlocked;
    public bool isAutoConversation;
    public bool isClickConversation;
    public string[] conversations;
}
