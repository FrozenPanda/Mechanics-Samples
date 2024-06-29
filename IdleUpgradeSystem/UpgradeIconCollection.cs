using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "lib/UpgradeIconCollection", fileName = "UpgradeIconCollection")]
public class UpgradeIconCollection : ScriptableObject
{
    [SerializeField] private List<CharacterIcon> characterIcons;
    [SerializeField] private List<UpgradeTypeIcon> upgradeTypeIcons;

    private readonly Dictionary<PoolType, CharacterIcon> characterIconsDictionary = new Dictionary<PoolType, CharacterIcon>();
    private readonly Dictionary<UpgradeIconType, Sprite> upgradeTypeIconsDictionary = new Dictionary<UpgradeIconType, Sprite>();

    private bool IsLoaded { get; set; }
    
    public void Load()
    {
        foreach (var characterIcon in characterIcons)
        {
            characterIconsDictionary[characterIcon.CharacterType] = characterIcon;
        }
        foreach (var upgradeTypeIcon in upgradeTypeIcons)
        {
            upgradeTypeIconsDictionary[upgradeTypeIcon.UpgradeIconType] = upgradeTypeIcon.Icon;
        }

        IsLoaded = true;
    }

    public Sprite GetCharacterIcon(PoolType characterType)
    {
        return characterIconsDictionary.ContainsKey(characterType) ? characterIconsDictionary[characterType].Icon : null;
    }

    public Sprite GetCharacterPanelIcon(PoolType characterType)
    {
        return characterIconsDictionary.ContainsKey(characterType) ? characterIconsDictionary[characterType].PanelIcon : null;
    }

    public Sprite GetUpgradeTypeIcon(UpgradeIconType upgradeIconType)
    {
        return upgradeTypeIconsDictionary.ContainsKey(upgradeIconType) ? upgradeTypeIconsDictionary[upgradeIconType] : null;
    }
}

[Serializable]
public class CharacterIcon
{
    public PoolType CharacterType => characterType;
    public Sprite Icon => icon;
    public Sprite PanelIcon => panelIcon;

    [SerializeField] private PoolType characterType;
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite panelIcon;
}

[Serializable]
public class UpgradeTypeIcon
{
    public UpgradeIconType UpgradeIconType => upgradeIconType;
    public Sprite Icon => icon;
    
    [SerializeField] private UpgradeIconType upgradeIconType;
    [SerializeField] private Sprite icon;
}

public enum UpgradeIconType
{
    Undefined = 0,
    CountUpgrade = 1,
    IncomeUpgrade = 2,
    ProductSpeedUpgrade = 3,
    MoveSpeedUpgrade = 4,
    AllObjects = 5,
    ResourceUpgrade = 6,
}

