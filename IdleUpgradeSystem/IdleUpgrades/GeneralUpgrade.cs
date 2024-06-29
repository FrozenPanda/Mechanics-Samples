using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralUpgrade : BaseIdleUpgrade
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
        var customerCountUpgrade = new GeneralUpgrade
        {
            IdleUpgradeItem = data
        };
        return customerCountUpgrade;
    }

    public override void GainUpgrade()
    {
        EventService.OnGeneralUpgradePurchased.Invoke(IdleUpgradeItem);
    }
}
