using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DaengSaka.BattleSystem
{
    /// <summary>
    /// Interface dasar untuk unit yang terlibat dalam kalkulasi pertarungan.
    /// Unit Karakter / Musuh dapat mengimplementasikan interface ini.
    /// </summary>
    public interface IBattleUnit
    {
        string UnitName { get; }
        float Attack { get; }
        float Defense { get; }
        float Speed { get; }
        float CriticalRate { get; }     // Nilai dalam % (0 - 100)
        float CriticalDamage { get; }   // Multiplier (misal 1.5f untuk +50% damage)
    }

    /// <summary>
    /// Struktur data hasil kalkulasi damage untuk dikembalikan ke pemanggil.
    /// </summary>
    public struct DamageResult
    {
        public int FinalDamage;
        public bool IsCritical;
        public float RawDamage;
    }

    /// <summary>
    /// Subtask: PB-CM-06 - Battle Calculation
    /// Kelas statis murni untuk menangani kalkulasi matematika pertarungan.
    /// </summary>
    public static class BattleCalculation
    {
        // Variasi damage acak (95% hingga 105%)
        private const float MIN_VARIANCE = 0.95f;
        private const float MAX_VARIANCE = 1.05f;

        #region Damage & Critical Calculations

        /// <summary>
        /// Menghitung total damage yang diberikan penyerang kepada target.
        /// </summary>
        public static DamageResult CalculateDamage(IBattleUnit attacker, IBattleUnit defender, float skillMultiplier = 1.0f)
        {
            // 1. Pengecekan Critical Hit
            bool isCrit = RollCriticalHit(attacker.CriticalRate);
            float critMult = isCrit ? attacker.CriticalDamage : 1.0f;

            // 2. Kalkulasi Damage Dasar dengan pengurangan Defense
            float safeDefense = Mathf.Max(0f, defender.Defense);
            float defFactor = 100f / (100f + safeDefense);
            float rawDamage = attacker.Attack * defFactor * skillMultiplier;

            // 3. Terapkan Multiplier Critical
            float damageWithCrit = rawDamage * critMult;

            // 4. Tambahkan Variasi Damage Acak (RNG)
            float randomFactor = Random.Range(MIN_VARIANCE, MAX_VARIANCE);
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damageWithCrit * randomFactor));

            return new DamageResult
            {
                FinalDamage = finalDamage,
                IsCritical = isCrit,
                RawDamage = rawDamage
            };
        }

        /// <summary>
        /// Menentukan apakah serangan menghasilkan Critical Hit berdasarkan persentase Crit Rate.
        /// </summary>
        public static bool RollCriticalHit(float critRate)
        {
            float roll = Random.Range(0f, 100f);
            return roll < critRate;
        }

        #endregion

        #region Turn Order Calculation

        /// <summary>
        /// Mengurutkan daftar giliran bertindak unit dari Speed tertinggi ke terkecil.
        /// </summary>
        public static List<T> CalculateTurnOrder<T>(List<T> units) where T : IBattleUnit
        {
            if (units == null || units.Count == 0)
                return new List<T>();

            // Mengurutkan berdasarkan Speed (Descending). 
            // ThenBy dengan Random.value digunakan sebagai tie-breaker jika Speed sama.
            return units
                .OrderByDescending(unit => unit.Speed)
                .ThenBy(_ => Random.value)
                .ToList();
        }

        #endregion
    }
}