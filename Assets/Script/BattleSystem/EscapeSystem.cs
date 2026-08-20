using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Menangani logika mencoba kabur dari battle.
/// Aturan:
/// - Battle melawan Boss (ada minimal 1 unit Boss di antara musuh) -> escape SELALU diblokir.
/// - Battle melawan Elite (tanpa Boss) -> chance 40%-50%.
/// - Battle melawan Common saja -> chance 70%-80%.
/// - Jika battle campuran Common + Elite (tanpa Boss) -> dipakai chance Elite (lebih ketat),
///   karena musuh terkuat yang menentukan sulit-tidaknya kabur.
/// Subtask: PB-CM-06 - Battle Escape Technical
/// </summary>
public static class EscapeSystem
{
    // Range chance sesuai spesifikasi
    private const float COMMON_ESCAPE_MIN = 0.70f;
    private const float COMMON_ESCAPE_MAX = 0.80f;

    private const float ELITE_ESCAPE_MIN = 0.40f;
    private const float ELITE_ESCAPE_MAX = 0.50f;

    public enum EscapeResult
    {
        Success,
        Failed,
        Blocked // battle melawan Boss, escape tidak tersedia
    }

    /// <summary>
    /// Cek apakah battle ini mengizinkan escape sama sekali.
    /// False jika ada unit Boss di antara musuh yang masih hidup.
    /// </summary>
    public static bool CanAttemptEscape(List<BattleUnit> aliveEnemies)
    {
        return aliveEnemies.All(e => e.baseStats.enemyType != EnemyType.Boss);
    }

    /// <summary>
    /// Ambil peluang escape saat ini berdasarkan tipe musuh terkuat yang masih hidup.
    /// Berguna juga untuk ditampilkan di UI (misal "Escape Chance: 75%") sebelum player menekan tombol.
    /// </summary>
    public static float GetEscapeChance(List<BattleUnit> aliveEnemies)
    {
        if (!CanAttemptEscape(aliveEnemies))
            return 0f;

        bool hasElite = aliveEnemies.Any(e => e.baseStats.enemyType == EnemyType.Elite);

        return hasElite
            ? Random.Range(ELITE_ESCAPE_MIN, ELITE_ESCAPE_MAX)
            : Random.Range(COMMON_ESCAPE_MIN, COMMON_ESCAPE_MAX);
    }

    /// <summary>
    /// Coba lakukan escape berdasarkan chance yang SUDAH dihitung sebelumnya
    /// (misal lewat GetEscapeChance, yang juga dipakai untuk ditampilkan di UI).
    /// Dipisah dari GetEscapeChance supaya nilai yang ditampilkan ke player
    /// dan nilai yang benar-benar dipakai untuk roll selalu identik.
    /// </summary>
    public static EscapeResult RollEscape(List<BattleUnit> aliveEnemies, float escapeChance)
    {
        if (!CanAttemptEscape(aliveEnemies))
            return EscapeResult.Blocked;

        bool success = Random.value < escapeChance;
        return success ? EscapeResult.Success : EscapeResult.Failed;
    }

    /// <summary>
    /// Versi praktis: hitung chance baru lalu langsung roll dalam satu panggilan.
    /// Gunakan RollEscape() + GetEscapeChance() terpisah jika chance-nya perlu
    /// ditampilkan ke UI dulu sebelum player menekan tombol escape.
    /// </summary>
    public static EscapeResult TryEscape(BattleUnit player, List<BattleUnit> aliveEnemies)
    {
        if (!CanAttemptEscape(aliveEnemies))
            return EscapeResult.Blocked;

        float escapeChance = GetEscapeChance(aliveEnemies);
        return RollEscape(aliveEnemies, escapeChance);
    }
}