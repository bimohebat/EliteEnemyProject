using System;
 
/// <summary>
/// 1 slot inventory: item apa + berapa banyak stack-nya.
/// [Serializable] supaya bisa diisi langsung lewat Inspector di BattleInventory.
/// </summary>
[Serializable]
public class InventorySlot
{
    public ConsumableItem item;
    public int quantity;
}