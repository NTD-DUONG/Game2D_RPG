using UnityEngine;

public class TrainingPlayerBot : MonoBehaviour
{
    [SerializeField] private Transform enemy;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private TrainingHitbox attackHitbox;
    [SerializeField] private TrainingHealth trainingHealth;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TrainingArenaManager arenaManager;
    [SerializeField] private Knockback knockback;
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float acceleration = 14f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float directionChangeTime = 1f;

    private Vector2 wanderDirection;
    private Vector2 targetMoveDirection;
    private Vector2 currentMoveDirection;
    private float nextAttackTime;
    private float directionTimer;

    public void Configure(Transform enemyTarget, Rigidbody2D targetBody, TrainingHitbox targetAttackHitbox)
    {
        enemy = enemyTarget;
        body = targetBody != null ? targetBody : GetComponent<Rigidbody2D>();
        CacheHealth();
        CacheKnockback();
        CacheArenaManager();

        if (targetAttackHitbox != null)
        {
            attackHitbox = targetAttackHitbox;
        }
        else if (attackHitbox == null)
        {
            attackHitbox = GetComponentInChildren<TrainingHitbox>(true);
        }

        if (body != null)
        {
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void Awake()
    {
        CacheHealth();
        CacheKnockback();
        CacheArenaManager();

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (body != null)
        {
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (attackHitbox == null)
        {
            attackHitbox = GetComponentInChildren<TrainingHitbox>(true);
        }
    }

    private void FixedUpdate()
    {
        if (IsDead())
        {
            StopMovement();
            return;
        }

        if (knockback != null && knockback.GettingKnockedBack)
        {
            currentMoveDirection = Vector2.zero;
            targetMoveDirection = Vector2.zero;
            return;
        }

        if (enemy == null || body == null)
        {
            return;
        }

        float distanceToEnemy = Vector2.Distance(transform.position, enemy.position);

        if (distanceToEnemy <= attackRange)
        {
            Vector2 awayFromEnemy = ((Vector2)transform.position - (Vector2)enemy.position).normalized;
            Vector2 strafe = new Vector2(-awayFromEnemy.y, awayFromEnemy.x);
            targetMoveDirection = (awayFromEnemy * 0.4f + strafe * 0.6f).normalized;
            ApplyMovement();
            TryAttack();
            return;
        }

        directionTimer -= Time.fixedDeltaTime;
        if (directionTimer <= 0f)
        {
            wanderDirection = Random.insideUnitCircle.normalized;
            directionTimer = directionChangeTime;
        }

        targetMoveDirection = wanderDirection;
        ApplyMovement();
    }

    private void ApplyMovement()
    {
        currentMoveDirection = Vector2.MoveTowards(
            currentMoveDirection,
            targetMoveDirection,
            acceleration * Time.fixedDeltaTime
        );

        Vector2 nextPosition = body.position + currentMoveDirection * (moveSpeed * Time.fixedDeltaTime);
        if (arenaManager != null)
        {
            nextPosition = arenaManager.ClampToArena(nextPosition);
        }

        body.MovePosition(nextPosition);
    }

    private bool IsDead()
    {
        return trainingHealth != null && trainingHealth.IsDead
            || playerHealth != null && playerHealth.isDead;
    }

    private void StopMovement()
    {
        targetMoveDirection = Vector2.zero;
        currentMoveDirection = Vector2.zero;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }

    private void CacheHealth()
    {
        if (trainingHealth == null)
        {
            trainingHealth = GetComponent<TrainingHealth>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }
    }

    private void CacheArenaManager()
    {
        if (arenaManager == null)
        {
            arenaManager = FindFirstObjectByType<TrainingArenaManager>();
        }
    }

    private void CacheKnockback()
    {
        if (knockback == null)
        {
            knockback = GetComponent<Knockback>();
        }
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime || attackHitbox == null)
        {
            return;
        }

        AimAtEnemy();
        nextAttackTime = Time.time + attackCooldown;
        attackHitbox.Activate();
    }

    private void AimAtEnemy()
    {
        Vector2 direction = enemy.position - transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.right = direction.normalized;
        }
    }
}
