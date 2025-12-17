using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target & Speed")]
    [SerializeField] private Transform target; // Drag Player ke sini
    [SerializeField] private float smoothSpeed = 0.125f; // Seberapa cepat kamera mengejar target (damping)
    [SerializeField] private float lookAheadY = 2f; // Seberapa jauh kamera 'melihat' ke atas dari Player
    
    [Header("Vertical Lock")]
    [SerializeField] private float minCameraY = 0f; // Kamera tidak boleh turun di bawah Y=0 (Start Tower)

    private Vector3 currentVelocity;
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // 1. Tentukan Target Posisi
        // Kamera melihat ke target.y + lookAheadY
        Vector3 desiredPosition = new Vector3(
            target.position.x, 
            target.position.y + lookAheadY, 
            transform.position.z
        );

        // 2. Vertical Lock (PENTING!)
        // Kamera TIDAK boleh turun di bawah posisi Y minimum yang ditentukan (atau posisi Y kamera saat ini).
        float targetY = desiredPosition.y;
        
        // Kamera hanya bergerak ke Y jika Player di atas minCameraY DAN di atas posisi kamera saat ini
        if (targetY < minCameraY)
        {
            targetY = minCameraY;
        }
        else if (targetY < transform.position.y)
        {
            // Jika Player jatuh, kamera tidak turun (kecuali sampai minCameraY)
            targetY = transform.position.y;
        }

        desiredPosition.y = targetY;


        // 3. Smooth Damping
        // Gunakan SmoothDamp untuk pergerakan yang mulus
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position, 
            desiredPosition, 
            ref currentVelocity, 
            smoothSpeed
        );

        transform.position = smoothedPosition;
    }
}