using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI mengambang di atas GameObject player: HANYA menampilkan NAMA.
/// (HP bar & MP bar milik player ditampilkan terpisah di panel party HUD
/// pojok kanan atas -- lihat PartyHUDPanel.cs -- bukan di sini.)
///
/// Cara pasang sama seperti EnemyOverheadUI: prefab kecil di Canvas Screen
/// Space - Overlay, isinya cukup 1 Text. Attach PlayerOverheadNameUI +
/// WorldSpaceUIFollower ke root prefab itu.
/// </summary>
[RequireComponent(typeof(WorldSpaceUIFollower))]
public class PlayerOverheadNameUI : MonoBehaviour
{
    [Header("Referensi UI (drag dari child prefab)")]
    public Text nameText;

    private WorldSpaceUIFollower follower;

    void Awake()
    {
        follower = GetComponent<WorldSpaceUIFollower>();
    }

    /// <summary>
    /// Hubungkan UI ini ke data BattleUnit player dan Transform yang diikuti.
    /// Nama tidak berubah selama battle, jadi cukup di-set sekali di sini.
    /// </summary>
    public void Bind(BattleUnit unit, Transform followTarget)
    {
        follower.SetTarget(followTarget);

        if (nameText != null)
            nameText.text = unit.baseStats.unitName;
    }
}