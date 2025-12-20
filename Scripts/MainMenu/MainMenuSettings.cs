using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuSettings : MonoBehaviour
{

    public Slider GameVolume;
    public Slider MenuVolume;
    public Slider EffectVolume;

    public Toggle AimAssist;
    public Slider AimAngle;

    public Toggle AimScale;
    public Toggle Vibration;
    public TMP_Dropdown QualityLevel;

    public AudioSource MenuSound;


    public void OnSettingsChanged(string ChangedSettings)
    {
        switch (ChangedSettings)
        {
            case "GameVolume":
                DataSystem.instance._GameData[0]._GameSettings[0].GameVolume = GameVolume.value;
                break;
            case "MenuVolume":
                DataSystem.instance._GameData[0]._GameSettings[0].MenuVolume = MenuVolume.value;
                break;
            case "EffectVolume":
                DataSystem.instance._GameData[0]._GameSettings[0].EffectVolume = EffectVolume.value;
                break;
            case "AimAssistAngle":
                DataSystem.instance._GameData[0]._GameSettings[0].AimAssistAngle = AimAngle.value;
                break;
            case "AimAssist":
                DataSystem.instance._GameData[0]._GameSettings[0].IsAimAssistEnable = AimAssist.isOn;
                break;
            case "AimScale":
                DataSystem.instance._GameData[0]._GameSettings[0].IsAimScaleEnable = AimScale.isOn;
                break;
            case "Vibration":
                DataSystem.instance._GameData[0]._GameSettings[0].IsVibrationEnable = Vibration.isOn;
                break;
            case "QualityLevel":
                DataSystem.instance._GameData[0]._GameSettings[0].Quality = QualityLevel.value;
                break;
        }
    }

   
}
