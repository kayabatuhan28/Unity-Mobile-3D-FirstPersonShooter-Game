using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

public class ThrowBombSystem : MonoBehaviour
{
    InputAction throwBombAction;
    bool isTouchingJoystick;
    [SerializeField] float cameraRotateSensivity;

    public int BombAmount;
    [SerializeField] TextMeshProUGUI bombAmountText;
    [SerializeField] Button throwBombButton;

    [SerializeField] GameObject[] bombObjectPool;
    int bombObjectPoolIndex;

    [Header("----- Grenade Trajectory Indicator ")]
    int MaxSegmentCount = 100;
    float TimeStep = 0.05f;
    public float ThrowForce = 15f;
    public LayerMask _LayerMask;
    [SerializeField] GameObject ImpactIndicator; // Indicator showing where the bomb will land on the ground
    LineRenderer lineRenderer;

    [Header("----- Effect / Sfx -----")]
    [SerializeField] AudioSource[] sounds;
    public GameObject explosionVfx;

    void Start()
    {
        throwBombAction = InputSystem.actions.FindAction("BombSystem/Look");
        throwBombAction.started += x => isTouchingJoystick = true;
        throwBombAction.canceled += x => ThrowBomb();       
        lineRenderer = GetComponent<LineRenderer>();
        UpdateBombPanel();
    }

    void Update()
    {
        if (isTouchingJoystick)
        {
            ThrowTrajectory();
            UpdateCameraRotation();
        }
    }

    void ThrowBomb()
    {
        isTouchingJoystick = false;
        lineRenderer.enabled = false;
        ImpactIndicator.SetActive(false);

        PlaySound(0); // Bomb throw sound

        bombObjectPool[bombObjectPoolIndex].transform.SetPositionAndRotation(transform.position, transform.rotation);
        bombObjectPool[bombObjectPoolIndex].SetActive(true);

        Rigidbody rb = bombObjectPool[bombObjectPoolIndex].GetComponent<Rigidbody>();
        rb.AddTorque(new(Random.Range(0.2f, 2f), Random.Range(0.3f, 1f), Random.Range(0.5f, 2.3f)));
        rb.linearVelocity = transform.forward * ThrowForce; // Launches the bomb straight forward

        BombAmount--;
        UpdateBombPanel();
        DataSystem.instance.UpdateData(3);

        if (bombObjectPoolIndex != bombObjectPool.Length - 1)
        {
            bombObjectPoolIndex++;
        }
        else
        {
            bombObjectPoolIndex = 0;
        }
    }

    void ThrowTrajectory()
    {
        bool isHit = false;
        Vector3 ImpactPoint = Vector3.zero;
        Vector3 ImpactRotation = Vector3.zero;
        int currentPoint = 0;

        lineRenderer.enabled = true;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = transform.forward * ThrowForce;

        Vector3[] trajectoryPoints = new Vector3[MaxSegmentCount];

        for (int i = 0; i < MaxSegmentCount; i++)
        {
            float t = i * TimeStep;
            Vector3 point = startPosition + endPosition * t + 0.3f * t * t * Physics.gravity;

            trajectoryPoints[i] = point;

            if (i > 0)
            {
                Vector3 dir = trajectoryPoints[i] - trajectoryPoints[i - 1];
                float Distance = dir.magnitude;

                if (Physics.Raycast(trajectoryPoints[i-1], dir.normalized, out RaycastHit hit, Distance, _LayerMask))
                {
                    ImpactPoint = hit.point;
                    ImpactRotation = hit.normal;
                    isHit = true;
                    currentPoint = i + 1;
                    break;
                }
            }

            currentPoint = i + 1;
        }

        lineRenderer.positionCount = currentPoint;

        for (int i = 0; i < currentPoint; i++)
        {
            lineRenderer.SetPosition(i, trajectoryPoints[i]);
        }

        if (isHit)
        {
            ImpactIndicator.SetActive(true);
            ImpactIndicator.transform.SetPositionAndRotation(ImpactPoint + Vector3.up * 0.06f, Quaternion.LookRotation(ImpactRotation));
        }
        else
        {
            ImpactIndicator.SetActive(false);
        }

    }

    public void UpdateBombPanel()
    {
        bombAmountText.text = BombAmount.ToString();

        if (BombAmount != 0)
        {
            throwBombButton.interactable = true;
            throwBombButton.GetComponent<OnScreenStick>().enabled = true;
        }
        else
        {
            throwBombButton.interactable = false;
            throwBombButton.GetComponent<OnScreenStick>().enabled = false;
        }
    }

    void UpdateCameraRotation()
    {
        Vector2 rotateAxis;
        Vector2 rotateVector = throwBombAction.ReadValue<Vector2>();
        rotateAxis = cameraRotateSensivity * rotateVector;
        Player.instance.transform.Rotate(Vector3.up * rotateAxis.x);

        float newRotation = Player.instance.camera.transform.localEulerAngles.x - rotateAxis.y;
        if (newRotation > 180)
        {
            newRotation -= 360;
        }

        newRotation = Mathf.Clamp(newRotation, Player.instance.rotationAngleLimitY, Player.instance.rotationAngleLimitX);
        Player.instance.camera.transform.localEulerAngles = new Vector3(newRotation, 0, 0);
    }

    public void PlaySound(int ID)
    {
        sounds[ID].Play();
    }

    // Sound Management
    public void SetDataSystemVolumeLevel(float EffectVolumeLevel)
    {
        sounds[0].volume = sounds[1].volume = EffectVolumeLevel;
    }


}
