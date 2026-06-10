using UnityEngine;

public class TrainingArenaManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform enemy;
    [SerializeField] private TrainingHealth playerHealth;
    [SerializeField] private TrainingHealth enemyHealth;
    [SerializeField] private Rigidbody2D playerRigidbody;
    [SerializeField] private Rigidbody2D enemyRigidbody;
    [SerializeField] private Vector2 arenaSize = new(8f, 5f);
    [SerializeField] private bool randomizeSpawn = true;
    [SerializeField] private float minimumSpawnDistance = 2f;
    [Header("Training Player Setup")]
    [SerializeField] private bool disablePlayerInputOnPlay = true;
    [SerializeField] private bool disablePlayerWeaponOnPlay = true;
    [SerializeField] private bool disablePlayerBotOnPlay = true;
    [SerializeField] private bool forcePlayerIdleAnimation = true;

    private readonly int moveXHash = Animator.StringToHash("MoveX");
    private readonly int moveYHash = Animator.StringToHash("MoveY");
    private readonly int playerIdleHash = Animator.StringToHash("Idie");

    private void Awake()
    {
        ConfigureBody(playerRigidbody);
        ConfigureBody(enemyRigidbody);
        ApplyTrainingPlayerSetup();
    }

    private void Start()
    {
        ApplyTrainingPlayerSetup();
    }

    public void ResetArena()
    {
        TrainingProjectileDamage.ClearAll();
        ApplyTrainingPlayerSetup();

        playerHealth?.ResetHealth();
        enemyHealth?.ResetHealth();

        if (player == null || enemy == null)
        {
            return;
        }

        Vector2 playerPosition;
        Vector2 enemyPosition;
        GetSpawnPositions(out playerPosition, out enemyPosition);

        player.position = playerPosition;
        enemy.position = enemyPosition;

        StopBody(playerRigidbody);
        StopBody(enemyRigidbody);
    }

    private void GetSpawnPositions(out Vector2 playerPosition, out Vector2 enemyPosition)
    {
        if (!randomizeSpawn)
        {
            playerPosition = transform.position + Vector3.left * 2f;
            enemyPosition = transform.position + Vector3.right * 2f;
            return;
        }

        playerPosition = GetRandomPoint();
        enemyPosition = GetRandomPoint();

        int safety = 0;
        while (Vector2.Distance(playerPosition, enemyPosition) < minimumSpawnDistance && safety < 30)
        {
            enemyPosition = GetRandomPoint();
            safety++;
        }
    }

    private Vector2 GetRandomPoint()
    {
        Vector2 center = transform.position;
        return new Vector2(
            center.x + Random.Range(-arenaSize.x * 0.5f, arenaSize.x * 0.5f),
            center.y + Random.Range(-arenaSize.y * 0.5f, arenaSize.y * 0.5f)
        );
    }

    private static void StopBody(Rigidbody2D body)
    {
        if (body == null)
        {
            return;
        }

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    private static void ConfigureBody(Rigidbody2D body)
    {
        if (body == null)
        {
            return;
        }

        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void ApplyTrainingPlayerSetup()
    {
        if (player == null)
        {
            return;
        }

        if (disablePlayerInputOnPlay)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            if (forcePlayerIdleAnimation)
            {
                ForcePlayerIdleAnimation();
            }
        }

        if (disablePlayerBotOnPlay)
        {
            TrainingPlayerBot playerBot = player.GetComponent<TrainingPlayerBot>();
            if (playerBot != null)
            {
                playerBot.enabled = false;
            }
        }

        if (disablePlayerWeaponOnPlay)
        {
            DisablePlayerWeapon();
        }
    }

    private void DisablePlayerWeapon()
    {
        foreach (Sword sword in player.GetComponentsInChildren<Sword>(true))
        {
            sword.enabled = false;
        }

        foreach (DamageSource damageSource in player.GetComponentsInChildren<DamageSource>(true))
        {
            damageSource.enabled = false;

            Collider2D weaponCollider = damageSource.GetComponent<Collider2D>();
            if (weaponCollider != null)
            {
                weaponCollider.enabled = false;
            }
        }

        foreach (TrainingHitbox trainingHitbox in player.GetComponentsInChildren<TrainingHitbox>(true))
        {
            trainingHitbox.enabled = false;

            Collider2D hitboxCollider = trainingHitbox.GetComponent<Collider2D>();
            if (hitboxCollider != null)
            {
                hitboxCollider.enabled = false;
            }
        }
    }

    private void ForcePlayerIdleAnimation()
    {
        Animator playerAnimator = player.GetComponent<Animator>();

        if (playerAnimator == null)
        {
            return;
        }

        playerAnimator.SetFloat(moveXHash, 0f);
        playerAnimator.SetFloat(moveYHash, 0f);
        playerAnimator.Play(playerIdleHash, 0, 0f);
        playerAnimator.Update(0f);
    }

}
