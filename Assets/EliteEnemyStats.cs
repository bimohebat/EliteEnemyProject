using UnityEngine;

public class EliteEnemyStats : MonoBehaviour
{
    [Header("Enemy Identity")]
    public string enemyName = "Elite Mechanical Hunter";
    public int currentLevel;

    [Header("Base Stats (Level 0)")]
    [SerializeField] private float VectorBaseHP = 450f;
    [SerializeField] private float VectorBaseMP = 100f;
    [SerializeField] private float baseATK = 80f;
    [SerializeField] private float baseMATK = 60f;
    [SerializeField] private float baseDEF = 55f;
    [SerializeField] private float baseMDEF = 40f;

    [Header("Current Live Stats")]
    public float maxHP;
    public float currentHP;
    public float maxMP;
    public float currentMP;
    public float currentATK;
    public float currentMATK;
    public float currentDEF;
    public float currentMDEF;
    
    public float maxDEF;
    public float maxMDEF;

    // ATURAN GDD HALAMAN 25: Critical Rate constant 50%, DMG 200%
    private const float CRIT_RATE_CHANCE = 0.5f; 
    private const float CRIT_DMG_MULTIPLIER = 2.0f;

    public bool IsDead => currentHP <= 0;

    public void InitializeEliteStats(int highestPlayerLevel)
    {
        // ATURAN GDD HALAMAN 30: Level Elite = Player Tertinggi + 1
        currentLevel = highestPlayerLevel + 1;
        CalculateStatsForLevel();
        
        currentHP = maxHP;
        currentMP = maxMP;
    }

    private void CalculateStatsForLevel()
    {
        // Formula GDD Halaman 22 & 23
        float hpIncrement = VectorBaseHP * 0.40f;
        maxHP = VectorBaseHP + (hpIncrement * currentLevel);

        float mpIncrement = VectorBaseMP * 0.30f;
        maxMP = VectorBaseMP + (mpIncrement * currentLevel);

        maxDEF = baseDEF + (28 * currentLevel);
        currentDEF = maxDEF;

        maxMDEF = baseMDEF + (40 * currentLevel);
        currentMDEF = maxMDEF;

        // Formula ATK Halaman 24
        float atkIncrement = (baseATK / 0.1f) * 0.05f;
        currentATK = baseATK + (atkIncrement * currentLevel);

        float matkIncrement = (baseMATK / 0.1f) * 0.05f;
        currentMATK = baseMATK + (matkIncrement * currentLevel);
    }

    // FUNGSI BARU: Menghitung damage keluar musuh dengan hitungan CRIT & BALANCING TURN GANDA
    public float GetCalculatedOutputDamage(bool isMagicAttack, bool isDoubleTurn)
    {
        // 1. Ambil base damage serangan
        float rawDamage = isMagicAttack ? currentMATK : currentATK;

        // 2. ATURAN GDD HALAMAN 25: Hitung kocok dadu Critical (50% Chance)
        bool isCritical = Random.value <= CRIT_RATE_CHANCE;
        if (isCritical)
        {
            rawDamage *= CRIT_DMG_MULTIPLIER;
            Debug.Log($"<color=yellow>[CRITICAL HIT!] Serangan Elite menghasilkan Critical 200%!</color>");
        }

        // 3. ATURAN GDD HALAMAN 10: Balancing Turn Ganda (Potong 50% jika berturut-turut)
        if (isDoubleTurn)
        {
            rawDamage /= 2f;
            Debug.Log($"<color=orange>[BALANCING] Elite mendapat Turn Ganda, damage dipotong setengah (50%)</color>");
        }

        return rawDamage;
    }

    public void TakeDamage(float incomingDamage, bool isMagic)
    {
        float finalDamage = incomingDamage;
        if (isMagic) finalDamage -= currentMDEF;
        else finalDamage -= currentDEF;

        finalDamage = Mathf.Max(0, finalDamage);
        currentHP -= finalDamage;

        Debug.Log($"{enemyName} menerima {finalDamage} damage. Sisa HP: {currentHP}");

        if (currentHP <= 0)
        {
            currentHP = 0;
            OnDeath();
        }
    }

    private void OnDeath()
    {
        Debug.Log($"{enemyName} telah dikalahkan!");
        Destroy(gameObject, 0.2f);
    }
}
