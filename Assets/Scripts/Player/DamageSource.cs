using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DamageSource : MonoBehaviour
{
    [SerializeField] private int damageAmount = 1; // Khai báo biến damageAmount
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<EnemyHealthy>())
        {
            EnemyHealthy enemyHealth = other.gameObject.GetComponent<EnemyHealthy>();
            enemyHealth?.TakeDamage(damageAmount);
        }

        if (other.gameObject.GetComponent<TrainingHealth>())
        {
            TrainingHealth trainingHealth = other.gameObject.GetComponent<TrainingHealth>();
            trainingHealth?.TakeDamage(damageAmount, gameObject);
        }
    }

}
