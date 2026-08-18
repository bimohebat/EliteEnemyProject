using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representasi unit SAAT battle berlangsung (runtime state).
/// UnitStats tidak boleh dimodifikasi langsung karena itu asset yang dishare,
/// jadi semua nilai yang berubah selama battle (HP, MP, buff/debuff) disimpan di sini.
/// Subtask terkait: PB-CM-06 - Battle Stats Technical
/// </summary>
public class BattleUnit
{
    public UnitStats baseStats;
    public int currentHP;
    public int currentMP;
    public bool isPlayerSide;
    public bool isDefending;   // true jika memilih aksi "Defend" turn ini
    public bool isDead => currentHP <= 0;

    // --- Untuk Charged Attack ---
    // true jika unit ini sedang "mengisi" charged attack, akan meledak otomatis
    // di giliran unit ini berikutnya (lihat BattleManager.NextTurn & ResolveChargedAttack).
    public bool isCharging;
    public BattleUnit chargeTarget;

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