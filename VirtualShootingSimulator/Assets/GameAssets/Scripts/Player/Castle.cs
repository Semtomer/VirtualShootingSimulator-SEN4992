using UnityEngine;

public class Castle : MonoBehaviour
{
    [Header("Castle Stats")]
    [Tooltip("The maximum and starting health of the castle.")]
    [SerializeField] private int maxHealth = 500;
    private int currentHealth;

    [Header("UI")]
    [Tooltip("Reference to the HealthBar UI component for this castle.")]
    [SerializeField] private HealthBar healthBar;

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (healthBar == null)
        {
            Debug.LogWarning($"HealthBar not assigned for castle: {gameObject.name}. UI will not update.", this);
        }
        else
        {
            healthBar.SetMaxHealth(maxHealth);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0 || GameManager.Instance.IsGameOver())
            return;

        int damageTaken = Mathf.RoundToInt(damageAmount);
        currentHealth -= damageTaken;
        currentHealth = Mathf.Max(currentHealth, 0);

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCastleDestroyed);
        }
        else
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCastleHit);
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has been destroyed!");

        Destroy(gameObject);
    }
}