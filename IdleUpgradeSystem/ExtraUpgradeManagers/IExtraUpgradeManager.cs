using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IExtraUpgradeManager
{

}

public interface IExtraObjectUpgradeManager : IExtraUpgradeManager
{
    float GetUpgradedValue(string objectId, ObjectDataType objectDataType, float baseValue, PoolType poolType = PoolType.Undefined, bool isNextLevel = false);
    IdleNumber GetUpgradedValue(string objectId, ObjectDataType objectDataType, IdleNumber baseValue, PoolType poolType = PoolType.Undefined, bool isNextLevel = false);
}

public interface IExtraCharacterUpgradeManager : IExtraUpgradeManager
{
    float GetUpgradedValue(PoolType characterPooltype, CharacterDataType characterDataType, float baseValue);
    IdleNumber GetUpgradedValue(PoolType characterPooltype, CharacterDataType characterDataType, IdleNumber baseValue);
}

public interface IExtraGeneralUpgradeManager : IExtraUpgradeManager
{
    float GetUpgradedValue(GeneralUpgradeType generalSettingType, float baseValue);
    IdleNumber GetUpgradedValue(GeneralUpgradeType generalSettingType, IdleNumber baseValue);
}


public enum ExtraUpgradeManagerType
{
    Character,
    Object,
    Genaral
}