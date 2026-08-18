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
    [Header("Setup Battle (drag GameObject 2D player & musuh dari Hierarchy)")]
    public List<BattleActorView> playerActors;
    public List<BattleActorView> enemyActors;

    [Header("Konfigurasi Attack")]
    public int heavyAttackManaCost = 10; // MP yang dikonsumsi Heavy Attack

    // Tipe attack yang sedang dipilih player dari submenu Attack (Basic/Heavy/Charged),
    // dipakai saat player mengklik GameObject musuh di scene sebagai target.
    private AttackType pendingAttackType = AttackType.Basic;

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
        playerUnits.Clear();
        enemyUnits.Clear();

        // Untuk tiap GameObject visual di scene, buat data runtime-nya (BattleUnit)
        // lalu hubungkan balik ke GameObject itu lewat Initialize().
        foreach (var actor in playerActors)
        {
            var unit = new BattleUnit(actor.stats, true);
            playerUnits.Add(unit);
            actor.isPlayerSide = true;
            actor.Initialize(unit, this);
        }

        foreach (var actor in enemyActors)
        {
            var unit = new BattleUnit(actor.stats, false);
            enemyUnits.Add(unit);
            actor.isPlayerSide = false;
            actor.Initialize(unit, this);
        }

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

        // Kalau unit ini sedang "charging" (habis pilih Charged Attack giliran lalu),
        // giliran ini otomatis dipakai untuk melepaskan serangan -- player tidak
        // perlu (dan tidak bisa) memilih aksi lain lagi.
        if (activeUnit.isCharging)
        {
            ResolveChargedAttack();
            return;
        }

        ChangeState(activeUnit.isPlayerSide ? BattleState.PlayerTurn : BattleState.EnemyTurn);
    }

    /// <summary>
    /// Melepaskan Charged Attack yang sudah "diisi" di giliran sebelumnya.
    /// Dipanggil otomatis dari NextTurn(), bukan lewat tombol UI.
    /// </summary>
    void ResolveChargedAttack()
    {
        BattleUnit target = activeUnit.chargeTarget;

        // Kalau target charge sudah mati duluan (misal kena serangan unit lain),
        // alihkan otomatis ke musuh/player lain yang masih hidup dari sisi lawan.
        if (target == null || target.isDead)
        {
            var fallbackPool = activeUnit.isPlayerSide ? enemyUnits : playerUnits;
            target = fallbackPool.FirstOrDefault(u => !u.isDead);
        }

        activeUnit.isCharging = false;
        activeUnit.chargeTarget = null;

        if (target == null)
        {
            Debug.Log($"[Battle] {activeUnit.baseStats.unitName} melepas Charged Attack tapi tidak ada target tersisa.");
            EndPlayerAction();
            return;
        }

        int damage = BattleCalculator.CalculateSkillDamage(
            activeUnit, target, BattleCalculator.CHARGED_ATTACK_MULTIPLIER, out bool isCrit);
        target.TakeDamage(damage);

        Debug.Log($"[Battle] {activeUnit.baseStats.unitName} melepaskan CHARGED ATTACK ke {target.baseStats.unitName}: " +
                   $"{damage} damage{(isCrit ? " (CRITICAL!)" : "")}!");

        RefreshActorVisual(target);
        EndPlayerAction();
    }

    // ---------------------------------------------------------
    // SUBMENU ATTACK — panggil dari 3 tombol tipe attack (Basic/Heavy/Charged)
    // ---------------------------------------------------------

    /// <summary>
    /// Dipanggil saat player menekan tombol "Basic Attack" di submenu Attack.
    /// Setelah ini, player tinggal klik GameObject musuh di scene untuk menyerang
    /// (lewat BattleActorView.OnMouseDown -> ExecutePendingAttack).
    /// </summary>
    public void SelectBasicAttackType()
    {
        pendingAttackType = AttackType.Basic;
        Debug.Log("[Battle] Pilih target untuk Basic Attack (klik musuh di scene).");
    }

    public void SelectHeavyAttackType()
    {
        pendingAttackType = AttackType.Heavy;
        Debug.Log($"[Battle] Pilih target untuk Heavy Attack (butuh {heavyAttackManaCost} MP).");
    }

    public void SelectChargedAttackType()
    {
        pendingAttackType = AttackType.Charged;
        Debug.Log("[Battle] Pilih target untuk Charged Attack (akan meledak giliran berikutnya).");
    }

    /// <summary>
    /// Dipanggil oleh BattleActorView.OnMouseDown saat player klik target musuh,
    /// mengeksekusi tipe attack yang sedang aktif (hasil pilihan submenu).
    /// </summary>
    public void ExecutePendingAttack(BattleUnit target)
    {
        switch (pendingAttackType)
        {
            case AttackType.Basic:
                PlayerBasicAttack(target);
                break;
            case AttackType.Heavy:
                PlayerHeavyAttack(target);
                break;
            case AttackType.Charged:
                PlayerChargedAttack(target);
                break;
        }
    }

    // ---------------------------------------------------------
    // PLAYER ACTIONS — panggil method ini dari tombol UI
    // ---------------------------------------------------------

    /// <summary>
    /// Basic Attack: tidak mengonsumsi apapun (tanpa MP, tanpa cost turn ekstra).
    /// Damage normal (1.0x), langsung dieksekusi saat ini juga.
    /// </summary>
    public void PlayerBasicAttack(BattleUnit target)
    {
        if (currentState != BattleState.PlayerTurn) return;

        int damage = BattleCalculator.CalculateDamage(activeUnit, target, out bool isCrit);
        target.TakeDamage(damage);

        Debug.Log($"[Battle] {activeUnit.baseStats.unitName} BASIC ATTACK ke {target.baseStats.unitName}: " +
                   $"{damage} damage{(isCrit ? " (CRITICAL!)" : "")}. HP tersisa: {target.currentHP}");

        RefreshActorVisual(target);
        EndPlayerAction();
    }

    /// <summary>
    /// Heavy Attack: mengonsumsi MP (jumlahnya diatur lewat field heavyAttackManaCost
    /// di Inspector). Damage lebih besar dari Basic Attack, langsung dieksekusi.
    /// Gagal (tidak menghabiskan giliran) jika MP tidak cukup.
    /// </summary>
    public void PlayerHeavyAttack(BattleUnit target)
    {
        if (currentState != BattleState.PlayerTurn) return;

        if (!activeUnit.UseMP(heavyAttackManaCost))
        {
            Debug.Log($"[Battle] MP tidak cukup untuk Heavy Attack! (butuh {heavyAttackManaCost} MP, " +
                       $"punya {activeUnit.currentMP} MP)");
            return; // tidak memanggil EndPlayerAction -- player boleh pilih aksi lain
        }

        int damage = BattleCalculator.CalculateSkillDamage(
            activeUnit, target, BattleCalculator.HEAVY_ATTACK_MULTIPLIER, out bool isCrit);
        target.TakeDamage(damage);

        Debug.Log($"[Battle] {activeUnit.baseStats.unitName} HEAVY ATTACK ke {target.baseStats.unitName}: " +
                   $"{damage} damage{(isCrit ? " (CRITICAL!)" : "")}. Sisa MP: {activeUnit.currentMP}");

        RefreshActorVisual(target);
        EndPlayerAction();
    }

    /// <summary>
    /// Charged Attack: tidak menyerang saat ini juga. Unit "mengisi tenaga" giliran
    /// ini (tidak melakukan apapun secara visual/damage), lalu di giliran unit ini
    /// BERIKUTNYA otomatis melepaskan serangan besar (2.5x) ke target yang dipilih
    /// sekarang. Total mengonsumsi 1 giliran ekstra dibanding Basic Attack.
    /// </summary>
    public void PlayerChargedAttack(BattleUnit target)
    {
        if (currentState != BattleState.PlayerTurn) return;

        activeUnit.isCharging = true;
        activeUnit.chargeTarget = target;

        Debug.Log($"[Battle] {activeUnit.baseStats.unitName} mulai charge serangan ke {target.baseStats.unitName}... " +
                   $"(akan meledak di giliran berikutnya)");

        EndPlayerAction();
    }

    /// <summary>
    /// Wrapper tanpa parameter khusus untuk dihubungkan ke tombol UI (OnClick)
    /// saat testing -- otomatis menargetkan musuh pertama yang masih hidup.
    /// Ganti dengan sistem target-selection sungguhan (klik GameObject musuh
    /// lewat BattleActorView.OnMouseDown) untuk versi final.
    /// </summary>
    public void PlayerBasicAttackFirstEnemy()
    {
        var target = enemyUnits.FirstOrDefault(e => !e.isDead);
        if (target == null) { Debug.Log("[Battle] Tidak ada musuh untuk diserang."); return; }
        PlayerBasicAttack(target);
    }

    public void PlayerHeavyAttackFirstEnemy()
    {
        var target = enemyUnits.FirstOrDefault(e => !e.isDead);
        if (target == null) { Debug.Log("[Battle] Tidak ada musuh untuk diserang."); return; }
        PlayerHeavyAttack(target);
    }

    public void PlayerChargedAttackFirstEnemy()
    {
        var target = enemyUnits.FirstOrDefault(e => !e.isDead);
        if (target == null) { Debug.Log("[Battle] Tidak ada musuh untuk diserang."); return; }
        PlayerChargedAttack(target);
    }

    /// <summary>
    /// Cari BattleActorView yang cocok dengan sebuah BattleUnit, lalu minta
    /// dia update tampilannya (dipanggil tiap kali HP unit berubah).
    /// </summary>
    void RefreshActorVisual(BattleUnit unit)
    {
        var actor = playerActors.Concat(enemyActors).FirstOrDefault(a => a.battleUnit == unit);
        actor?.RefreshVisual();
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

    /// <summary>
    /// Struct kecil untuk membawa info escape ke UI (chance, apakah diblokir, dsb)
    /// tanpa melakukan roll. Dipakai saat menampilkan panel konfirmasi.
    /// </summary>
    public struct EscapeInfo
    {
        public bool isBlocked;
        public float chance; // 0-1, sudah 0 kalau isBlocked true
    }

    /// <summary>
    /// Dipanggil saat player MENEKAN tombol Escape (bukan konfirmasi).
    /// Tidak melakukan roll -- hanya mengembalikan info untuk ditampilkan
    /// di panel konfirmasi Yes/No + chance oleh EscapeUIController.
    /// </summary>
    public EscapeInfo PeekEscapeInfo()
    {
        var aliveEnemies = enemyUnits.Where(e => !e.isDead).ToList();
        bool blocked = !EscapeSystem.CanAttemptEscape(aliveEnemies);

        return new EscapeInfo
        {
            isBlocked = blocked,
            chance = blocked ? 0f : EscapeSystem.GetEscapeChance(aliveEnemies)
        };
    }

    /// <summary>
    /// Dipanggil setelah player menekan "Yes" di panel konfirmasi DAN sudah
    /// melihat chance-nya. Melakukan roll sungguhan memakai chance yang SAMA
    /// dengan yang ditampilkan di UI (dikirim dari EscapeUIController), lalu
    /// mengembalikan hasilnya supaya UI bisa menampilkan pesan sukses/gagal.
    /// </summary>
    public EscapeSystem.EscapeResult ConfirmEscape(float shownChance)
    {
        if (currentState != BattleState.PlayerTurn)
            return EscapeSystem.EscapeResult.Blocked;

        var aliveEnemies = enemyUnits.Where(e => !e.isDead).ToList();
        EscapeSystem.EscapeResult result = EscapeSystem.RollEscape(aliveEnemies, shownChance);

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
                break;
        }

        return result;
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

            RefreshActorVisual(target);
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