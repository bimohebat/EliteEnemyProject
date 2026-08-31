using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
 
/// <summary>
/// Mengatur alur lengkap UI menu Item/Consumable:
/// 1) Tombol "Item" utama ditekan -> panel KATEGORI muncul (Potion / Food / Back)
/// 2) Pilih Potion atau Food -> panel LIST ITEM muncul, maksimal 3 item per
///    halaman. Kalau item lebih dari 3, tombol ">" muncul untuk buka halaman
///    berikutnya (dan "<" untuk kembali ke halaman sebelumnya). Ada tombol
///    Back untuk kembali ke panel kategori.
/// 3) Pilih salah satu item di list -> semua panel item tertutup, player
///    tinggal klik/tap karakter PLAYER di scene sebagai target (memakai
///    infrastruktur klik yang sama dengan target-selection Attack).
///
/// Attach ke GameObject kosong di Canvas, hubungkan semua referensi lewat Inspector.
/// </summary>
public class ItemMenuController : MonoBehaviour
{
    [Header("Referensi")]
    public BattleManager battleManager;
    public BattleInventory inventory;
 
    [Header("Panel 1: Kategori (Potion / Food)")]
    public GameObject categoryPanel;
    public Button potionCategoryButton;
    public Button foodCategoryButton;
    public Button categoryBackButton; // kembali ke menu aksi battle utama
 
    [Header("Panel 2: List Item (WAJIB isi persis 3 slot, sesuai batas per halaman)")]
    public GameObject listPanel;
    public List<ItemSlotButton> itemSlotButtons; // drag 3 slot UI dari Inspector
    public Button nextPageButton; // ">"
    public Button prevPageButton; // "<" (opsional tapi disarankan untuk UX)
    public Button listBackButton; // kembali ke panel kategori
 
    private const int ITEMS_PER_PAGE = 3;
    private ConsumableType currentCategory;
    private List<InventorySlot> currentList = new List<InventorySlot>();
    private int currentPage = 0;
 
    void Awake()
    {
        HideAllPanels();
 
        potionCategoryButton.onClick.AddListener(() => OpenList(ConsumableType.Potion));
        foodCategoryButton.onClick.AddListener(() => OpenList(ConsumableType.Food));
        categoryBackButton.onClick.AddListener(CloseAll);
 
        nextPageButton.onClick.AddListener(() => ChangePage(1));
        prevPageButton.onClick.AddListener(() => ChangePage(-1));
        listBackButton.onClick.AddListener(BackToCategory);
    }
 
    void HideAllPanels()
    {
        categoryPanel.SetActive(false);
        listPanel.SetActive(false);
    }
 
    /// <summary>
    /// Panggil dari tombol menu aksi utama "Item" (Consumable).
    /// </summary>
    public void OnItemButtonPressed()
    {
        listPanel.SetActive(false);
        categoryPanel.SetActive(true);
    }
 
    void OpenList(ConsumableType type)
    {
        currentCategory = type;
        currentPage = 0;
        RefreshList();
 
        categoryPanel.SetActive(false);
        listPanel.SetActive(true);
    }
 
    void RefreshList()
    {
        currentList = inventory.GetSlotsByType(currentCategory);
 
        int totalPages = Mathf.Max(Mathf.CeilToInt(currentList.Count / (float)ITEMS_PER_PAGE), 1);
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
 
        int startIndex = currentPage * ITEMS_PER_PAGE;
 
        for (int i = 0; i < itemSlotButtons.Count; i++)
        {
            int idx = startIndex + i;
            if (idx < currentList.Count)
            {
                itemSlotButtons[i].gameObject.SetActive(true);
                itemSlotButtons[i].Bind(currentList[idx], OnItemSelected);
            }
            else
            {
                itemSlotButtons[i].gameObject.SetActive(false);
            }
        }
 
        // Tombol ">" hanya muncul kalau masih ada halaman berikutnya.
        nextPageButton.gameObject.SetActive(currentPage < totalPages - 1);
        // Tombol "<" hanya muncul kalau bukan di halaman pertama.
        prevPageButton.gameObject.SetActive(currentPage > 0);
    }
 
    void ChangePage(int delta)
    {
        currentPage += delta;
        RefreshList();
    }
 
    void OnItemSelected(ConsumableItem item)
    {
        battleManager.SelectConsumableItem(item);
        HideAllPanels();
        Debug.Log($"[Battle] Pilih target untuk pakai {item.itemName} (tap karakter player di scene).");
    }
 
    void BackToCategory()
    {
        listPanel.SetActive(false);
        categoryPanel.SetActive(true);
    }
 
    /// <summary>
    /// Tutup semua panel item TANPA menghabiskan giliran (player batal pakai item).
    /// Dipanggil oleh tombol Back di panel kategori.
    /// </summary>
    public void CloseAll()
    {
        HideAllPanels();
    }
}
 