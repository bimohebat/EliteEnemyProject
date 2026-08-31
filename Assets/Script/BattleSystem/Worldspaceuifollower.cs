using UnityEngine;

/// <summary>
/// Membuat sebuah RectTransform UI (Screen Space - Overlay) terus mengikuti
/// posisi sebuah Transform di scene (world space), dengan offset ke atas.
/// Dipakai untuk nama tag / HP bar yang "mengambang" di atas kepala karakter.
///
/// Attach ke root GameObject dari prefab UI (misal panel kecil berisi Text/Slider),
/// pastikan Canvas induknya bertipe "Screen Space - Overlay".
/// </summary>
public class WorldSpaceUIFollower : MonoBehaviour
{
    [Header("Target yang diikuti")]
    public Transform target;

    [Header("Offset di atas target (dalam world unit)")]
    public Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

    private RectTransform rectTransform;
    private Camera mainCamera;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 worldPos = target.position + worldOffset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        // Kalau target di belakang kamera, sembunyikan supaya tidak "terbalik" di layar.
        bool isBehindCamera = screenPos.z < 0f;
        if (rectTransform != null)
        {
            gameObject.SetActive(!isBehindCamera);
            if (!isBehindCamera)
                rectTransform.position = screenPos;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}