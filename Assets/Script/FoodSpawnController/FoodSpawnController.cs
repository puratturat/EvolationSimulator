using UnityEngine;
using UnityEngine.EventSystems;

public class FoodSpawnController : MonoBehaviour
{
    [Header("Arayüz (UI) Referansları")]
        public GameObject spawnPanel; // Açılıp kapanacak olan alt panel

        [Header("Yaratılacak Objeler (Prefablar)")]
        public GameObject otPrefab;
        public GameObject etPrefab;

        // Aklımızda tutacağımız "Şu an farenin ucunda hangi yemek var?" bilgisi
        private GameObject selectedFoodToSpawn; 

        // Unity'de Cursor (İmleç) görsellerini değiştirmek için (İleride ekleyeceğiz)
        // public Texture2D otCursor;
        // public Texture2D etCursor;

        void Start()
        {
            spawnPanel.SetActive(false);
            selectedFoodToSpawn = null;
        }

        void Update()
        {
            // Eğer elimizde (faremizin ucunda) yaratılacak bir yemek varsa ve sol tıka bastıysak
            if (selectedFoodToSpawn != null && Input.GetMouseButtonDown(0))
            {
                // ÖNEMLİ: Eğer fare o an bir UI butonunun (örneğin İptal butonu) üzerindeyse spawnlama yapma!
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    SpawnFood();
                }
            }
        }

        void SpawnFood()
        {
            // Farenin ekran üzerindeki konumunu, oyun dünyasındaki konuma çevir
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // Seçilen yemeği, farenin tam ucunda yarat!
            Instantiate(selectedFoodToSpawn, mousePosition, Quaternion.identity);
        }

        // --- AŞAĞIDAKİ METOTLARI BUTONLARA BAĞLAYACAĞIZ ---

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

        public void SelectOt()
        {
            selectedFoodToSpawn = otPrefab;
            //Debug.Log("Ot seçildi! Tıkladığın yere ot eklenecek.");
            // Cursor.SetCursor(otCursor, Vector2.zero, CursorMode.Auto); // İmleç değişimi (Görseli ekleyince açarız)
        }

        public void SelectEt()
        {
            selectedFoodToSpawn = etPrefab;
            //Debug.Log("Et seçildi! Tıkladığın yere et eklenecek.");
            // Cursor.SetCursor(etCursor, Vector2.zero, CursorMode.Auto); 
        }

        public void CancelSelection()
        {
            selectedFoodToSpawn = null;
            //Debug.Log("Seçim iptal edildi. İmleç normale döndü.");
            // Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // İmleci varsayılana döndür
        }
}
