using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Panel HUD tetap di pojok KANAN ATAS layar, menampilkan nama + HP bar + MP bar
/// untuk SETIAP anggota tim player (statis di layar, tidak mengikuti karakter
/// di scene -- beda dengan overhead UI musuh/nama player yang mengambang).
///
/// Cara pasang:
/// 1. Buat Canvas Screen Space - Overlay (kalau belum ada).
/// 2. Buat Panel kosong, anchor ke pojok kanan atas (anchor min/max = (1,1)).
///    Kasih Vertical Layout Group supaya entry-entry-nya rapi ke bawah.
/// 3. Buat prefab 1 baris (PartyHUDEntry.cs) berisi Text + 2 Slider.
/// 4. Attach script ini ke Panel, drag prefab & container ke field di bawah.
/// 5. Drag GameObject Panel ini ke field 'Party Hud Panel' di BattleManager.
/// </summary>
public class PartyHUDPanel : MonoBehaviour
{
    [Header("Setup")]
    public PartyHUDEntry entryPrefab;
    public Transform entryContainer; // parent dengan Vertical Layout Group

    private readonly List<PartyHUDEntry> entries = new List<PartyHUDEntry>();

    /// <summary>
    /// Bangun 1 baris HUD untuk tiap anggota tim player. Panggil sekali dari
    /// BattleManager setelah playerUnits selesai dibuat (di SetupBattle).
    /// </summary>
    public void BuildForPlayers(List<BattleUnit> playerUnits)
    {
        // Bersihkan entry lama (misal kalau battle di-restart)
        foreach (var entry in entries)
        {
            if (entry != null) Destroy(entry.gameObject);
        }
        entries.Clear();

        foreach (var unit in playerUnits)
        {
            PartyHUDEntry entry = Instantiate(entryPrefab, entryContainer);
            entry.Bind(unit);
            entries.Add(entry);
        }
    }

    /// <summary>
    /// Refresh semua baris HUD (HP/MP terbaru). Panggil tiap kali ada unit
    /// player yang berubah HP/MP-nya (dipanggil dari BattleManager.RefreshActorVisual).
    /// </summary>
    public void RefreshAll()
    {
        foreach (var entry in entries)
        {
            entry?.UpdateDisplay();
        }
    }
}