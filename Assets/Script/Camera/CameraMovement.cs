using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private Vector3 cameraPosition;
    private Camera cam;

    [Header("Hareket Ayarları")]
    public float baseCameraSpeed = 2f; 
    public float movementSmoothTime = 5f; 

    [Header("Zoom Ayarları")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;   
    public float maxZoom = 25f;  
    public float zoomSmoothTime = 8f; 

    private float targetZoom; 

    void Start()
    {
        cameraPosition = this.transform.position;
        cam = GetComponent<Camera>();
        
        if (cam != null)
        {
            targetZoom = cam.orthographicSize;
        }
    }

    void Update()
    {
        if (cam == null) return;

        // ==========================================
        // 1. PÜRÜZSÜZ ZOOM (SMOOTH ZOOM)
        // ==========================================
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            targetZoom -= scroll * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        // 🌟 FİX: Time.deltaTime yerine Time.unscaledDeltaTime kullanıyoruz
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.unscaledDeltaTime * zoomSmoothTime);


        // ==========================================
        // 2. DİNAMİK HIZ HESAPLAMASI (ZOOM-BASED SPEED)
        // ==========================================
        float currentDynamicSpeed = baseCameraSpeed * cam.orthographicSize;


        // ==========================================
        // 3. KİLİTLENME VE MANUEL HAREKET MODU
        // ==========================================
        if (DebugController.instance != null && DebugController.instance.selectedCreature != null)
        {
            // Takip Modu
            Vector3 targetPosition = DebugController.instance.selectedCreature.transform.position;
            targetPosition.z = this.transform.position.z; 
            
            // 🌟 FİX: Time.deltaTime yerine Time.unscaledDeltaTime
            this.transform.position = Vector3.Lerp(this.transform.position, targetPosition, Time.unscaledDeltaTime * movementSmoothTime);
            cameraPosition = this.transform.position;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
            {
                DebugController.instance.Deselect();
            }
        }
        else
        {
            // Serbest Hareket Modu
            // 🌟 FİX: Tüm hareket kodlarında Time.unscaledDeltaTime kullanıldı
            if (Input.GetKey(KeyCode.W)) cameraPosition.y += currentDynamicSpeed * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.S)) cameraPosition.y -= currentDynamicSpeed * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.A)) cameraPosition.x -= currentDynamicSpeed * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.D)) cameraPosition.x += currentDynamicSpeed * Time.unscaledDeltaTime;

            // ==========================================
            // 4. KAMERA HARİTA SINIRLAMASI (CAMERA BOUNDS)
            // ==========================================
            if (EcosystemManager.instance != null)
            {
                cameraPosition.x = Mathf.Clamp(cameraPosition.x, EcosystemManager.instance.minX, EcosystemManager.instance.maxX);
                cameraPosition.y = Mathf.Clamp(cameraPosition.y, EcosystemManager.instance.minY, EcosystemManager.instance.maxY);
            }

            // 🌟 FİX: Time.deltaTime yerine Time.unscaledDeltaTime
            this.transform.position = Vector3.Lerp(this.transform.position, cameraPosition, Time.unscaledDeltaTime * movementSmoothTime);
        }
    }
}