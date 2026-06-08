using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainingHitbox : MonoBehaviour
{
    [SerializeField] private TrainingHealth owner;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float activeTime = 0.15f;

    public event Action<TrainingHealth> Hit;

    private readonly HashSet<TrainingHealth> hitTargets = new();
    private Collider2D hitboxCollider;
    private Coroutine activeRoutine;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider2D>();

        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = false;
        }
    }

    public void Activate()
    {
        if (hitboxCollider == null)
        {
            return;
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(ActivateRoutine());
    }

    private IEnumerator ActivateRoutine()
    {
        hitTargets.Clear();
        hitboxCollider.enabled = true;
        yield return new WaitForSeconds(activeTime);
        hitboxCollider.enabled = false;
        activeRoutine = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hitboxCollider.enabled)
        {
            return;
        }

        TrainingHealth targetHealth = other.GetComponentInParent<TrainingHealth>();

        if (targetHealth == null || targetHealth == owner || hitTargets.Contains(targetHealth))
        {
            return;
        }

        hitTargets.Add(targetHealth);
        targetHealth.TakeDamage(damageAmount, owner != null ? owner.gameObject : gameObject);
        Hit?.Invoke(targetHealth);
    }
}
