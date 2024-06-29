using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.HouseCoffee;
using _Game.Scripts.Systems.StarUpgradeSystem;
using UnityEngine;

public class UnlockTileExpendable : UnlockProductExpendable
{
    public override List<ProductCostTuple> UnlockCost => StarUpgradeManager.Instance.UpdateUnlockCost(unlockCost);
    
    [Header("Unlock Cost")]
    [SerializeField] private List<ProductCostTuple> unlockCost;
    [SerializeField] private List<Transform> lockedPartVisualObjects;
    [SerializeField] private bool unlockCustomerTables = true;

    public override void SetState(bool _isExpended, bool isAnimated = false)
    {
        base.SetState(_isExpended, isAnimated);
        if(lockedPartVisualObjects.Count == 0) return;
        foreach (var lockedPartVisualObject in lockedPartVisualObjects)
        {
            lockedPartVisualObject.SetParent((_isExpended ? lockedPart.transform : transform));
        }
    }

    protected override void Expend()
    {
        SetChildrenExpendState<UnlockProductExpendable>(true);
        SetChildrenExpendState<NCUnlockHouseExpandable>(true);
        if(unlockCustomerTables) SetChildrenExpendState<CustomerTable>(true);
    }
}
