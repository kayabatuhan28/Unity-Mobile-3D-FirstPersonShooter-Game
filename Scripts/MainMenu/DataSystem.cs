using MobileFPS;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class DataSystem : MonoBehaviour
{
    public static DataSystem instance;

    public List<GameData> _GameData;

    private string FilePath;
    private BinaryFormatter _Bf = new();
    FileStream _File;

    bool isApplicationPaused = false;

    private void Awake()
    {
        FilePath = Application.persistentDataPath + "/GameData.gd";

        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (!PlayerPrefs.HasKey("isFirstRun")) 
        {
            CheckDataFileAndCreateFile();
            PlayerPrefs.SetInt("isFirstRun", 1);
        }
        else
        {
            if (SceneManager.GetActiveScene().buildIndex == 0) // Main Menu
            {
                LoadData();
            }
            else
            {
                GameSceneLoadData();
            }
        }

        //Debug.Log("FilePath: " + FilePath);
    }

    void CheckDataFileAndCreateFile()
    {
        if (!File.Exists(FilePath))
        {
            _File = File.Create(FilePath);
            _Bf.Serialize(_File, _GameData);
            _File.Close();
            
            LoadData();
        }
    }

    public void SaveAllData()
    {
        if (File.Exists(FilePath))
        {
            _File = File.OpenWrite(FilePath);
            _Bf.Serialize(_File, _GameData);
            _File.Close();
        }
    }

    async void LoadData()
    {
        if (File.Exists(FilePath))
        {
            _File = File.Open(FilePath, FileMode.Open);
            _GameData = (List<GameData>)_Bf.Deserialize(_File);
            _File.Close();

            await LoadWeaponData();

            await LoadSettingsData();
        }
    }

    // Update Weapon Panel
    async Task LoadWeaponData()
    {
        await Task.Delay(1000);

        for (int i = 0; i < _GameData[0]._GameWeaponData.Count; i++)
        {
            MainMenuManager.instance._UpgradeWeapon._UpgradeWeaponData[i].WeaponID = _GameData[0]._GameWeaponData[i].WeaponID;
            MainMenuManager.instance._UpgradeWeapon._UpgradeWeaponData[i].WeaponLevel = _GameData[0]._GameWeaponData[i].WeaponLevel;
            MainMenuManager.instance._UpgradeWeapon._UpgradeWeaponData[i].UpgradeCost = _GameData[0]._GameWeaponData[i].UpgradeCost;
            MainMenuManager.instance._UpgradeWeapon._UpgradeWeaponData[i].MaximumAmmoAmount = _GameData[0]._GameWeaponData[i].MaximumAmmoAmount;
            MainMenuManager.instance._UpgradeWeapon._UpgradeWeaponData[i].WeaponDamage = _GameData[0]._GameWeaponData[i].WeaponDamage;
        }

        MainMenuManager.instance._UpgradeWeapon.CurrentMoney = _GameData[0]._GameOtherData[0].CurrentMoney;
        await Task.Delay(500);
    }
    async Task LoadSettingsData()
    {
        await Task.Delay(500);

        MainMenuManager.instance._MainMenuSettings.GameVolume.value = _GameData[0]._GameSettings[0].GameVolume;
        MainMenuManager.instance._MainMenuSettings.MenuVolume.value = _GameData[0]._GameSettings[0].MenuVolume;
        MainMenuManager.instance._MainMenuSettings.EffectVolume.value = _GameData[0]._GameSettings[0].EffectVolume;
        MainMenuManager.instance._MainMenuSettings.AimAssist.isOn = _GameData[0]._GameSettings[0].IsAimAssistEnable;
        MainMenuManager.instance._MainMenuSettings.AimAngle.value = _GameData[0]._GameSettings[0].AimAssistAngle;
        MainMenuManager.instance._MainMenuSettings.AimScale.isOn = _GameData[0]._GameSettings[0].IsAimScaleEnable;
        MainMenuManager.instance._MainMenuSettings.Vibration.isOn = _GameData[0]._GameSettings[0].IsVibrationEnable;
        MainMenuManager.instance._MainMenuSettings.QualityLevel.value = _GameData[0]._GameSettings[0].Quality;

    }

    // ---------------------------------

    async void GameSceneLoadData()
    {
        if (File.Exists(FilePath))
        {
            _File = File.Open(FilePath, FileMode.Open);
            _GameData = (List<GameData>)_Bf.Deserialize(_File);
            _File.Close();

            await LoadGameSceneAllData();         
        }
    }
    async Task LoadGameSceneAllData()
    {
        await Task.Delay(1000);

        // Weapon Data
        for (int i = 0; i < _GameData[0]._GameWeaponData.Count; i++)
        {
            GameManager.instance._Weapons[i].GetComponent<WeaponManager>().maxAmmoCapacity = _GameData[0]._GameWeaponData[i].MaximumAmmoAmount;
            GameManager.instance._Weapons[i].GetComponent<WeaponManager>().weaponDamage = _GameData[0]._GameWeaponData[i].WeaponDamage;
            GameManager.instance._Weapons[i].GetComponent<WeaponManager>().totalAmmo = _GameData[0]._GameWeaponData[i].TotalAmmo;

            // Updates the effect volumes for all weapons.
            GameManager.instance._Weapons[i].GetComponent<WeaponManager>().SetDataSystemVolumeLevel(_GameData[0]._GameSettings[0].EffectVolume);
        }

        Player.instance.activeWeapon.InitializeAmmo();
        Player.instance.activeWeapon.UpdateAmmo();

        // Bomb, Heal Kit Data
        Player.instance.UpdateHealAndBombPanel(_GameData[0]._GameOtherData[0].BombAmount, _GameData[0]._GameOtherData[0].HealKitAmount);

        // Settings
        GameManager.instance.gameSound.volume = _GameData[0]._GameSettings[0].GameVolume;

        // Sound Management
        Player.instance.SetDataSystemVolumeLevel(_GameData[0]._GameSettings[0].EffectVolume);
        Player.instance._ThrowBombSystem.SetDataSystemVolumeLevel(_GameData[0]._GameSettings[0].EffectVolume);

        // Aim
        GameManager.instance.IsAimAutoFireAssistEnable = _GameData[0]._GameSettings[0].IsAimAssistEnable;
        GameManager.instance.AimRotationAssistAngle = _GameData[0]._GameSettings[0].AimAssistAngle;
        GameManager.instance.IsCrossHairScaleAssistEnable = _GameData[0]._GameSettings[0].IsAimScaleEnable;

        // Vibration
        GameManager.instance.isVibrationEnabled = _GameData[0]._GameSettings[0].IsVibrationEnable;

        // Quality
        QualitySettings.SetQualityLevel(_GameData[0]._GameSettings[0].Quality, true);


    }

    
    public void UpdateData(int RegisterID)
    {
        switch (RegisterID)
        {
            case 0:
                // Pistol
                _GameData[0]._GameWeaponData[0].TotalAmmo = GameManager.instance._Weapons[0].GetComponent<WeaponManager>().totalAmmo;               
                break;
            case 1:
                // Rifle
                _GameData[0]._GameWeaponData[1].TotalAmmo = GameManager.instance._Weapons[1].GetComponent<WeaponManager>().totalAmmo;
                break;
            case 2:
                // Sniper
                _GameData[0]._GameWeaponData[2].TotalAmmo = GameManager.instance._Weapons[2].GetComponent<WeaponManager>().totalAmmo;
                break;
            case 3:
                // Bomb
                _GameData[0]._GameOtherData[0].BombAmount = Player.instance._ThrowBombSystem.BombAmount;
                break;
            case 4:
                // HealKit
                _GameData[0]._GameOtherData[0].HealKitAmount = Player.instance.HealKitAmount;
                break;
        }
    }


   // Application Focus - Pause
    private void OnApplicationFocus(bool focus)
    {
        isApplicationPaused = !focus;

        //Runs when the app goes to background
        if (isApplicationPaused)
        {
            if (SceneManager.GetActiveScene().buildIndex != 0)
            {             
                SaveAllData();               
            }
        }
    }
    private void OnApplicationPause(bool pause)
    {       
        isApplicationPaused = pause;

        //Runs when the app returns from background
        if (!isApplicationPaused)
        {
            SceneManager.LoadScene(0);
        }
    }

   




}
