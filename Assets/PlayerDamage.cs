using UnityEngine;
using UnityEngine.InputSystem; // WAJIB ditambahkan untuk menggunakan Input System baru

public class PlayerDamage : MonoBehaviour
{
    [Header("Attack Settings")]
    public Transform attackPoint;      // Objek kosong di depan Player sebagai titik pusat serangan
    public float attackRange = 1.5f;   // Radius jarak serang (3D Sphere)
    public float attackDamage = 25f;   // Besar damage per pukulan
    public LayerMask enemyLayers;      // Pilih Layer "Enemy" di Inspector

          void Update()
    {
        // Alternatif deteksi input baru yang jauh lebih responsif di Unity 2022 ke atas
        
        // 1. Cek Klik Kiri Mouse
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Debug.Log("Pemicu Input: Klik Kiri Terdeteksi!");
                Attack();
            }
        }
        
        // 2. Cek Tombol Space Keyboard
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Debug.Log("Pemicu Input: Tombol Space Terdeteksi!");
                Attack();
            }
        }
    }



    void Attack()
    {
        // Menggunakan sistem 3D OverlapSphere untuk mendeteksi Collider 3D musuh
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider enemy in hitEnemies)
        {
            // Mengambil komponen skrip 3D musuh
            EliteEldritchHunter eliteEnemy = enemy.GetComponent<EliteEldritchHunter>();
            
            if (eliteEnemy != null)
            {
                eliteEnemy.TakeDamage(attackDamage);
            }
        }
    }

    // Menampilkan visual bola area serang di jendela Scene editor
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
