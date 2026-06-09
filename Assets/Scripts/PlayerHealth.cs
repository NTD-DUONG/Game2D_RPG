using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;


public class PlayerHealth : MonoBehaviour
{
    public bool isDead {  get; private set; }
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float knockBackThrustAmount = 10f;
    [SerializeField] private float damageRecoveryTime = 1f;

    private Slider healthSlider;
    private int currentHealth;
    private bool canTakeDamage = true;
    private Knockback knockback;
    private Flash flash;

    const string HEALTH_SLIDER_TEXT = "Health Slider";
    const string TOWN_TEXT = "Scene1";
    readonly int DEATH_HASH = Animator.StringToHash("Death");


    private void Awake()
    {
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
    }

    private void Start()
    {
        isDead = false;
        currentHealth = maxHealth;
        UpdateHealthSlider();
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        EnemyAI enemy = other.gameObject.GetComponent<EnemyAI>();

        if (enemy != null && enemy.enabled)
        {
            TakeDamage(1, other.transform);
        }
    }
    public void HealPlayer()
    {
        if (currentHealth < maxHealth)
        {
            currentHealth += 1;
            UpdateHealthSlider();
        }
        }

    public void TakeDamage(int damageAmount, Transform hitTransform) 
    {
        if (!canTakeDamage) {return; }

        if (knockback != null)
        {
            knockback.GetKnockedBack(hitTransform, knockBackThrustAmount);
        }

        if (flash != null)
        {
            StartCoroutine(flash.FlashRoutine());
        }

        canTakeDamage = false;
        currentHealth -= damageAmount;
        StartCoroutine(DamageRecoveryRoutine());
        UpdateHealthSlider();
        CheckIfPlayerDeath();
    }

    private void CheckIfPlayerDeath()
    {
        if (currentHealth <= 0 && !isDead)
        {
            isDead=true;
            //Destroy(ActiveWeapon.Instance.gameObject);
            currentHealth = 0;
            GetComponent<Animator>().SetTrigger(DEATH_HASH);
            StartCoroutine(DeathLoadSceneRoutine());
        }
    }
    private IEnumerator DeathLoadSceneRoutine()
    {
        yield return new WaitForSeconds(damageRecoveryTime);
        canTakeDamage= true;
    }

    private IEnumerator DamageRecoveryRoutine()
    {
        yield return new WaitForSeconds(damageRecoveryTime);
        canTakeDamage = true;
    }

    private void UpdateHealthSlider()
    {
        if (healthSlider == null)
        {
            GameObject sliderObject = GameObject.Find(HEALTH_SLIDER_TEXT);
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
        healthSlider.value = currentHealth;
    }

}
