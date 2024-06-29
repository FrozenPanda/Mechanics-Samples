using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseIdleUpgrade
{
	public IdleUpgradeItem IdleUpgradeItem;

	public abstract void GainUpgrade();
	public abstract IdleNumber ApplyUpgrade(IdleNumber baseValue);
	public abstract float ApplyUpgrade(float baseValue);

	public abstract BaseIdleUpgrade CreateNew(IdleUpgradeItem data);
}
