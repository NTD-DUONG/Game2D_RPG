using System;
using UnityEngine;

public class TrainingHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;

    public event Action<TrainingHealth, int, GameObject> Damaged;
    public event Action<TrainingHealth> Died;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;
    public float Health01 => maxHealth <= 0 ? 0f : Mathf.Clamp01((float)CurrentHealth / maxHealth);

    private void Awake()
    {
        ResetHealth();
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount, GameObject source)
    {
        if (IsDead)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - damageAmount);
        Damaged?.Invoke(this, damageAmount, source);

        if (IsDead)
        {
            Died?.Invoke(this);
        }
    }
}
