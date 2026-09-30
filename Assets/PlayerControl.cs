using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public string characterName = "MC";
    public float maxHealth = 100f;
    public float currentHealth;
    public float attackDamage = 20f;

    public HealthBar healthBar;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null) healthBar.SetMaxHealth((int)maxHealth);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        
        if (healthBar != null) healthBar.SetHealth((int)currentHealth);

        Debug.Log($"{characterName} menerima {damage} damage! Sisa HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            Debug.Log($"{characterName} Kalah!");
        }
    }

    // Fungsi saat MC Menyerang Boss Eldritch
    public void AttackEnemy(EliteEldritchHunter enemy)
    {
        Debug.Log($"{characterName} menyerang {enemy.enemyName}!");
        enemy.TakeDamage(attackDamage);
    }
}