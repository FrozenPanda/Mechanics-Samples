using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterUpgrade : BaseIdleUpgrade
{
    public override float ApplyUpgrade(float baseValue)
    {
        float upgradedValue = baseValue;
        upgradedValue *= IdleUpgradeItem.Multiplier;
        upgradedValue += IdleUpgradeItem.Addition;
        return upgradedValue;
    }

    public override IdleNumber ApplyUpgrade(IdleNumber baseValue)
    {
        throw new System.NotImplementedException();
    }

    public override BaseIdleUpgrade CreateNew(IdleUpgradeItem data)
    {
        var characterUpgrade = new CharacterUpgrade()
        {
            IdleUpgradeItem = data
        };
        return characterUpgrade;
    }

    public override void GainUpgrade()
    {
        EventService.OnIdleCharacterUpgradePurchased.Invoke(IdleUpgradeItem);
    }
}
