using UnityEngine;

/// <summary>
/// Ditempel ke GameObject 2D (player atau musuh) di scene battle.
/// Menjembatani data BattleUnit (class biasa, bukan MonoBehaviour) dengan
/// tampilan visual di scene: sprite, posisi, dan interaksi klik untuk target.
/// INI yang di-drag ke GameObject 2D-mu -- bukan UnitStats atau BattleUnit.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BattleActorView : MonoBehaviour
{
    [Header("Data sumber (isi lewat Inspector)")]
    public UnitStats stats;      // asset ScriptableObject yang jadi acuan unit ini
    public bool isPlayerSide;    // true = player, false = enemy

    [Header("Referensi visual (opsional, drag manual atau auto-cari)")]
    public SpriteRenderer spriteRenderer;

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
    /// GameObject visual ini dengan data BattleUnit runtime-nya.
    /// </summary>
    public void Initialize(BattleUnit unit, BattleManager manager)
    {
        battleUnit = unit;
        battleManager = manager;
    }

    /// <summary>
    /// Diklik lewat mouse/tap di scene (butuh Collider2D di GameObject ini).
    /// Kalau ini musuh, mengeksekusi tipe attack yang sedang dipilih player
    /// di submenu Attack (Basic/Heavy/Charged) ke unit ini sebagai target.
    /// </summary>
    void OnMouseDown()
    {
        if (isPlayerSide) return; // player tidak bisa dipilih sebagai target serangan sendiri
        if (battleUnit == null || battleUnit.isDead) return;

        battleManager?.ExecutePendingAttack(battleUnit);
    }

    /// <summary>
    /// Panggil ini tiap kali HP berubah supaya nanti gampang disambungkan
    /// ke HP bar/UI (subtask HUD Battle milik Indra).
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
    }
}