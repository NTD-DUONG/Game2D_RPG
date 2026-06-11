using System;
using UnityEngine;
using UnityEngine.UI;

public class TrainingHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private bool updateHealthSlider;
    [SerializeField] private string healthSliderName = "Health Slider";
    [SerializeField] private bool playHitFlash = true;
    [SerializeField] private bool playKnockback = true;
    [SerializeField] private float knockbackThrustAmount = 10f;
    [SerializeField] private bool playDeathAnimation = true;
    [SerializeField] private bool disableControlsOnDeath;

    public event Action<TrainingHealth, int, GameObject> Damaged;
    public event Action<TrainingHealth> Died;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;
    public float Health01 => maxHealth <= 0 ? 0f : Mathf.Clamp01((float)CurrentHealth / maxHealth);

    private readonly int deathHash = Animator.StringToHash("Death");
    private readonly int idleHash = Animator.StringToHash("Idie");
    private readonly int moveXHash = Animator.StringToHash("MoveX");
    private readonly int moveYHash = Animator.StringToHash("MoveY");
    private Slider healthSlider;
    private Flash flash;
    private Knockback knockback;
    private Animator animator;
    private Rigidbody2D body;

    private void Awake()
    {
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        ResetHealth();
    }

    public void ResetHealth()
    {
        StopAllCoroutines();
        CurrentHealth = maxHealth;
        lastDamageTime = -1f;
        ResetFeedbackState();
        SetControlsEnabled(true);
        UpdateHealthSlider();
    }

    private float lastDamageTime = -1f;

    public void TakeDamage(int damageAmount, GameObject source)
    {
        if (IsDead || Time.time - lastDamageTime < 0.2f)
        {
            return;
        }

        lastDamageTime = Time.time;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damageAmount);
        PlayHitFeedback(source);
        Damaged?.Invoke(this, damageAmount, source);
        UpdateHealthSlider();

        if (IsDead)
        {
            PlayDeathFeedback();
            Died?.Invoke(this);
        }
    }

    private void PlayHitFeedback(GameObject source)
    {
        if (playHitFlash && flash != null)
        {
            StartCoroutine(flash.FlashRoutine());
        }

        if (playKnockback && knockback != null && source != null)
        {
            knockback.GetKnockedBack(source.transform, knockbackThrustAmount);
        }
    }

    private void PlayDeathFeedback()
    {
        if (playDeathAnimation && animator != null)
        {
            animator.SetTrigger(deathHash);
        }

        if (disableControlsOnDeath)
        {
            SetControlsEnabled(false);
        }
    }

    private void ResetFeedbackState()
    {
        if (flash != null)
        {
            flash.RestoreDefaultMaterial();
        }

        if (knockback != null)
        {
            knockback.CancelKnockback();
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        if (animator != null)
        {
            animator.ResetTrigger(deathHash);
            animator.SetFloat(moveXHash, 0f);
            animator.SetFloat(moveYHash, 0f);
            animator.Play(idleHash, 0, 0f);
            animator.Update(0f);
        }
    }

    private void SetControlsEnabled(bool isEnabled)
    {
        if (!disableControlsOnDeath)
        {
            return;
        }

        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = isEnabled;
        }

        TrainingPlayerBot trainingPlayerBot = GetComponent<TrainingPlayerBot>();
        if (trainingPlayerBot != null)
        {
            trainingPlayerBot.enabled = isEnabled;
        }

        foreach (Sword sword in GetComponentsInChildren<Sword>(true))
        {
            sword.enabled = isEnabled;
        }

        foreach (DamageSource damageSource in GetComponentsInChildren<DamageSource>(true))
        {
            damageSource.enabled = isEnabled;
        }

        if (!isEnabled && body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }

    private void UpdateHealthSlider()
    {
        if (!updateHealthSlider)
        {
            return;
        }

        if (healthSlider == null)
        {
            GameObject sliderObject = GameObject.Find(healthSliderName);
            if (sliderObject == null)
            {
                return;
            }

            healthSlider = sliderObject.GetComponent<Slider>();
            if (healthSlider == null)
            {
                return;
            }
        }

        healthSlider.maxValue = maxHealth;
        healthSlider.value = CurrentHealth;
    }
}
