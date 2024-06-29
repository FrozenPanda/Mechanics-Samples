using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class LeveledUnlockExpendable : TileExpendable
{
    [SerializeField] private int unlockLevel;

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
        if (IsExpend) return;
        SetState(false);
    }

    public override void SetExpendState(int expendedId = -1)
    {
        if (expendedId != ExpandableId || IsExpend) return;
        base.SetExpendState(expendedId);
    }

    public override void SetExpendState(bool state, bool isFirst = false)
    {
        var level = LevelManager.Instance.GetLevelData() % 100;
        var canUnlock = (unlockLevel - 1) <= level;

        state = canUnlock;

        base.SetExpendState(state, isFirst);

    }

    public override void SetState(bool _isExpended, bool isAnimated = false)
    {
        var level = LevelManager.Instance.GetLevelData() % 100;
        var canUnlock = (unlockLevel - 1) <= level;

        _isExpended = canUnlock;
        base.SetState(_isExpended, isAnimated);
    }
}
