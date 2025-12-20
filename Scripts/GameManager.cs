using MobileFPS;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum ItemType
{
    PistolAmmo,
    AutomaticAmmo,
    SniperAmmo,
    HealKit,
    Grenade
}


public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool isVibrationEnabled;
    public bool isGameFinish;


    [Header("-------- Aim Assist - Rotation --------")]
    public bool IsAimRotationAssistEnable;
    [UnityEngine.Range(5, 15)] public float AimRotationAssistAngle;

    [Header("-------- Aim Assist - AutoFire --------")]
    public bool IsAimAutoFireAssistEnable;

    [Header("-------- Aim Assist - Crosshair Scale --------")]
    public bool IsCrossHairScaleAssistEnable;

    [Header("-------- Item Spawn and Manager System --------")]
    [SerializeField] Transform[] spawnPositions;
    int spawnPointIndex;
    [SerializeField] GameObject[] itemObjectPool;
    [SerializeField] int amountToSpawn;
    [SerializeField] int SpawnTimeRate; // Delay between item spawns.

    int[] itemAmmoAmount = { 8, 10, 15, 20, 25, 35, 40 };
    ItemType[] itemTypes =
    {
        ItemType.PistolAmmo,
        ItemType.AutomaticAmmo,
        ItemType.SniperAmmo,
        ItemType.HealKit,
        ItemType.Grenade
    };
    int activeIndexItem = -1;

    [Header("----------------")]
    [SerializeField] GameObject itemPanel;
    [SerializeField] Image itemIcon;
    [SerializeField] TextMeshProUGUI itemAmountText;
    [SerializeField] Button itemPickUpButton;
    [SerializeField] Sprite[] itemIcons;

    [Header("----------------")]
    public List<ItemManager> _ItemManager;
    public GameObject[] _Weapons;

    public AudioSource gameSound;

    [Header("-------- End Game --------")]
    [SerializeField] GameObject[] panels;
    [SerializeField] TextMeshProUGUI[] enemiesKilledText;
    [SerializeField] TextMeshProUGUI[] moneyEarnedText;
    public int TotalEnemiesKilled;

    [Header("-------- Find the Key --------")]
    [SerializeField] Transform[] keySpawnTransform;   
    [SerializeField] GameObject keyObject;
    [SerializeField] GameObject keyCanvas;
    public TextMeshProUGUI QuestText;
    public bool IsKeyCanvasOpen = false;
    bool isKeyCollected;

    // Door
    [SerializeField] GameObject doorCanvas;
    public bool IsDoorCanvasOpen;
    [SerializeField] Button doorButton;
    [SerializeField] TextMeshProUGUI buttonText;

    


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;           
            // DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(ItemSpawn());
        SpawnKey();
    }

   
    // Item 
    IEnumerator ItemSpawn()
    {
        for (int i = 0; i < amountToSpawn; i++)
        {
            yield return new WaitForSeconds(SpawnTimeRate);

            itemObjectPool[spawnPointIndex].transform.position = spawnPositions[spawnPointIndex].position;

            int RandomIndex = Random.Range(0, itemAmmoAmount.Length - 1);
            int Amount = itemAmmoAmount[RandomIndex];
            ItemType selectedItemType = itemTypes[Random.Range(0, itemTypes.Length)];


            // Item ID 3 contains a Heal Kit.Extra logic added to ensure only one Heal Kit can spawn.The same applies to grenades.
            if (selectedItemType == ItemType.Grenade || selectedItemType == ItemType.HealKit)
            {
                Amount = 1;
            }

            _ItemManager[0]._Item[i].ListID = i;
            _ItemManager[0]._Item[i].ItemID = (int)selectedItemType;
            _ItemManager[0]._Item[i].ItemAmount = Amount;
            _ItemManager[0]._Item[i].ItemGameObject = itemObjectPool[spawnPointIndex];

            //itemObjectPool[spawnPointIndex].GetComponent<ItemBox>().PickUpItemType = i;
            itemObjectPool[spawnPointIndex].SetActive(true);
            spawnPointIndex++;
        }
    }
    public void UpdateItemPickUpPanel(bool IsItemPanelOpen, int ListID = -1)
    {
        if (IsItemPanelOpen)
        {         
            itemAmountText.text = _ItemManager[0]._Item[ListID].ItemAmount.ToString();         
            int SelectedItemID = _ItemManager[0]._Item[ListID].ItemID;
            itemIcon.sprite = itemIcons[SelectedItemID];           
            itemPanel.SetActive(true);
            activeIndexItem = ListID;

            // If the item is not a Heal Kit or a grenade
            if (SelectedItemID != 3 && SelectedItemID != 4)
            {
                // If max magazine capacity equals max ammo count (hide pickup widget, show ammo full, etc.)          
                WeaponManager weaponManager = _Weapons[_ItemManager[0]._Item[activeIndexItem].ItemID].GetComponent<WeaponManager>();
                if (weaponManager.maxAmmoCapacity == weaponManager.totalAmmo)
                {
                    itemPickUpButton.interactable = false;
                }
                else
                {
                    itemPickUpButton.interactable = true;
                }
            }               
        }
        else
        {
            itemPanel.SetActive(false);
            activeIndexItem = -1; // In the array, -1 represents undefined, 0 represents the pistol
        }
    }
    public void ItemPickUp()
    {      
        if (_ItemManager[0]._Item[activeIndexItem].ItemID != 3 && _ItemManager[0]._Item[activeIndexItem].ItemID != 4) // If the item is not a Heal Kit or a grenade
        {
            // When picking up ammo, the maximum ammo capacity must not be exceeded
            int SelectedItemID = _ItemManager[0]._Item[activeIndexItem].ItemID;
            WeaponManager weaponManager =  _Weapons[SelectedItemID].GetComponent<WeaponManager>();

            int remainingAmmoCapacity = weaponManager.maxAmmoCapacity - weaponManager.totalAmmo;
            if (_ItemManager[0]._Item[activeIndexItem].ItemAmount <= remainingAmmoCapacity)
            {
                // All ammo can be picked up, there is enough space
                weaponManager.totalAmmo += _ItemManager[0]._Item[activeIndexItem].ItemAmount;
            }
            else
            {
                weaponManager.totalAmmo = weaponManager.maxAmmoCapacity;
            }
          
            weaponManager.SendToDataSystem(_ItemManager[0]._Item[activeIndexItem].ItemID);
        }
        else if (_ItemManager[0]._Item[activeIndexItem].ItemID == 4) // Grenade
        {
            Player.instance._ThrowBombSystem.BombAmount++;
            Player.instance._ThrowBombSystem.UpdateBombPanel();
            DataSystem.instance.UpdateData(3);
        }
        else // healkit ise
        {
            Player.instance.HealKitAmount++;
            Player.instance.healKitAmountText.text = Player.instance.HealKitAmount.ToString();
            DataSystem.instance.UpdateData(4);
        }

        
        _ItemManager[0]._Item[activeIndexItem].ItemGameObject.SetActive(false);
    }
    public void SpawnLootOnEnemyDead(Transform EnemyPosition)
    {
        itemObjectPool[spawnPointIndex].transform.position = EnemyPosition.position;

        int RandomIndex = Random.Range(0, itemAmmoAmount.Length - 1);
        int Amount = itemAmmoAmount[RandomIndex];
        ItemType selectedItemType = itemTypes[Random.Range(0, itemTypes.Length)];


        if (selectedItemType == ItemType.Grenade || selectedItemType == ItemType.HealKit)
        {
            Amount = 1;
        }

        _ItemManager[0]._Item[spawnPointIndex].ListID = spawnPointIndex;
        _ItemManager[0]._Item[spawnPointIndex].ItemID = (int)selectedItemType;
        _ItemManager[0]._Item[spawnPointIndex].ItemAmount = Amount;
        _ItemManager[0]._Item[spawnPointIndex].ItemGameObject = itemObjectPool[spawnPointIndex];

        int value = (int)itemObjectPool[spawnPointIndex].GetComponent<ItemBox>().PickUpItemType;
        //itemObjectPool[spawnPointIndex].GetComponent<ItemBox>().PickUpItemType = spawnPointIndex;
        itemObjectPool[spawnPointIndex].SetActive(true);

        // Force
        Rigidbody rb = itemObjectPool[spawnPointIndex].GetComponent<Rigidbody>();
        Vector3 randomDirection = new Vector3(Random.Range(-0.5f, 0.5f), 5f, Random.Range(-0.5f, 0.5f)).normalized;
        rb.AddForce(randomDirection * Random.Range(2, 5), ForceMode.Impulse);
        //rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);

        spawnPointIndex++;
    }


    // End of Game Processes - Find the key and reach the escape point
    public void ShowEndGamePanels(int panelID)
    {
        panels[panelID].SetActive(true);
        if (panelID == 0)
        {
            // Win
            enemiesKilledText[panelID].text = TotalEnemiesKilled.ToString();
            moneyEarnedText[panelID].text = (TotalEnemiesKilled * 500).ToString();

            Player.instance.enabled = false;

            DataSystem.instance._GameData[0]._GameOtherData[0].CurrentMoney += TotalEnemiesKilled * 500;
            DataSystem.instance.SaveAllData();

            doorCanvas.SetActive(false);
        }
        else
        {
            // Lose
            Player.instance.enabled = false;
            isGameFinish = true;

            enemiesKilledText[panelID].text = TotalEnemiesKilled.ToString();
            moneyEarnedText[panelID].text = "0";
        }
    }
    public void SpawnKey()
    {
        keyObject.transform.position = keySpawnTransform[Random.Range(0, keySpawnTransform.Length - 1)].position;
        keyObject.SetActive(true);
    }
    public void KeyCollected()
    {
        isKeyCollected = true;
        keyObject.SetActive(false);
        keyCanvas.SetActive(false);

        QuestText.text = "Find the door and escape!";
    }
    public void ShowKeyCanvas(bool IsOpen)
    {
        IsKeyCanvasOpen = IsOpen;
        keyCanvas.SetActive(IsKeyCanvasOpen);
    }
    public void ShowDoorCanvas(bool IsOpen)
    {
        IsDoorCanvasOpen = IsOpen;
        doorCanvas.SetActive(IsDoorCanvasOpen);

        if (isKeyCollected)
        {
            doorButton.interactable = true;
            buttonText.text = "Open Door";
        }
        else
        {
            doorButton.interactable = false;
            buttonText.text = "Find Key";
        }
    }
    public void OpenDoor()
    {    
        ShowEndGamePanels(0);       
    }


    // Main Menu Button
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(0);
    }



}
