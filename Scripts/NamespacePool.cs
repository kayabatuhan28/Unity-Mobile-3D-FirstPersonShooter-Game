using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MobileFPS
{
    #region WEAPON MANAGER

    [System.Serializable]
    public class WeaponPoolHandler
    {
        public int ActiveWeaponSlotID;
        public List<Weapons> _Weapons;
    }

    [System.Serializable]
    public class Weapons
    {
        public int SlotID;
        public int TotalAmmo;
        public int CurrentAmmo;
        public float FireRate;
        public int WeaponDamage;
    }
    
    [System.Serializable]
    public class WeaponObjectManager
    {
        public RawImage ButtonBackgroundImage;
        public GameObject WeaponGameObject;
    }

    #endregion WEAPON MANAGER


    #region ITEM MANAGER

    [System.Serializable]
    public class ItemManager
    {
        public List<Item> _Item;
    }

    [System.Serializable]
    public class Item
    {
        public int ListID;
        public int ItemID;
        public int ItemAmount;
        public GameObject ItemGameObject;
    }

    #endregion ITEM MANAGER

    #region DATA SYSTEM

    [System.Serializable]
    public class GameData
    {
        public List<GameWeaponData> _GameWeaponData;
        public List<GameSettings> _GameSettings;
        public List<GameOtherData> _GameOtherData;     
    }

    [System.Serializable]
    public class GameWeaponData
    {
        public int WeaponID;
        public int WeaponLevel = 1;
        public int UpgradeCost;
        public int MaximumAmmoAmount;
        public int WeaponDamage;
        public int TotalAmmo;
    }

    [System.Serializable]
    public class GameSettings
    {
        public float GameVolume;
        public float MenuVolume;
        public float EffectVolume;
        public bool IsAimAssistEnable;
        public float AimAssistAngle;
        public bool IsAimScaleEnable;
        public bool IsVibrationEnable;
        public int Quality;
    }

    // item, saglik kiti para vs. gibi
    [System.Serializable]
    public class GameOtherData
    {
        public int CurrentMoney;
        public int BombAmount;
        public int HealKitAmount;
    }

    #endregion DATA SYSTEM


}
