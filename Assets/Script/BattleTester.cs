using System.Collections.Generic;
using UnityEngine;

namespace DaengSaka.BattleSystem
{
    public class BattleTester : MonoBehaviour
    {
        [Header("Units for Testing")]
        public CharacterUnit playerUnit;
        public CharacterUnit enemyUnit;

        [ContextMenu("Test Calculate Damage")]
        public void TestAttack()
        {
            if (playerUnit == null || enemyUnit == null)
            {
                Debug.LogWarning("Harap masukkan Player Unit dan Enemy Unit di Inspector!");
                return;
            }

            Debug.Log("--- MEMULAI KALKULASI SERANGAN ---");
            
            // 1. Hitung Damage dari Player ke Enemy
            DamageResult result = BattleCalculation.CalculateDamage(playerUnit, enemyUnit);

            Debug.Log($"[SERANGAN] {playerUnit.UnitName} -> {enemyUnit.UnitName}");
            Debug.Log($"Crit Roll: {(result.IsCritical ? "CRITICAL HIT!" : "Normal Hit")}");
            Debug.Log($"Raw Damage: {result.RawDamage:F2} | Final Damage (RNG): {result.FinalDamage}");

            // 2. Terapkan damage ke target
            enemyUnit.TakeDamage(result.FinalDamage);
        }

        [ContextMenu("Test Calculate Turn Order")]
        public void TestTurnOrder()
        {
            if (playerUnit == null || enemyUnit == null) return;

            List<CharacterUnit> units = new List<CharacterUnit> { playerUnit, enemyUnit };
            List<CharacterUnit> sortedUnits = BattleCalculation.CalculateTurnOrder(units);

            Debug.Log("--- URUTAN GILIRAN BERTINDAK ---");
            for (int i = 0; i < sortedUnits.Count; i++)
            {
                Debug.Log($"Urutan {i + 1}: {sortedUnits[i].UnitName} (Speed: {sortedUnits[i].Speed})");
            }
        }
    }
}