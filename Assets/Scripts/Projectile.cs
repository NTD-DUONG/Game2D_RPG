using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 22f;
    [SerializeField] private GameObject particleOnHitPrefabVFX;
    [SerializeField] private bool isEnemyProjectile = false;
    [SerializeField] private float projectileRange= 10f;
    [Header("Enemy Projectile Visual")]
    [SerializeField] private bool enhanceEnemyProjectileVisual = false;
    [SerializeField] private float enemyProjectileVisualScale = 1f;
    [SerializeField] private Color enemyProjectileColor = new(0f, 0.95f, 1f, 1f);
    [SerializeField] private int enemyProjectileSortingOrder = 80;
    [SerializeField] private bool addEnemyProjectileLine = false;
    [SerializeField] private bool addEnemyProjectileTrail = false;
    [SerializeField] private bool addEnemyProjectileSpriteMarker = false;
    [SerializeField] private float enemyProjectileLineLength = 0.45f;
    [SerializeField] private float enemyProjectileLineWidth = 0.12f;
    [SerializeField] private float enemyProjectileTrailTime = 0.2f;
    [SerializeField] private float enemyProjectileTrailWidth = 0.09f;

    private bool runtimeVisualEnabled;
    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;

        if (runtimeVisualEnabled)
        {
            ApplyEnemyProjectileVisual();
        }
    }

    private void Update()
    {
        MoveProjectile();
        DetectFireDistance();
    }

    public void UpdateProjectileRange(float projectileRange)
    {
        this.projectileRange = projectileRange;
    }

    public void UpdateMoveSpeed(float moveSpeed)
    {
        this.moveSpeed = moveSpeed;
    }

    public void EnableRuntimeEnemyProjectileVisual()
    {
        runtimeVisualEnabled = true;
        ApplyEnemyProjectileVisual();
    }

    public void IgnoreCollisionWith(GameObject owner)
    {
        if (owner == null)
        {
            return;
        }

        Collider2D[] projectileColliders = GetComponentsInChildren<Collider2D>();
        Collider2D[] ownerColliders = owner.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D projectileCollider in projectileColliders)
        {
            foreach (Collider2D ownerCollider in ownerColliders)
            {
                if (projectileCollider != null && ownerCollider != null)
                {
                    Physics2D.IgnoreCollision(projectileCollider, ownerCollider, true);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GetComponent<TrainingProjectileDamage>() != null)
        {
            return;
        }

        if (other.isTrigger)
        {
            return;
        }

        EnemyHealthy enemyHealth = other.gameObject.GetComponent<EnemyHealthy>();
        TrainingHealth trainingHealth = other.GetComponentInParent<TrainingHealth>();
        PlayerHealth player = other.gameObject.GetComponent<PlayerHealth>();

        if (isEnemyProjectile)
        {
            if (trainingHealth != null)
            {
                trainingHealth.TakeDamage(1, gameObject);
                SpawnHitVfx();
                Destroy(gameObject);
                return;
            }

            if (player == null)
            {
                return;
            }

            player.TakeDamage(1, transform);
            SpawnHitVfx();
            Destroy(gameObject);
            return;
        }

        if (enemyHealth != null)
        {
            SpawnHitVfx();
            Destroy(gameObject);
        }
    }
    private void DetectFireDistance()
    {
        if(Vector3.Distance(transform.position, startPosition) > projectileRange)
        {
            Destroy(gameObject);
        }
    }

    private void MoveProjectile()
    {
        transform.Translate(Vector3.right * Time.deltaTime * moveSpeed);
    }

    public void SpawnHitVfx()
    {
        if (particleOnHitPrefabVFX != null)
        {
            Instantiate(particleOnHitPrefabVFX, transform.position, transform.rotation);
        }
    }

    private void ApplyEnemyProjectileVisual()
    {
        if (!runtimeVisualEnabled || !isEnemyProjectile || !enhanceEnemyProjectileVisual)
        {
            return;
        }

        transform.localScale = Vector3.one * enemyProjectileVisualScale;

        if (TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = enemyProjectileColor;
            spriteRenderer.sortingOrder = enemyProjectileSortingOrder;
        }

        if (addEnemyProjectileSpriteMarker && transform.Find("EnemyProjectileVisual") == null)
        {
            GameObject visualMarker = new("EnemyProjectileVisual");
            visualMarker.transform.SetParent(transform, false);
            visualMarker.transform.localPosition = Vector3.zero;
            visualMarker.transform.localRotation = Quaternion.identity;
            visualMarker.transform.localScale = Vector3.one * 1.1f;

            SpriteRenderer markerRenderer = visualMarker.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            markerRenderer.color = enemyProjectileColor;
            markerRenderer.sortingOrder = enemyProjectileSortingOrder + 2;
        }

        Material projectileMaterial = CreateProjectileMaterial();

        if (addEnemyProjectileLine && GetComponent<LineRenderer>() == null)
        {
            LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, Vector3.left * enemyProjectileLineLength * 0.5f);
            lineRenderer.SetPosition(1, Vector3.right * enemyProjectileLineLength * 0.5f);
            lineRenderer.startWidth = enemyProjectileLineWidth;
            lineRenderer.endWidth = enemyProjectileLineWidth;
            lineRenderer.numCapVertices = 6;
            lineRenderer.sortingOrder = enemyProjectileSortingOrder + 1;
            if (projectileMaterial != null)
            {
                lineRenderer.material = projectileMaterial;
            }
            lineRenderer.startColor = enemyProjectileColor;
            lineRenderer.endColor = enemyProjectileColor;
        }

        if (addEnemyProjectileTrail && GetComponent<TrailRenderer>() == null)
        {
            TrailRenderer trailRenderer = gameObject.AddComponent<TrailRenderer>();
            trailRenderer.time = enemyProjectileTrailTime;
            trailRenderer.startWidth = enemyProjectileTrailWidth;
            trailRenderer.endWidth = 0f;
            trailRenderer.numCapVertices = 6;
            trailRenderer.sortingOrder = enemyProjectileSortingOrder;
            if (projectileMaterial != null)
            {
                trailRenderer.material = projectileMaterial;
            }
            trailRenderer.startColor = enemyProjectileColor;
            trailRenderer.endColor = new Color(enemyProjectileColor.r, enemyProjectileColor.g, enemyProjectileColor.b, 0f);
        }
    }

    private static Material CreateProjectileMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        return shader != null ? new Material(shader) : null;
    }

}
