using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor;

public class IdleUpgradeLevel : ScriptableObject
{
    public string BaristaName { get { return baristaName; }set { baristaName = value; } }
    public string ChefName { get { return chefName; } set { chefName = value; } }
    
    [SerializeField] public int Level;
    [SerializeField] private string baristaName = "Barista";
    [SerializeField] private string chefName = "Chef";
    public List<IdleUpgradeItem> Items = new List<IdleUpgradeItem>();

    public void FixCustomerType(bool isCarLevel)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].UpgradeType is IdleUpgradeType.GeneralUpgrade &&
                Items[i].GeneralUpgradeType is GeneralUpgradeType.CustomerCountWithUnlock 
                    or GeneralUpgradeType.CarCustomerCountWithUnlock)
            {
                // Items[i].CharacterPoolType = isCarLevel ? PoolType.CarCustomer : PoolType.Customer;
                Items[i].GeneralUpgradeType = isCarLevel ? GeneralUpgradeType.CarCustomerCountWithUnlock : GeneralUpgradeType.CustomerCountWithUnlock;
                // Debug.Log($"Level : {Level}, item name : " + Items[i].Name);
            }
        }
    }
    
    public void DisableChefUpgrades()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].UpgradeType is IdleUpgradeType.CharacterUpgrade && Items[i].CharacterPoolType == PoolType.CheffStaff ||
                Items[i].UpgradeType is IdleUpgradeType.UnlockStaffUpgrade && Items[i].UnlockedStaffTypeCounts[0].CharacterType == PoolType.CheffStaff)
            {
                // Items[i].CharacterPoolType = isCarLevel ? PoolType.CarCustomer : PoolType.Customer;
                Items[i].IsActive = false;
                // ColoredLogUtility.PrintColoredLog($"Chef Unlock Id : {i}", LogColor.Green);
                // Debug.Log($"Level : {Level}, item name : " + Items[i].Name);
            }
        }
    }
    
    public void BaristaToCourier()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].UpgradeType is IdleUpgradeType.CharacterUpgrade && Items[i].CharacterPoolType == PoolType.BaristaStaff)
            {
                Items[i].CharacterPoolType = PoolType.CourierStaff;
            }
            else if (Items[i].UpgradeType is IdleUpgradeType.UnlockStaffUpgrade && Items[i].UnlockedStaffTypeCounts[0].CharacterType == PoolType.BaristaStaff)
            {
                Items[i].UnlockedStaffTypeCounts[0].CharacterType = PoolType.CourierStaff;
            }
        }
    }    
    
    public void DisableContainerUpgrades(List<string> containerIds)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].UpgradeType == IdleUpgradeType.ObjectUpgrade &&
                containerIds.Contains(Items[i].ObjectId))
            {
                Items[i].IsActive = false;
                // ColoredLogUtility.PrintColoredLog($"{Items[i].ObjectId} container upgrade disabled!!, name : {Items[i].Name}", LogColor.Blue);
                // Debug.Log($"Level : {Level}, item name : " + Items[i].Name);
            }
        }
    }
    
    public void FixMoveSpeedUpgrade()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].UpgradeType == IdleUpgradeType.CharacterUpgrade &&
                Items[i].CharacterDataType == CharacterDataType.MoveSpeed)
            {
                var upgrade = Items[i];
                var oldValue = Items[i].Addition;
                Items[i].Addition /= 2;
                Debug.Log($"Upgrade name :{upgrade.Name}, id :{upgrade.Id}, old value :{oldValue}, new value : {upgrade.Addition}");
            }
        }
    }

    // private Dictionary<string, string> objectIdConvertDictionary = new Dictionary<string, string>()
    // {
    //     {"StrawberryContainer", "C1"},
    //     {"AppleContainer", "C2"},
    //     {"CoconutContainer", "C3"},
    //     {"AnanasContainer", "C4"},
    //     {"CherryContainer", "C5"},
    //     {"OrangeContainer", "C6"},
    //     {"WatermelonContainer", "C7"},
    //     {"MelonContainer", "C8"},
    //     {"GrapeContainer", "C9"},
    //     {"ApricotContainer", "C10"},
    //     {"BananaContainer", "C11"},
    //     {"PearContainer", "C12"},
    // };
    // public void FixIdleUpgradeId()
    // {
    //     for (int i = 0; i < Items.Count; i++)
    //     {
    //         if (Items[i].UpgradeType == IdleUpgradeType.ObjectUpgrade )
    //         {
    //             if (objectIdConvertDictionary.ContainsKey(Items[i].ObjectId))
    //             {
    //                 Items[i].ObjectId = objectIdConvertDictionary[Items[i].ObjectId];
    //             }
    //         }
    //     }
    // }
    public void FixExpendId()
    {
        foreach (var item in Items)
        {
            if (item.UnlockedObjectExpendableId.Count > 0)
            {
                ColoredLogUtility.PrintColoredLog($"Level : {Level}, upgradeId : {item.Id} expend id list cleared!!", LogColor.Green);
                item.UnlockedObjectExpendableId.Clear();
            }
        }
    }

    public void EditorFixID()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].Id = i;
        }
        
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].Id = Items[i].Id % 100 + Level * 200;
        }
        
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public void EditorReorderID()
    {
        /*for (int i = 0; i < Items.Count; i++)
        {
            Items[i].Id = i;
        }*/

        bool needRefresh = false;
        for (int j = 0; j < Items.Count() - 1; j++)
        {
            if (Items[j].UpgradePrice > Items[j + 1].UpgradePrice)
            {
                int oldID = Items[j].Id;
                Items[j].Id = Items[j + 1].Id;
                Items[j + 1].Id = oldID;
                needRefresh = true;
            }
        }

        Items = Items.OrderBy(x => x.Id).ToList();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
        
        if(needRefresh)
            EditorReorderID();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(IdleUpgradeLevel))]
public class IdleUpgradeLevelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        IdleUpgradeLevel myScript = (IdleUpgradeLevel)target;
        /*if(GUILayout.Button("ReOrder Idle Upgrade Depends On Price"))
        {
            myScript.EditorReorderID();
        }*/
        
        if(GUILayout.Button("Fix Idle Upgrade ID"))
        {
            myScript.EditorFixID();
        }
    }
}
#endif
