using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class EnemyTrainingAgent : Agent
{
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private TrainingHealth playerHealth;
    [SerializeField] private TrainingHealth enemyHealth;
    [SerializeField] private TrainingHitbox attackHitbox;
    [SerializeField] private TrainingRangedAttack rangedAttack;
    [SerializeField] private TrainingArenaManager arenaManager;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private float maxObservationDistance = 10f;
    [SerializeField] private float maxEpisodeTime = 120f;
    [SerializeField] private bool enforceMinimumEpisodeTime = true;
    [SerializeField] private float minimumEpisodeTime = 120f;
    [Header("Visual Smoothing")]
    [SerializeField] private bool pauseAnimatorWhenStill = true;
    [SerializeField] private float stillVelocityThreshold = 0.05f;

    private float previousDistance;
    private float nextAttackTime;
    private float episodeTimer;
    private int idleStepCount;
    private bool episodeEnding;
    private Vector2 targetVelocity;
    private Animator agentAnimator;

    public override void Initialize()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        ConfigureBody(body);
        agentAnimator = GetComponent<Animator>();

        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<TrainingHealth>();
        }

        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponent<TrainingHealth>();
        }

        if (attackHitbox == null)
        {
            attackHitbox = GetComponentInChildren<TrainingHitbox>();
        }

        if (rangedAttack == null)
        {
            rangedAttack = GetComponent<TrainingRangedAttack>();
        }

        if (arenaManager == null)
        {
            arenaManager = FindFirstObjectByType<TrainingArenaManager>();
        }

        if (enemyHealth != null)
        {
            enemyHealth.Damaged += OnEnemyDamaged;
            enemyHealth.Died += OnEnemyDied;
        }

        if (playerHealth != null)
        {
            playerHealth.Died += OnPlayerDied;
        }

        if (attackHitbox != null)
        {
            attackHitbox.Hit += OnAttackHit;
        }

        if (rangedAttack != null)
        {
            rangedAttack.HitTarget += OnRangedAttackHit;
            rangedAttack.MissedTarget += OnRangedAttackMissed;
        }
    }

    public override void OnEpisodeBegin()
    {
        episodeEnding = false;
        episodeTimer = 0f;
        idleStepCount = 0;
        nextAttackTime = 0f;
        targetVelocity = Vector2.zero;

        if (arenaManager != null)
        {
            arenaManager.ResetArena();
        }

        previousDistance = GetDistanceToPlayer();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (player == null || playerHealth == null || enemyHealth == null)
        {
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            return;
        }

        Vector2 relativePosition = player.position - transform.position;
        float distance = relativePosition.magnitude;

        sensor.AddObservation(relativePosition.x / maxObservationDistance);
        sensor.AddObservation(relativePosition.y / maxObservationDistance);
        sensor.AddObservation(distance / maxObservationDistance);
        sensor.AddObservation(playerHealth.Health01);
        sensor.AddObservation(enemyHealth.Health01);
        sensor.AddObservation(Time.time >= nextAttackTime ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (episodeEnding)
        {
            return;
        }

        episodeTimer += Time.fixedDeltaTime;
        if (episodeTimer >= GetEpisodeTimeLimit())
        {
            AddReward(-0.5f);
            EndTrainingEpisode();
            return;
        }

        int action = actions.DiscreteActions[0];
        Vector2 moveDirection = GetMoveDirection(action);

        targetVelocity = moveDirection * moveSpeed;

        RewardMovement(moveDirection);

        if (action == 5)
        {
            TryAttack();
        }

        AddReward(-0.001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        Vector2 toPlayer = player.position - transform.position;

        if (toPlayer.magnitude <= attackRange && Time.time >= nextAttackTime)
        {
            discreteActions[0] = 5;
            return;
        }

        if (Mathf.Abs(toPlayer.x) > Mathf.Abs(toPlayer.y))
        {
            discreteActions[0] = toPlayer.x > 0f ? 4 : 3;
        }
        else
        {
            discreteActions[0] = toPlayer.y > 0f ? 1 : 2;
        }
    }

    private Vector2 GetMoveDirection(int action)
    {
        return action switch
        {
            1 => Vector2.up,
            2 => Vector2.down,
            3 => Vector2.left,
            4 => Vector2.right,
            _ => Vector2.zero
        };
    }

    private void FixedUpdate()
    {
        if (body == null)
        {
            return;
        }

        if (episodeEnding)
        {
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, Vector2.zero, acceleration * Time.fixedDeltaTime);
            return;
        }

        body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        UpdateAnimatorPlayback();
    }

    private void RewardMovement(Vector2 moveDirection)
    {
        float currentDistance = GetDistanceToPlayer();
        float distanceDelta = previousDistance - currentDistance;

        if (moveDirection == Vector2.zero)
        {
            idleStepCount++;
            if (idleStepCount > 10)
            {
                AddReward(-0.02f);
            }
        }
        else
        {
            idleStepCount = 0;
        }

        if (currentDistance > attackRange && distanceDelta > 0f)
        {
            AddReward(0.02f);
        }
        else if (currentDistance > attackRange && distanceDelta < -0.01f)
        {
            AddReward(-0.01f);
        }

        previousDistance = currentDistance;
    }

    private void TryAttack()
    {
        float distanceToPlayer = GetDistanceToPlayer();

        if (Time.time < nextAttackTime)
        {
            AddReward(-0.02f);
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        AimAtPlayer();

        if (distanceToPlayer > attackRange)
        {
            AddReward(-0.05f);
        }

        if (rangedAttack != null)
        {
            rangedAttack.Fire();
        }
        else if (attackHitbox != null)
        {
            attackHitbox.Activate();
        }
    }

    private void OnAttackHit(TrainingHealth target)
    {
        if (target == playerHealth)
        {
            AddReward(0.5f);
        }
    }

    private void OnRangedAttackHit(TrainingHealth target)
    {
        if (target == playerHealth)
        {
            AddReward(0.5f);
        }
    }

    private void OnRangedAttackMissed()
    {
        AddReward(-0.05f);
    }

    private void OnEnemyDamaged(TrainingHealth target, int damageAmount, GameObject source)
    {
        AddReward(-0.3f);
    }

    private void OnPlayerDied(TrainingHealth target)
    {
        AddReward(5f);
        EndTrainingEpisode();
    }

    private void OnEnemyDied(TrainingHealth target)
    {
        AddReward(-5f);
        EndTrainingEpisode();
    }

    private float GetDistanceToPlayer()
    {
        if (player == null)
        {
            return maxObservationDistance;
        }

        return Vector2.Distance(transform.position, player.position);
    }

    private void AimAtPlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector2 direction = player.position - transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.right = direction.normalized;
        }
    }

    private void EndTrainingEpisode()
    {
        if (episodeEnding)
        {
            return;
        }

        episodeEnding = true;
        EndEpisode();
    }

    private float GetEpisodeTimeLimit()
    {
        return enforceMinimumEpisodeTime ? Mathf.Max(maxEpisodeTime, minimumEpisodeTime) : maxEpisodeTime;
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.Damaged -= OnEnemyDamaged;
            enemyHealth.Died -= OnEnemyDied;
        }

        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
        }

        if (attackHitbox != null)
        {
            attackHitbox.Hit -= OnAttackHit;
        }

        if (rangedAttack != null)
        {
            rangedAttack.HitTarget -= OnRangedAttackHit;
            rangedAttack.MissedTarget -= OnRangedAttackMissed;
        }
    }

    private static void ConfigureBody(Rigidbody2D targetBody)
    {
        if (targetBody == null)
        {
            return;
        }

        targetBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        targetBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void UpdateAnimatorPlayback()
    {
        if (!pauseAnimatorWhenStill || agentAnimator == null || body == null)
        {
            return;
        }

        agentAnimator.speed = body.linearVelocity.sqrMagnitude > stillVelocityThreshold * stillVelocityThreshold ? 1f : 0f;
    }
}
