using UnityEngine;
using TMPro; 
using System.Collections.Generic;

public class EcosystemManager : MonoBehaviour
{
    public static EcosystemManager instance;

    [Header("Arayüz (UI) Referansları")]
    public TextMeshProUGUI populationText; 
    private float uiUpdateTimer = 0f; 

    // Kayıt Defterleri
    public List<CreatureStats> allLivingCreatures = new List<CreatureStats>();
    public List<PlantHub> allLivingPlants = new List<PlantHub>();

    [Header("Kıyamet Protokolü (Oto Yeniden Doğuş)")]
    public GameObject leaterPrefab; // 🌟 YENİ: Doğurulacak canlı Prefab'ını buraya sürükle!
    public bool autoRespawnEnabled = true; // 🌟 YENİ: İleride Butonla değiştireceğin Tick
    public int respawnAmount = 100; // 🌟 YENİ: Kaç tane doğacak?
    private bool hasLifeEverExisted = false; // Sistem oyuna ilk canlının girmesini bekler

    [Header("Doğa Ayarları (Bitki Üretimi)")]
    public GameObject normalPlantPrefab; 
    public GameObject poisonousPlantPrefab; 
    
    public float spawnInterval = 10f; 
    public int maxTotalPlantLimit = 20; 

    [Range(0, 100)] public int poisonSpawnChance = 15; 

    [Header("Dünya Sınırları (Harita Büyüklüğü)")]
    public float minX = -15f;
    public float maxX = 15f;
    public float minY = -10f;
    public float maxY = 10f;

    private float timer;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        int startingPoisonous = Mathf.RoundToInt(maxTotalPlantLimit * (poisonSpawnChance / 100f));
        int startingNormal = maxTotalPlantLimit - startingPoisonous;

        for (int i = 0; i < startingNormal; i++) SpawnSinglePlant(normalPlantPrefab);
        for (int i = 0; i < startingPoisonous; i++) SpawnSinglePlant(poisonousPlantPrefab);

        timer = spawnInterval; 
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            CheckAndSpawnPlant();
            timer = spawnInterval; 
        }

        uiUpdateTimer -= Time.deltaTime;
        if (uiUpdateTimer <= 0f)
        {
            UpdatePopulationCount();
            uiUpdateTimer = 0.5f;
        }
    }

    void UpdatePopulationCount()
    {
        // Hayalet (Null) kalmış objeleri temizle!
        allLivingCreatures.RemoveAll(item => item == null);
        
        // 🌟 KIYAMET PROTOKOLÜ KONTROLÜ 🌟
        if (allLivingCreatures.Count > 0)
        {
            // Ekranda en az 1 canlı varsa, hayat başlamış demektir.
            hasLifeEverExisted = true;
        }
        else if (hasLifeEverExisted && autoRespawnEnabled)
        {
            // Eğer hayat daha önce başladıysa ama şu an 0 canlı varsa ve sistem açıksa, KIVILCIMI ÇAK!
            TriggerMassRespawn();
            hasLifeEverExisted = false; // Tekrar döngüye girmemesi için sıfırlıyoruz, yeni canlılar listeye girince tekrar true olacak.
        }

        if (populationText != null)
        {
            populationText.text = "Leater Popülasyonu: " + allLivingCreatures.Count;
        }
    }

    // 🌟 YENİ: Haritaya rastgele dağılmış 100 canlı fırlatma motoru
    public void TriggerMassRespawn()
    {
        if (leaterPrefab == null)
        {
            Debug.LogWarning("Kıyamet Protokolü çalıştı ama Leater Prefab'ı atanmamış!");
            return;
        }

        float margin = 3f; // Sınırlardan biraz içeride doğsunlar
        for (int i = 0; i < respawnAmount; i++)
        {
            float randomX = Random.Range(minX + margin, maxX - margin);
            float randomY = Random.Range(minY + margin, maxY - margin);
            
            Vector2 randomPosition = new Vector2(randomX, randomY);
            Instantiate(leaterPrefab, randomPosition, Quaternion.identity);
        }
    }

    void CheckAndSpawnPlant()
    {
        if (allLivingPlants.Count >= maxTotalPlantLimit)
        {
            return; 
        }

        GameObject prefabToSpawn = normalPlantPrefab; 
        if (Random.Range(0, 100) < poisonSpawnChance)
        {
            prefabToSpawn = poisonousPlantPrefab; 
        }

        SpawnSinglePlant(prefabToSpawn);
    }

    void SpawnSinglePlant(GameObject prefab)
    {
        float margin = 5f; 
        float randomX = Random.Range(minX + margin, maxX - margin);
        float randomY = Random.Range(minY + margin, maxY - margin);
        
        Vector2 randomPosition = new Vector2(randomX, randomY);

        if (prefab != null)
        {
            GameObject newPlant = Instantiate(prefab, randomPosition, Quaternion.identity);
            PlantHub plantScript = newPlant.GetComponentInChildren<PlantHub>();
            
            if (plantScript != null && !allLivingPlants.Contains(plantScript))
            {
                allLivingPlants.Add(plantScript);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((minX + maxX) / 2, (minY + maxY) / 2, 0);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0);
        Gizmos.DrawWireCube(center, size);
    }
}
