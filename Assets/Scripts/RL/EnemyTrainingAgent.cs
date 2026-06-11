using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
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
    [SerializeField] private float directionAcceleration = 12f;
    [SerializeField] private float attackRange = 4f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float maxObservationDistance = 10f;
    [SerializeField] private bool endEpisodeWhenPlayerDies = true;
    [SerializeField] private bool requireClearShotForAttack = false;
    [Header("Obstacle Awareness")]
    [SerializeField] private float obstacleRayDistance = 3f;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float obstacleHitPenalty = -0.015f;
    [SerializeField] private float stuckPenalty = -0.02f;
    [SerializeField] private float minMovementDelta = 0.005f;
    [SerializeField] private float maxEpisodeTime = 120f;
    [SerializeField] private bool enforceMinimumEpisodeTime = true;
    [SerializeField] private float minimumEpisodeTime = 120f;
    [Header("Visual Smoothing")]
    [SerializeField] private bool pauseAnimatorWhenStill = false;
    [SerializeField] private float stillVelocityThreshold = 0.05f;
    [Header("Inference Tuning")]
    [SerializeField] private bool useInferenceAssist = true;
    [SerializeField] private float inferenceMoveSpeed = 3.2f;
    [SerializeField] private float inferenceAssistDistance = 1.5f;
    [SerializeField] private float inferenceAssistBlend = 0.35f;

    private float previousDistance;
    private float nextAttackTime;
    private float episodeTimer;
    private int idleStepCount;
    private bool episodeEnding;
    private Vector2 targetMoveDirection;
    private Vector2 smoothedMoveDirection;
    private Vector2 previousStepPosition;
    private Animator agentAnimator;
    private EnemyPathfinding enemyPathfinding;
    private BehaviorParameters behaviorParameters;

    private static readonly Vector2[] ObstacleRayDirections =
    {
        Vector2.up,
        new(1f, 1f),
        Vector2.right,
        new(1f, -1f),
        Vector2.down,
        new(-1f, -1f),
        Vector2.left,
        new(-1f, 1f)
    };

    public override void Initialize()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        ConfigureBody(body);
        agentAnimator = GetComponent<Animator>();
        enemyPathfinding = GetComponent<EnemyPathfinding>();
        behaviorParameters = GetComponent<BehaviorParameters>();

        if (IsInferenceOnly() && enemyPathfinding != null && inferenceMoveSpeed > 0f)
        {
            enemyPathfinding.MoveSpeed = inferenceMoveSpeed;
        }

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
        targetMoveDirection = Vector2.zero;
        smoothedMoveDirection = Vector2.zero;

        if (arenaManager != null)
        {
            arenaManager.ResetArena();
        }

        StopMovement();

        previousDistance = GetDistanceToPlayer();
        previousStepPosition = transform.position;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (player == null || playerHealth == null || enemyHealth == null)
        {
            AddEmptyObservations(sensor);
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
        sensor.AddObservation(distance <= attackRange ? 1f : 0f);
        sensor.AddObservation(HasClearShotToPlayer(distance) ? 1f : 0f);
        AddObstacleObservations(sensor);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (episodeEnding)
        {
            return;
        }

        episodeTimer += Time.fixedDeltaTime;
        if (UsesTrainingEpisodeReset() && IsParticipantOutsideArena())
        {
            AddReward(-0.5f);
            EndTrainingEpisode();
            return;
        }

        if (UsesTrainingEpisodeReset() && episodeTimer >= GetEpisodeTimeLimit())
        {
            AddReward(-0.5f);
            EndTrainingEpisode();
            return;
        }

        int action = actions.DiscreteActions[0];
        Vector2 moveDirection = GetMoveDirection(action);

        targetMoveDirection = GetInferenceAssistedMoveDirection(moveDirection);

        RewardMovement(moveDirection);

        if (action == 9)
        {
            TryAttack();
        }

        AddReward(-0.001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        Vector2 toPlayer = player.position - transform.position;

        float distanceToPlayer = toPlayer.magnitude;
        if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime && CanAttackPlayer(distanceToPlayer))
        {
            discreteActions[0] = 9;
            return;
        }

        discreteActions[0] = GetClosestMoveAction(toPlayer);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (player == null)
        {
            actionMask.SetActionEnabled(0, 9, false);
            return;
        }

        float distanceToPlayer = GetDistanceToPlayer();
        bool canAttack = distanceToPlayer <= attackRange
            && Time.time >= nextAttackTime
            && CanAttackPlayer(distanceToPlayer);

        actionMask.SetActionEnabled(0, 9, canAttack);
    }

    private Vector2 GetMoveDirection(int action)
    {
        return action switch
        {
            1 => Vector2.up,
            2 => Vector2.down,
            3 => Vector2.left,
            4 => Vector2.right,
            5 => new Vector2(-1f, 1f).normalized,
            6 => new Vector2(1f, 1f).normalized,
            7 => new Vector2(-1f, -1f).normalized,
            8 => new Vector2(1f, -1f).normalized,
            _ => Vector2.zero
        };
    }

    private void FixedUpdate()
    {
        if (episodeEnding)
        {
            StopMovement();
            return;
        }

        ApplyMovement();
    }

    private void RewardMovement(Vector2 moveDirection)
    {
        float currentDistance = GetDistanceToPlayer();
        float distanceDelta = previousDistance - currentDistance;
        float movementDelta = Vector2.Distance(transform.position, previousStepPosition);

        if (moveDirection == Vector2.zero)
        {
            idleStepCount++;
            if (idleStepCount > 10)
            {
                AddReward(currentDistance > attackRange ? -0.04f : -0.02f);
            }
        }
        else
        {
            idleStepCount = 0;

            if (movementDelta < minMovementDelta)
            {
                AddReward(stuckPenalty);
            }

            if (IsObstacleInDirection(moveDirection))
            {
                AddReward(obstacleHitPenalty);
            }
        }

        if (currentDistance > attackRange && distanceDelta > 0f)
        {
            AddReward(0.04f);
        }
        else if (currentDistance > attackRange && distanceDelta < -0.01f)
        {
            AddReward(-0.03f);
        }

        previousDistance = currentDistance;
        previousStepPosition = transform.position;
    }

    private void TryAttack()
    {
        float distanceToPlayer = GetDistanceToPlayer();

        if (Time.time < nextAttackTime)
        {
            AddReward(-0.01f);
            return;
        }

        if (distanceToPlayer > attackRange)
        {
            AddReward(-0.03f);
            return;
        }

        if (!CanAttackPlayer(distanceToPlayer))
        {
            AddReward(-0.03f);
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        AimAtPlayer();

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
            AddReward(1f);
        }
    }

    private void OnRangedAttackHit(TrainingHealth target)
    {
        if (target == playerHealth)
        {
            AddReward(1f);
        }
    }

    private void OnRangedAttackMissed()
    {
        AddReward(-0.15f);
    }

    private void OnEnemyDamaged(TrainingHealth target, int damageAmount, GameObject source)
    {
        AddReward(-0.3f);
    }

    private void OnPlayerDied(TrainingHealth target)
    {
        AddReward(5f);
        if (!endEpisodeWhenPlayerDies || IsInferenceOnly())
        {
            episodeEnding = true;
            StopMovement();
            return;
        }

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

    private void AddEmptyObservations(VectorSensor sensor)
    {
        sensor.AddObservation(Vector2.zero);
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);
        sensor.AddObservation(0f);

        for (int i = 0; i < ObstacleRayDirections.Length; i++)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(1f);
        }
    }

    private void AddObstacleObservations(VectorSensor sensor)
    {
        foreach (Vector2 direction in ObstacleRayDirections)
        {
            float normalizedDistance = GetObstacleDistance01(direction.normalized, out bool hasObstacle);
            sensor.AddObservation(hasObstacle ? 1f : 0f);
            sensor.AddObservation(normalizedDistance);
        }
    }

    private bool IsObstacleInDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            return false;
        }

        GetObstacleDistance01(direction.normalized, out bool hasObstacle);
        return hasObstacle;
    }

    private bool HasClearShotToPlayer(float distanceToPlayer)
    {
        if (player == null)
        {
            return false;
        }

        if (distanceToPlayer <= 0.001f)
        {
            return true;
        }

        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, distanceToPlayer, obstacleMask);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger || ShouldIgnoreObstacleHit(hit.collider))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private bool CanAttackPlayer(float distanceToPlayer)
    {
        return !requireClearShotForAttack || HasClearShotToPlayer(distanceToPlayer);
    }

    private float GetObstacleDistance01(Vector2 direction, out bool hasObstacle)
    {
        hasObstacle = false;

        if (obstacleRayDistance <= 0f)
        {
            return 1f;
        }

        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, obstacleRayDistance, obstacleMask);
        float closestDistance = obstacleRayDistance;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger || ShouldIgnoreObstacleHit(hit.collider))
            {
                continue;
            }

            hasObstacle = true;
            closestDistance = Mathf.Min(closestDistance, hit.distance);
        }

        return Mathf.Clamp01(closestDistance / obstacleRayDistance);
    }

    private bool ShouldIgnoreObstacleHit(Collider2D hitCollider)
    {
        Transform hitTransform = hitCollider.transform;

        if (hitTransform == transform || hitTransform.IsChildOf(transform))
        {
            return true;
        }

        if (player != null && (hitTransform == player || hitTransform.IsChildOf(player)))
        {
            return true;
        }

        return hitCollider.GetComponent<TrainingHealth>() != null
            || hitCollider.GetComponent<PlayerHealth>() != null
            || hitCollider.GetComponent<EnemyHealthy>() != null
            || hitCollider.GetComponent<Projectile>() != null;
    }

    private int GetClosestMoveAction(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            return 0;
        }

        Vector2 normalizedDirection = direction.normalized;
        int bestAction = 0;
        float bestDot = float.NegativeInfinity;

        for (int action = 1; action <= 8; action++)
        {
            float dot = Vector2.Dot(normalizedDirection, GetMoveDirection(action));
            if (dot > bestDot)
            {
                bestDot = dot;
                bestAction = action;
            }
        }

        return bestAction;
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
        StopMovement();

        if (!UsesTrainingEpisodeReset())
        {
            return;
        }

        EndEpisode();
    }

    private float GetEpisodeTimeLimit()
    {
        return enforceMinimumEpisodeTime ? Mathf.Max(maxEpisodeTime, minimumEpisodeTime) : maxEpisodeTime;
    }

    private bool IsParticipantOutsideArena()
    {
        if (arenaManager == null)
        {
            return false;
        }

        bool enemyOutside = arenaManager.IsOutsideArena(transform.position);
        bool playerOutside = player != null && arenaManager.IsOutsideArena(player.position);
        return enemyOutside || playerOutside;
    }

    private bool UsesTrainingEpisodeReset()
    {
        return arenaManager != null && arenaManager.UsesTrainingEpisodeReset;
    }

    private void OnDestroy()
    {
        StopMovement();

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

    private void ApplyMovement()
    {
        smoothedMoveDirection = Vector2.MoveTowards(
            smoothedMoveDirection,
            targetMoveDirection,
            directionAcceleration * Time.fixedDeltaTime
        );

        if (smoothedMoveDirection.sqrMagnitude < 0.001f)
        {
            StopMovement();
            UpdateAnimatorPlayback(false);
            return;
        }

        Vector2 moveDirection = Vector2.ClampMagnitude(smoothedMoveDirection, 1f);

        if (enemyPathfinding != null)
        {
            enemyPathfinding.MoveTo(moveDirection);
        }
        else if (body != null)
        {
            body.linearVelocity = Vector2.MoveTowards(
                body.linearVelocity,
                moveDirection * moveSpeed,
                acceleration * Time.fixedDeltaTime
            );
        }

        UpdateAnimatorPlayback(true);
    }

    private Vector2 GetInferenceAssistedMoveDirection(Vector2 policyMoveDirection)
    {
        if (!useInferenceAssist || !IsInferenceOnly() || player == null)
        {
            return policyMoveDirection;
        }

        Vector2 toPlayer = player.position - transform.position;
        if (toPlayer.magnitude <= inferenceAssistDistance)
        {
            return policyMoveDirection;
        }

        Vector2 approachDirection = toPlayer.normalized;
        if (policyMoveDirection.sqrMagnitude < 0.001f)
        {
            return approachDirection;
        }

        return Vector2.ClampMagnitude(
            Vector2.Lerp(policyMoveDirection.normalized, approachDirection, inferenceAssistBlend),
            1f
        );
    }

    private bool IsInferenceOnly()
    {
        return behaviorParameters != null && behaviorParameters.BehaviorType == BehaviorType.InferenceOnly;
    }

    private void StopMovement()
    {
        targetMoveDirection = Vector2.zero;
        smoothedMoveDirection = Vector2.zero;

        if (enemyPathfinding != null)
        {
            enemyPathfinding.StopMoving();
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, Vector2.zero, acceleration * Time.fixedDeltaTime);
        }
    }

    private void UpdateAnimatorPlayback()
    {
        bool isMoving = body != null && body.linearVelocity.sqrMagnitude > stillVelocityThreshold * stillVelocityThreshold
            || smoothedMoveDirection.sqrMagnitude > stillVelocityThreshold * stillVelocityThreshold;

        UpdateAnimatorPlayback(isMoving);
    }

    private void UpdateAnimatorPlayback(bool isMoving)
    {
        if (!pauseAnimatorWhenStill || agentAnimator == null)
        {
            return;
        }

        agentAnimator.speed = isMoving ? 1f : 0f;
    }
}
