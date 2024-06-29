using System.Collections.Generic;
using UnityEngine;
using lib.Managers.AnalyticsSystem;
using UnityEngine.Events;

public class UnlockManager : Singleton<UnlockManager>
{
    private Dictionary<InventoryType, List<int>> unlockableData = new Dictionary<InventoryType, List<int>>();
    public ItemCollection itemCollection;
    public UnlockEvent OnUnlock = new UnlockEvent();

    private const string itemCollectionPath = "Configurations/ItemCollection";
    private bool isLoaded;

    private void Awake()
    {
        Load();
    }

    #region Save & Load

    private void Load()
    {
        // itemCollection = Resources.Load<ItemCollection>(itemCollectionPath);
        // itemCollection.Load();
        unlockableData = DataService.Instance.GetData<Dictionary<InventoryType, List<int>>>(DataType.UNLOCK);
        isLoaded = true;
    }

    private void Save()
    {
        DataService.Instance.SetData(DataType.UNLOCK, unlockableData, true);
    }

    #endregion
    
    
    public bool IsUnlocked(ItemData item)
    {
        return IsUnlocked(item.InventoryType, item.ItemId);
    }

    public bool IsUnlocked(InventoryType type, int itemId)
    {
        return unlockableData.ContainsKey(type) && unlockableData[type].Contains(itemId);
    }

    public void Unlock(ItemData item)
    {
        Unlock(item.InventoryType, item.ItemId);
    }

    public List<int> GetUnlocks(InventoryType type)
    {
        if (unlockableData.ContainsKey(type)) return unlockableData[type];
        return new List<int>();
    }

    public void RemoveUnlocks(InventoryType type)
    {
        if (unlockableData.ContainsKey(type)) unlockableData[type].Clear();
    }

    public void Unlock(InventoryType type, int itemId)
    {
        if (!unlockableData.ContainsKey(type))
        {
            unlockableData.Add(type, new List<int>());
        }

        if (unlockableData[type].Contains(itemId))
        {
//            Debug.LogWarning($"Item : {itemId} is already unlocked");
            return;
        }
        unlockableData[type].Add(itemId);
        OnUnlock?.Invoke(type, itemId);
        Save();
    }
    /*
    public ItemData RandomUnlock()
    {
        var unlockableList = itemCollection.items.FindAll(IsUnlocked);
        var randomItem = unlockableList[Random.Range(0, unlockableList.Count)];
        Unlock(randomItem);
        return randomItem;
    }
    */
}

public class UnlockEvent : UnityEvent<InventoryType, int> { }