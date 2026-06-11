using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Flash : MonoBehaviour
{
    [SerializeField] private Material whiteFlashMat;
    [SerializeField] private float restoreDefaultMatTime = .2f;

    private Material defaultMat;
    private SpriteRenderer spriteRenderer;
    //private EnemyHealthy enemyHealth;

    private void Awake()
    {
        //enemyHealth = GetComponent<EnemyHealthy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            defaultMat = spriteRenderer.material;
        }
    }
    public float GetRestoreMatTime()
    {
        return restoreDefaultMatTime;
    }

    public IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null)
        {
            yield break;
        }

        spriteRenderer.material = whiteFlashMat;
        yield return new WaitForSeconds(restoreDefaultMatTime);
        spriteRenderer.material = defaultMat;
        //enemyHealth.DetectDeath();
    }

    public void RestoreDefaultMaterial()
    {
        if (spriteRenderer != null && defaultMat != null)
        {
            spriteRenderer.material = defaultMat;
        }
    }

}
