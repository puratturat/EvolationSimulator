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

    [Header("Doğa Ayarları (Bitki Üretimi)")]
    // 🌟 DEĞİŞTİ: Artık iki farklı Prefab'ımız var
    public GameObject normalPlantPrefab; 
    public GameObject poisonousPlantPrefab; 
    
    public float spawnInterval = 10f; 
    public int maxTotalPlantLimit = 20; // 🌟 DEĞİŞTİ: Haritadaki "Toplam" ağaç sınırı

    // 🌟 YENİ: Zehirli ağacın nadirliğini belirleyen şans zarı (Örn: %15)
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
        // 🌟 FİX: Oyun başlarken şans oranına göre kaç tane zehirli, kaç tane normal atacağını hesaplar
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

        if (populationText != null)
        {
            uiUpdateTimer -= Time.deltaTime;
            if (uiUpdateTimer <= 0f)
            {
                UpdatePopulationCount();
                uiUpdateTimer = 0.5f; 
            }
        }
    }

    void UpdatePopulationCount()
    {
        // 🌟 KESİN ÇÖZÜM: Listede bir nedenden dolayı "Hayalet (Null)" kalmış objeler varsa, sayım yapmadan önce onları listeden temizle!
        allLivingCreatures.RemoveAll(item => item == null);
        
        populationText.text = "Leater Popülasyonu: " + allLivingCreatures.Count;
    }

    void CheckAndSpawnPlant()
    {
        if (allLivingPlants.Count >= maxTotalPlantLimit)
        {
            return; 
        }

        // 🌟 YENİ MANTIK: Hangi ağacın üretileceğini şans zarı belirler!
        GameObject prefabToSpawn = normalPlantPrefab; // Varsayılan olarak normal üret
        if (Random.Range(0, 100) < poisonSpawnChance)
        {
            prefabToSpawn = poisonousPlantPrefab; // Zar tutarsa zehirli yap!
        }

        SpawnSinglePlant(prefabToSpawn);
    }


    void SpawnSinglePlant(GameObject prefab)
    {
        // 🌟 FİX: Ağaçların tam sınırda çıkmasını engellemek için 2 birimlik güvenlik payı (margin) bırakıyoruz
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