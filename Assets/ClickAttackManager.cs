using UnityEngine;
using UnityEngine.InputSystem;

public class ClickAttackManager : MonoBehaviour
{
    [Header("Click Settings")]
    public float clickDamage = 25f;
    public LayerMask clickLayer; 

    [Header("Damage Text Visual")]
    public GameObject damageTextPrefab;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Tetap aktifkan fitur klik kiri langsung ke tubuh musuh
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            ExecuteRaycastAttack(mousePosition);
        }
    }

    // 1. Logika Serangan jika mengklik langsung tubuh musuh di layar
    void ExecuteRaycastAttack(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickLayer))
        {
            EliteEldritchHunter targetMusuh = hit.collider.GetComponent<EliteEldritchHunter>();

            if (targetMusuh == null && hit.collider.CompareTag("WeakSpot"))
            {
                targetMusuh = hit.collider.GetComponentInParent<EliteEldritchHunter>();
            }

            if (targetMusuh != null)
            {
                ApplyDamageToEnemy(targetMusuh, hit.point);
            }
        }
    }

    // 2. FUNGSI BARU UNTUK TOMBOL UI: Otomatis menyerang musuh tanpa meleset karena tombol
       public void TriggerAttackFromButton()
    {
        // Alternatif pencarian objek yang lebih aman untuk segala jenis compiler Unity
        EliteEldritchHunter targetMusuh = GameObject.FindFirstObjectByType<EliteEldritchHunter>();

        if (targetMusuh != null)
        {
            Vector3 spawnPosition = targetMusuh.transform.position;
            ApplyDamageToEnemy(targetMusuh, spawnPosition);
        }
    }


    // Logika pembagian pemberian damage dan efek angka melayang
    void ApplyDamageToEnemy(EliteEldritchHunter enemy, Vector3 spawnPos)
    {
        enemy.TakeDamage(clickDamage);
        
        if (damageTextPrefab != null)
        {
            GameObject textObj = Instantiate(damageTextPrefab, spawnPos + new Vector3(0, 1f, -0.5f), Quaternion.identity);
            TMPro.TMP_Text textMesh = textObj.GetComponent<TMPro.TMP_Text>();
            if (textMesh != null)
            {
                textMesh.text = enemy.currentPhase == 1 ? $"-{clickDamage}" : "HIT!";
            }
        }
    }
}
