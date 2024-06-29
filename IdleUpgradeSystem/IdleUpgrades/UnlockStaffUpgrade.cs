using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnlockStaffUpgrade : BaseIdleUpgrade
{
    public override float ApplyUpgrade(float baseValue)
    {
        throw new System.NotImplementedException();
    }

    public override IdleNumber ApplyUpgrade(IdleNumber baseValue)
    {
        throw new System.NotImplementedException();
    }

    public int ApplyUpgrade(PoolType staffType, int baseValue)
    {
        foreach (var staffTypeCount in IdleUpgradeItem.UnlockedStaffTypeCounts)
        {
            if (staffTypeCount.CharacterType == staffType)
            {
                baseValue += staffTypeCount.CharacterCount;
            }
        }

        return baseValue;
    }
    
    public override BaseIdleUpgrade CreateNew(IdleUpgradeItem data)
    {
        var unlockCharacterUpgrade = new UnlockStaffUpgrade
        {
            IdleUpgradeItem = data
        };
        return unlockCharacterUpgrade;
    }

    public override void GainUpgrade()
    {
        var unlockedStaffs = IdleUpgradeItem.UnlockedStaffTypeCounts;
        StaffManager.Instance.HireStaff(unlockedStaffs);
    }
}
