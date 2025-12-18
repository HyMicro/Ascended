using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target & Speed")]
    [SerializeField] private Transform target; 
    [SerializeField] private float smoothSpeed = 0.125f; 
    [SerializeField] private float lookAheadY = 2f; 
    
    [Header("Vertical Lock Settings")]
    [SerializeField] private float minCameraY = 0f; 
    [SerializeField] private bool allowFallingCamera = false; // Set ke true kalau mau kamera ngikut pas jatuh biasa
    [SerializeField] private float teleportThreshold = 10f; // Jarak jauh untuk anggap player teleport/jatuh parah

    private Vector3 currentVelocity;
    private float lastTargetY;

    void Start()
    {
        if (target != null)
        {
            // Set posisi awal kamera pas start
            transform.position = new Vector3(target.position.x, target.position.y + lookAheadY, transform.position.z);
            lastTargetY = target.position.y;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        
        // 1. Tentukan Target Posisi
        Vector3 desiredPosition = new Vector3(
            target.position.x, 
            target.position.y + lookAheadY, 
            transform.position.z
        );

        float targetY = desiredPosition.y;

        // 2. Logika Deteksi Teleport / Jatuh Parah (Fall Consequence)
        // Kalau jarak player sekarang ama posisi terakhir jauh banget (melebihi threshold), 
        // kita paksa kamera buat ikut turun instan atau sangat cepat.
        float distanceMoved = Mathf.Abs(target.position.y - lastTargetY);
        
        if (distanceMoved > teleportThreshold)
        {
            // Player baru aja teleport/jatuh ke biome bawah
            // Kita reset posisi kamera supaya nggak nahan di atas
            Vector3 teleportPos = new Vector3(transform.position.x, targetY, transform.position.z);
            transform.position = teleportPos; 
            currentVelocity = Vector3.zero; // Reset momentum kamera
        }
        else
        {
            // 3. Logika Vertical Lock Normal
            if (targetY < minCameraY)
            {
                targetY = minCameraY;
            }
            else if (!allowFallingCamera && targetY < transform.position.y)
            {
                // Tetap nahan di atas kalau jatuh-jatuh kecil biasa biar nggak janky
                targetY = transform.position.y;
            }
        }

        desiredPosition.y = targetY;
        lastTargetY = target.position.y; // Simpan posisi buat cek frame depan

        // 4. Smooth Damping
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            desiredPosition, 
            ref currentVelocity, 
            smoothSpeed
        );
    }
}