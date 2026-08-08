using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EscapeSystem
{
    private const float BASE_ESCAPE_BONUS = 50f; // offset dasar supaya escape tidak terlalu sulit
    private const float MIN_ESCAPE_CHANCE = 0.1f; // selalu ada minimal 10% peluang
    private const float MAX_ESCAPE_CHANCE = 0.95f; // tidak pernah 100% pasti berhasil

    public enum EscapeResult
    {
        Success,
        Failed,
        Blocked // battle yang memang tidak bisa di-escape (misal boss)
    }

    /// <summary>
    /// Cek apakah battle ini mengizinkan escape sama sekali.
    /// </summary>
    public static bool CanAttemptEscape(List<BattleUnit> enemies)
    {
        return enemies.All(e => e.baseStats.canBeEscapedFrom);
    }

    /// <summary>
    /// Coba lakukan escape. Mengembalikan hasilnya.
    /// </summary>
    public static EscapeResult TryEscape(BattleUnit player, List<BattleUnit> aliveEnemies)
    {
        if (!CanAttemptEscape(aliveEnemies))
            return EscapeResult.Blocked;

        float avgEnemyAgility = (float)aliveEnemies.Average(e => e.baseStats.agility);

        float escapeChance = (player.baseStats.agility - avgEnemyAgility + BASE_ESCAPE_BONUS) / 100f;
        escapeChance = Mathf.Clamp(escapeChance, MIN_ESCAPE_CHANCE, MAX_ESCAPE_CHANCE);

        bool success = Random.value < escapeChance;
        return success ? EscapeResult.Success : EscapeResult.Failed;
    }
}
