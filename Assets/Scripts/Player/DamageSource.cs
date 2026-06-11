using System.Collections.Generic;
using UnityEngine;

public class DamageSource : MonoBehaviour
{
    [SerializeField] private int damageAmount = 1;

    private readonly HashSet<GameObject> damagedTargets = new();

    private void OnEnable()
    {
        damagedTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject target = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (!damagedTargets.Add(target))
        {
            return;
        }

        TrainingHealth trainingHealth = other.GetComponentInParent<TrainingHealth>();
        if (trainingHealth != null)
        {
            trainingHealth.TakeDamage(damageAmount, gameObject);
            return;
        }

        EnemyHealthy enemyHealth = other.GetComponentInParent<EnemyHealthy>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount);
        }
    }
}
