using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "LeveledSpinItemHolder",  menuName = "lib/LeveledSpinItemHolder")]
public class LeveledSpinItemHolder : ScriptableObject
{
    public int City;
    public int Level;
    public bool isSpecialSpinReward;
    public List<SpinItem> SpinItems;
    public IdleUpgradeLevel IdleUpgradeData;
    public List<Converter> ChangeItemsList = new List<Converter>();
    
    public void GetIdleUpgrades()
    {
        if (IdleUpgradeData == null)
        {
            ReorderSpinItems();
            return;
        }

        for (int i = SpinItems.Count - 1 , j = 0 ; i >= 0; i--)
        {
            if (SpinItems[i].SpinItemType == SpinItemType.IdleUpgrade)
            {
                SpinItems[i].IdleUpgradeItem = null;
                SpinItems.RemoveAt(i);
            }
        }

        var idleItemList = new List<IdleUpgradeItem>();

        foreach (var idleItem in IdleUpgradeData.Items)
        {
            var cloneOne = idleItem;
            if (cloneOne.IsActive)
            {
                idleItemList.Add(cloneOne);
            }
        }
        
        foreach (var idleItem in idleItemList)
        {
            if (idleItem.IsActive)
            {
                //var clonedOne = idleItem;
                IdleUpgradeItem newItem = new IdleUpgradeItem();
                newItem.CopyIdleUpgadeItem(idleItem.Id , idleItem);
                SpinItem spinItem = new SpinItem();
                spinItem.SpinItemType = SpinItemType.IdleUpgrade;
                spinItem.IldeUpgradeID = newItem.Id;
                spinItem.IldeUpgradeName = newItem.Name;
                spinItem.IdleUpgradeType = newItem.UpgradeType;
                spinItem.IdleUpgradeItem = newItem;
                spinItem.Name = "IdleUp " + newItem.Name + " " + newItem.ObjectDataType;
                SpinItems.Add(spinItem);
            }
        }
        
        ReorderSpinItems();
        
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public void ReorderSpinItems()
    {
        int idx = 0;

        foreach (var spinItem in SpinItems)
        {
            if (!isSpecialSpinReward)
            {
                spinItem.SpinItemID = idx + 100 + Level * 1000 + City * 10000;
            }
            else
            {
                spinItem.SpinItemID = idx;
            }
            if (spinItem.SpinItemType == SpinItemType.Package)
                spinItem.Name = "Package";
            idx++;
        }

        IdleUpgradeData = null;
        
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public void CheckSprites()
    {
        foreach (var spinItem in SpinItems)
        {
            if (spinItem.SpinItemType == SpinItemType.Package)
            {
                if (spinItem.RewardBySecond < 1)
                {
                    Debug.Log("Hatalı second" + spinItem.SpinItemID );
                }
                
                if(spinItem.SpinPackageRewardType == SpinPackageRewardType.None)
                    Debug.Log("Hatalı sprite: spin id:" + spinItem.SpinItemID );
            
                if (spinItem.SpinPackageRewardType == SpinPackageRewardType.Product)
                {
                
                    PackageContent content = NF_Spin_Manager.Instance.GetContentBySpinItem(spinItem);
                    spinItem.PackageContent = content.ContentMods[0].Content;
                    var data = NF_Spin_Manager.Instance.GetSpinImageAndTextForContent(spinItem);
                    if(data.Item1 == null)
                        Debug.Log("Hatalı sprite: spin id:" + spinItem.SpinItemID );
                
                }else if (spinItem.SpinPackageRewardType == SpinPackageRewardType.Currency)
                {
                    Sprite _sprite = NF_Spin_Manager.Instance.IndexRangeByCurrency(spinItem);
                
                    if(_sprite == null)
                        Debug.Log("Hatalı sprite: spin id:" + spinItem.SpinItemID);
                }
            }
        }
    }

    public void ChangeItems()
    {
        foreach (var item in SpinItems)
        {
            foreach (var converter in ChangeItemsList)
            {
                if (item.PoolType == converter.oldOne)
                {
                    item.PoolType = converter.newOne;
                    continue;
                }
            }
        }
        
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
}

[Serializable]
public class GatchaSpin
{
    public int SpinNumber;
    public int GiveSpinID;
}

#if UNITY_EDITOR
[CustomEditor(typeof(LeveledSpinItemHolder))]
public class SpinGetIdleUpgrades : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LeveledSpinItemHolder myScript = (LeveledSpinItemHolder)target;
        if(GUILayout.Button("Get Idle Upgrades"))
        {
            myScript.GetIdleUpgrades();
        }
        
        if(GUILayout.Button("Check Sprites"))
        {
            myScript.CheckSprites();
        }
        
        if(GUILayout.Button("Change Items"))
        {
            myScript.ChangeItems();
        }
    }
}
#endif
