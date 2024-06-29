using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnlockObjectUpgrade : BaseIdleUpgrade
{
    public override float ApplyUpgrade(float baseValue)
    {
        throw new System.NotImplementedException();
    }

    public override IdleNumber ApplyUpgrade(IdleNumber baseValue)
    {
        throw new System.NotImplementedException();
    }

    public override BaseIdleUpgrade CreateNew(IdleUpgradeItem data)
    {
        var unlockObjectUpgrade = new UnlockObjectUpgrade
        {
            IdleUpgradeItem = data
        };
        return unlockObjectUpgrade;
    }

    public override void GainUpgrade()
    {
        var unlockedExpendables = IdleUpgradeItem.UnlockedObjectExpendableId;
        foreach (var unlockedExpendable in unlockedExpendables)
        {
            var targetExpendable = ExpendManager.Instance.GetCloseExpendableWithId(unlockedExpendable);
            ExpendManager.Instance.Expend(targetExpendable);
            var boxUnlock = PoolingSystem.Instance.Create<BoxUnlock>(PoolType.BoxUnlock);
            var boxUnlockPos = targetExpendable.transform.position;
            boxUnlock.InitializeBox(boxUnlockPos, () => targetExpendable.TileUnlocked(false));
        }
      
    }
}
