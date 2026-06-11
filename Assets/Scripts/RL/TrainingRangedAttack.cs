using System;
using UnityEngine;

public class TrainingRangedAttack : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private TrainingHealth ownerHealth;
    [SerializeField] private TrainingHealth targetHealth;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpawnOffset = 0.7f;
    [SerializeField] private float bulletMoveSpeed = 4f;
    [SerializeField] private float projectileRange = 4.5f;
    [SerializeField] private int damageAmount = 1;
    [Header("Projectile Visual")]
    [SerializeField] private float spawnedProjectileScale = 1f;
    [SerializeField] private Color spawnedProjectileColor = new(0.1f, 0.9f, 1f, 1f);
    [SerializeField] private int spawnedProjectileSortingOrder = 50;
    [SerializeField] private bool addProjectileTrail = false;
    [SerializeField] private float trailTime = 0.25f;
    [SerializeField] private float trailWidth = 0.16f;

    public event Action<TrainingHealth> HitTarget;
    public event Action MissedTarget;

    private void Awake()
    {
        damageAmount = 1;
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
        Vector2 shootDirection = direction.normalized;
        Vector3 spawnPosition = spawnPoint.position + (Vector3)(shootDirection * projectileSpawnOffset);
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        bullet.transform.right = shootDirection;

        if (bullet.TryGetComponent(out Projectile projectile))
        {
            projectile.UpdateMoveSpeed(bulletMoveSpeed);
            projectile.UpdateProjectileRange(projectileRange);
            projectile.IgnoreCollisionWith(gameObject);
            projectile.EnableRuntimeEnemyProjectileVisual();
        }

        TrainingProjectileDamage trainingProjectile = bullet.GetComponent<TrainingProjectileDamage>();
        if (trainingProjectile == null)
        {
            trainingProjectile = bullet.AddComponent<TrainingProjectileDamage>();
        }

        trainingProjectile.Initialize(ownerHealth, targetHealth, damageAmount, OnProjectileHit, OnProjectileMissed);
    }

    private void ApplyProjectileVisual(GameObject bullet)
    {
        bullet.transform.localScale = Vector3.one * spawnedProjectileScale;

        if (bullet.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.color = spawnedProjectileColor;
            spriteRenderer.sortingOrder = spawnedProjectileSortingOrder;
        }

        if (!addProjectileTrail || bullet.GetComponent<TrailRenderer>() != null)
        {
            return;
        }

        TrailRenderer trailRenderer = bullet.AddComponent<TrailRenderer>();
        trailRenderer.time = trailTime;
        trailRenderer.startWidth = trailWidth;
        trailRenderer.endWidth = 0f;
        trailRenderer.sortingOrder = spawnedProjectileSortingOrder - 1;
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.startColor = spawnedProjectileColor;
        trailRenderer.endColor = new Color(spawnedProjectileColor.r, spawnedProjectileColor.g, spawnedProjectileColor.b, 0f);
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
