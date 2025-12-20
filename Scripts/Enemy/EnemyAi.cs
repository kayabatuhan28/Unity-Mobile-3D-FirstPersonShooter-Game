using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[System.Serializable]
public class PatrolPath
{
    public Transform[] Paths;
}

public class EnemyAi : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack, InvestigateAttacker }
    State currentState = State.Idle;

    float distanceToPlayer, distance;
    bool canSeePlayer, isPlayerClose, isPlayerTargetedAndInSight, isPlayerDetected;
    Vector3 sightStartPosition, sightTargetPosition, raycastDirection;
    

    [Header("------ Components -------")]
    [SerializeField] NavMeshAgent navmeshAgent;
    [SerializeField] Animator animator;
    [SerializeField] Transform playerTarget;

    [Header("------ Health Management -------")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] Image healthBarImage;
    [SerializeField] GameObject healthBarCanvas;
    float healthBarVisibleDuration = 3f;
    float timeSinceLastHit; // Timer representing the duration until the enemy hides after each hit.
    bool isHealthBarVisible;
    float currentHealth;
    bool isDead = false;
    

    [Header("------ Perception -------")]
    [SerializeField] float seeDistance = 10f;
    [SerializeField] float attackDistance = 9f;
    [SerializeField] float closeDistance = 5f; // Can be triggered even if the enemy doesn't see the player when the player gets too close from behind.                                            
    [SerializeField] float attackRate = 0.2f;
    [SerializeField] LayerMask raycastLayerMask;
    float playerLastSeenTime = Mathf.NegativeInfinity; 
    float playerForgetTime = 3f; // Stops chasing the player if the enemy cannot see the player for a while.


    [Header("------ Attack -------")]
    [SerializeField] Transform bulletSpawnPoint;
    [SerializeField] ParticleSystem muzzleEffect;
    public ParticleSystem bloodEffect;
    [SerializeField] AudioSource fireSound;
    float lastAttackTime = 0;
    [SerializeField] float Damage = 10f;

    [Header("------ Patrol -------")]
    public PatrolPath[] PatrolPaths;
    Transform[] selectedPath;
    int currentPathIndex;

    // Noise Hearing
    public Transform soundPosition;
    bool isReachToSound = false;

    static List<int> usedPaths = new();

    void Start()
    {
        currentHealth = maxHealth;
        SetPatrolPath();
    }

    void Update()
    {
        if (isDead) return;

        if (GameManager.instance.isGameFinish)
        {
            Patrol();
            return;
        }

        distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        canSeePlayer = CheckLineOfSight();
        isPlayerClose = distanceToPlayer <= closeDistance;
        isPlayerTargetedAndInSight = distanceToPlayer <= seeDistance && canSeePlayer;
        isPlayerDetected = isPlayerTargetedAndInSight || isPlayerClose;
        
        switch (currentState)
        {
            case State.Idle:                          
            case State.Patrol:                            
                Patrol();              
                break;
            case State.Chase:
                ChaseState();
                break;
            case State.Attack:
                AttackState();
                break;
            case State.InvestigateAttacker:
                InvestigateAttackerState();
                break;
        }
        // Health Bar visiblity
        UpdateHealthBarVisibilityAndRotation();

        // Investigate Sound
        if (soundPosition != null) return;
        if (isReachToSound)
        {
            navmeshAgent.SetDestination(soundPosition.position);
        }

        if (soundPosition == null) return;

        if (!isReachToSound && Vector3.Distance(transform.position, soundPosition.position) < 8f)
        {
            isReachToSound = true;
            soundPosition = null;
            currentState = State.Patrol;
        }



    }

    

    void FireWeapon()
    {       
        Ray ray = new(bulletSpawnPoint.position, bulletSpawnPoint.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, attackDistance))
        {         
            if (hit.transform.CompareTag("Player"))
            {
                hit.transform.GetComponent<Player>().UpdateHealth(false, Damage, transform.position);              
                muzzleEffect.Play();
                fireSound.Play();
                Player.instance.bloodEffect.gameObject.SetActive(true);
                Player.instance.bloodEffect.transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
            }
            
            Debug.DrawRay(bulletSpawnPoint.position, bulletSpawnPoint.forward * attackDistance, Color.green);
        }
    }


    public void TakeDamage(float TakingDamage)
    {
        currentHealth -= TakingDamage;

        if (currentHealth <= 0)
        {
            OnDead();
        }
        else
        {
            healthBarImage.fillAmount = currentHealth / maxHealth;
            animator.Play("TakeDamage");
            SetHealthBarVisiblity(true);

            if (currentState == State.Idle || currentState == State.Patrol)
            {
                currentState = State.InvestigateAttacker;
            } 
        }
    }
    void OnDead()
    {
        healthBarImage.fillAmount = 0;
        animator.Play("Die");
        Destroy(gameObject, 4f);
        isDead = true;
        SetHealthBarVisiblity(false);
    }
    private void OnDestroy()
    {
        if (isDead)
        {
            GameManager.instance.SpawnLootOnEnemyDead(transform);
            GameManager.instance.TotalEnemiesKilled++;
        }
    }

    void UpdateAnimationState(bool Walk, bool Run, bool Attack)
    {
        animator.SetBool("Walk", Walk);
        animator.SetBool("Run", Run);
        animator.SetBool("Attack", Attack);
    }


    // Perception
    bool CheckLineOfSight()
    {
        sightStartPosition = transform.position + Vector3.up * 1.5f;
        sightTargetPosition = playerTarget.position;
        raycastDirection = (sightTargetPosition - sightStartPosition).normalized;     
        distance = Vector3.Distance(sightStartPosition, sightTargetPosition);
        
        if (Physics.Raycast(sightStartPosition, raycastDirection, out RaycastHit hit, distance, raycastLayerMask))
        {
            if (hit.transform.CompareTag("Player"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        Debug.DrawRay(sightStartPosition, raycastDirection * distance, Color.red);
        return false;
    }

    
    // -------- Ai States -------- 
    void SetPatrolPath()
    {
        List<int> currentIndices = new();

        for (int i = 0; i < PatrolPaths.Length; i++)
        {
            if (!usedPaths.Contains(i))
            {
                currentIndices.Add(i);
            }
        }

        // If there is no patrol point or no patrol points left.
        if (currentIndices.Count == 0)
        {
            return;
        }

        int selectedPathIndex = currentIndices[Random.Range(0, currentIndices.Count)];
        selectedPath = PatrolPaths[selectedPathIndex].Paths;
        usedPaths.Add(selectedPathIndex);
        currentPathIndex = 0;

        if (selectedPath.Length > 0)
        {
            navmeshAgent.SetDestination(selectedPath[currentPathIndex].position);
        }
    }
    void Patrol()
    {
        if (selectedPath == null || selectedPath.Length == 0) return;

        //UpdateAnimationState(true, false, false);
        UpdateAnimationState(Walk: true, Run: false, Attack: false);

        if (!navmeshAgent.pathPending && navmeshAgent.remainingDistance < 0.5f)
        {
            currentPathIndex = (currentPathIndex + 1) % selectedPath.Length;
            navmeshAgent.SetDestination(selectedPath[currentPathIndex].position);
        }

        if (isPlayerDetected)
        {
            playerLastSeenTime = Time.time;
            currentState = State.Chase;
        }
    }
    void ChaseState()
    {
        UpdateAnimationState(Walk: false, Run: true, Attack: false);
        navmeshAgent.SetDestination(playerTarget.position);

        // Starts attacking when within attack distance
        if (distanceToPlayer <= attackDistance && CheckLineOfSight())
        {
            currentState = State.Attack;
        }

        // Loses interest in the player if the distance becomes too great
        if (!isPlayerDetected && Time.time - playerLastSeenTime > playerForgetTime)
        {
            currentState = State.Patrol;
        }

        if (isPlayerDetected)
        {
            playerLastSeenTime = Time.time;
        }
    }
    void AttackState()
    {
        navmeshAgent.SetDestination(transform.position); // Stops the enemy
        transform.LookAt(playerTarget.transform.position - new Vector3(0, 0.5f, 0));

        // When line of sight is lost
        if (!CheckLineOfSight())
        {
            currentState = State.Chase;
            return;
        }

        // When the player escapes / leaves the attack range
        if (distanceToPlayer > attackDistance)
        {
            currentState = State.Chase;
            return;
        }

        // If attack conditions are met
        UpdateAnimationState(Walk: false, Run: false, Attack: true);
        if (Time.time - lastAttackTime >= attackRate)
        {
            lastAttackTime = Time.time;
            FireWeapon();
        }
    }
    void InvestigateAttackerState()
    {
        UpdateAnimationState(Walk: false, Run: true, Attack: false);
        navmeshAgent.SetDestination(playerTarget.position);

        // Attacks when the player enters attack range
        if (distanceToPlayer <= attackDistance && CheckLineOfSight())
        {
            currentState = State.Attack;
        }
      
        if (isPlayerDetected)
        {
            playerLastSeenTime = Time.time;
        }
    }

   
    public void InvestigateSound(Transform soundSourceTransform)
    {
        if (!isReachToSound)
        {
            soundPosition = soundSourceTransform;
            navmeshAgent.SetDestination(soundPosition.position);
        }
    }
    
    // Health Bar Visiblity + Rotation
    void UpdateHealthBarVisibilityAndRotation()
    {
        if (isHealthBarVisible)
        {
            // Countdown
            timeSinceLastHit -= Time.deltaTime;
            if (timeSinceLastHit <= 0f)
            {
                SetHealthBarVisiblity(false);
            }

            // Look Player
            if (healthBarCanvas.activeInHierarchy)
            {
                healthBarCanvas.transform.forward = Player.instance.camera.transform.forward;
            }
        }
    }
    void SetHealthBarVisiblity(bool Visiblity)
    {
        if (Visiblity)
        {
            healthBarCanvas.SetActive(true);
            timeSinceLastHit = healthBarVisibleDuration;
            isHealthBarVisible = true;
        }
        else
        {
            healthBarCanvas.SetActive(false);         
            isHealthBarVisible = false;
        }
    }

    
    // Draw Gizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, seeDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, closeDistance);
    }

}
