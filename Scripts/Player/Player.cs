using MobileFPS;
using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;


public class Player : MonoBehaviour
{

    public static Player instance;

    InputAction movementAction;
    InputAction rotateAction;
    CharacterController characterController;

    public WeaponManager activeWeapon;

    [Header("------ Health Management ------")]
    public int HealKitAmount;
    public float maxHealth = 100f;
    float currentHealth;
    [SerializeField] Image healthBarImage;
    [SerializeField] Button useHealKitButton;
    public TextMeshProUGUI healText;
    public TextMeshProUGUI healKitAmountText;

    [Header("------ Hit Direction ------")]
    public Image TakeDamageSprite;
    public float FirstTime = 1f;
    public float ExtraTime = 0.5f;
    public float MaximumTime = 3f;
    float takeDamageDisplayTime;
    float takeDamageSpriteAlphaValue; // Shows the damage sprite and gradually fades it out. 
    Color takeDamageSpriteColor;


    [Header("------ Rotate Data ------")]
    public Camera camera;
    [SerializeField] float cameraRotateSensivity;

    // Camera look angle limit
    [SerializeField][UnityEngine.Range(-50, 50)] public float rotationAngleLimitX = 40f, rotationAngleLimitY = -40f;

    // Represents an invisible panel on the right side of the screen
    // Created to allow the player to perform rotation by touching with their finger.
    [SerializeField] TouchRotateArea touchRotateArea;

    [Header("------ Aim Data ------")]
    [SerializeField] float normalFOV = 60f;
    [SerializeField] float aimFOV = 40f; // Used to set the camera field of view (FOV) 
    [SerializeField] float aimVelocity = 10f;
    public bool isAiming = false;
    bool isAimEnable = false;
    [SerializeField] Button aimAndShootButton;

    [Header("------ CrossHair Data ------")]
    [SerializeField] Sprite automaticCrossHairSprite;
    [SerializeField] Sprite singleCrossHairSprite;
    [SerializeField] Image crossHairImage;
    [SerializeField] AudioSource changeFireModeSound;

    // Crosshair Scale
    float CrossHairScaleAmount = 1.3f;
    float CrossHairAnimationSpeed = 5f;
    Vector3 crossHairDefaultScale = new(0.4f, 0.4f, 0.4f);
    bool isCrossHairOnEnemy;

    [Header("------ Ammo Object Pool ------")]
    [SerializeField] TextMeshProUGUI currentAmmoText;
    [SerializeField] ParticleSystem[] bulletDecalPool;
    int bulletDecalPoolIndex;

    [Header("------ Empty Shell Object Pool ------")]
    [SerializeField] GameObject[] emptyShellObjectPool;
    int emptyShellPoolIndex;

    [Header("------ Weapon Change ------")]
    public List<WeaponPoolHandler> _WeaponPoolHandler;
    public List<WeaponObjectManager> _WeaponObjectManager;
    [SerializeField] GameObject changeFireModeButton;
    [SerializeField] Color buttonDefaultColor;
    [SerializeField] Color buttonSelectedColor;


    [Header("------ Movement Data ------")]
    [SerializeField] float jumpForce;
    [SerializeField] float moveSensitivity;
    [SerializeField] AudioSource jumpSound;
    // Gravity must be handled manually because CharacterController is used instead of Rigidbody
    [SerializeField] float gravity = -10f;
    float verticalVelocity;
   
    Vector2 movementVector;
    Vector3 move;

    [Header("------ Crouch Data ------")]
    [SerializeField] float normalHeight = 2f;
    [SerializeField] float crouchHeight = 0.5f;
    [SerializeField] float transitionSmoothness = 0.04f;
   
    [SerializeField] Image crouchButtonImage;
    [SerializeField] Sprite crouchEnableSprite;
    [SerializeField] Sprite crouchNotEnableSprite;
    [SerializeField] AudioSource crouchSound;
    bool IsCrouching = false;
    bool IsCrouchStart = false;

    [Header("------ Throw Bomb System ------")]
    public ThrowBombSystem _ThrowBombSystem;

    [Header("------ Hit Blood Effect ------")]
    public ParticleSystem bloodEffect;

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

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        movementAction = InputSystem.actions.FindAction("Navigate");
        rotateAction = InputSystem.actions.FindAction("Look");

        // OnRotateStart 
        rotateAction.started += x => OnRotateStart();
        rotateAction.canceled += x => OnRotateEnd();

        SetSelectedWeapon();

        currentHealth = maxHealth;

        healKitAmountText.text = HealKitAmount.ToString();
    }

    void Update()
    {
        AimRaycast();
        HandleCrosshair();       
        HandleDamageSprite();
        HandleMovement();          
    }

    void HandleDamageSprite()
    {
         // Take Damage Sprite
        if (takeDamageDisplayTime > 0)
        {
            takeDamageDisplayTime -= Time.deltaTime;
            takeDamageSpriteAlphaValue = Mathf.Clamp01(takeDamageDisplayTime / FirstTime);
            
            takeDamageSpriteColor = TakeDamageSprite.color;
            takeDamageSpriteColor.a = takeDamageSpriteAlphaValue;
            TakeDamageSprite.color = takeDamageSpriteColor;
        }
    }

    void HandleCrosshair()
    {
        if (GameManager.instance.IsCrossHairScaleAssistEnable)
        {
            CrossHairAnimation();
        }

        if (isAimEnable)
        {          
            StartAimTransition();
        }   
    }

    void HandleMovement()
    {
        CharacterMovement();
        CharacterRotation();
    }

    // CrossHair Raycast
    void AimRaycast()
    {
        // Raycast
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // Middle of the screen

        // Enemy Interact
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform.gameObject.CompareTag("Enemy"))
            {
                isCrossHairOnEnemy = true;
                SetCrossHairColor(true);

                // Aim Rotation Assist
                if (GameManager.instance.IsAimRotationAssistEnable)
                {
                    Vector3 assistedPosition = hit.collider.bounds.center - camera.transform.position;
                    Quaternion assistedRotation = Quaternion.LookRotation(assistedPosition);
                    float angle = Quaternion.Angle(camera.transform.rotation, assistedRotation);
                    if (angle < GameManager.instance.AimRotationAssistAngle)
                    {
                        camera.transform.rotation = Quaternion.Slerp(camera.transform.rotation, assistedRotation, Time.deltaTime * 5f);
                    }
                }

                // Aim AutoFire Assist
                if (GameManager.instance.IsAimAutoFireAssistEnable)
                {
                    activeWeapon.AimAutoFire = true;
                }

            }
            else
            {
                isCrossHairOnEnemy = false;
                // Aim AutoFire Assist
                if (GameManager.instance.IsAimAutoFireAssistEnable)
                {
                    activeWeapon.AimAutoFire = false;
                }
                SetCrossHairColor(false);
            }     
        }

        // Item Interact - Distance is important for this action
        if (Physics.Raycast(ray, out RaycastHit hit2, 2.5f))
        {          
            if (hit2.transform.gameObject.CompareTag("ItemBox"))
            {
                int ItemBoxListID = (int)hit2.transform.GetComponent<ItemBox>().PickUpItemType;
                GameManager.instance.UpdateItemPickUpPanel(true, ItemBoxListID);
            }
            else
            {
                GameManager.instance.UpdateItemPickUpPanel(false);
            }
         
            if (hit2.transform.gameObject.CompareTag("Key"))
            {
                GameManager.instance.ShowKeyCanvas(true);
            }
            else
            {
                if (GameManager.instance.IsKeyCanvasOpen)
                {
                    GameManager.instance.ShowKeyCanvas(false);
                }
            }
           
            if (hit2.transform.gameObject.CompareTag("Door"))
            {
                GameManager.instance.ShowDoorCanvas(true);
            }
            else
            {
                if (GameManager.instance.IsDoorCanvasOpen)
                {
                    GameManager.instance.ShowDoorCanvas(false);
                }
            }
        }

    }


    // Initializes data at start and updates it whenever the weapon is changed
    public void Initialize(int currentAmmo, int totalAmmo)
    {            
        UpdateAmmoText(currentAmmo, totalAmmo);
        CheckWeaponFireMode();
    }


    // Movement + Rotation
    void CharacterMovement()
    {
        movementVector = movementAction.ReadValue<Vector2>();
        move = transform.right * movementVector.x + transform.forward * movementVector.y;

        // When moving forward
        if (movementVector.y == 1)
        {
            Debug.Log("Run");
            move *= moveSensitivity * 1.12f;
        }
        else // If moving right, left, or backward
        {
            move *= moveSensitivity;
        }

        // Gravity - Jump
        if (characterController.isGrounded && verticalVelocity < 0) // If there is ground contact
        {
            verticalVelocity = -2f;
        }
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;
        

        characterController.Move(move * Time.deltaTime);

        // Run - Idle Anim
        if (movementAction.IsPressed())
        {
            if (activeWeapon != null)
            {
                activeWeapon._Animator.SetBool("Idle", false);
                activeWeapon._Animator.SetBool("Run", true);
            }
        }
        if (movementAction.WasReleasedThisFrame())
        {
            if (activeWeapon != null)
            {
                activeWeapon._Animator.SetBool("Idle", true);
                activeWeapon._Animator.SetBool("Run", false);
            }
        }
       

        // Crouch      
        if (IsCrouchStart)
        {
            Debug.Log("dadsad");
            SmoothCrouchTransition();
        }
       
    }
    void CharacterRotation()
    {     
        if (rotateAction.IsPressed())
        {
            //  Rotation is handled via the joystick, and shooting is enabled while using the right joystick unlike the touch area.          
            UpdateCameraRotation("JoystickRotation");
            // Prevents an unwanted shot when beginning to aim as sniper and pistol trigger a single shot on right stick release.           
            if (activeWeapon._WeaponType != WeaponType.Sniper && activeWeapon._WeaponType != WeaponType.Pistol)
            {
                activeWeapon.CanShootWhileRotating = true;
            }          
            return;
        }
        if (rotateAction.WasReleasedThisFrame())
        {
            if (activeWeapon._WeaponType != WeaponType.Sniper && activeWeapon._WeaponType != WeaponType.Pistol)
            {
                activeWeapon.CanShootWhileRotating = false;
            }         
        }

        UpdateCameraRotation("FromTouchArea");      
    }


    public void Aim(string FromWhere="NormalAimButton")
    {      
        isAiming = !isAiming;
       

        if (isAiming)
        {            
            activeWeapon._Animator.SetBool("Zoom",true);
            if (FromWhere == "NormalAimButton")
            {              
                aimAndShootButton.GetComponent<OnScreenStick>().enabled = false;
                aimAndShootButton.interactable = false;
            }          
        }
        else
        {
            activeWeapon._Animator.SetBool("Zoom", false);

            // silah snipersa
            if (activeWeapon.SniperScope != null)
            {
                activeWeapon.WeaponScope("Hide");
            }

            if (FromWhere == "NormalAimButton")
            {
                aimAndShootButton.GetComponent<OnScreenStick>().enabled = true;
                aimAndShootButton.interactable = true;
            }        
        }
    }
    void StartAimTransition()
    {       
        float NewFOV;

        if (isAiming)
        {
            
            NewFOV = aimFOV;
        }
        else
        {
            
            NewFOV = normalFOV;
        }

        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, NewFOV, Time.deltaTime * aimVelocity);

        // When the camera reaches the aim FOV, aiming is set to false preventing continuous execution in Update.    
        if (isAiming)
        {
            if (camera.fieldOfView <= aimFOV) 
            {
                isAimEnable = false;
            }
            crossHairImage.gameObject.SetActive(false);
        }
        else 
        {
            // Transition from aim mode back to normal.
            if (camera.fieldOfView >= normalFOV - 0.01)
            {
                isAimEnable = false;
            }
            crossHairImage.gameObject.SetActive(true);         
        }
    }

    
    public void AnimationTrigger(string AnimationToPlay)
    {
        if (AnimationToPlay == "Aim")
        {
            isAimEnable = true;
        }      
    }


    // Determines whether rotation comes from joystick or touch area and acts accordingly.
    void UpdateCameraRotation(string RotationSource)
    {
        Vector2 rotateAxis;
        if (RotationSource == "FromTouchArea")
        {
            rotateAxis = touchRotateArea.TouchPosition * cameraRotateSensivity;
            transform.Rotate(Vector3.up * rotateAxis.x);
        }
        else 
        {
            // Joystick
            Vector2 rotateVector = rotateAction.ReadValue<Vector2>();
            rotateAxis = 3f * cameraRotateSensivity * rotateVector;
            transform.Rotate(Vector3.up * rotateAxis.x);
        }

        float newRotation = camera.transform.localEulerAngles.x - rotateAxis.y;
        if (newRotation > 180)
        {
            newRotation -= 360;
        }

        newRotation = Mathf.Clamp(newRotation, rotationAngleLimitY, rotationAngleLimitX);
        camera.transform.localEulerAngles = new Vector3(newRotation, 0, 0);

    }

    
    void OnRotateStart()
    {
        Aim("RightStickAim");
    }
    void OnRotateEnd()
    {
        Aim("RightStickAim");

        // For pistols and snipers, aiming is done while holding the right stick and shooting occurs on release, unlike automatic weapons.       
        if (activeWeapon._WeaponType == WeaponType.Sniper || activeWeapon._WeaponType == WeaponType.Pistol)
        {
            activeWeapon.isFireOnReleaseEnabled = true;
            activeWeapon.isFireButtonReleased = true;
        }
    }

    
    // Crouch - Jump
    public void Jump()
    {
        if (characterController.isGrounded)
        {
            if (!jumpSound.isPlaying)
            {
                jumpSound.Play();
            }

            //Handles landing after a jump.
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity); 
        }
    }
    public void Crouch()
    {
        float newHeight = IsCrouching ? crouchHeight : normalHeight;
        characterController.height = newHeight;

        IsCrouching = !IsCrouching;
        IsCrouchStart = true;

        if (!crouchSound.isPlaying)
        {
            crouchSound.Play();
        }

        if (IsCrouching)
        {
            crouchButtonImage.sprite = crouchEnableSprite;
        }
        else
        {
            crouchButtonImage.sprite = crouchNotEnableSprite;
        }
    }
    void SmoothCrouchTransition()
    {     
        float Height = IsCrouching ? crouchHeight : normalHeight;
        characterController.height = Mathf.Lerp(characterController.height, Height, transitionSmoothness);
        if (Height == 2)
        {
            if (characterController.height >= 1.990104)
            {
                IsCrouchStart = false;
            }
        }
        else if (Height == crouchHeight)
        {
            if (characterController.height <= 0.5000002)
            {
                IsCrouchStart = false;
            }
        }
    }
    

    // Crosshair Color - CrossHair Scale
    public void SetCrossHairColor(bool IsRed)
    {
        if (IsRed)
        {
            crossHairImage.color = Color.red;
        }
        else
        {
            crossHairImage.color = Color.white;
        }
    }
    void CrossHairAnimation()
    {
        Vector3 crossHairNewScale = isCrossHairOnEnemy ? crossHairDefaultScale * CrossHairScaleAmount : crossHairDefaultScale;
        crossHairImage.rectTransform.localScale = Vector3.Lerp( crossHairImage.rectTransform.localScale, crossHairNewScale, 
            Time.deltaTime * CrossHairAnimationSpeed);
       
    }

    
    // Object Pool
    public void CreateBulletDecal(Vector3 DecalPosition, Quaternion DecalRotation)
    {
        bulletDecalPool[bulletDecalPoolIndex].transform.SetPositionAndRotation(DecalPosition, DecalRotation);
        bulletDecalPool[bulletDecalPoolIndex].gameObject.SetActive(true);
        bulletDecalPool[bulletDecalPoolIndex].Play();

        if (bulletDecalPoolIndex >= bulletDecalPool.Length - 1)
        {
            bulletDecalPoolIndex = 0;
        }
        else
        {
            bulletDecalPoolIndex++;
        }
    }
    public void CreateEmptyShell(Transform SpawnTransform)
    {
        emptyShellObjectPool[emptyShellPoolIndex].transform.position = SpawnTransform.position;
        emptyShellObjectPool[emptyShellPoolIndex].SetActive(true);

        // Silahtan bos kovanin rastgele firlamasi 
        Rigidbody rb = emptyShellObjectPool[emptyShellPoolIndex].GetComponent<Rigidbody>();
        float randomDirectionX = Random.Range(-0.3f, 0.3f);
        float randomDirectionY = Random.Range(0.1f, 0.3f);
        float randomDirectionZ = Random.Range(-0.15f, 0.2f);
        Vector3 RandomPosition = -SpawnTransform.forward + new Vector3(randomDirectionX, randomDirectionY, randomDirectionZ);

        float forceToApply = Random.Range(1.8f, 4f);
        rb.AddForce(RandomPosition * forceToApply, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);

        // Pool islemleri
        if (emptyShellPoolIndex != emptyShellObjectPool.Length - 1)
        {
            emptyShellPoolIndex++;
        }
        else
        {
            emptyShellPoolIndex = 0;
        }

    }


    // Ammo Ui Update
    public void UpdateAmmoText(int currentAmmo, int totalAmmo)
    {
        currentAmmoText.text = currentAmmo + " / " + totalAmmo;
    }


    // Reload Button Trigger
    public void Reload()
    {
        activeWeapon.Reload();
    }


    // Weapon Fire Mode And Weapon Change System
    public void ChangeWeapon(int SlotID)
    {
        // Cannot switch weapons while reloading.
        if (activeWeapon.isReloading) return;

        // Current Weapon
        _WeaponObjectManager[_WeaponPoolHandler[0].ActiveWeaponSlotID].ButtonBackgroundImage.color = buttonDefaultColor;
        _WeaponObjectManager[_WeaponPoolHandler[0].ActiveWeaponSlotID].WeaponGameObject.SetActive(false);

        // Reset Technical Settings
        camera.fieldOfView = normalFOV;
        isAiming = false;
        if (activeWeapon.SniperScope != null)
        {
            activeWeapon.SniperScope.SetActive(false);
            activeWeapon.SniperObject.SetActive(true); // The scope GameObject is reactivated because it was hidden during sniper aim.
        }    
        activeWeapon._Animator.Rebind(); // Resets animator state.

        // New Weapon
        _WeaponObjectManager[SlotID].ButtonBackgroundImage.color = buttonSelectedColor;
        _WeaponObjectManager[SlotID].WeaponGameObject.SetActive(true);
        _WeaponPoolHandler[0].ActiveWeaponSlotID = SlotID;

        SetAimAndShootButtonActivate();


        if (activeWeapon._WeaponType == WeaponType.Sniper)
        {
            aimFOV = 25;
        }
        else
        {
            aimFOV = 40;
        }
    }
    void SetSelectedWeapon()
    {
        // Invoked once when the game starts to initialize.
        _WeaponObjectManager[_WeaponPoolHandler[0].ActiveWeaponSlotID].ButtonBackgroundImage.color = buttonSelectedColor;
        _WeaponObjectManager[_WeaponPoolHandler[0].ActiveWeaponSlotID].WeaponGameObject.SetActive(true);

        if (activeWeapon._WeaponType == WeaponType.Sniper)
        {
            aimFOV = 25;
        }
        else
        {
            aimFOV = 40;
        }
    }
    public void ChangeWeaponFireMode()
    {
        if (activeWeapon.WeaponFireMode == FireMode.Automatic)
        {
            activeWeapon.WeaponFireMode = FireMode.Single;
        }
        else
        {
            activeWeapon.WeaponFireMode = FireMode.Automatic;
        }
        changeFireModeSound?.Play();

        // In single shot mode, this is disabled to avoid continuous firing when rotating with the right stick.
        if (activeWeapon.WeaponFireMode == FireMode.Single)
        {
            aimAndShootButton.GetComponent<OnScreenStick>().enabled = false;
            aimAndShootButton.interactable = false;         
        }
        else
        {
            aimAndShootButton.GetComponent<OnScreenStick>().enabled = true;
            aimAndShootButton.interactable = true;
        }

        if (crossHairImage == null) return;

        if (activeWeapon.WeaponFireMode == FireMode.Automatic)
        {
            crossHairImage.sprite = automaticCrossHairSprite;
        }
        else
        {
            crossHairImage.sprite = singleCrossHairSprite;
        }
    }
    public void CheckWeaponFireMode()
    {
        if (crossHairImage == null) return;

        if (activeWeapon.WeaponFireMode == FireMode.Automatic)
        {
            crossHairImage.sprite = automaticCrossHairSprite;
        }
        else
        {
            crossHairImage.sprite = singleCrossHairSprite;
        }

        if (!activeWeapon.CanChangeFireMode)
        {
            changeFireModeButton.SetActive(false);
        }
        else
        {
            changeFireModeButton.SetActive(true);
        }
    }
    void SetAimAndShootButtonActivate()
    {
        if (activeWeapon.WeaponFireMode == FireMode.Single)
        {
            if (activeWeapon._WeaponType == WeaponType.Pistol || activeWeapon._WeaponType == WeaponType.Sniper)
            {
                aimAndShootButton.GetComponent<OnScreenStick>().enabled = true;
                aimAndShootButton.interactable = true;
            }
            else
            {
                aimAndShootButton.GetComponent<OnScreenStick>().enabled = false;
                aimAndShootButton.interactable = false;
            }
        }       
    }


    // Health Management
    public void UpdateHealth(bool IsHeal, float Amount, Vector3 DamageCauserPosition)
    {
        if (IsHeal)
        {
            currentHealth = currentHealth + Amount > maxHealth ? maxHealth : currentHealth + Amount;          
            healthBarImage.fillAmount = currentHealth / maxHealth;
        }
        else // Take Damage
        {
            currentHealth -= Amount;          
            
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                GameManager.instance.ShowEndGamePanels(1);
            }
            else
            {              
                if (HealKitAmount > 0)
                {
                    useHealKitButton.interactable = true;
                }
                else
                {
                    useHealKitButton.interactable = false;
                }
            }

            ShowTakeDamageDirection(DamageCauserPosition);
        }

        healthBarImage.fillAmount = currentHealth / maxHealth;
        healText.text = currentHealth.ToString();
    }
    public void UseHealKit()
    {
        HealKitAmount--;
        healKitAmountText.text = HealKitAmount.ToString();
        DataSystem.instance.UpdateData(4);
        
        UpdateHealth(true, 25, new(0,0,0));
        
        if (currentHealth >= maxHealth)
        {
            useHealKitButton.interactable = false;
        }
        else
        {
            if (HealKitAmount > 0)
            {
                useHealKitButton.interactable = true;
            }
            else
            {
                useHealKitButton.interactable = false;
            }
        }
    }


    // Hit Direction System
    public void ShowTakeDamageDirection(Vector3 DamageCauserPosition)
    {       
        Vector3 source = (DamageCauserPosition - camera.transform.position).normalized;
        
        Vector3 planeSource = Vector3.ProjectOnPlane(source, Vector3.up);
        Vector3 planeForward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);

        float angle = Vector3.SignedAngle(planeForward, planeSource, Vector3.up);
        TakeDamageSprite.rectTransform.rotation = Quaternion.Euler(0, 0, -angle);

        // Taking damage again before the direction sprite disappears refreshes its display time on screen.
        takeDamageDisplayTime = Mathf.Min(takeDamageDisplayTime + ExtraTime, MaximumTime);

        takeDamageSpriteColor = TakeDamageSprite.color;
        takeDamageSpriteColor.a = 1;
        TakeDamageSprite.color = takeDamageSpriteColor;
    }

    
    // Data System
    public void UpdateHealAndBombPanel(int BombCount, int HealKitCount)
    {
        _ThrowBombSystem.BombAmount = BombCount;
        _ThrowBombSystem.UpdateBombPanel();

        HealKitAmount = HealKitCount;
        healKitAmountText.text = HealKitAmount.ToString();
    }

    // Sound Management
    public void SetDataSystemVolumeLevel(float EffectVolumeLevel)
    {
        jumpSound.volume = EffectVolumeLevel;
        crouchSound.volume = EffectVolumeLevel;
        changeFireModeSound.volume = EffectVolumeLevel;
    }


}
