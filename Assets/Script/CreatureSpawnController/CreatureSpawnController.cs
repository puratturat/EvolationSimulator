using UnityEngine;
using UnityEngine.EventSystems;

public class CreatureSpawnController : MonoBehaviour
{
    public static CreatureSpawnController instance;
    
    [Header("Arayüz (UI) Referansları")]
    public GameObject spawnPanel; // Açılıp kapanacak olan alt panel

    [Header("Yaratılacak Objeler (Prefablar)")]
    public GameObject LeaterPrefab;
    public GameObject PretorPrefab;
    
    private GameObject selectedCreatureToSpawn; 

    // Unity'de Cursor (İmleç) görsellerini değiştirmek için (İleride ekleyeceğiz)
    // public Texture2D otCursor;
    // public Texture2D etCursor;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        spawnPanel.SetActive(false);
        selectedCreatureToSpawn = null;
    }

    void Update()
    {
        // Eğer bir yaratık seçiliyse ve farenin sol tuşuna basıldıysa
        if (selectedCreatureToSpawn != null && Input.GetMouseButtonDown(0))
        {
            // Tıklanılan yer bir UI (Buton, Panel vb.) elemanı değilse spawnla
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                SpawnCreature();
            }
        }
    }

    void SpawnCreature()
    {
        if (Camera.main == null) return;

        // Farenin ekrandaki pozisyonunu oyun dünyasındaki (2D) pozisyona çevir
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // Seçili olan yaratığı farenin olduğu yerde yarat
        Instantiate(selectedCreatureToSpawn, mousePosition, Quaternion.identity);
    }

    public void ToggleSpawnPanel()
    {
        // Eğer panel açıksa kapat, kapalıysa aç
        spawnPanel.SetActive(!spawnPanel.activeSelf);
        
        // Paneli kapatıyorsak, faredeki seçili yaratığı da iptal et
        if (!spawnPanel.activeSelf)
        {
            CancelSelection();
        }
    }

 
    public void SelectLeater()
    { 
        if (FoodSpawnController.instance != null) FoodSpawnController.instance.CancelSelection();
        selectedCreatureToSpawn = LeaterPrefab;
        Debug.Log("Leater seçildi! Ekrana tıklayarak spawnlayabilirsin.");
    }

    public void SelectPretor()
    {
        if (FoodSpawnController.instance != null) FoodSpawnController.instance.CancelSelection();
        selectedCreatureToSpawn = PretorPrefab;
        Debug.Log("Pretor seçildi! Ekrana tıklayarak spawnlayabilirsin.");
    }

    // ---------------------------------------

    public void CancelSelection()
    {
        selectedCreatureToSpawn = null;
        Debug.Log("Seçim iptal edildi. Artık boş tıklıyorsun.");
        // Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // İmleci varsayılana döndür
    }

    public bool IsSpawning()
    {
        return selectedCreatureToSpawn != null;
    }
}
