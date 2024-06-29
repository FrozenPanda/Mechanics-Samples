using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab.EconomyModels;
using UnityEngine;
using UnityEngine.Events;

public class NFInventoryManager : Singleton<NFInventoryManager>
{
    public UnityEvent OnItemCountChanged = new UnityEvent();
    public UnityEvent<PoolType> OnAddedItemToInventory = new UnityEvent<PoolType>();
    public UnityEvent<PoolType, IdleNumber> OnAddedItem = new UnityEvent<PoolType, IdleNumber>();
    public UnityEvent<PoolType, IdleNumber> OnRemoveItem = new UnityEvent<PoolType, IdleNumber>();

    private Dictionary<PoolType, IdleNumber> InventoryItems = new Dictionary<PoolType, IdleNumber>();

    private void OnEnable()
    {
        LevelManager.Instance.RevisitEvent.AddListener(BeforeLevelLoadedListener);
        LevelManager.Instance.BeforeCityChanged.AddListener(BeforeLevelLoadedListener);
    }

    private void OnDisable()
    {
        if (LevelManager.IsAvailable())
        {
            LevelManager.Instance.BeforeCityChanged.RemoveListener(BeforeLevelLoadedListener);
            LevelManager.Instance.RevisitEvent.RemoveListener(BeforeLevelLoadedListener);
        }

    }

    private void Awake()
    {
        LoadData();
    }

    private void LoadData()
    {
        InventoryItems.Clear();

        var data = DataService.Instance.GetData<Dictionary<PoolType, IdleNumber>>(DataType.INVENTORYITEM);
        if (data != null)
            InventoryItems = data;
    }

    private void SaveData()
    {
        DataService.Instance.SetData(DataType.INVENTORYITEM, InventoryItems);
    }

    private void BeforeLevelLoadedListener()
    {
        ResetInventory();
        //AddTutorialItem();
    }

    public void ResetInventory()
    {
        InventoryItems.Clear();
        SaveData();
    }

    private void AddTutorialItem()
    {
        if (LevelManager.Instance.GetLevelData() == InventoryTutorial.TUTORIAL_LEVEL_DATA - 1)
        {
            AddItemToInventory(ConfigurationService.Configurations.FirstStarUpgradeTutorialItemType, ConfigurationService.Configurations.FirstStarUpgradeTutorialItemCount);
        }
    }

    public void AddItemToInventory(PoolType itemType, IdleNumber count)
    {
        if (InventoryItems.ContainsKey(itemType))
        {
            InventoryItems[itemType] += count;
        }
        else
        {
            InventoryItems.Add(itemType, count);
        }

        OnAddedItemToInventory?.Invoke(itemType);
        OnItemCountChanged?.Invoke();
        OnAddedItem?.Invoke(itemType , count);
        SaveData();
    }

    public void RemoveItemToInventory(PoolType itemType, IdleNumber count)
    {
        if (count.digits == NumberDigits.Empty)
        {
            //var rounded = Mathf.Round(count.number);
            //count.number = rounded;
        }
        
        if (InventoryItems.ContainsKey(itemType))
        {
            IdleNumber removedCount = count > InventoryItems[itemType] ? InventoryItems[itemType] : count;
            InventoryItems[itemType] -= count;
        }
        
        OnRemoveItem?.Invoke(itemType , count);
        OnItemCountChanged?.Invoke();
        SaveData();
    }

    public Dictionary<PoolType, IdleNumber> GetAllItems()
    {
        return InventoryItems;
    }

    public IdleNumber GetCountInInventory(PoolType itemType)
    {
        return InventoryItems.ContainsKey(itemType) ? InventoryItems[itemType] : new IdleNumber( 0, NumberDigits.Empty);
    }
}

[Serializable]
public class CountByType
{
    public PoolType PoolType;
    public IdleNumber Count;
}
