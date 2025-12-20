using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField] float delayBeforeExplosion = 2f;
    [SerializeField] float Damage = 60f;
    float explosionForce = 500f;
    float explosionDistance = 5f;
    float countdownTimer;
    bool isExploded = false;

    Collider[] HitObjects = new Collider[10];

    // Created to prevent dealing damage multiple times due to multiple colliders on the Player object
    private HashSet<GameObject> damagedObjects = new HashSet<GameObject>();

    // Represents the maximum number of AIs that will investigate the sound after a bomb explosion
    Collider[] detectedEnemies = new Collider[3];

    private void OnEnable()
    {
        countdownTimer = delayBeforeExplosion;
    }

    private void OnDisable()
    {
        isExploded = false;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    void Update()
    {
        countdownTimer -= Time.deltaTime;
        if (countdownTimer <= 0 && !isExploded)
        {
            Explode();
        }
    }

    void Explode()
    {
        isExploded = true;
        Player.instance._ThrowBombSystem.explosionVfx.transform.position = transform.position;
        Player.instance._ThrowBombSystem.explosionVfx.SetActive(true);
        Player.instance._ThrowBombSystem.PlaySound(1);

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, explosionDistance, HitObjects);
        damagedObjects.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            var hittedObject = HitObjects[i].gameObject;

            //  Skip if the same object is encountered again
            if (!damagedObjects.Add(hittedObject))
            {
                continue;
            }

            if (hittedObject.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionDistance);
            }

            if (hittedObject.CompareTag("Enemy"))
            {
                hittedObject.GetComponent<EnemyAi>().TakeDamage(Damage);
            }

            if (hittedObject.CompareTag("Player"))
            {
                Debug.Log("Name " + HitObjects[i].gameObject.name);
                hittedObject.GetComponent<Player>().UpdateHealth(false, Damage, transform.position);
            }
        }

        // Informs nearby AI about the weapon firing for sound based checks.
        int enemies = Physics.OverlapSphereNonAlloc(transform.position, 16, detectedEnemies, LayerMask.GetMask("Enemy"));
        for (int i = 0; i < enemies; i++)
        {
            detectedEnemies[i].GetComponent<EnemyAi>().InvestigateSound(transform);
        }

        gameObject.SetActive(false);    
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionDistance);
    }
}
