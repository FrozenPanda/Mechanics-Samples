using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VersionUpdater
{
    public static void Update(ref GameData gameData)
    {
        int version = VersionFormatter(Application.version);
        //Version000000002(ref gameData);
        //Version000000008(ref gameData);
        //Version000000009(ref gameData);
#if !UNITY_EDITOR
    //Version000000007(ref gameData);
#endif
        gameData.State[StateType.Version] = version;

        if (!gameData.State.ContainsKey(StateType.StartingVersion))
        {
            gameData.State[StateType.StartingVersion] = version;
        }
    }

    public static int VersionFormatter(string version)
    {
        var numbers = version.Split('.');
        int vers = int.Parse(numbers[0]) * 1000000;
        int major = int.Parse(numbers[1]) * 1000;
        int minor = int.Parse(numbers[2]);

        int formattedVersion = vers + major + minor; 
        return formattedVersion;
    }
    
    private static void GiveGem(ref GameData gameData, IdleNumber gemAmount)
    {
        GiveCurrency(ref gameData, CurrencyType.Gem, gemAmount);
    }    
    
    private static void GiveDollar(ref GameData gameData, IdleNumber dollarAmount)
    {
        GiveCurrency(ref gameData, CurrencyType.Dollar, dollarAmount);
    }    
    
    private static void GiveCurrency(ref GameData gameData, CurrencyType currencyType, IdleNumber currencyAmount)
    {
        if (!gameData.IdleCurrency.ContainsKey(currencyType))
        {
            gameData.IdleCurrency[currencyType] = new IdleNumber(0, NumberDigits.Empty);
        }

        gameData.IdleCurrency[currencyType] += currencyAmount;
    } 
    
    private static void GiveManager(ref GameData gameData, int managerId, int cardCount, int managerLevel = -1)
    {
        gameData.CollectableItem ??= new Dictionary<CollectableItemType, Dictionary<int, CardSaveData>>();
        
        var datas = gameData.CollectableItem;
        
        if (!datas.ContainsKey(CollectableItemType.DirectorCard))
        {
            datas[CollectableItemType.DirectorCard] = new Dictionary<int, CardSaveData>();
        }

        if (!datas[CollectableItemType.DirectorCard].ContainsKey(managerId))
        {
            datas[CollectableItemType.DirectorCard][managerId] = new CardSaveData(1);
        }
        
        if(managerLevel > 0) datas[CollectableItemType.DirectorCard][managerId].Level = managerLevel;

        datas[CollectableItemType.DirectorCard][managerId].Count += cardCount;
        Debug.Log($" managerId :{managerId}, managerLevel :{managerLevel}, card count :{cardCount}");
    }
    
    private static void GiveCard(ref GameData gameData, int cardId, int cardCount, int cardLevel = -1)
    {
        gameData.CollectableItem ??= new Dictionary<CollectableItemType, Dictionary<int, CardSaveData>>();
        
        var datas = gameData.CollectableItem;
        
        if (!datas.ContainsKey(CollectableItemType.Card))
        {
            datas[CollectableItemType.Card] = new Dictionary<int, CardSaveData>();
        }

        if (!datas[CollectableItemType.Card].ContainsKey(cardId))
        {
            datas[CollectableItemType.Card][cardId] = new CardSaveData(1);
        }

        if(cardLevel > 0) datas[CollectableItemType.Card][cardId].Level = cardLevel;
        
        datas[CollectableItemType.Card][cardId].Count += cardCount;
    }
    

    private static void ResetStickerStateData(ref GameData gameData)
    {
        gameData.EventStickerStateDatas.Clear();
        gameData.PlayerModeStickerStateDatas.Clear();
    }
    
#region 0.0.2
    private static readonly Dictionary<int, Dictionary<int, int>> LevelChestIds = new()
    {
        {0, new Dictionary<int, int>(){}},
        {1, new Dictionary<int, int>()
        {
            // (id:0)BasicBox(1) + (id:1)BasicBox (2) + 3x(id : 2)Lucky Box
            /*{0, 1}, {1, 1},*/ {2, 3},
        }},
        {2, new Dictionary<int, int>()
        {
            //3x(id : 2)Lucky Box + x2 (id : 4)Silver Box + (id : 5)Silver Box (1)
            {2, 3}, {4, 2}, /*{5, 1},*/
        }},
        {3, new Dictionary<int, int>()
        {
            //3x(id : 2)Lucky Box  + x3 (id : 4)Silver Box + (id : 7)Silver Box (2) + (id : 6)Gold Box (2)
            {2, 3}, {4, 3},/* {6, 1}, {7, 1},*/
        }},
        {4, new Dictionary<int, int>()
        {
            //6x(id : 2)Lucky Box + x4 (id : 4)Silver Box + (id : 9)Silver Box (3) + (id : 10)Silver Box (4) + (id : 8)Gold Box (3)
            {2, 6}, {4, 4},/* {8, 1}, {9, 1}, {10, 1},*/
        }}, 
        {5, new Dictionary<int, int>()
        {
            //6x(id : 2)Lucky Box + x6 (id : 4)Silver Box +  (id : 11)Silver Box (5) + (id : 12)Silver Box (6)
            {2, 6}, {4, 6},/* {11, 1}, {12, 1}*/
        }},
        {6, new Dictionary<int, int>()
        {
            //9x(id : 2)Lucky Box + x8 (id : 4)Silver Box + (id : 13)Silver Box (7) + (id : 14)Silver Box (8)
            {2, 9}, {4, 8}, /*{13, 1}, {14, 1}*/
        }},
    };

    private static readonly Dictionary<ChestType, List<int>> _chestTypeDictionary = new Dictionary<ChestType, List<int>>()
    {
        {ChestType.Basic, new List<int>() {0, 1}},
        {ChestType.Lucky, new List<int>() {2}},
        {ChestType.Silver, new List<int>() {4, 5, 7, 9, 10, 11, 12, 13, 14}},
        {ChestType.Gold, new List<int>() {6, 8, 15}},
    };
    private static void Version000000002(ref GameData gameData)
    {
        if (!gameData.State.TryGetValue(StateType.Version, out int version) || version < 0000002)
        {
            if (gameData.State.TryGetValue(StateType.Level, out int level))
            {
                ResetStickerStateData(ref gameData);
                
                gameData.Chest.Clear();
                gameData.Chest[ChestDataType.TotalChests] = new Dictionary<ChestType, Dictionary<int, int>>();
                
                var earnedChests = GetEarnedChestsByActiveLevel(level);
                if(earnedChests.Count <= 0) return;
                foreach (var chestsPair in earnedChests)
                {
                    ChestType chestType = GetChestType(chestsPair.Key);
                    if (!gameData.Chest[ChestDataType.TotalChests].ContainsKey(chestType))
                    {
                        gameData.Chest[ChestDataType.TotalChests][chestType] = new Dictionary<int, int>();
                    }
                    if (!gameData.Chest[ChestDataType.TotalChests][chestType].ContainsKey(chestsPair.Key))
                    {
                        gameData.Chest[ChestDataType.TotalChests][chestType][chestsPair.Key] = 0;
                    }
                    
                    gameData.Chest[ChestDataType.TotalChests][chestType][chestsPair.Key] += chestsPair.Value;
                }

                foreach (var parentDic in gameData.Chest[ChestDataType.TotalChests])
                {
                    foreach (var pair in parentDic.Value)
                    {
                        ColoredLogUtility.PrintColoredLog($"EarnedChests => Type : {parentDic.Key}, Id :{pair.Key}, Count : {pair.Value}", LogColor.Green);
                    }
                }

                GiveExtraRewardsV002(ref gameData,  level);
            }
        }
    }

    private static void Version000000007(ref GameData gameData)
    {
        var playerId = "NiceFarm_Developer_Account";

        if (!gameData.State.TryGetValue(StateType.Version, out int version) || version < 0000007)
        {
            if (gameData.MetaData.ContainsKey(MetaDataType.PlayerId) && !(gameData.MetaData[MetaDataType.PlayerId] == ""))
            {
                playerId = gameData.MetaData[MetaDataType.PlayerId];
            }

            gameData = new GameData();
            gameData.MetaData = new Dictionary<MetaDataType, string>();
            gameData.MetaData[MetaDataType.PlayerId] = playerId;
        }
    }

    private static void Version000000009(ref GameData gameData)
    {
        var playerId = "NiceFarm_Developer_Account";
        
        if (!gameData.State.TryGetValue(StateType.Version, out int version) || version < 9)
        {
            gameData = new GameData();
            gameData.MetaData = new Dictionary<MetaDataType, string>();
            gameData.MetaData[MetaDataType.PlayerId] = playerId;
        }
    }

    private static void GiveExtraRewardsV002(ref GameData gameData, int level)
    {
        if (level >= 1)
        {
            GiveManager(ref gameData, 1, 1);
            GiveManager(ref gameData, 2, 1);
            GiveDollar(ref gameData, new IdleNumber(20, NumberDigits.Empty));
        }
        if (level >= 2)
        {
            GiveManager(ref gameData, 3, 3);
            GiveDollar(ref gameData, new IdleNumber(30, NumberDigits.Empty));
        }    
        if (level >= 3)
        {
            GiveManager(ref gameData, 4, 4);
            GiveCard(ref gameData, 18, 1);
            GiveDollar(ref gameData, new IdleNumber(40, NumberDigits.Empty));
        }
        if (level >= 3)
        {
            GiveManager(ref gameData, 5, 4);
            GiveManager(ref gameData, 6, 4);
            GiveDollar(ref gameData, new IdleNumber(150, NumberDigits.Empty));
        }
        if (level >= 5)
        {
            GiveManager(ref gameData, 7, 10);
            GiveManager(ref gameData, 8, 1);
            GiveDollar(ref gameData, new IdleNumber(100, NumberDigits.Empty));
        }
        if (level >= 6)
        {
            GiveManager(ref gameData, 9, 6);
            GiveManager(ref gameData, 10, 6);
            GiveDollar(ref gameData, new IdleNumber(100, NumberDigits.Empty));
        }
    }


    private static ChestType GetChestType(int chestsId)
    {
        foreach (var pair in _chestTypeDictionary)
        {
            if (pair.Value.Contains(chestsId))
                return pair.Key;
        }

        ColoredLogUtility.PrintColoredError($"chestsId {chestsId} not in the Chest Type Dictionary");
        return ChestType.Basic;
    }

    private static Dictionary<int, int> GetEarnedChestsByActiveLevel(int level)
    {
        Dictionary<int, int> earnedChests = new Dictionary<int, int>();
        
        foreach (var levelChestId in LevelChestIds)
        {
            if(levelChestId.Key > level) continue;
            foreach (var chestsDic in levelChestId.Value)
            {
                if (!earnedChests.ContainsKey(chestsDic.Key)) earnedChests[chestsDic.Key] = 0;
                earnedChests[chestsDic.Key] += chestsDic.Value;
            }
        }

        return earnedChests;
    }

#endregion
    
#region 0.0.8
    private static readonly Dictionary<int, Dictionary<int, int>> LevelManagerIdLevels = new()
    {
        {0, new Dictionary<int, int>(){}},
        {1, new Dictionary<int, int>()
        {
            {1, 1}, {2, 1}, 
        }},
        {2, new Dictionary<int, int>()
        {
            //3x(id : 2)Lucky Box + x2 (id : 4)Silver Box + (id : 5)Silver Box (1)
            {1, 2}, {2, 2}, {3, 2} 
        }},
        {3, new Dictionary<int, int>()
        {
            {2, 3}, {3, 3}, {4, 2}, {12, 1},   
        }},
        {4, new Dictionary<int, int>()
        {
            {7, 3}, {4, 3}, {3, 3}, {13, 1}, {12, 2}, {6, 2}, 
        }}, 
        {5, new Dictionary<int, int>()
        {
            {9, 3}, {12, 3}, {14, 1}, {7, 3}, {4, 4}, {2, 4}, {18, 1}, {6, 3}, 
        }},
        {6, new Dictionary<int, int>()
        {
            //9x(id : 2)Lucky Box + x8 (id : 4)Silver Box + (id : 13)Silver Box (7) + (id : 14)Silver Box (8)
            {10, 3}, {11, 3}, {7, 4}, {1, 4}, {16, 1}, {8, 3}, {4, 4}, {14, 2}, {13, 3}, {9, 4},
        }},
        
        {101, new Dictionary<int, int>()
        {
            {15, 1}
        }},
        {102, new Dictionary<int, int>()
        {
            {6, 4}, {3, 4}, 
        }},
        {103, new Dictionary<int, int>()
        {
            //3x(id : 2)Lucky Box + x2 (id : 4)Silver Box + (id : 5)Silver Box (1)
            {17, 1}, {15, 2}, {14, 3} 
        }},
        {104, new Dictionary<int, int>()
        {
            {10, 4}, {16, 3}, {11, 4}, {8, 4},   
        }},
        {105, new Dictionary<int, int>()
        {
            {7, 4}, {19, 2}, {3, 5}, {2, 5}, {1, 5}, {21, 2}, 
        }}, 
        {106, new Dictionary<int, int>()
        {
            {20, 2}, {17, 2}, {18, 3}, {14, 4}, {9, 5}, {6, 5}, {4, 5}, {11, 5}, 
        }},
        {107, new Dictionary<int, int>()
        {
            {5, 3}, {19, 3}, {17, 3}, {10, 5}, {13, 4}, {21, 3}, {12, 4}, {3, 6}, {16, 4}, {8, 5},
        }},
    };


    private static void Version000000008(ref GameData gameData)
    {
        if (!gameData.State.TryGetValue(StateType.Version, out int version) || version < 0000008)
        {
            if (gameData.State.TryGetValue(StateType.Level, out int level))
            {
                ResetStickerStateData(ref gameData);
                gameData.CollectableItem[CollectableItemType.DirectorCard] = new Dictionary<int, CardSaveData>();
                GiveExtraRewardsV008(ref gameData,  level);
            }
        }
    }
    
    private static void GiveExtraRewardsV008(ref GameData gameData, int level)
    {
        List<int> levelIds = new List<int>() {0, 1, 2, 3, 4, 5, 6, 101, 102, 103, 104, 105, 106, 107};

        foreach (var levelId in levelIds)
        {
            if (level >= levelId)
            {
                foreach (var managers in LevelManagerIdLevels[levelId])
                {
                    GiveManager(ref gameData, managers.Key, 0, managers.Value);
                }
            }    
        }

        if (level >= 3)
        {
            GiveGem(ref gameData, new IdleNumber(50, NumberDigits.Empty));
        }
    }
#endregion

}


