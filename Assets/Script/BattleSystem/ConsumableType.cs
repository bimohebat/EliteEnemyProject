using UnityEngine;

/// <summary>
/// Kategori item consumable, dipakai untuk filter tampilan per kategori (Food/Potion).
/// </summary>
public enum ConsumableType
{
    Food,
    Potion
}

[CreateAssetMenu(fileName = "NewConsumableItem", menuName = "Battle/Consumable Item")]
public class ConsumableItem : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "New Item";
    [TextArea] public string description;
    public Sprite icon;
    public ConsumableType type;

    [Header("Restore Amount")]
    public int hpRestore = 30;
    public int mpRestore = 10;

    [Header("Efek tambahan (opsional, biasanya khusus Potion)")]
    [Tooltip("Placeholder untuk efek non-heal (misal buff ATK sementara). " +
             "Belum diimplementasikan otomatis -- baru sebatas data/deskripsi.")]
    [TextArea] public string bonusEffectDescription;
}