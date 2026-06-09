using UnityEngine;

public class TrainingPlayerBot : MonoBehaviour
{
    [SerializeField] private Transform enemy;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private TrainingHitbox attackHitbox;
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

    private void Awake()
    {
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

        body.MovePosition(body.position + currentMoveDirection * (moveSpeed * Time.fixedDeltaTime));
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
