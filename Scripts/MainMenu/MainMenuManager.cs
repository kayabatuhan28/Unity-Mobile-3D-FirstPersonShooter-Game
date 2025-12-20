using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager instance;
    
    public UpgradeWeapon _UpgradeWeapon;
    public MainMenuSettings _MainMenuSettings;
    [SerializeField] GameObject[] panels;

    [SerializeField] Slider loadingSlider;
    [SerializeField] TextMeshProUGUI loadingRateText;
   

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void PanelManagement(int PanelIndex)
    {
        switch (PanelIndex)
        {
            case 1:              
                panels[0].SetActive(false); // Main Menu panel
                _UpgradeWeapon.UpdateWeaponUpgradePanel();
                panels[PanelIndex].SetActive(true);
                break;
            case 2:
                panels[0].SetActive(false);               
                panels[PanelIndex].SetActive(true);
                break;
            case 3:
                panels[PanelIndex].SetActive(true);
                break;
            case 4: // Yes button clicked on Exit Game panel
                Application.Quit();
                break;
            case 5: // No button clicked on Exit Game panel
                panels[3].SetActive(false);
                break;
            case 6: // Upgrade Panel Exited by pressing the X button
                panels[1].SetActive(false);
                panels[0].SetActive(true);
                // Data is saved when exiting
                DataSystem.instance.SaveAllData();
                break;
            case 7: // Settings Panel Exited by pressing the X button
                panels[2].SetActive(false);
                panels[0].SetActive(true);
                // Data is saved when exiting
                DataSystem.instance.SaveAllData();
                break;
        }
    }

    public void StartGame()
    {
        StartCoroutine(LoadScene(1));
    }

    IEnumerator LoadScene(int SceneID)
    {
        AsyncOperation Op = SceneManager.LoadSceneAsync(SceneID);

        panels[0].SetActive(false);
        panels[4].SetActive(true);

        while (!Op.isDone)
        {
            float Progress = Mathf.Clamp01(Op.progress / 0.9f);
            loadingSlider.value = Progress;
            loadingRateText.text = "%" + Mathf.RoundToInt(Progress * 100);

            yield return null;
        }


    }


}
