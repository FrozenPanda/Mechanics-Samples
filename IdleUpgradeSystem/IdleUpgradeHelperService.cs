using System;
using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Systems.StarUpgradeSystem;
using UnityEngine;

public static class IdleUpgradeHelperService
{
    static IdleUpgradeHelperService()
    {
        LoadCollection();
    }

    private const string CollectionPath = "Configurations/UpgradeIconCollection";

    private static UpgradeIconCollection upgradeIconCollection;
    
    private static void LoadCollection()
    {
        upgradeIconCollection = Resources.Load<UpgradeIconCollection>(CollectionPath);
        upgradeIconCollection.Load();
    }

    #region Upgrade Message Part
    public static string GetUpgradeDescriptionMessage(IdleUpgradeItem idleUpgradeItem)
    {
        if (idleUpgradeItem.UpgradeType == IdleUpgradeType.UnlockStaffUpgrade)
        {
            return GetStaffUnlockMessage(idleUpgradeItem);
        }
        else if (idleUpgradeItem.UpgradeType == IdleUpgradeType.ObjectUpgrade)
        {
            return GetObjectUpgradeMessage(idleUpgradeItem);
        }
        else if (idleUpgradeItem.UpgradeType == IdleUpgradeType.GeneralUpgrade)
        {
            return GetGeneralUpgradeMessage(idleUpgradeItem);
        }
        else if (idleUpgradeItem.UpgradeType == IdleUpgradeType.CharacterUpgrade)
        {
            return GetCharacterUpgradeMessage(idleUpgradeItem);
        }
        return "NAN";
    }

    private static string GetCharacterUpgradeMessage(IdleUpgradeItem idleUpgradeItem)
    {
        if (idleUpgradeItem.CharacterDataType != CharacterDataType.MoveSpeed)
        {
            ColoredLogUtility.PrintColoredError("CharacterDataType Should be MoveSpeed!!");
            return "NAN";
        }
        
        string characterName = GetCharacterName(idleUpgradeItem.CharacterPoolType);
        return $"{characterName} {(idleUpgradeItem.CharacterPoolType == PoolType.CourierStaff ? "drive":"walks")} faster";
    }

    private static string GetObjectUpgradeMessage(IdleUpgradeItem idleUpgradeItem)
    {
        string objectName;
        string objectId = idleUpgradeItem.ObjectId;
        bool isAllUpgrade = IdleUpgradeManager.IsObjectIdAll(objectId);

        if (isAllUpgrade)
        {
            objectName = "All";
        }
        else
        {
            var starUpgradeData = StarUpgradeManager.Instance.GetObjectDataByInteractableId(objectId);
            if(starUpgradeData == null)
            {
                ColoredLogUtility.PrintColoredError($"Object id :{idleUpgradeItem.ObjectId}, Collectable Object Data is null!!");
                objectName = "NAN!!";
            }
            else
            {
                objectName = CollectableObjectService.GetObjectName(starUpgradeData.PoolType);
            }
        }

        string upgradeMessage = $"{objectName} ";

        if (objectId.StartsWith("M"))
        {
            if (idleUpgradeItem.ObjectDataType == ObjectDataType.ProductIncome)
            {
                upgradeMessage += $"Profit x{idleUpgradeItem.Multiplier}";
            }
            else if (idleUpgradeItem.ObjectDataType == ObjectDataType.ProductTime)
            {
                upgradeMessage = $"Speed up {objectName} sales";
            }
            else if (idleUpgradeItem.ObjectDataType == ObjectDataType.ProductResourceCount)
            {
                //upgradeMessage += $"efficiency x{1 / idleUpgradeItem.Multiplier}";
                upgradeMessage += $"works with less input";
            }
            else if (idleUpgradeItem.ObjectDataType == ObjectDataType.ProductCount)
            {
                upgradeMessage += $"income x{idleUpgradeItem.Multiplier}";
            }
        }
        else
        {
            
            
            if (idleUpgradeItem.ObjectDataType == ObjectDataType.ProductIncome)
            {
                upgradeMessage += $"Profit x{idleUpgradeItem.Multiplier}";
            }
            else if (idleUpgradeItem.ObjectDataType == ObjectDataType.ProductTime)
            {
                var interactor = InteractionManager.Instance.GetInteractableById(objectId);
                if (interactor != null)
                {
                    var nfContainer = interactor.GetComponent<NFProductContainer>();
                    if (nfContainer != null)
                        upgradeMessage = $"{nfContainer.GetObjectType()} ";
                }
                
                upgradeMessage += $"is produced faster";
            }
            else if (idleUpgradeItem.ObjectDataType == ObjectDataType.ProductResourceCount)
            {
                //upgradeMessage += $"efficiency x{1 / idleUpgradeItem.Multiplier}";
                upgradeMessage += $"requires less input";
            }
            else if (idleUpgradeItem.ObjectDataType == ObjectDataType.ProductCount)
            {
                upgradeMessage += $"production x{idleUpgradeItem.Multiplier}";
            }
        }

        return upgradeMessage;
    }

    private static string GetStaffUnlockMessage(IdleUpgradeItem idleUpgradeItem)
    {
        if (idleUpgradeItem.UnlockedStaffTypeCounts.Count == 0)
        {
            ColoredLogUtility.PrintColoredError("UnlockedStaffTypeCounts is zero!!");
            return "NAN";
        }

        var staff = idleUpgradeItem.UnlockedStaffTypeCounts[0];
        string characterName = GetCharacterName(staff.CharacterType);
        return $" +{staff.CharacterCount}  {characterName}";
    }

    private static string GetGeneralUpgradeMessage(IdleUpgradeItem idleUpgradeItem)
    {
        var generalUpgradeType = idleUpgradeItem.GeneralUpgradeType;
       
        if ( generalUpgradeType == GeneralUpgradeType.CustomerCountWithUnlock ||
             generalUpgradeType == GeneralUpgradeType.CarCustomerCountWithUnlock ||
             generalUpgradeType == GeneralUpgradeType.VehicleCustomerCountWithUnlock)
        {
            string customerName = GetCustomerName(idleUpgradeItem);
            return $" +{idleUpgradeItem.Addition}  {customerName}";
            
        }
        else if(generalUpgradeType == GeneralUpgradeType.TruckTime)
        {
            return " Deliver order faster";
        }
        else if (generalUpgradeType == GeneralUpgradeType.UnlockOrderBoard)
        {
            return " The delivery truck & order board will repair";
        }
        else
        {
            ColoredLogUtility.PrintColoredError("General Upgrade Type Should be CustomerCountWithUnlock or CarCustomerCountWithUnlock!!");
            return "NAN";
        }
       
    }

    private static string GetCharacterName(PoolType characterPoolType)
    {
        return IdleUpgradeManager.Instance.GetStaffName(characterPoolType);
    }  
    
    private static string GetCustomerName(IdleUpgradeItem idleUpgradeItem)
    {
        var generalUpgradeType = idleUpgradeItem.GeneralUpgradeType;
        
        if (generalUpgradeType == GeneralUpgradeType.CustomerCountWithUnlock)
        {
            return "Customer";
        }  
        if (generalUpgradeType == GeneralUpgradeType.CarCustomerCountWithUnlock)
        {
            return "Car";
        }
        
        if (generalUpgradeType == GeneralUpgradeType.VehicleCustomerCountWithUnlock)
        {
            float customerCount = idleUpgradeItem.Addition;
            return VehicleService.GetVehicleName(idleUpgradeItem.CharacterPoolType) + (customerCount > 1.01f ? " Customers" : " Customer");
        }
        
        return "NAN";
    }

    #endregion
    
    #region Upgrade Icon Part
    public static Sprite GetUpgradeIcon(IdleUpgradeItem idleUpgradeItem, out Sprite subUpgradeIcon)
    {
        Sprite upgradeIcon = null;
        subUpgradeIcon = null;
        
        if (idleUpgradeItem.UpgradeType == IdleUpgradeType.UnlockStaffUpgrade)
        {
            AssignStaffUnlockIcons(idleUpgradeItem, out upgradeIcon, out subUpgradeIcon);
        }
        else if (idleUpgradeItem.UpgradeType == IdleUpgradeType.ObjectUpgrade)
        {
            AssignObjectUpgradeIcons(idleUpgradeItem, out upgradeIcon, out subUpgradeIcon);
        }
        else if (idleUpgradeItem.UpgradeType == IdleUpgradeType.GeneralUpgrade)
        {
            AssignGeneralUpgradeIcons(idleUpgradeItem, out upgradeIcon, out subUpgradeIcon);
        }
        else if (idleUpgradeItem.UpgradeType == IdleUpgradeType.CharacterUpgrade)
        {
            AssignCharacterUpgradeIcons(idleUpgradeItem, out upgradeIcon, out subUpgradeIcon);
        }

        return upgradeIcon;
    }

    private static void AssignCharacterUpgradeIcons(IdleUpgradeItem idleUpgradeItem, out Sprite upgradeIcon, out Sprite subUpgradeIcon)
    {
        if (idleUpgradeItem.CharacterDataType != CharacterDataType.MoveSpeed)
        {
            ColoredLogUtility.PrintColoredError("CharacterDataType Should be MoveSpeed!!");
            upgradeIcon = null;
            subUpgradeIcon = null;
            return;
        }

        bool isCourierStaff = idleUpgradeItem.CharacterPoolType == PoolType.CourierStaff; 
        upgradeIcon = isCourierStaff ? GetCharacterIcon(PoolType.CourierStaff) : GetUpgradeTypeIcon(UpgradeIconType.MoveSpeedUpgrade);
        subUpgradeIcon = GetUpgradeTypeIcon(UpgradeIconType.ProductSpeedUpgrade);
    }

    private static void AssignObjectUpgradeIcons(IdleUpgradeItem idleUpgradeItem, out Sprite upgradeIcon, out Sprite subUpgradeIcon)
    {
        string objectId = idleUpgradeItem.ObjectId;
        bool isAllUpgrade = IdleUpgradeManager.IsObjectIdAll(objectId);

        if (isAllUpgrade)
        {
            upgradeIcon = GetUpgradeTypeIcon(UpgradeIconType.AllObjects);
            subUpgradeIcon = GetUpgradeTypeIcon(UpgradeIconType.CountUpgrade);
            return;
        }
        
        var collectableObjectData = StarUpgradeManager.Instance.GetObjectDataByInteractableId(objectId);
        if(collectableObjectData == null)
        {
            ColoredLogUtility.PrintColoredError($"Object id :{idleUpgradeItem.ObjectId}, Collectable Object Data is null!!");
            upgradeIcon = null;
            subUpgradeIcon = null;
            return;
        }
        
        upgradeIcon = collectableObjectData.Icon;
        
        UpgradeIconType upgradeIconType = UpgradeIconType.Undefined;
        
        if (idleUpgradeItem.ObjectId.StartsWith("M"))
        {
            Sprite subIconMarket;
            string MarketToGenrator = idleUpgradeItem.ObjectId.Replace("M", "G");
            var collectableObjectData2 = StarUpgradeManager.Instance.GetObjectDataByInteractableId(MarketToGenrator);
            if(collectableObjectData == null)
            {
                ColoredLogUtility.PrintColoredError($"Object id :{idleUpgradeItem.ObjectId}, Collectable Object Data is null!!");
                upgradeIcon = null;
                subUpgradeIcon = null;
                return;
            }

            subIconMarket = collectableObjectData2.Icon;
            subUpgradeIcon = subIconMarket;
        }
        else
        {
            if (idleUpgradeItem.ObjectDataType == ObjectDataType.ProductIncome)
                upgradeIconType = UpgradeIconType.IncomeUpgrade;
            else if (idleUpgradeItem.ObjectDataType == ObjectDataType.ProductTime)
                upgradeIconType = UpgradeIconType.ProductSpeedUpgrade;
            else if (idleUpgradeItem.ObjectDataType is ObjectDataType.ObjectCount or ObjectDataType.ProductCount)
                upgradeIconType = UpgradeIconType.CountUpgrade;
            else if(idleUpgradeItem.ObjectDataType == ObjectDataType.ProductResourceCount)
                upgradeIconType = UpgradeIconType.ResourceUpgrade;
        
            subUpgradeIcon = GetUpgradeTypeIcon(upgradeIconType);
        }
        
    }

    private static void AssignStaffUnlockIcons(IdleUpgradeItem idleUpgradeItem, out Sprite upgradeIcon, out Sprite subUpgradeIcon)
    {
        PoolType staffType = idleUpgradeItem.UnlockedStaffTypeCounts[0].CharacterType;
        upgradeIcon = GetCharacterIcon(staffType);
        subUpgradeIcon = GetUpgradeTypeIcon(UpgradeIconType.CountUpgrade);
    }
    
    private static void AssignGeneralUpgradeIcons(IdleUpgradeItem idleUpgradeItem, out Sprite upgradeIcon, out Sprite subUpgradeIcon)
    {
        var generalUpgradeType = idleUpgradeItem.GeneralUpgradeType;
       
        if ( generalUpgradeType == GeneralUpgradeType.CustomerCountWithUnlock ||
             generalUpgradeType == GeneralUpgradeType.CarCustomerCountWithUnlock ||
             generalUpgradeType == GeneralUpgradeType.VehicleCustomerCountWithUnlock)
        {
            if (generalUpgradeType == GeneralUpgradeType.VehicleCustomerCountWithUnlock)
            {
                upgradeIcon = VehicleService.GetVehicleIcon(idleUpgradeItem.CharacterPoolType);
            }
            else
            {
                PoolType characterPoolType = (generalUpgradeType == GeneralUpgradeType.CustomerCountWithUnlock) ? PoolType.Customer : PoolType.CarCustomer;
                upgradeIcon = GetCharacterIcon(characterPoolType);
            }
            subUpgradeIcon = GetUpgradeTypeIcon(UpgradeIconType.CountUpgrade);

           
        }
        else if(generalUpgradeType == GeneralUpgradeType.TruckTime)
        {
            upgradeIcon = GetCharacterIcon(PoolType.Truck);
            subUpgradeIcon = GetUpgradeTypeIcon(UpgradeIconType.ProductSpeedUpgrade);
        }

        else if (generalUpgradeType == GeneralUpgradeType.UnlockOrderBoard)
        {
            upgradeIcon = GetCharacterIcon(PoolType.OrderBoard);
            subUpgradeIcon = GetUpgradeTypeIcon(UpgradeIconType.CountUpgrade);
        }
        else
        {
            ColoredLogUtility.PrintColoredError("General Upgrade Type Should be CustomerCountWithUnlock or CarCustomerCountWithUnlock!!");
            upgradeIcon = null;
            subUpgradeIcon = null;
            return;
        }

        
    }
    
    private static Sprite GetUpgradeTypeIcon(UpgradeIconType upgradeIconType)
    {
        return upgradeIconCollection.GetUpgradeTypeIcon(upgradeIconType);
    }

    public static Sprite GetCharacterIcon(PoolType staffType)
    {
        return upgradeIconCollection.GetCharacterIcon(staffType);
    }

    public static Sprite GetCharacterPanelIcon(PoolType staffType)
    {
        return upgradeIconCollection.GetCharacterPanelIcon(staffType);
    }
    #endregion
}
