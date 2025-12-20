using UnityEngine;
using UnityEngine.InputSystem;

public enum FireMode
{
    Single,
    Automatic
}

public enum WeaponType
{
    Automatic, 
    Pistol,
    Sniper
}

public class WeaponManager : MonoBehaviour
{
    
    public Animator _Animator;
    [SerializeField] Camera mainCamera;
    InputAction shootAction;
     
    [Header("-------- Weapon Data --------")]
    public FireMode WeaponFireMode = FireMode.Automatic;
    public WeaponType _WeaponType = WeaponType.Automatic;
    public bool CanChangeFireMode; 
    public bool CanShootWhileRotating;
    public int maxAmmoCapacity;
    public int totalAmmo;
    [SerializeField] int magazineCapacity;
    [SerializeField] float fireRate = 0.1f;
    public int weaponDamage = 20;
    [SerializeField] Transform shellEjectPoint; 
    public GameObject SniperScope;
    public GameObject SniperObject;

    int currentAmmo;
    public bool isReloading = false;
    float nextFireTime; // The cooldown time between two shots.
    public bool isFireButtonReleased = true;
    public bool isFireOnReleaseEnabled; // Represents firing when the fire button is released.
    public bool AimAutoFire;

    [Header("-------- Effects and Sounds --------")]
    [SerializeField] ParticleSystem muzzleFlash;
    [SerializeField] AudioSource fireSound;
    [SerializeField] AudioSource reloadSound;
    [SerializeField] AudioSource reload2Sound;
    [SerializeField] AudioSource emptyAmmoSound;

    Collider[] detectedEnemies = new Collider[3];

    private void OnEnable()
    {
        // Triggered during weapon swapping because weapons are disabled and enabled.
        Player.instance.activeWeapon = this;
        Player.instance.Initialize(currentAmmo, totalAmmo);     
        _Animator.SetBool("Idle", true);
    }

    void Start()
    {
        shootAction = InputSystem.actions.FindAction("Fire");

        InitializeAmmo();
        UpdateAmmo();
    }

   
    void Update()
    {       
        if (isReloading) return;

        // Reload
        if (currentAmmo <= 0 && totalAmmo != 0)
        {
            Reload();
            return;
        }

        CanFireWeapon();
        
        if (shootAction.WasReleasedThisFrame())
        {
            isFireButtonReleased = true;
        }
    }

  
    // Fire Weapon
    void CanFireWeapon()
    {
        if (shootAction.IsPressed() || CanShootWhileRotating || isFireOnReleaseEnabled || AimAutoFire)
        {       
            if (currentAmmo > 0)
            {
                if (WeaponFireMode == FireMode.Automatic)
                {
                    PlayFireWeaponMontage();
                    if (Time.time >= nextFireTime)
                    {
                        FireWeapon();
                        Debug.Log("Fire");
                    }
                }
                else if (WeaponFireMode == FireMode.Single)
                {
                    if (isFireButtonReleased && Time.time >= nextFireTime)
                    {
                        PlayFireWeaponMontage();
                        FireWeapon();
                        isFireButtonReleased = false;
                    }
                }
            }
            else
            {
                emptyAmmoSound?.Play();
            }
        }

        if (isFireOnReleaseEnabled || AimAutoFire)
        {
            isFireOnReleaseEnabled = false;
            isFireButtonReleased = true;
        }

    }
    void FireWeapon()
    {
        nextFireTime = Time.time + fireRate;
        currentAmmo--;
        UpdateAmmo();
        Player.instance.CreateEmptyShell(shellEjectPoint);
        muzzleFlash?.Play();
        fireSound?.Play();

        if (GameManager.instance.isVibrationEnabled)
        {
            Handheld.Vibrate();
        }

        // Raycast
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // The exact center of the screen.
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform.gameObject.CompareTag("Enemy"))
            {
                if (hit.transform.TryGetComponent<EnemyAi>(out var enemy))
                {                 
                    enemy.TakeDamage(weaponDamage);
                    enemy.bloodEffect.gameObject.SetActive(true);
                    enemy.bloodEffect.transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
            else
            {
                Player.instance.CreateBulletDecal(hit.point, Quaternion.LookRotation(hit.normal));
            }
        }

        // Silahla ates edildigini ai lara bildirilmesi. (sesi kontrol etme vs. icin)
        int enemies = Physics.OverlapSphereNonAlloc(transform.position, 16, detectedEnemies, LayerMask.GetMask("Enemy"));
        for (int i = 0; i < enemies; i++)
        {
            detectedEnemies[i].GetComponent<EnemyAi>().InvestigateSound(transform);
        }

    }

    

    void PlayFireWeaponMontage()
    {
        if (Player.instance.isAiming)
        {
            _Animator.Play("ZoomAndFire");
        }
        else
        {
            _Animator.Play("Fire");
        }
    }
    


    // Reload
    public void Reload()
    {
        if (totalAmmo <= 0)
        {
            emptyAmmoSound?.Play();
            return;
        }

        // If the magazine is full (20/20), reload will not occur. It can also be 19/20, for example.
        if (!isReloading && currentAmmo < magazineCapacity)
        {
            ReloadState("Start");
        }
    }
    void ReloadState(string State)
    {
        if (State == "Start")
        {
            isReloading = true;
            _Animator.Play("Reload");
        }
        else if (State == "Sound")
        {
            reloadSound?.Play();
        }
        else if (State == "MagazinePull")
        {
            reload2Sound?.Play();
        }
    }
    void EndReload()
    {
        // If the current ammo is 20 and the total ammo is 45, assuming a magazine capacity of 30, we take 10 bullets from the total ammo..
        int ammoToAdd = magazineCapacity - currentAmmo;

        // If we have only 10 total bullets but need 15 bullets to reload, we can take a maximum of 10 bullets.
        if (ammoToAdd > totalAmmo)
        {
            ammoToAdd = totalAmmo;
            totalAmmo = 0;
            currentAmmo += ammoToAdd;
        }
        else // If 15 bullets are needed and 10 are sufficient, only the required amount is taken and the magazine is filled.
        {
            totalAmmo -= ammoToAdd;
            currentAmmo = magazineCapacity;
        }

        isReloading = false;
        isFireButtonReleased = true;
        UpdateAmmo();

        if (_WeaponType == WeaponType.Pistol)
        {
            DataSystem.instance.UpdateData(0);            
        }
        else if (_WeaponType == WeaponType.Automatic)
        {
            DataSystem.instance.UpdateData(1);
        }
        else if (_WeaponType == WeaponType.Sniper)
        {
            DataSystem.instance.UpdateData(2);
        }
    }


    // Update Ammo
    public void InitializeAmmo()
    {
        if (totalAmmo >= magazineCapacity) // ammo 25, magazine 30 --> 5
        {         
            currentAmmo = magazineCapacity;
            totalAmmo -= magazineCapacity;          
        }
        else 
        {
            currentAmmo = totalAmmo;
            totalAmmo = 0;
        }

        if (_WeaponType == WeaponType.Pistol)
        {
            DataSystem.instance.UpdateData(0);
        }
        else if (_WeaponType == WeaponType.Automatic)
        {
            DataSystem.instance.UpdateData(1);
        }
        else if (_WeaponType == WeaponType.Sniper)
        {
            DataSystem.instance.UpdateData(2);
        }
    }
    public void UpdateAmmo()
    {     
        Player.instance.UpdateAmmoText(currentAmmo, totalAmmo);
    }


    // Aim
    public void Aim()
    {
        Player.instance.AnimationTrigger("Aim");
    }
    public void WeaponScope(string State)
    {
        if (State == "Show")
        {
            SniperScope.SetActive(true);
        }
        else
        {
            SniperScope.SetActive(false);
        }
    }

    public void SendToDataSystem(int ItemID)
    {
        DataSystem.instance.UpdateData(ItemID);
        UpdateAmmo();
    }

    // Sound Management
    public void SetDataSystemVolumeLevel(float EffectVolumeLevel)
    {
        fireSound.volume = EffectVolumeLevel;
        reloadSound.volume = EffectVolumeLevel;
        reload2Sound.volume = EffectVolumeLevel;
        emptyAmmoSound.volume = EffectVolumeLevel;
    }

    // Draw Gizmos
    private void OnDrawGizmosSelected()
    {
        // Silah ates edildiginde ortaya cikacak olan, enemyleri investigate statesine geciren raycast debugu.
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, 10);   
    }

}
