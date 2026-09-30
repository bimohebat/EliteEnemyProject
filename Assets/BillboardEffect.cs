using UnityEngine;

public class BillboardEffect : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        // Mencari kamera utama di dalam map 3D saat game dimulai
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // Memaksa orientasi UI Slider untuk selalu menghadap lurus ke arah kamera
            transform.LookAt(transform.position + mainCameraTransform.forward);
        }
    }
}
