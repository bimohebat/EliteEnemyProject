using UnityEngine;

namespace DaengSaka.BattleSystem
{
    /// <summary>
    /// Komponen MonoBehaviour untuk menampung data statistik karakter/musuh di Unity.
    /// Mengimplementasikan IBattleUnit agar bisa diproses oleh BattleCalculation.
    /// </summary>
    public class CharacterUnit : MonoBehaviour, IBattleUnit
    {
        [Header("Unit Information")]
        [SerializeField] private string unitName = "Hero";
        [SerializeField] private int maxHealth = 100;
        private int currentHealth;

        [Header("Battle Stats")]
        [Tooltip("Kekuatan serangan dasar")]
        [SerializeField] private float attack = 25f;

        [Tooltip("Ketahanan pertahanan terhadap serangan")]
        [SerializeField] private float defense = 10f;

        [Tooltip("Kecepatan menentukan urutan giliran bertindak")]
        [SerializeField] private float speed = 15f;

        [Header("Critical Stats")]
        [Tooltip("Peluang serangan kritikal dalam persen (0 - 100)")]
        [Range(0f, 100f)]
        [SerializeField] private float criticalRate = 20f;

        [Tooltip("Pengali damage saat kritikal terjadi (misal 1.5 = +50% damage)")]
        [SerializeField] private float criticalDamage = 1.5f;

        #region IBattleUnit Implementation

        public string UnitName => unitName;
        public float Attack => attack;
        public float Defense => defense;
        public float Speed => speed;
        public float CriticalRate => criticalRate;
        public float CriticalDamage => criticalDamage;

        #endregion

        #region Health Management

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => currentHealth <= 0;

        private void Awake()
        {
            // Inisialisasi HP awal saat game mulai
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Menerima damage dari hasil kalkulasi pertarungan dan mengurangi HP unit.
        /// </summary>
        public void TakeDamage(int damage)
        {
            currentHealth = Mathf.Max(0, currentHealth - damage);
            Debug.Log($"[{unitName}] Menerima {damage} Damage! Sisa HP: {currentHealth}/{maxHealth}");

            if (IsDead)
            {
                OnDeath();
            }
        }

        private void OnDeath()
        {
            Debug.Log($"[{unitName}] Telah Kalahkan!");
            // Tambahkan logika animasi/kematian di sini
        }

        #endregion
    }
}