using System;
using System.Collections.Generic;
using UnityEngine;

public class TrainingProjectileDamage : MonoBehaviour
{
    private static readonly List<TrainingProjectileDamage> ActiveProjectiles = new();
    private static bool isClearingProjectiles;

    private TrainingHealth ownerHealth;
    private TrainingHealth targetHealth;
    private Action<TrainingHealth> hitCallback;
    private Action missedCallback;
    private int damageAmount = 1;
    private bool initialized;
    private bool hitSomething;
    private bool suppressMissedCallback;

    public static void ClearAll()
    {
        isClearingProjectiles = true;

        for (int index = ActiveProjectiles.Count - 1; index >= 0; index--)
        {
            TrainingProjectileDamage projectile = ActiveProjectiles[index];
            if (projectile != null)
            {
                projectile.SuppressMissedCallback();
                Destroy(projectile.gameObject);
            }
        }

        ActiveProjectiles.Clear();
        isClearingProjectiles = false;
    }

    public void Initialize(
        TrainingHealth owner,
        TrainingHealth target,
        int damage,
        Action<TrainingHealth> onHit,
        Action onMissed)
    {
        ownerHealth = owner;
        targetHealth = target;
        damageAmount = damage;
        hitCallback = onHit;
        missedCallback = onMissed;
        initialized = true;
    }

    private void SuppressMissedCallback()
    {
        suppressMissedCallback = true;
    }

    private void OnEnable()
    {
        ActiveProjectiles.Add(this);
    }

    private void OnDisable()
    {
        ActiveProjectiles.Remove(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized || hitSomething)
        {
            return;
        }

        TrainingHealth hitHealth = other.GetComponentInParent<TrainingHealth>();

        if (hitHealth == null || hitHealth == ownerHealth)
        {
            return;
        }

        if (targetHealth != null && hitHealth != targetHealth)
        {
            return;
        }

        hitSomething = true;
        hitHealth.TakeDamage(damageAmount, ownerHealth != null ? ownerHealth.gameObject : gameObject);
        hitCallback?.Invoke(hitHealth);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        ActiveProjectiles.Remove(this);

        if (initialized && !hitSomething && !suppressMissedCallback && !isClearingProjectiles)
        {
            missedCallback?.Invoke();
        }
    }
}
