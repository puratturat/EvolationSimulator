using UnityEngine;
using UnityEngine.EventSystems;

public class CreatureSpawnController : MonoBehaviour
{
    public static CreatureSpawnController instance;
    [Header("Arayüz (UI) Referansları")]
        public GameObject spawnPanel; // Açılıp kapanacak olan alt panel

        [Header("Yaratılacak Objeler (Prefablar)")]
        public GameObject LeaterPrefab;

        
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
            
            if (selectedCreatureToSpawn != null && Input.GetMouseButtonDown(0))
            {
                
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    SpawnCreature();
                }
            }
        }

        void SpawnCreature()
        {
            
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            Instantiate(selectedCreatureToSpawn, mousePosition, Quaternion.identity);
        }

        public void ToggleSpawnPanel()
        {
            // Eğer panel açıksa kapat, kapalıysa aç (Menü butonuna basınca çalışacak)
            spawnPanel.SetActive(!spawnPanel.activeSelf);
            
            // Paneli kapatıyorsak, faredeki yemeği de iptal et
            if (!spawnPanel.activeSelf)
            {
                CancelSelection();
            }
        }

        public void SelectLeater()
        {
            selectedCreatureToSpawn = LeaterPrefab;
        }

        public void CancelSelection()
        {
            selectedCreatureToSpawn = null;
            //Debug.Log("Seçim iptal edildi. İmleç normale döndü.");
            // Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // İmleci varsayılana döndür
        }

        public bool IsSpawning()
        {
            return selectedCreatureToSpawn != null;
        }
}
