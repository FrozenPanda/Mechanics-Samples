using System;
using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.HouseCoffee;
using _Game.Scripts.Systems.StarUpgradeSystem;
using UnityEngine;

public class NCUnlockHouseExpandable : UnlockProductExpendable
{
    public override List<ProductCostTuple> UnlockCost => StarUpgradeManager.Instance.UpdateUnlockCost(unlockCost);
    
    [Header("Unlock Cost")]
    [SerializeField] private List<ProductCostTuple> unlockCost;
    
    [Header("Punch Animation")]
    [SerializeField] private Vector3 scaleChange = new Vector3(0f, 0.15f, 0f);
    [SerializeField] private float duration = 0.15f;

    private NCCustomerTable CustomerTable => customerTable ??= GetComponentInChildren<NCCustomerTable>(true);
    private NCCustomerTable customerTable;
    

    private Collider lockedPartObstacleCollider;

    protected override void Start()
    {
        base.Start();
        lockedPartObstacleCollider = lockedPart.GetComponent<Collider>();
    }

    protected override void Initialize()
    {
        base.Initialize();
        CoroutineDispatcher.ExecuteWithDelay(.5f, () => UpdateLockedPartCollider(ExpendManager.Instance.IsExpended(this)));
    }

    protected override void ShowMiniUnlockPanel()
    {
        base.ShowMiniUnlockPanel();
        PunchClickEffect(transform, scaleChange, duration);
    }

    public override void SetExpendState(bool state, bool isFirst = false)
    {
        base.SetExpendState(state, isFirst);
        bool expended = ExpendManager.IsAvailable() && ExpendManager.Instance.IsExpended(this);
        CustomerTable.SetExpendState(expended, isFirst);
        // UpdateLockedPartCollider(expended);
    }

    public override void UnlockExpandable()
    {
        base.UnlockExpandable();
        //SmartlookStarter.Instance.SmartlookEvent(SmartLookEvent.First_House_Unlock);
        UpdateLockedPartCollider(ExpendManager.Instance.IsExpended(this));
        // PunchClickEffect(transform, scaleChange, duration);
    }

    private void UpdateLockedPartCollider(bool expended)
    {
        if (lockedPartObstacleCollider != null)
        {
            lockedPartObstacleCollider.enabled = !expended;
            UpdateAStarPath(new[] {lockedPartObstacleCollider});
        }
    }
}
