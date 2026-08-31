using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Ditempel ke GameObject 2D (player atau musuh) di scene battle.
/// Menjembatani data BattleUnit (class biasa, bukan MonoBehaviour) dengan
/// tampilan visual di scene: sprite, posisi, dan interaksi tap/klik untuk target.
/// INI yang di-drag ke GameObject 2D-mu -- bukan UnitStats atau BattleUnit.
///
/// CATATAN MOBILE: pakai IPointerClickHandler (lewat EventSystem), BUKAN
/// OnMouseDown. OnMouseDown adalah API mouse lama yang: (1) tidak terpanggil
/// sama sekali kalau project pakai Input System package baru, (2) tidak
/// reliable untuk touch/multi-touch di device mobile, (3) rawan "tertutup"
/// oleh UI Canvas di atasnya. IPointerClickHandler + EventSystem menangani
/// mouse dan touch dengan cara yang sama sehingga jauh lebih aman untuk build
/// mobile/HP. Wajib ada Physics2DRaycaster di kamera dan EventSystem di scene
/// (lihat komentar di bawah untuk setup).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class BattleActorView : MonoBehaviour, IPointerClickHandler
{
    [Header("Data sumber (isi lewat Inspector)")]
    public UnitStats stats;      // asset ScriptableObject yang jadi acuan unit ini
    public bool isPlayerSide;    // true = player, false = enemy

    [Header("Referensi visual (opsional, drag manual atau auto-cari)")]
    public SpriteRenderer spriteRenderer;

    [Header("Overhead UI (isi SALAH SATU sesuai isPlayerSide)")]
    [Tooltip("Isi ini kalau unit ini MUSUH -- prefab instance dari EnemyOverheadUI (nama + HP bar mengambang).")]
    public EnemyOverheadUI enemyOverheadUI;
    [Tooltip("Isi ini kalau unit ini PLAYER -- prefab instance dari PlayerOverheadNameUI (nama saja, mengambang).")]
    public PlayerOverheadNameUI playerOverheadNameUI;

    // Data runtime unit ini, di-assign oleh BattleManager saat battle mulai.
    [HideInInspector] public BattleUnit battleUnit;

    private BattleManager battleManager;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (stats != null && spriteRenderer != null && stats.portrait != null)
            spriteRenderer.sprite = stats.portrait;
    }

    /// <summary>
    /// Dipanggil oleh BattleManager saat battle disiapkan, menghubungkan
    /// GameObject visual ini dengan data BattleUnit runtime-nya, sekaligus
    /// mem-bind overhead UI yang sesuai (nama+HP untuk musuh, nama saja untuk player).
    /// </summary>
    public void Initialize(BattleUnit unit, BattleManager manager)
    {
        battleUnit = unit;
        battleManager = manager;

        if (isPlayerSide)
            playerOverheadNameUI?.Bind(unit, transform);
        else
            enemyOverheadUI?.Bind(unit, transform);
    }

    /// <summary>
    /// Dipanggil otomatis oleh EventSystem saat unit ini di-tap/diklik
    /// (butuh Collider2D di GameObject ini + Physics2DRaycaster di kamera).
    /// Kalau ini musuh, mengeksekusi tipe attack yang sedang dipilih player
    /// di submenu Attack (Basic/Heavy/Charged) ke unit ini sebagai target.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isPlayerSide) return; // player tidak bisa dipilih sebagai target serangan sendiri
        if (battleUnit == null || battleUnit.isDead) return;
        if (battleManager == null) return;

        battleManager.ExecutePendingAttack(battleUnit);
    }

    /// <summary>
    /// Panggil ini tiap kali HP berubah -- juga otomatis refresh HP bar
    /// overhead musuh (kalau unit ini musuh). Player tidak punya HP bar
    /// overhead (HP/MP player ditampilkan di PartyHUDPanel pojok kanan atas).
    /// </summary>
    public void RefreshVisual()
    {
        if (battleUnit == null) return;

        // Contoh sederhana: unit yang mati jadi transparan.
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = battleUnit.isDead ? 0.3f : 1f;
            spriteRenderer.color = c;
        }

        if (!isPlayerSide)
            enemyOverheadUI?.UpdateDisplay();
    }
}