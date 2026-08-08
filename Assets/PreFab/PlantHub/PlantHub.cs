using UnityEngine;

public class PlantHub : MonoBehaviour
{
    [Header("Üretim Ayarları")]
    public GameObject foodPrefab; 
    
    // YENİ: Bitkinin kendi gövdesine (merkeze) yemek düşmemesi için minimum mesafe!
    public float minSpawnRadius = 0.8f; 
    public float maxSpawnRadius = 2.5f; 
    public float spawnInterval = 5f; 
    public int maxFoodLimit = 5; 

    [Header("Çakışma Önleme (Overlap) Ayarları")]
    // YENİ: Otun boyutu kadar bir alanı tarayacağız ki başka otun üstüne binmesin
    public float foodCheckRadius = 0.3f; 
    // YENİ: Boş yer bulmak için kaç kere şansını deneyecek? (Oyun kilitlenmesin diye sınır koyuyoruz)
    public int maxSpawnAttempts = 10; 

    [Header("Yaşam Süresi")]
    public float minLifespan = 30f;
    public float maxLifespan = 60f;
    
    private float lifeTimer;
    private float spawnTimer;

    void Start()
    {
        lifeTimer = Random.Range(minLifespan, maxLifespan);
        spawnTimer = 0f; 
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnFood();
            spawnTimer = spawnInterval; 
        }

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Die();
        }
    }

    void SpawnFood()
    {
        // 1. ETRAFTAKİ YEMEK SAYISINI KONTROL ET (Maksimum limite ulaştı mı?)
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, maxSpawnRadius);
        int currentFoodCount = 0;

        foreach (var hit in hitColliders)
        {
            if (hit.GetComponent<FoodObject>() != null)
            {
                currentFoodCount++;
            }
        }

        // Limit doluysa hiç yer arama, üretimi iptal et
        if (currentFoodCount >= maxFoodLimit) return; 

        // 2. BOŞ VE GÜVENLİ POZİSYON BULMA 
        Vector2 validSpawnPosition = Vector2.zero;
        bool positionFound = false;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized; 
            float randomDistance = Random.Range(minSpawnRadius, maxSpawnRadius);
            
            Vector2 attemptPosition = (Vector2)transform.position + (randomDirection * randomDistance);

            // 🌟 YENİ FİX: HARİTA SINIRI KONTROLÜ 🌟
            if (EcosystemManager.instance != null)
            {
                // Eğer fırlatılacak yemek haritanın min/max sınırlarının dışına çıkıyorsa...
                if (attemptPosition.x < EcosystemManager.instance.minX || attemptPosition.x > EcosystemManager.instance.maxX ||
                    attemptPosition.y < EcosystemManager.instance.minY || attemptPosition.y > EcosystemManager.instance.maxY)
                {
                    continue; // Bu denemeyi pas geç, iptal et! Döngü başa dönecek ve yemeği başka yöne fırlatmayı deneyecek.
                }
            }

            // O hedef noktaya görünmez bir dedektör çemberi at. Başka bir objeye çarpıyor mu?
            Collider2D overlappingCollider = Physics2D.OverlapCircle(attemptPosition, foodCheckRadius);

            // Eğer dedektör hiçbir şeye çarpmadıysa (nokta tamamen boşsa)
            if (overlappingCollider == null) 
            {
                validSpawnPosition = attemptPosition;
                positionFound = true;
                break; 
            }
        }

        // 3. EĞER GÜVENLİ VE BOŞ BİR YER BULUNDUYSA ÜRET
        if (positionFound && foodPrefab != null)
        {
            Instantiate(foodPrefab, validSpawnPosition, Quaternion.identity);
        }
        else
        {
            // Etraf tamamen otlarla (veya canlılarla) doluysa pas geçer, diğer döngüde tekrar dener
            Debug.Log("Bitki etrafında boş yer bulamadı, spawn iptal edildi.");
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

    // Editor'de güvenli alanı (donut şeklini) görmek için
    void OnDrawGizmosSelected()
    {
        // Gövdenin içi (Buraya yemek DÜŞEMEZ)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minSpawnRadius);

        // Üretim Sınırı (Yemekler Kırmızı ve Yeşil çizgi arasına düşer)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, maxSpawnRadius);
    }

    void OnDestroy()
    {
        if (EcosystemManager.instance != null && EcosystemManager.instance.allLivingPlants.Contains(this))
        {
            EcosystemManager.instance.allLivingPlants.Remove(this);
        }
    }
}