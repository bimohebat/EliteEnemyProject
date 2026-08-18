using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mengatur alur UI escape secara lengkap:
/// 1) Player tekan tombol Escape -> muncul panel konfirmasi ("Yakin ingin kabur?" Yes/No)
/// 2) Tekan Yes -> panel konfirmasi tertutup, muncul panel chance (menampilkan %
///    peluang berhasil kabur, atau pesan "tidak bisa kabur" jika lawan Boss)
/// 3) Dari panel chance, player tekan tombol "Coba Kabur" -> roll sungguhan dieksekusi,
///    hasil (berhasil/gagal) ditampilkan di panel result.
///
/// Attach script ini ke sebuah GameObject kosong di Canvas (misal "EscapeUIController"),
/// lalu drag semua referensi UI di bawah lewat Inspector.
/// </summary>
public class EscapeUIController : MonoBehaviour
{
    [Header("Referensi ke BattleManager")]
    public BattleManager battleManager;

    [Header("Panel 1: Konfirmasi Yes/No")]
    public GameObject confirmPanel;
    public Text confirmMessageText;   // contoh isi: "Yakin ingin kabur dari pertarungan?"
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("Panel 2: Info Chance")]
    public GameObject chancePanel;
    public Text chanceText;           // contoh isi: "Peluang kabur: 75%"
    public Image chanceFillBar;       // opsional, Image bertipe Filled untuk bar visual
    public Button tryEscapeButton;    // tombol untuk benar-benar melakukan roll
    public Button cancelChanceButton; // batal, kembali ke aksi battle biasa

    [Header("Panel 3: Hasil")]
    public GameObject resultPanel;
    public Text resultText;
    public Button closeResultButton;

    // Menyimpan chance yang sedang ditampilkan supaya roll memakai angka yang SAMA
    // dengan yang dilihat player (konsisten, tidak curang).
    private float shownChance;

    void Awake()
    {
        HideAllPanels();

        confirmYesButton.onClick.AddListener(OnConfirmYes);
        confirmNoButton.onClick.AddListener(OnConfirmNo);
        tryEscapeButton.onClick.AddListener(OnTryEscapeClicked);
        cancelChanceButton.onClick.AddListener(OnCancelChance);
        closeResultButton.onClick.AddListener(OnCloseResult);
    }

    void HideAllPanels()
    {
        confirmPanel.SetActive(false);
        chancePanel.SetActive(false);
        resultPanel.SetActive(false);
    }

    /// <summary>
    /// Panggil method ini dari tombol "Escape" utama di battle menu
    /// (ganti pemanggilan langsung ke BattleManager.PlayerEscape lama).
    /// </summary>
    public void OnEscapeButtonPressed()
    {
        confirmMessageText.text = "Yakin ingin kabur dari pertarungan?";
        confirmPanel.SetActive(true);
    }

    void OnConfirmNo()
    {
        confirmPanel.SetActive(false);
        // Tidak menghabiskan giliran player -- kembali ke menu aksi battle seperti biasa.
    }

    void OnConfirmYes()
    {
        confirmPanel.SetActive(false);

        BattleManager.EscapeInfo info = battleManager.PeekEscapeInfo();
        shownChance = info.chance;

        if (info.isBlocked)
        {
            chanceText.text = "Tidak bisa kabur dari musuh ini!";
            if (chanceFillBar != null) chanceFillBar.fillAmount = 0f;
            tryEscapeButton.gameObject.SetActive(false); // sembunyikan tombol coba, karena diblokir
        }
        else
        {
            chanceText.text = $"Peluang kabur: {shownChance:P0}";
            if (chanceFillBar != null) chanceFillBar.fillAmount = shownChance;
            tryEscapeButton.gameObject.SetActive(true);
        }

        chancePanel.SetActive(true);
    }

    void OnCancelChance()
    {
        chancePanel.SetActive(false);
    }

    void OnTryEscapeClicked()
    {
        chancePanel.SetActive(false);

        EscapeSystem.EscapeResult result = battleManager.ConfirmEscape(shownChance);
        ShowResult(result);
    }

    void ShowResult(EscapeSystem.EscapeResult result)
    {
        switch (result)
        {
            case EscapeSystem.EscapeResult.Success:
                resultText.text = "Berhasil kabur!";
                break;
            case EscapeSystem.EscapeResult.Failed:
                resultText.text = "Gagal kabur! Giliran hangus.";
                break;
            case EscapeSystem.EscapeResult.Blocked:
                resultText.text = "Tidak bisa kabur dari musuh ini!";
                break;
        }

        resultPanel.SetActive(true);
    }

    void OnCloseResult()
    {
        resultPanel.SetActive(false);
        // Jika battle sudah berpindah ke state BattleEscaped, BattleManager
        // yang akan menangani transisi keluar dari scene battle.
    }
}
