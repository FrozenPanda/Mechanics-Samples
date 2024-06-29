using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectUpgrade : BaseIdleUpgrade
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
        var upgradedValue = new IdleNumber(baseValue);
        upgradedValue *= IdleUpgradeItem.Multiplier;
        upgradedValue += IdleUpgradeItem.Addition;
        return upgradedValue;
    }

    public override BaseIdleUpgrade CreateNew(IdleUpgradeItem data)
    {
        var objectUpgrade = new ObjectUpgrade
        {
            IdleUpgradeItem = data
        };
        return objectUpgrade;
    }

    public override void GainUpgrade()
    {
        //Debug.Log($"{IdleUpgradeItem.Name} Upgrade Gained");
    }
}
