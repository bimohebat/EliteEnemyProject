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

    [Header("UI Attack")]
    public GameObject attackUI;

    [Header("UI Party HUD (pojok kanan atas)")]
    public PartyHUDPanel partyHUDPanel;

    // Tipe attack yang sedang dipilih player dari submenu Attack (Basic/Heavy/Charged),
    // dipakai saat player mengklik GameObject musuh di scene sebagai target.
    private AttackType pendingAttackType = AttackType.Basic;

    [Header("Konfigurasi Turn Order (Random per-Pihak)")]
    [Tooltip("Maksimal berapa kali giliran boleh menumpuk berturut-turut untuk pihak yang sama.")]
    public int maxConsecutiveTurnsPerSide = 2;

    [Header("Runtime State (read-only, untuk debug)")]
    public BattleState currentState;
    public List<BattleUnit> playerUnits = new List<BattleUnit>();
    public List<BattleUnit> enemyUnits = new List<BattleUnit>();
    private BattleUnit activeUnit;

    // Tracking untuk cap turn stacking: sisi mana yang barusan jalan, dan sudah
    // berapa kali berturut-turut sisi itu yang dapat giliran.
    private BattleSide? lastSide = null;
    private int consecutiveSideTurns = 0;

    // Antrian anggota party player yang belum bertindak dalam SATU giliran-pihak
    // Player saat ini. Diisi ulang tiap kali giliran-pihak Player dimulai.
    private Queue<BattleUnit> playerPartyQueue = new Queue<BattleUnit>();

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

        partyHUDPanel?.BuildForPlayers(playerUnits);
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

        NextTurn();
    }

    // ---------------------------------------------------------
    // TURN ORDER — RANDOM PER-PIHAK dengan cap stacking
    // ---------------------------------------------------------
    // Giliran TIDAK lagi berbasis antrian speed tetap (player -> enemy -> player).
    // Sebaliknya, tiap giliran, sistem mengundi PIHAK mana (Player/Enemy) yang
    // jalan berikutnya. Satu pihak BISA dapat giliran berturut-turut (misal
    // Player -> Player -> Enemy -> Player), tapi dibatasi oleh
    // 'maxConsecutiveTurnsPerSide' (default 2) supaya tidak ada pihak yang
    // menumpuk giliran tanpa batas.

    void NextTurn()
    {
        // Cek kondisi menang/kalah dulu sebelum lanjut turn berikutnya
        if (CheckBattleEnd()) return;

        BattleSide nextSide = DecideNextSide();

        // Update counter stacking: kalau pihak yang sama dengan giliran
        // sebelumnya, tambah hitungan; kalau beda, reset ke 1.
        // Catatan: untuk Player, "1 hitungan" = 1 kali PARTY PHASE (seluruh
        // anggota party bergerak), bukan 1 unit saja.
        consecutiveSideTurns = (lastSide == nextSide) ? consecutiveSideTurns + 1 : 1;
        lastSide = nextSide;

        if (nextSide == BattleSide.Player)
        {
            StartPlayerPartyPhase();
            return;
        }

        // Pihak Enemy: masih 1 unit acak per giliran-pihak (AI sederhana).
        // Kalau nanti mau enemy juga full-party seperti player, tinggal
        // terapkan pola StartPlayerPartyPhase yang sama di sini.
        activeUnit = PickActingUnit(BattleSide.Enemy);

        if (activeUnit == null)
        {
            Debug.LogWarning("[Battle] Tidak ada unit musuh hidup untuk bertindak.");
            return;
        }

        activeUnit.isDefending = false;

        if (activeUnit.isCharging)
        {
            ResolveChargedAttack();
            return;
        }

        ChangeState(BattleState.EnemyTurn);
    }

    /// <summary>
    /// Memulai giliran-pihak Player: SEMUA anggota party yang masih hidup akan
    /// bertindak satu per satu (masing-masing memilih 1 tipe attack + 1 target),
    /// sebelum giliran berpindah ke pihak lain (mengikuti aturan random+cap di
    /// DecideNextSide). Unit yang sedang "charging" diprioritaskan paling awal
    /// supaya charged attack-nya pasti meledak di awal phase ini.
    /// </summary>
    void StartPlayerPartyPhase()
    {
        var alive = playerUnits.Where(u => !u.isDead).ToList();
        var charging = alive.Where(u => u.isCharging);
        var notCharging = alive.Where(u => !u.isCharging);

        playerPartyQueue = new Queue<BattleUnit>(charging.Concat(notCharging));

        AdvancePlayerPartyPhase();
    }

    /// <summary>
    /// Majukan ke anggota party berikutnya dalam antrian party phase saat ini.
    /// Kalau antrian habis (semua anggota sudah bertindak), giliran-pihak Player
    /// ini selesai -> lanjut tentukan giliran-pihak berikutnya lewat NextTurn().
    /// </summary>
    void AdvancePlayerPartyPhase()
    {
        if (CheckBattleEnd()) return;

        if (playerPartyQueue.Count == 0)
        {
            NextTurn(); // party phase selesai, tentukan giliran-pihak berikutnya
            return;
        }

        activeUnit = playerPartyQueue.Dequeue();

        if (activeUnit.isDead)
        {
            AdvancePlayerPartyPhase(); // jaga-jaga, skip kalau mati di tengah phase
            return;
        }

        activeUnit.isDefending = false;

        if (activeUnit.isCharging)
        {
            ResolveChargedAttack();
            return;
        }

        ChangeState(BattleState.PlayerTurn);
    }

    /// <summary>
    /// Mengundi pihak mana yang jalan berikutnya. Kalau pihak yang sama sudah
    /// menumpuk sampai batas (maxConsecutiveTurnsPerSide), pihak lain DIPAKSA
    /// jalan (asal masih ada unit hidup di sana).
    /// </summary>
    BattleSide DecideNextSide()
    {
        bool playerAlive = playerUnits.Any(u => !u.isDead);
        bool enemyAlive = enemyUnits.Any(u => !u.isDead);

        // Cap tercapai -> paksa ganti ke pihak sebaliknya (kalau masih ada yang hidup di sana)
        if (lastSide.HasValue && consecutiveSideTurns >= maxConsecutiveTurnsPerSide)
        {
            BattleSide forcedSide = lastSide.Value == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;
            bool forcedSideAlive = forcedSide == BattleSide.Player ? playerAlive : enemyAlive;
            if (forcedSideAlive) return forcedSide;
            // Kalau pihak lawan sudah habis semua, biarkan lanjut ke pengundian
            // biasa di bawah (praktiknya CheckBattleEnd sudah menghentikan battle
            // sebelum sampai sini, jadi ini murni jaring pengaman).
        }

        // Pengundian biasa, hanya di antara pihak yang masih punya unit hidup
        List<BattleSide> options = new List<BattleSide>();
        if (playerAlive) options.Add(BattleSide.Player);
        if (enemyAlive) options.Add(BattleSide.Enemy);

        return options[Random.Range(0, options.Count)];
    }

    /// <summary>
    /// Pilih unit yang akan bertindak dari pihak yang terpilih.
    /// Unit yang sedang "charging" SELALU diprioritaskan (supaya Charged Attack
    /// pasti meledak begitu pihaknya dapat giliran lagi, tidak keundur-undur
    /// oleh unit lain di pihak yang sama). Kalau tidak ada yang charging,
    /// pilih acak dari unit hidup di pihak itu.
    /// </summary>
    BattleUnit PickActingUnit(BattleSide side)
    {
        List<BattleUnit> sideUnits = side == BattleSide.Player ? playerUnits : enemyUnits;

        var charging = sideUnits.FirstOrDefault(u => !u.isDead && u.isCharging);
        if (charging != null) return charging;

        var alive = sideUnits.Where(u => !u.isDead).ToList();
        if (alive.Count == 0) return null;

        return alive[Random.Range(0, alive.Count)];
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
            EndUnitAction();
            return;
        }

        int damage = BattleCalculator.CalculateSkillDamage(
            activeUnit, target, BattleCalculator.CHARGED_ATTACK_MULTIPLIER, out bool isCrit);
        target.TakeDamage(damage);

        Debug.Log($"[Battle] {activeUnit.baseStats.unitName} melepaskan CHARGED ATTACK ke {target.baseStats.unitName}: " +
                   $"{damage} damage{(isCrit ? " (CRITICAL!)" : "")}!");

        RefreshActorVisual(target);
        EndUnitAction();
    }

    // ---------------------------------------------------------
    // SUBMENU ATTACK — panggil dari 3 tombol tipe attack (Basic/Heavy/Charged)
    // ---------------------------------------------------------

    /// <summary>
    /// Membuka UI Attack.
    /// Dipanggil oleh tombol Attack utama.
    /// </summary>
    public void OpenAttackUI()
    {
        if (attackUI != null)
        {
            attackUI.SetActive(true);
        }
    }

    /// <summary>
    /// Hanya menyembunyikan panel attackUI, TANPA mereset pendingAttackType.
    /// Dipakai internal setelah player MEMILIH tipe attack (Basic/Heavy/Charged),
    /// supaya panel tidak menghalangi tap ke musuh, tapi pilihan tipe tetap tersimpan.
    /// </summary>
    void HideAttackPanel()
    {
        if (attackUI != null)
        {
            attackUI.SetActive(false);
        }
    }

    /// <summary>
    /// Menutup UI Attack DAN membatalkan pilihan tipe attack (kembali ke Basic).
    /// Dipanggil oleh tombol Close/Cancel pada UI Attack -- yaitu saat player
    /// BATAL memilih attack sama sekali, bukan setelah memilih salah satu tipe.
    /// </summary>
    public void CloseAttackUI()
    {
        HideAttackPanel();
        pendingAttackType = AttackType.Basic;

        Debug.Log("[Battle] Attack UI ditutup.");
    }

    /// <summary>
    /// Dipanggil saat player menekan tombol "Basic Attack" di submenu Attack.
    /// Setelah ini, player tinggal klik GameObject musuh di scene untuk menyerang
    /// (lewat BattleActorView.OnMouseDown -> ExecutePendingAttack).
    /// </summary>
    public void SelectBasicAttackType()
    {
        pendingAttackType = AttackType.Basic;
        HideAttackPanel(); // sembunyikan panel saja, tidak reset pilihan tipe
        Debug.Log("[Battle] Pilih target untuk Basic Attack (klik musuh di scene).");
    }

    public void SelectHeavyAttackType()
    {
        pendingAttackType = AttackType.Heavy;
        HideAttackPanel();
        Debug.Log($"[Battle] Pilih target untuk Heavy Attack (butuh {heavyAttackManaCost} MP).");
    }

    public void SelectChargedAttackType()
    {
        pendingAttackType = AttackType.Charged;
        HideAttackPanel();
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
        EndUnitAction();
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
            return; // tidak memanggil EndUnitAction -- player boleh pilih aksi lain
        }

        int damage = BattleCalculator.CalculateSkillDamage(
            activeUnit, target, BattleCalculator.HEAVY_ATTACK_MULTIPLIER, out bool isCrit);
        target.TakeDamage(damage);

        Debug.Log($"[Battle] {activeUnit.baseStats.unitName} HEAVY ATTACK ke {target.baseStats.unitName}: " +
                   $"{damage} damage{(isCrit ? " (CRITICAL!)" : "")}. Sisa MP: {activeUnit.currentMP}");

        RefreshActorVisual(target);
        EndUnitAction();
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

        EndUnitAction();
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

        // HP/MP player ditampilkan di panel HUD terpisah (bukan overhead),
        // jadi refresh panel itu juga setiap kali ada perubahan.
        partyHUDPanel?.RefreshAll();
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

        EndUnitAction();
    }

    public void PlayerDefend()
    {
        if (currentState != BattleState.PlayerTurn) return;

        activeUnit.isDefending = true;
        Debug.Log($"[Battle] {activeUnit.baseStats.unitName} bertahan.");

        EndUnitAction();
    }

    public void PlayerUseItem(System.Action<BattleUnit> itemEffect, BattleUnit target)
    {
        if (currentState != BattleState.PlayerTurn) return;

        itemEffect?.Invoke(target); // contoh: (u) => u.Heal(30)
        Debug.Log($"[Battle] {activeUnit.baseStats.unitName} menggunakan item ke {target.baseStats.unitName}.");

        EndUnitAction();
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
                EndUnitAction();
                break;

            case EscapeSystem.EscapeResult.Blocked:
                Debug.Log("[Battle] Battle ini tidak bisa di-escape!");
                break;
        }

        return result;
    }

    /// <summary>
    /// Dipanggil setiap kali SATU unit selesai bertindak (baik player maupun enemy).
    /// Kalau unit itu dari pihak Player -> lanjut ke anggota party berikutnya
    /// dalam party phase yang SAMA (tidak mengundi pihak baru dulu).
    /// Kalau dari pihak Enemy -> langsung tentukan giliran-pihak berikutnya
    /// (model 1 unit acak per giliran-pihak, belum full-party seperti player).
    /// </summary>
    void EndUnitAction()
    {
        if (CheckBattleEnd()) return;

        if (activeUnit.isPlayerSide)
            AdvancePlayerPartyPhase();
        else
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