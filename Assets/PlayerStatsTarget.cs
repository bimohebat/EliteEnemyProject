using UnityEngine;

public class PlayerStatsTarget : MonoBehaviour
{
    public float physicalDefense = 55f;
    public float magicDefense = 40f;
    public float currentHP = 500f;
    public bool isDead = false;

    public void ReceiveDamageFromEnemy(float rawDamage, bool isMagic)
    {
        float defense = isMagic ? magicDefense : physicalDefense;
        float finalDamage = Mathf.Max(0, rawDamage - defense);

        currentHP -= finalDamage;
        Debug.Log($"{gameObject.name} terkena hit! Kehilangan {finalDamage} HP. Sisa HP: {currentHP}");

        if (currentHP <= 0)
        {
            isDead = true;
            Debug.Log($"{gameObject.name} KO!");
        }
    }
}
