using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EliteEldritchHunter : MonoBehaviour
{
    [Header("Enemy Info & Defense")]
    public string enemyName = "Eldritch Hunter";
    [Range(0f, 0.8f)] 
    public float damageReductionPercent = 0.3f; // Pengurangan damage 30% di Phase 1

    [Header("Enemy Skills Base Damage")]
    public float skill1DamagePhase1 = 10f;
    public float skill2DamagePhase1 = 15f;
    public float skill1DamagePhase2 = 20f; // Lebih sakit di Phase 2 (Rage Mode)
    public float skill2DamagePhase2 = 30f;

    [Header("Phase System")]
    public int currentPhase = 1;
    public float maxHealthPhase1 = 250f;
    private float currentHealth;
    
    [Header("Phase 2 Settings (Rage Mode)")]
    public Sprite horrorEnemySprite;      // Aset gambar musuh mengerikan
    public int phase2RequiredClicks = 7;   // Harus 7 kali klik di fase 2
    private int phase2ClickCount = 0;
    
    [Header("UI References")]
    public Slider healthSlider;
    public TMP_Text turnText;             // Teks "Turn 1" / "Turn 2"
    
    [Header("Weak Spots")]
    public GameObject weakSpotsContainer; // Menampung titik-titik serang 3D/2D

    [Header("Mekanik Kontak Fisik Baru")]
    public float attackCooldown = 1.0f; // Jeda waktu musuh menyerang balik (1 detik)
    private float nextAttackTime = 0f;

    void Start()
    {
        currentHealth = maxHealthPhase1;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealthPhase1;
            healthSlider.value = currentHealth;
        }
        
        if (turnText != null) turnText.text = "Turn 1";
        if (weakSpotsContainer != null) weakSpotsContainer.SetActive(false);
    }

    // --- MEKANIK KONTAK FISIK TABRAKAN ---
    // Menggunakan OnCollisionStay2D karena game Anda menggunakan fisika 2D (terlihat dari lambang Rigidbody2D di skrip lain)
    private void OnCollisionStay2D(Collision2D collision)
    {
        // 1. Deteksi apakah objek yang menabrak memiliki komponen PlayerController
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            // 2. Player otomatis memberikan damage ke musuh saat menempel
            float damageKeMusuh = player.attackDamage * Time.deltaTime;
            TakeDamage(damageKeMusuh);

            // 3. Musuh otomatis menyerang balik Player sesuai jeda Cooldown
            if (Time.time >= nextAttackTime)
            {
                PerformTurn(player); // Memanggil fungsi acak skill musuh ke player
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    // --- MEKANIK TERIMA DAMAGE (SUDAH DENGAN DEFENSE / REDUKSI) ---
    public void TakeDamage(float incomingDamage)
    {
        if (currentPhase == 1)
        {
            // Potong damage yang masuk dengan persentase reduksi agar tidak terlalu sakit
            float finalDamage = incomingDamage * (1f - damageReductionPercent);
            currentHealth -= finalDamage;

            if (healthSlider != null) healthSlider.value = currentHealth;

            Debug.Log($"{enemyName} menahan serangan! Damage masuk: {finalDamage} | Sisa HP: {currentHealth}");

            if (currentHealth <= 0)
            {
                EnterPhase2();
            }
        }
        else if (currentPhase == 2)
        {
            // Di fase 2, damage dihitung per klik mekanik
            phase2ClickCount++;
            
            if (healthSlider != null)
            {
                healthSlider.value = phase2RequiredClicks - phase2ClickCount;
            }

            Debug.Log($"Fase 2 - Hits pada Weak Spot: {phase2ClickCount} / {phase2RequiredClicks}");

            if (phase2ClickCount >= phase2RequiredClicks)
            {
                Die();
            }
        }
    }

    // --- SKILL ENEMY ---
    // Skill 1: Serangan Ringan / Cepat
    public void UseSkill1(PlayerController player)
    {
        float damage = (currentPhase == 1) ? skill1DamagePhase1 : skill1DamagePhase2;
        Debug.Log($"{enemyName} menggunakan [Skill 1: Eldritch Slash] sebesar {damage} damage!");
        player.TakeDamage(damage);
    }

    // Skill 2: Serangan Area / Void Blast
    public void UseSkill2(PlayerController player)
    {
        float damage = (currentPhase == 1) ? skill2DamagePhase1 : skill2DamagePhase2;
        Debug.Log($"{enemyName} menggunakan [Skill 2: Void Blast] sebesar {damage} damage!");
        player.TakeDamage(damage);
    }

    // --- GILIRAN MUSUH SERANG BALIK (DIPANGGUL BATTLE MANAGER) ---
    public void PerformTurn(PlayerController player)
    {
        if (currentHealth <= 0 && currentPhase == 1) return;

        // ACaK: Choose Skill 1 or Skill 2 (50% chance)
        int randomSkill = Random.Range(1, 3);
        if (randomSkill == 1)
        {
            UseSkill1(player);
        }
        else
        {
            UseSkill2(player);
        }
    }

    void EnterPhase2()
    {
        currentPhase = 2;
        Debug.Log("MUSUH BERUBAH FASE! BERTAMBAH KUAT & MENGERIKAN!");

        // 1. Ubah aset visual musuh
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && horrorEnemySprite != null)
        {
            spriteRenderer.sprite = horrorEnemySprite;
        }

        // 2. Setel ulang HP Slider sesuai hitungan klik
        if (healthSlider != null)
        {
            healthSlider.maxValue = phase2RequiredClicks;
            healthSlider.value = phase2RequiredClicks;
        }

        // 3. Update UI Turn
        if (turnText != null)
        {
            turnText.text = "Turn 2";
            turnText.color = Color.red;
        }

        // 4. Munculkan Weak Spots
        if (weakSpotsContainer != null)
        {
            weakSpotsContainer.SetActive(true);
        }
    }

    void Die()
    {
        Debug.Log("Boss Eldritch Hunter Berhasil Dikalahkan Sepenuhnya!");
        Destroy(gameObject);
    }
}
