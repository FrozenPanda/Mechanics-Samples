using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "NF_Spin_Collection",  menuName = "lib/NF_Spin_Collection")]
public class NF_Spin_Collection : ScriptableObject
{
    public List<CityLeveledSpins> CityLeveledSpinsList = new List<CityLeveledSpins>();

    [Header("--- Icons and Sprites")] [Space(2f)]
    public List<ProductItemsImages> ProductItemsImages = new List<ProductItemsImages>();
    public List<CurrencyItemsImages> CurrencyItemsImagesList = new List<CurrencyItemsImages>();
    public Sprite EmptySprite;
    
    public static NF_Spin_Collection LoadCollection(string collectionPath)
    {
        return Resources.Load<NF_Spin_Collection>(collectionPath);
    }

    public List<SpinItem> GetLevelSpinItems(int cityID)
    {
        var allSpinItems = new List<SpinItem>();
        foreach (var cityLists in CityLeveledSpinsList)
        {
            if (cityLists.cityID == cityID)
            {
                foreach (var data in cityLists.LevelSpins)
                {
                    foreach (var spin in data.SpinItems)
                    {
                        allSpinItems.Add(spin);
                    }
                }

                return allSpinItems;
            }
            
            
        }
        return new List<SpinItem>();
    }

    public List<SpinItem> GetLevelSpinItemsSpecial(int cityID)
    {
        var allSpinItems = new List<SpinItem>();
        foreach (var cityLists in CityLeveledSpinsList)
        {
            if (cityLists.cityID == cityID)
            {
                foreach (var spin in cityLists.SpecialSpins.SpinItems)
                {
                    allSpinItems.Add(spin);
                }

                return allSpinItems;
            }
        }
        return new List<SpinItem>();
    }

    public List<GatchaSpin> GetLevelGatchaSpins(int cityID)
    {
        foreach (var cityLists in CityLeveledSpinsList)
        {
            if (cityLists.cityID == cityID)
            {
                return cityLists.GatchaSpins;
            }
        }

        return new List<GatchaSpin>();
    }
    
    public void ReorderGatcha()
    {
        foreach (var data in CityLeveledSpinsList)
        {
            int x = 0;
            foreach (var gatchaSpin in data.GatchaSpins)
            {
                gatchaSpin.SpinNumber = x;
                x++;
            }
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    
    /*[Header("Change Items - Apply Hardening")]
    public int selectedCity = 0;
    public List<Converter> ProductConverter = new List<Converter>();
    public float productionMultiply = 1;
    public float goldMultiply = 1;
    public void ChangeItems()
    {
        foreach (var levelSpin in CityLeveledSpinsList[selectedCity].LevelSpins)
        {
            foreach (var spinItem in levelSpin.SpinItems)
            {
                if (spinItem.SpinItemType == SpinItemType.Package)
                {
                    foreach (var productData in spinItem.PackageContent.ProductDatas)
                    {
                        foreach (var vConverter in ProductConverter)
                        {
                            if (productData.PoolType == vConverter.oldOne)
                            {
                                productData.PoolType = vConverter.newOne;
                                productData.Count *= productionMultiply;
                            }
                        }
                    }

                    foreach (var productData in spinItem.PackageContent.Currencies)
                    {
                        if (productData.CurrencyType == CurrencyType.Coin)
                            productData.Price *= goldMultiply;
                    }
                }
            }
            
#if UNITY_EDITOR
            EditorUtility.SetDirty(levelSpin);
#endif
            
        }
        
        Debug.Log("SUCCESS CLONED");
        
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        AssetDatabase.Refresh();
#endif
    }*/
}

[Serializable]
public class SpinItem
{
    [HideInInspector]public string Name;
    public float Chance = 1;
    public int SpinItemID;
    public SpinItemType SpinItemType;
    public SpinPackageRewardType SpinPackageRewardType;
    [HideInInspector]public IdleUpgradeType IdleUpgradeType;
    [HideInInspector]public IdleUpgradeItem IdleUpgradeItem;
    [DrawIf("SpinItemType", global::SpinItemType.IdleUpgrade)] public int IldeUpgradeID;
    [DrawIf("SpinItemType", global::SpinItemType.IdleUpgrade)] public string IldeUpgradeName;
    [HideInInspector][DrawIf("SpinItemType", global::SpinItemType.Package)] public Content PackageContent;
    [DrawIf("SpinPackageRewardType", global::SpinPackageRewardType.Currency)] public CurrencyType CurrencyType;
    [DrawIf("SpinPackageRewardType", global::SpinPackageRewardType.Chest)] public int ChestID;
    #if UNITY_EDITOR
    [Searchable]
    #endif
    [DrawIf("SpinItemType", global::SpinItemType.Package)] public PoolType PoolType;
    [DrawIf("SpinItemType", global::SpinItemType.Package)] public int RewardBySecond;

    [HideInInspector] public Content FinalContent;
    //[HideInInspector] public SpinItemType _SpinItemType;
}

[Serializable]
public class CityLeveledSpins
{
    public int cityID;
    public List<LeveledSpinItemHolder> LevelSpins;
    public LeveledSpinItemHolder SpecialSpins;
    public List<GatchaSpin> GatchaSpins;
}

public enum SpinItemType
{
    IdleUpgrade,
    Package,
}

public enum SpinPackageRewardType
{
    None,
    Product,
    Currency,
    Chest,
}

[Serializable]
public struct ProductItemsImages
{
    [Range(0f, 1000000f)] public float RangeBySecond;
    [Range(1,3)]public int ProductSpriteAmount;
}

[Serializable]
public struct CurrencyItemsImages
{
    public CurrencyType CurrencyType;
    [Range(0f, 1000000f)] public float Range;
    public Sprite CurrencySprite;
}

/*#if UNITY_EDITOR
[CustomEditor(typeof(NF_Spin_Collection))]
public class ReorderGatchasEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NF_Spin_Collection myScript = (NF_Spin_Collection)target;
        if(GUILayout.Button("Reorder Gatcha List"))
        {
            myScript.ReorderGatcha();
        }
        
        if(GUILayout.Button("Change Items - Apply Hardening"))
        {
            myScript.ChangeItems();
        }
    }
}
#endif*/



