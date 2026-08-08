using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Berisi semua rumus perhitungan battle: damage, critical hit, dan turn order.
/// Static class supaya bisa dipanggil dari mana saja tanpa perlu instance.
/// </summary>

public static class BattleCalculator
{
    [Header("Tuning values - sesuaikan dengan game balance kamu")]
    private const float DAMAGE_VARIANCE_MIN = 0.9f;
    private const float DAMAGE_VARIANCE_MAX = 1.1f;
    private const float CRIT_MULTIPLIER = 1.5f;
    private const float LUCK_TO_CRIT_CHANCE = 0.01f; // 1 poin luck = 1% crit chance
    private const int DEFEND_DAMAGE_REDUCTION_PERCENT = 50;

    /// <summary>
    /// Hitung damage fisik dasar: ATK - DEF, dengan variasi random dan crit.
    /// </summary>
    public static int CalculateDamage(BattleUnit attacker, BattleUnit defender, out bool isCritical)
    {
        isCritical = IsCritical(attacker);

        int baseDamage = attacker.baseStats.attack - defender.baseStats.defense;
        baseDamage = Mathf.Max(baseDamage, 1); // damage minimal 1, tidak boleh 0/negatif

        float variance = Random.Range(DAMAGE_VARIANCE_MIN, DAMAGE_VARIANCE_MAX);
        float finalDamage = baseDamage * variance;

        if (isCritical)
            finalDamage *= CRIT_MULTIPLIER;

        // Jika target sedang defend, kurangi damage
        if (defender.isDefending)
            finalDamage *= (1f - DEFEND_DAMAGE_REDUCTION_PERCENT / 100f);

        return Mathf.RoundToInt(finalDamage);
    }

    /// <summary>
    /// Damage untuk skill/skill khusus, dengan power multiplier dari skill itu sendiri.
    /// </summary>
    public static int CalculateSkillDamage(BattleUnit attacker, BattleUnit defender, float skillPowerMultiplier, out bool isCritical)
    {
        isCritical = IsCritical(attacker);

        int baseDamage = Mathf.RoundToInt(attacker.baseStats.attack * skillPowerMultiplier) - defender.baseStats.defense;
        baseDamage = Mathf.Max(baseDamage, 1);

        float finalDamage = baseDamage;
        if (isCritical)
            finalDamage *= CRIT_MULTIPLIER;

        if (defender.isDefending)
            finalDamage *= (1f - DEFEND_DAMAGE_REDUCTION_PERCENT / 100f);

        return Mathf.RoundToInt(finalDamage);
    }

    public static bool IsCritical(BattleUnit attacker)
    {
        float critChance = attacker.baseStats.luck * LUCK_TO_CRIT_CHANCE;
        return Random.value < critChance;
    }

    /// <summary>
    /// Urutkan semua unit (player + enemy) berdasarkan speed, dari tercepat ke terlambat.
    /// Dipanggil sekali di awal battle atau tiap "round" jika mau turn order dinamis.
    /// </summary>
    public static List<BattleUnit> GetTurnOrder(List<BattleUnit> allUnits)
    {
        return allUnits
            .Where(u => !u.isDead)
            .OrderByDescending(u => u.baseStats.speed)
            .ThenByDescending(u => Random.value) // tie-breaker acak jika speed sama
            .ToList();
    }
}
