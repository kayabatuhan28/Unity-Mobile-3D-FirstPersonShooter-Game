using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class UpgradeWeaponData
{
    public int WeaponID;
    public int WeaponLevel = 1;
    public int UpgradeCost;
    public int MaximumAmmoAmount;
    public int WeaponDamage;

    public TextMeshProUGUI CurrentMaxAmmoText;
    public TextMeshProUGUI NextMaxAmmoText;
    public TextMeshProUGUI CurrentWeaponDamageText;
    public TextMeshProUGUI NextWeaponDamageText;
    public TextMeshProUGUI CurrentWeaponLevelText;
    public TextMeshProUGUI NextWeaponLevelText;
    public TextMeshProUGUI UpgradeCostText;
    public Button UpgradeButton;
}

public class UpgradeWeapon : MonoBehaviour
{
    public List<UpgradeWeaponData> _UpgradeWeaponData;

    public int CurrentMoney;
    public TextMeshProUGUI CurrentMoneyText;

    void Start()
    {
        
    }

    public void UpdateWeaponUpgradePanel()
    {
        CurrentMoneyText.text = CurrentMoney.ToString();

        foreach(var item in _UpgradeWeaponData)
        {
            // Current
            item.CurrentMaxAmmoText.text = item.MaximumAmmoAmount.ToString();
            item.CurrentWeaponDamageText.text = item.WeaponDamage.ToString();
            item.CurrentWeaponLevelText.text = item.WeaponLevel.ToString();

            // Next
            item.NextMaxAmmoText.text = ((item.MaximumAmmoAmount * 20 / 100) + item.MaximumAmmoAmount).ToString();
            item.NextWeaponDamageText.text = ((item.WeaponDamage * 15 / 100) + item.WeaponDamage).ToString();
            item.NextWeaponLevelText.text = (item.WeaponLevel + 1).ToString();
            item.UpgradeCostText.text = item.UpgradeCost.ToString();

            // max level limit 10
            if (item.WeaponLevel != 10 && item.UpgradeCost <= CurrentMoney)
            {
                item.UpgradeButton.interactable = true;
            }
            else 
            {
                item.UpgradeButton.interactable = false;

                if (item.WeaponLevel == 10)
                {
                    item.NextMaxAmmoText.text = "MAX";
                    item.NextWeaponDamageText.text = "MAX";
                    item.NextWeaponLevelText.text = "MAX";
                    item.UpgradeCostText.text = "MAX";
                }
            }



        }
    }

    public void UpgradeWeaponButton(int WeaponID)
    {

        DataSystem.instance._GameData[0]._GameOtherData[0].CurrentMoney = CurrentMoney -= _UpgradeWeaponData[WeaponID].UpgradeCost;

        DataSystem.instance._GameData[0]._GameWeaponData[WeaponID].MaximumAmmoAmount = 
            _UpgradeWeaponData[WeaponID].MaximumAmmoAmount += (_UpgradeWeaponData[WeaponID].MaximumAmmoAmount * 20 / 100);

        DataSystem.instance._GameData[0]._GameWeaponData[WeaponID].WeaponDamage =
            _UpgradeWeaponData[WeaponID].WeaponDamage += (_UpgradeWeaponData[WeaponID].WeaponDamage * 15 / 100);

        // Cost increases by 30% per upgrade.
        DataSystem.instance._GameData[0]._GameWeaponData[WeaponID].UpgradeCost =
            _UpgradeWeaponData[WeaponID].UpgradeCost = (_UpgradeWeaponData[WeaponID].UpgradeCost * 30 / 100) + _UpgradeWeaponData[WeaponID].UpgradeCost;

        DataSystem.instance._GameData[0]._GameWeaponData[WeaponID].WeaponLevel =
            _UpgradeWeaponData[WeaponID].WeaponLevel = _UpgradeWeaponData[WeaponID].WeaponLevel + 1;

        UpdateWeaponUpgradePanel();
    }
}
