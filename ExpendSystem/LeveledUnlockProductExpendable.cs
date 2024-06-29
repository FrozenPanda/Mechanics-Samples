using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeveledUnlockProductExpendable : UnlockProductExpendable 
{
    [SerializeField] private int unlockLevel;
    [SerializeField] private GameObject unlockLevelPart;

    protected override void OnEnable()
    {
        base.OnEnable();
        LevelManager.Instance.LevelExpended.AddListener(OnLevelExpended);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (LevelManager.IsAvailable())
            LevelManager.Instance.LevelExpended.RemoveListener(OnLevelExpended);
    }

    private void OnLevelExpended()
    {
        if (isExpended) return;
        SetState(false);
    }

    public override void SetExpendState(int expendedId = -1)
    {
        if (expendedId != ExpandableId || isExpended)  return;
        base.SetExpendState(expendedId);
    }

    public override void SetExpendState(bool state, bool isFirst = false)
    {
        Debug.Log("Set expend state" + ExpandableId);
        base.SetExpendState(state, isFirst);

        var level = LevelManager.Instance.GetLevelData() % 100;
        var isLock = unlockLevel > level;

        unlockLevelPart.SetActive(isLock);

        if (isLock)
        {
            lockedPart.SetActive(false);
            unlockedPart.SetActive(false);
        }
    }

    public override void SetState(bool _isExpended, bool isAnimated = false)
    {
        base.SetState(_isExpended, isAnimated);

        var level = LevelManager.Instance.GetLevelData() % 100;
        var isLock = unlockLevel > level;

        unlockLevelPart.SetActive(isLock);

        if (isLock)
        {
            lockedPart.SetActive(false);
            unlockedPart.SetActive(false);
        }
    }
}
