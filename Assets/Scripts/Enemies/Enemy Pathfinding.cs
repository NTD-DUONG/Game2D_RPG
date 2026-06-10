using UnityEngine;

public class EnemyPathfinding : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    private Rigidbody2D rb;
    private Vector2 moveDir;
    private Knockback knockback;

    public float MoveSpeed
    {
        get { return moveSpeed; }
        set { moveSpeed = value; }
    }

    private void Awake()
    {
        knockback = GetComponent<Knockback>();
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }
    private void FixedUpdate()
    {
        if (rb == null || knockback != null && knockback.GettingKnockedBack) { return; }
        rb.MovePosition(rb.position+moveDir*(moveSpeed*Time.fixedDeltaTime));
    }
    public void MoveTo(Vector2 targetPosition)
    {
        moveDir= targetPosition;
    }
    public void StopMoving()
    {
        moveDir = Vector3.zero;
    }
}
