using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// "Wasit" utama yang mengatur seluruh alur battle turn-based:
/// turn order -> player action -> resolve -> enemy action -> resolve -> cek menang/kalah.
/// Menggabungkan: UnitStats, BattleUnit, BattleCalculator, EscapeSystem.
/// </summary>

public class BattleManager : MonoBehaviour
{
    [Header("Setup Battle (isi lewat Inspector)")]
    public List<UnitStats> playerStatsList;
    public List<UnitStats> enemyStatsList;

    [Header("Runtime State (read-only, untuk debug)")]
    public BattleState currentState;
    public List<BattleUnit> playerUnits = new List<BattleUnit>();
    public List<BattleUnit> enemyUnits = new List<BattleUnit>();
    private List<BattleUnit> turnQueue = new List<BattleUnit>();
    private BattleUnit activeUnit;

    void Start()
    {
        SetupBattle();
    }

    // ---------------------------------------------------------
    // SETUP
    // ---------------------------------------------------------
    void SetupBattle()
    {
        playerUnits = playerStatsList.Select(s => new BattleUnit(s, true)).ToList();
        enemyUnits = enemyStatsList.Select(s => new BattleUnit(s, false)).ToList();

        ChangeState(BattleState.Start);
    }

    void ChangeState(BattleState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case BattleState.Start:
                StartCoroutine(StartBattleRoutine());
                break;

            case BattleState.PlayerTurn:
                Debug.Log($"[Battle] Giliran {activeUnit.baseStats.unitName} (Player). Silakan pilih aksi.");
                // Di sini UI kamu harus menampilkan tombol Attack/Skill/Item/Defend/Escape
                // dan memanggil salah satu method Player...() di bawah saat ditekan.
                break;

            case BattleState.EnemyTurn:
                StartCoroutine(EnemyTurnRoutine());
                break;

            case BattleState.BattleWon:
                Debug.Log("[Battle] Menang!");
                break;

            case BattleState.BattleLost:
                Debug.Log("[Battle] Kalah...");
                break;

            case BattleState.BattleEscaped:
                Debug.Log("[Battle] Berhasil kabur dari battle.");
                break;
        }
    }

    IEnumerator StartBattleRoutine()
    {
        Debug.Log("[Battle] Battle dimulai!");
        yield return new WaitForSeconds(1f); // jeda untuk animasi intro battle, opsional

        BuildTurnQueue();
        NextTurn();
    }

    // ---------------------------------------------------------
    // TURN ORDER (pakai BattleCalculator)
    // ---------------------------------------------------------
    void BuildTurnQueue()
    {
        List<BattleUnit> allUnits = new List<BattleUnit>();
        allUnits.AddRange(playerUnits);
        allUnits.AddRange(enemyUnits);

        turnQueue = BattleCalculator.GetTurnOrder(allUnits);
    }

    void NextTurn()
    {
        // Cek kondisi menang/kalah dulu sebelum lanjut turn berikutnya
        if (CheckBattleEnd()) return;

        if (turnQueue.Count == 0)
        {
            BuildTurnQueue(); // round baru, susun ulang urutan giliran
        }

        activeUnit = turnQueue[0];
        turnQueue.RemoveAt(0);

        if (activeUnit.isDead)
        {
            NextTurn(); // skip unit yang sudah mati
            return;
        }

        activeUnit.isDefending = false; // reset status defend di awal giliran unit ini

        ChangeState(activeUnit.isPlayerSide ? BattleState.PlayerTurn : BattleState.EnemyTurn);
    }

    // ---------------------------------------------------------
    // PLAYER ACTIONS — panggil method ini dari tombol UI
    // ---------------------------------------------------------
    public void PlayerAttack(BattleUnit target)
    {
        if (currentState != BattleState.PlayerTurn) return;

        int damage = BattleCalculator.CalculateDamage(activeUnit, target, out bool isCrit);
        target.TakeDamage(damage);

        Debug.Log($"[Battle] {activeUnit.baseStats.unitName} menyerang {target.baseStats.unitName} " +
                   $"sebesar {damage} damage{(isCrit ? " (CRITICAL!)" : "")}. HP tersisa: {target.currentHP}");

        EndPlayerAction();
    }

    public void PlayerUseSkill(BattleUnit target, float skillPower, int mpCost)
    {
        if (currentState != BattleState.PlayerTurn) return;

        if (!activeUnit.UseMP(mpCost))
        {
            Debug.Log("[Battle] MP tidak cukup!");
            return;
        }

        int damage = BattleCalculator.CalculateSkillDamage(activeUnit, target, skillPower, out bool isCrit);
        target.TakeDamage(damage);

        Debug.Log($"[Battle] {activeUnit.baseStats.unitName} pakai skill ke {target.baseStats.unitName}: " +
                   $"{damage} damage{(isCrit ? " (CRITICAL!)" : "")}.");

        EndPlayerAction();
    }

    public void PlayerDefend()
    {
        if (currentState != BattleState.PlayerTurn) return;

        activeUnit.isDefending = true;
        Debug.Log($"[Battle] {activeUnit.baseStats.unitName} bertahan.");

        EndPlayerAction();
    }

    public void PlayerUseItem(System.Action<BattleUnit> itemEffect, BattleUnit target)
    {
        if (currentState != BattleState.PlayerTurn) return;

        itemEffect?.Invoke(target); // contoh: (u) => u.Heal(30)
        Debug.Log($"[Battle] {activeUnit.baseStats.unitName} menggunakan item ke {target.baseStats.unitName}.");

        EndPlayerAction();
    }

    public void PlayerEscape()
    {
        if (currentState != BattleState.PlayerTurn) return;
 
        var aliveEnemies = enemyUnits.Where(e => !e.isDead).ToList();
 
        // Hitung chance sekali saja, supaya nilai yang di-log/ditampilkan ke UI
        // sama persis dengan nilai yang dipakai untuk roll di bawah.
        float chance = EscapeSystem.GetEscapeChance(aliveEnemies);
        Debug.Log($"[Battle] Mencoba escape... (chance: {chance:P0})");
 
        EscapeSystem.EscapeResult result = EscapeSystem.RollEscape(aliveEnemies, chance);
 
        switch (result)
        {
            case EscapeSystem.EscapeResult.Success:
                ChangeState(BattleState.BattleEscaped);
                break;
 
            case EscapeSystem.EscapeResult.Failed:
                Debug.Log("[Battle] Gagal kabur! Giliran hangus.");
                EndPlayerAction();
                break;
 
            case EscapeSystem.EscapeResult.Blocked:
                Debug.Log("[Battle] Battle ini tidak bisa di-escape!");
                break; // tidak menghabiskan giliran, biarkan player pilih aksi lain
        }
    }
    
    void EndPlayerAction()
    {
        if (CheckBattleEnd()) return;
        NextTurn();
    }

    // ---------------------------------------------------------
    // ENEMY TURN — AI sederhana, bisa dikembangkan sesuai kebutuhan PB-AI subtasks
    // ---------------------------------------------------------
    IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(0.75f); // jeda supaya terasa natural

        var aliveTargets = playerUnits.Where(p => !p.isDead).ToList();
        if (aliveTargets.Count > 0)
        {
            BattleUnit target = aliveTargets[Random.Range(0, aliveTargets.Count)];
            int damage = BattleCalculator.CalculateDamage(activeUnit, target, out bool isCrit);
            target.TakeDamage(damage);

            Debug.Log($"[Battle] {activeUnit.baseStats.unitName} (Enemy) menyerang {target.baseStats.unitName}: " +
                       $"{damage} damage{(isCrit ? " (CRITICAL!)" : "")}.");
        }

        if (!CheckBattleEnd())
            NextTurn();
    }

    // ---------------------------------------------------------
    // WIN / LOSE CHECK
    // ---------------------------------------------------------
    bool CheckBattleEnd()
    {
        if (enemyUnits.All(e => e.isDead))
        {
            ChangeState(BattleState.BattleWon);
            return true;
        }

        if (playerUnits.All(p => p.isDead))
        {
            ChangeState(BattleState.BattleLost);
            return true;
        }

        return false;
    }
}
