using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 2f;      // Kecepatan teks melayang ke atas
    public float destroyTime = 1f;    // Waktu teks hilang (1 detik)
    
    private TextMeshPro textMesh;
    private Color textColor;
    private float timer;

    void Start()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            textColor = textMesh.color;
        }
        
        // Hancurkan objek teks ini otomatis setelah 1 detik
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // 1. Buat teks otomatis bergerak melayang ke atas ruang 3D
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

        // 2. Efek memudar (Fade Out) perlahan sampai hilang
        timer += Time.deltaTime;
        if (textMesh != null)
        {
            // PERBAIKAN: Langsung mengubah nilai alpha (a) pada textColor tanpa .color lagi
            textColor.a = Mathf.Lerp(textColor.a, 0, timer / destroyTime);
            textMesh.color = textColor;
        }

        // 3. Tambahan Billboard Effect: Memaksa teks angka selalu menghadap lurus ke arah layar/kamera kita
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.forward);
        }
    }
}
