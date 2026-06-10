using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Shooter : MonoBehaviour,IEnemy
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletMoveSpeed;
    [SerializeField] private int burstCount;
    [SerializeField] private float timeBetweenBursts;
    [SerializeField] private float restTime = 1f;
    [SerializeField] private float projectileSpawnOffset = 0.7f;

    private bool isShooting = false;

    public void Attack()
    {
        if (!isShooting)
        {
            StartCoroutine(ShootRoutine());
        }
    }

    private IEnumerator ShootRoutine()
    {
        isShooting = true;

        for (int i = 0; i < burstCount; i++)
        {
            if (PlayerController.Instance == null)
            {
                yield break;
            }

            Vector2 targetDirection = PlayerController.Instance.transform.position - transform.position;
            Vector2 shootDirection = targetDirection.sqrMagnitude > 0.001f ? targetDirection.normalized : transform.right;
            Vector3 spawnPosition = transform.position + (Vector3)(shootDirection * projectileSpawnOffset);

            GameObject newBullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
            newBullet.transform.right = shootDirection;

            if (newBullet.TryGetComponent(out Projectile projectile))
            {
                projectile.UpdateMoveSpeed(bulletMoveSpeed);
                projectile.IgnoreCollisionWith(gameObject);
                projectile.EnableRuntimeEnemyProjectileVisual();
            }

            yield return new WaitForSeconds(timeBetweenBursts);
        }

        yield return new WaitForSeconds(restTime);
        isShooting = false;
    }


}
