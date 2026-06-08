using System;
using UnityEngine;

public class TrainingRangedAttack : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private TrainingHealth ownerHealth;
    [SerializeField] private TrainingHealth targetHealth;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletMoveSpeed = 4f;
    [SerializeField] private float projectileRange = 10f;
    [SerializeField] private int damageAmount = 1;

    public event Action<TrainingHealth> HitTarget;
    public event Action MissedTarget;

    private void Awake()
    {
        if (ownerHealth == null)
        {
            ownerHealth = GetComponent<TrainingHealth>();
        }
    }

    public void Fire()
    {
        if (bulletPrefab == null || target == null)
        {
            return;
        }

        if (targetHealth == null)
        {
            targetHealth = target.GetComponent<TrainingHealth>();
        }

        Vector2 direction = target.position - transform.position;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Transform spawnPoint = firePoint != null ? firePoint : transform;
        GameObject bullet = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.identity);
        bullet.transform.right = direction.normalized;

        if (bullet.TryGetComponent(out Projectile projectile))
        {
            projectile.UpdateMoveSpeed(bulletMoveSpeed);
            projectile.UpdateProjectileRange(projectileRange);
        }

        TrainingProjectileDamage trainingProjectile = bullet.GetComponent<TrainingProjectileDamage>();
        if (trainingProjectile == null)
        {
            trainingProjectile = bullet.AddComponent<TrainingProjectileDamage>();
        }

        trainingProjectile.Initialize(ownerHealth, targetHealth, damageAmount, OnProjectileHit, OnProjectileMissed);
    }

    private void OnProjectileHit(TrainingHealth hitTarget)
    {
        HitTarget?.Invoke(hitTarget);
    }

    private void OnProjectileMissed()
    {
        MissedTarget?.Invoke();
    }
}
