using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UpgradeFactory
{
    private static Dictionary<IdleUpgradeType, BaseIdleUpgrade> types = new Dictionary<IdleUpgradeType, BaseIdleUpgrade>() {
        { IdleUpgradeType.CharacterUpgrade, new CharacterUpgrade()},
        { IdleUpgradeType.ObjectUpgrade, new ObjectUpgrade() },
        { IdleUpgradeType.UnlockStaffUpgrade, new UnlockStaffUpgrade()},
        { IdleUpgradeType.UnlockObjectUpgrade, new UnlockObjectUpgrade()},
        { IdleUpgradeType.GeneralUpgrade, new GeneralUpgrade()}
    };

    public static BaseIdleUpgrade Create(IdleUpgradeItem data)
    {
        return types[data.UpgradeType].CreateNew(data);
    }
}
