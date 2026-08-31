using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Satu "baris" di panel HUD party (pojok kanan atas): menampilkan nama,
/// HP bar, dan MP bar untuk SATU anggota tim player.
/// Attach ke prefab entry HUD, drag Text & 2 Slider ke field di bawah.
/// </summary>
public class PartyHUDEntry : MonoBehaviour
{
    [Header("Referensi UI (drag dari child prefab)")]
    public Text nameText;
    public Slider hpSlider;
    public Slider mpSlider;

    private BattleUnit boundUnit;

    /// <summary>
    /// Hubungkan entry ini ke data BattleUnit player. Dipanggil sekali saat
    /// PartyHUDPanel membangun daftar entry di awal battle.
    /// </summary>
    public void Bind(BattleUnit unit)
    {
        boundUnit = unit;

        if (nameText != null)
            nameText.text = unit.baseStats.unitName;

        if (hpSlider != null)
            hpSlider.maxValue = unit.baseStats.maxHP;
        if (mpSlider != null)
            mpSlider.maxValue = unit.baseStats.maxMP;

        UpdateDisplay();
    }

    /// <summary>
    /// Refresh angka HP/MP di baris ini. Dipanggil oleh PartyHUDPanel.RefreshAll()
    /// setiap kali ada perubahan (kena damage, pakai Heavy Attack, dsb).
    /// </summary>
    public void UpdateDisplay()
    {
        if (boundUnit == null) return;

        if (hpSlider != null)
            hpSlider.value = boundUnit.currentHP;
        if (mpSlider != null)
            mpSlider.value = boundUnit.currentMP;

        // Opsional: bikin baris jadi redup kalau unit itu sudah mati (bukan hilang,
        // supaya player tetap bisa lihat siapa saja anggota timnya).
        if (nameText != null)
            nameText.color = boundUnit.isDead ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
    }
}