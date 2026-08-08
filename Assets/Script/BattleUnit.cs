using System.Collections.Generic;
using UnityEngine;

public class BattleUnit
{
    public UnitStats baseStats;
    public int currentHP;
    public int currentMP;
    public bool isPlayerSide;
    public bool isDefending;   // true jika memilih aksi "Defend" turn ini
    public bool isDead => currentHP <= 0;

    // Contoh sederhana untuk buff/debuff sementara (opsional, bisa dikembangkan)
    public List<string> activeStatusEffects = new List<string>();

    public BattleUnit(UnitStats stats, bool isPlayerSide)
    {
        baseStats = stats;
        currentHP = stats.maxHP;
        currentMP = stats.maxMP;
        this.isPlayerSide = isPlayerSide;
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Clamp(currentHP - amount, 0, baseStats.maxHP);
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0, baseStats.maxHP);
    }

    public bool UseMP(int amount)
    {
        if (currentMP < amount) return false;
        currentMP -= amount;
        return true;
    }
}
