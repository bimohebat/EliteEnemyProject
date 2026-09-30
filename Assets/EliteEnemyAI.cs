using UnityEngine;
using System.Collections.Generic;

public class EliteEnemyAI : MonoBehaviour
{
    private EliteEnemyStats stats;

    private void Awake()
    {
        stats = GetComponent<EliteEnemyStats>();
    }

    // Diperbarui agar menerima parameter list PlayerController asli proyek Anda
    public void ExecuteEnemyTurn(List<PlayerController> activePlayers, bool useMagicAttack, bool isDoubleTurn)
    {
        if (activePlayers == null || activePlayers.Count == 0) return;

        // ATURAN GDD HALAMAN 37: Cari Player dengan defense terendah
        PlayerController targetPlayer = SelectTargetWithLowestDefense(activePlayers, useMagicAttack);

        if (targetPlayer != null)
        {
            // Panggil hitungan damage terintegrasi (Crit + Balancing Turn Ganda)
            float finalOutputDamage = stats.GetCalculatedOutputDamage(useMagicAttack, isDoubleTurn);
            
            Debug.Log($"[AI EXECUTE] {gameObject.name} menyerang {targetPlayer.characterName} dengan total damage keluar: {finalOutputDamage}");
            
            // Mengirim damage ke skrip PlayerController asli tim Anda
            targetPlayer.TakeDamage(finalOutputDamage);
        }
    }

    private PlayerController SelectTargetWithLowestDefense(List<PlayerController> players, bool checkMagicDef)
    {
        PlayerController bestTarget = null;
        float lowestDefenseValue = Mathf.Infinity;

        foreach (PlayerController player in players)
        {
            if (player != null && player.currentHealth > 0)
            {
                // Catatan: Karena skrip PlayerController tim Anda belum memiliki variabel defense fisik/sihir di dalamnya,
                // AI akan mengasumsikan nilai default, atau Anda bisa meminta programmer player menambahkannya nanti.
                float currentDef = checkMagicDef ? 30f : 45f; 

                if (currentDef < lowestDefenseValue)
                {
                    lowestDefenseValue = currentDef;
                    bestTarget = player;
                }
            }
        }

        return bestTarget;
    }
}
