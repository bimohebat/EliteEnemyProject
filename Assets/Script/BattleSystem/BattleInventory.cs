using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
/// <summary>
/// Menyimpan dan mengelola stock item consumable (Food & Potion) selama battle.
/// Attach ke GameObject BattleManager (atau GameObject terpisah), isi daftar
/// 'slots' lewat Inspector: drag ConsumableItem asset + tentukan quantity awal.
/// Item dengan tipe sama TIDAK otomatis digabung -- kalau mau item yang sama
/// bertumpuk, cukup pakai 1 slot dengan quantity besar (itulah sistem stack-nya).
/// </summary>
public class BattleInventory : MonoBehaviour
{
    [Header("Isi awal inventory battle")]
    public List<InventorySlot> slots = new List<InventorySlot>();
 
    /// <summary>
    /// Ambil semua slot dari 1 tipe (Food/Potion) yang stock-nya masih > 0.
    /// Dipakai UI untuk menampilkan list item per kategori.
    /// </summary>
    public List<InventorySlot> GetSlotsByType(ConsumableType type)
    {
        return slots.Where(s => s.item != null && s.item.type == type && s.quantity > 0).ToList();
    }
 
    /// <summary>
    /// Pakai 1 stack item ke target unit: memulihkan HP+MP sesuai data item,
    /// lalu mengurangi quantity stack sebanyak 1. Return false kalau item
    /// tidak ditemukan atau stock sudah habis (0).
    /// </summary>
    public bool UseItem(ConsumableItem item, BattleUnit target)
    {
        var slot = slots.FirstOrDefault(s => s.item == item);
        if (slot == null || slot.quantity <= 0) return false;
 
        target.Heal(item.hpRestore);
        target.RestoreMP(item.mpRestore);
 
        slot.quantity--;
        return true;
    }
}