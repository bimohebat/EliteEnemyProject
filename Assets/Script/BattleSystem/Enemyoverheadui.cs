using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI mengambang di atas GameObject musuh: menampilkan NAMA + HP BAR.
/// Attach script ini ke root prefab UI overhead musuh (butuh WorldSpaceUIFollower
/// di GameObject yang sama supaya posisinya ikut karakter).
///
/// Cara pasang: buat prefab UI kecil (child dari Canvas Screen Space - Overlay)
/// berisi: Text (nama) + Slider (HP bar). Attach EnemyOverheadUI + WorldSpaceUIFollower
/// ke root prefab itu, drag Text & Slider ke field di bawah.
/// </summary>
[RequireComponent(typeof(WorldSpaceUIFollower))]
public class EnemyOverheadUI : MonoBehaviour
{
    [Header("Referensi UI (drag dari child prefab)")]
    public Text nameText;
    public Slider hpSlider;

    private BattleUnit boundUnit;
    private WorldSpaceUIFollower follower;

    void Awake()
    {
        follower = GetComponent<WorldSpaceUIFollower>();
    }

    /// <summary>
    /// Hubungkan UI ini ke data BattleUnit musuh dan Transform yang diikuti.
    /// Panggil sekali saat battle setup (lihat BattleActorView.Initialize).
    /// </summary>
    public void Bind(BattleUnit unit, Transform followTarget)
    {
        boundUnit = unit;
        follower.SetTarget(followTarget);

        if (nameText != null)
            nameText.text = unit.baseStats.unitName;

        UpdateDisplay();
    }

    /// <summary>
    /// Refresh tampilan HP bar. Panggil tiap kali unit ini kena damage/heal
    /// (dipanggil dari BattleActorView.RefreshVisual()).
    /// </summary>
    public void UpdateDisplay()
    {
        if (boundUnit == null || hpSlider == null) return;

        hpSlider.maxValue = boundUnit.baseStats.maxHP;
        hpSlider.value = boundUnit.currentHP;

        // Opsional: sembunyikan overhead UI kalau unit sudah mati.
        gameObject.SetActive(!boundUnit.isDead);
    }
}