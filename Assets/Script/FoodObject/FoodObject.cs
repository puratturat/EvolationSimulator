using UnityEngine;

public class FoodObject : MonoBehaviour
{
    [Header("Yiyecek Verisi (DNA'sı)")]
    public FoodData foodData;

    [Header("Anlık Değerler")]
    public FoodType type;
    public float currentEnergy;
    
    private float spoilTimer;

    // 🌟 YENİ: GÜVENLİK KİLİDİ 🌟
    // İki canlının aynı anda yemesini (klonlama hatasını) engeller
    private bool isConsumed = false; 

    void Start()
    {
        if (foodData != null)
        {
            type = foodData.foodType;

            float minEnergy = foodData.baseEnergyRestore * 0.9f;
            float maxEnergy = foodData.baseEnergyRestore * 1.1f;
            currentEnergy = Random.Range(minEnergy, maxEnergy);

            float scaleRatio = currentEnergy / foodData.baseEnergyRestore;
            transform.localScale = new Vector3(scaleRatio, scaleRatio, scaleRatio);
            
            spoilTimer = Random.Range(foodData.minSpoilTime, foodData.maxSpoilTime);
        }
        else
        {
            Debug.LogWarning(gameObject.name + " objesinde FoodData eksik!");
        }

        // Eğer EcosystemManager listesi kullanıyorsan buraya listeye ekleme kodunu koyabilirsin
    }

    void Update()
    {
        spoilTimer -= Time.deltaTime;
        
        if (spoilTimer <= 0)
        {
            Spoil();
        }
    }

    void Spoil()
    {
        Destroy(gameObject);
    }

    // 🌟 FİX: Tüketme Fonksiyonu Güvenli Hale Getirildi
    public float Consume()
    {
        // Eğer benden saniyeler önce başka bir canlı bu fonksiyonu tetiklediyse (isConsumed true ise)
        // Ona "0" enerji döndür (Yani avucunu yalasın!)
        if (isConsumed) return 0f; 

        // Eğer ilk yiyen oysa kilidi kapat, kendini yok et ve enerjini gönder.
        isConsumed = true;
        Destroy(gameObject);
        return currentEnergy;
    }

    // Eğer Ekosistem Yöneticisine bağlıysan silinirken kendini listeden çıkarmayı unutma:
    /*
    void OnDestroy()
    {
        if (EcosystemManager.instance != null && EcosystemManager.instance.allLivingPlants.Contains(this))
            EcosystemManager.instance.allLivingPlants.Remove(this);
    }
    */
}