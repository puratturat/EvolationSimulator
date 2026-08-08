using UnityEngine;

public enum LifeStage { Young, Adult, Old }

public class CreatureStats : MonoBehaviour
{
    [Header("Genetik Veri")]
    public CreatureData dna; 

    [Header("Soy Ağacı")]
    public int generation = 1; 

    [Header("Anlık Değerler (Gözlem İçin)")]
    public float currentHealth;
    public float currentEnergy;
    public float currentSpeed;
    public string currentStateName = "Doğuyor...";
    public bool isMoving; 

    [Header("Büyüme ve Yaş Sistemi")]
    public int age = 0;
    public float growthProgress = 0f; 
    public LifeStage currentStage = LifeStage.Young;

    public float currentMaxHealth;
    public float currentMaxEnergy;
    public float ageEnergyDrainMultiplier = 1f; 

    [Header("Açlık ve Ölüm Ayarları")]
    public float starvationDamage = 5f; 
    public GameObject meatPrefab; // Öldüğünde yere düşecek olan et objesi

    // 🌟 1. KRİTİK FİX: DNA FOTOKOPİSİ (Bunu eklemezsek tüm türler aynı anda mutasyon geçirir!)
    void Awake()
    {
        if (dna != null)
        {
            dna = Instantiate(dna); 
            dna.name = "DNA_Gen_" + generation;
        }
    }

    void Start()
    {
        if (dna != null)
        {
            // 🌟 2. KRİTİK FİX: Evrimleşen Renk Genini canlının fiziksel bedenine uygula
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = dna.skinColor;

            UpdateLifeStage(); 
            currentHealth = currentMaxHealth;
            currentEnergy = currentMaxEnergy;
            currentSpeed = dna.moveSpeed;
        }
        else
        {
            Debug.LogWarning(gameObject.name + " objesinin DNA'sı (CreatureData) atanmamış!");
        }

        if (EcosystemManager.instance != null)
        {
            EcosystemManager.instance.allLivingCreatures.Add(this);
        }
    }

    void Update()
        {
            if (dna == null) return; 

            if (currentEnergy > 0)
            {
                currentSpeed = dna.moveSpeed; 
                float metabolismTax = dna.visionRadius * dna.visionEnergyTax;

                // 1. Standart Enerji Harcaması (Metabolizma)
                if (isMoving) currentEnergy -= ((dna.moveEnergyDrain * dna.moveSpeed) + metabolismTax) * ageEnergyDrainMultiplier * Time.deltaTime;
                else currentEnergy -= (dna.idleEnergyDrain + metabolismTax) * ageEnergyDrainMultiplier * Time.deltaTime;

                // 🌟 2. YENİ: CAN YENİLEME SİSTEMİ 🌟
                if (currentHealth < currentMaxHealth)
                {
                    // Saniyede ne kadar can iyileşecek?
                    float healAmount = dna.healingRate * Time.deltaTime;
                    
                    // İyileşme enerjiden ne kadar yiyecek? (1 birim can = healingEnergyCost)
                    float energyCostForHeal = healAmount * dna.healingEnergyCost;

                    // Eğer yeterli enerjisi varsa canı yenile
                    if (currentEnergy >= energyCostForHeal)
                    {
                        currentHealth += healAmount;
                        currentEnergy -= energyCostForHeal;
                    }
                    else 
                    {
                        // Enerjisi bitmek üzereyse, sadece elinde kalan enerji kadar ufak bir iyileşme yaşasın
                        float affordableHeal = currentEnergy / dna.healingEnergyCost;
                        currentHealth += affordableHeal;
                        currentEnergy = 0f; // Tüm enerjisini iyileşmeye harcayıp bitirdi
                    }
                    
                    // Sınır güvenliği: Canı, maksimumu aşmasın
                    if (currentHealth > currentMaxHealth) currentHealth = currentMaxHealth;
                }
            }
            else
            {
                currentEnergy = 0f; 
                currentSpeed = dna.moveSpeed / 2f; 
                currentHealth -= starvationDamage * Time.deltaTime; 
            }

            if (currentHealth <= 0)
            {
                currentHealth = 0f;
                Die();
            }
        }

    // 🌟 ARTIK TAM DİNAMİK: Canlı ne tür yemek yediyse, onun verimliliği (activeEfficiency) buraya gelir!
    public void AddGrowth(float foodValue, float activeEfficiency)
    {
        growthProgress += foodValue * activeEfficiency;

        if (growthProgress >= 100f)
        {
            growthProgress -= 100f; 
            age++;
            UpdateLifeStage(); 

            currentHealth = currentMaxHealth;
        }
    }

    public void UpdateLifeStage()
    {
        if (age < 3) currentStage = LifeStage.Young;
        else if (age >= 3 && age < 15) currentStage = LifeStage.Adult;
        else currentStage = LifeStage.Old; 

        float ageMultiplier = 1f;
        float sizeMultiplier = 1f; 

        switch (age)
        {
            case 0: ageMultiplier = 0.5f; sizeMultiplier = 0.5f; break;  
            case 1: ageMultiplier = 0.7f; sizeMultiplier = 0.7f; break;  
            case 2: ageMultiplier = 0.9f; sizeMultiplier = 0.9f; break;  
            case 3: 
            case 4: ageMultiplier = 1.0f; sizeMultiplier = 1.0f; break;  
            case 5: 
            case 6: ageMultiplier = 1.1f; sizeMultiplier = 1.1f; break;  
            case 7: 
            case 8: 
            case 9: 
            case 10: 
            case 11: ageMultiplier = 1.2f; sizeMultiplier = 1.2f; break; 
            case 12: 
            case 13: ageMultiplier = 1.1f; sizeMultiplier = 1.2f; break; 
            case 14: ageMultiplier = 1.0f; sizeMultiplier = 1.2f; break; 
            case 15: ageMultiplier = 0.9f; sizeMultiplier = 1.2f; break; 
            case 16: ageMultiplier = 0.7f; sizeMultiplier = 1.2f; break; 
            case 17: ageMultiplier = 0.5f; sizeMultiplier = 1.2f; break; 
            default: ageMultiplier = 0.2f; sizeMultiplier = 1.2f; break; 
        }

        currentMaxHealth = dna.maxHealth * ageMultiplier;
        currentMaxEnergy = dna.maxEnergy * ageMultiplier;

        // 🌟 4. KRİTİK FİX: Yaşa bağlı boyutu, DNA'daki GENETİK BOYUT (baseSize) ile çarpıyoruz!
        // Böylece mutasyonla devleşen türler gerçekten dev, cüceleşenler minik olacak.
        float finalSize = sizeMultiplier * dna.baseSize;
        transform.localScale = new Vector3(finalSize, finalSize, finalSize);

        if (age < 3) 
            ageEnergyDrainMultiplier = 0.6f + (age * 0.15f); 
        else if (age >= 3 && age < 15) 
            ageEnergyDrainMultiplier = 1f + ((age - 3) * 0.02f); 
        else 
            ageEnergyDrainMultiplier = 1.3f + ((age - 15) * 0.1f); 

        if (currentHealth > currentMaxHealth) currentHealth = currentMaxHealth;
        if (currentEnergy > currentMaxEnergy) currentEnergy = currentMaxEnergy;
    }

    void Die()
    {
        if (EcosystemManager.instance != null)
        {
            // Önce ölen canlıyı listeden çıkarıyoruz ki rastgele seçerken kendisini bir daha seçmesin
            EcosystemManager.instance.allLivingCreatures.Remove(this);

            // 🌟 YENİ: SOY BİTTİĞİNDE RASTGELE CANLIYA GEÇİŞ SİSTEMİ 🌟
            if (DebugController.instance != null && DebugController.instance.selectedCreature == this)
            {
                // Eğer dünyada hala yaşayan başka canlılar varsa...
                if (EcosystemManager.instance.allLivingCreatures.Count > 0)
                {
                    // Kalan canlılardan rastgele bir index seç
                    int randomIndex = Random.Range(0, EcosystemManager.instance.allLivingCreatures.Count);
                    
                    // Kamerayı ve paneli o rastgele canlıya kilitle!
                    DebugController.instance.selectedCreature = EcosystemManager.instance.allLivingCreatures[randomIndex];
                }
                else
                {
                    // Eğer ekosistemde hiç canlı kalmadıysa (kıyamet koptiyse) odağı boşalt
                    DebugController.instance.selectedCreature = null;
                }
            }
        }

        // Boyuta göre et saçma mekanizması (Aynen korunuyor)
        if (meatPrefab != null)
        {
            int meatAmount = Mathf.RoundToInt(Random.Range(2f, 4f) * dna.baseSize);
            for (int i = 0; i < meatAmount; i++)
            {
                Vector3 spawnOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f);
                Instantiate(meatPrefab, transform.position + spawnOffset, Quaternion.identity);
            }
        }
        
        Destroy(gameObject);
    }

    // 🌟 EVRİM MOTORU: Yavru doğduğu an çalışır ve DNA'sını saptırır 🌟
    public void ApplyMutation()
    {
        // 1. BOYUT VE GÜÇ MUTASYONU 
        float sizeMut = Random.Range(-0.05f, 0.05f); 
        dna.baseSize += dna.baseSize * sizeMut;
        dna.maxHealth += dna.maxHealth * sizeMut;
        dna.maxEnergy += dna.maxEnergy * sizeMut;
        dna.moveSpeed -= dna.moveSpeed * (sizeMut * 0.5f); 
        dna.idleEnergyDrain += dna.idleEnergyDrain * sizeMut; 

        // 2. SİNDİRİM VERİMLİLİĞİ MUTASYONU (Et vs Ot Tahterevallisi)
        float plantMut = Random.Range(-0.05f, 0.05f);
        dna.plantEfficiency += dna.plantEfficiency * plantMut;

        // 🌟 FİX 1: ET SİNDİRİMİ ÇOK DAHA NADİR UYANACAK 🌟
        if (dna.meatEfficiency <= 0.01f) 
        {
            // Eğer canlı saf otçulsa, et sindirim geninin uyanması için sadece %20 ihtimali var!
            if (Random.value < 0.20f) 
            {
                dna.meatEfficiency += Random.Range(0.01f, 0.03f); 
            }
        }
        else 
        {
            // Eğer gen zaten uyanmışsa (önceki nesillerden), normal mutasyonuna devam eder
            float meatMut = Random.Range(-0.05f, 0.05f);
            dna.meatEfficiency += dna.meatEfficiency * meatMut;
        }

        // Sınırlandırmalar
        dna.plantEfficiency = Mathf.Clamp(dna.plantEfficiency, 0f, 2f);
        dna.meatEfficiency = Mathf.Clamp(dna.meatEfficiency, 0f, 2f);

        // 🌟 HEPÇİL VERGİSİ (OMNIVORE TAX) 🌟
        float omnivoreTax = (dna.plantEfficiency * dna.meatEfficiency) * 0.05f; 
        dna.idleEnergyDrain += omnivoreTax; 
            
        // Yemek yeme süresi 
        dna.eatDuration += dna.eatDuration * (Random.Range(-0.05f, 0.05f));

        // 3. GÖRÜŞ ALANI MUTASYONU 
        float visionMut = Random.Range(-0.1f, 0.1f);
        dna.visionRadius += dna.visionRadius * visionMut;
        dna.visionAngle -= dna.visionAngle * (visionMut * 0.8f); 
        dna.visionAngle += Random.Range(-10f, 10f); 

        float totalVisionArea = dna.visionRadius * (dna.visionAngle / 360f); 
        dna.visionEnergyTax = totalVisionArea * 0.01f;

        // 4. KOKU/SEZGİ MUTASYONU 
        float smellMut = Random.Range(-0.08f, 0.08f);
        dna.smellRadius += dna.smellRadius * smellMut;
        float smellTax = dna.smellRadius * 0.005f; 
        dna.idleEnergyDrain += smellTax;
        
        // 5. İYİLEŞME MUTASYONU 
        float healMut = Random.Range(-0.05f, 0.05f);
        dna.healingRate += dna.healingRate * healMut;
        dna.healingEnergyCost += dna.healingEnergyCost * (healMut * 1.5f);

        // 🌟 FİX 2: ARZU VE ZEHİR MUTASYONLARI YAVAŞLATILDI 🌟
        // Zehir arzusu artık nesil başına maks %2 değişebilir. 
        dna.desirePoison += Random.Range(-0.02f, 0.02f);
        dna.desirePoison = Mathf.Clamp(dna.desirePoison, 0f, 1f);

        // Tahterevalli mantığı: Kalan tüm ilgi otomatik olarak Normal Ota kayar!
        dna.desirePlant = 1f - dna.desirePoison; 

        // Et arzusu 
        dna.desireMeat += Random.Range(-0.02f, 0.02f);
        dna.desireMeat = Mathf.Clamp(dna.desireMeat, 0f, 1f);

        // Zehir Direnci Mutasyonu 
        dna.poisonResistance += Random.Range(-0.02f, 0.02f); 
        dna.poisonResistance = Mathf.Clamp(dna.poisonResistance, 0f, 1f);

        // --- SINIRLANDIRMALAR (Güvenlik) ---
        dna.baseSize = Mathf.Clamp(dna.baseSize, 0.3f, 3f);
        dna.moveSpeed = Mathf.Clamp(dna.moveSpeed, 0.5f, 10f);
        dna.eatDuration = Mathf.Clamp(dna.eatDuration, 0.5f, 10f);
        dna.visionRadius = Mathf.Clamp(dna.visionRadius, 2f, 20f);
        dna.healingRate = Mathf.Clamp(dna.healingRate, 0.1f, 15f);
        dna.healingEnergyCost = Mathf.Clamp(dna.healingEnergyCost, 0.1f, 10f);
        dna.visionAngle = Mathf.Clamp(dna.visionAngle, 30f, 360f); 
        dna.smellRadius = Mathf.Clamp(dna.smellRadius, 2f, 25f);

        float poisonTax = dna.poisonResistance * 0.02f; 
        dna.idleEnergyDrain += poisonTax;

        // --- RENK VE BİÇİM MOTORU (TURUNCU LANETİNE SON!) ---
        
        // 1. ADIM: NORMALE ÇEVİRME (Değerleri 0 ile 1 arasına sıkıştırıyoruz)
        float sizeNorm = Mathf.InverseLerp(0.3f, 3f, dna.baseSize);
        float speedNorm = Mathf.InverseLerp(0.5f, 10f, dna.moveSpeed);
        
        // 🌟 YENİ: Etçil ve Otçul renkleri ayrıldı!
        float plantNorm = Mathf.InverseLerp(0f, 2f, dna.plantEfficiency); 
        float meatNorm = Mathf.InverseLerp(0f, 2f, dna.meatEfficiency);   
        
        float healNorm = Mathf.InverseLerp(0.1f, 15f, dna.healingRate);
        float poisonNorm = Mathf.InverseLerp(0f, 1f, dna.poisonResistance);

        // 2. ADIM: 5. KUVVET ALMA ("Winner Takes All" - Kazanan Hepsini Alır Mantığı)
        // Kuvveti 3'ten 5'e çıkardık. Artık renkler birbirine KARIŞMAYACAK.
        // Hangi gen baskınsa, canlı direkt o saf renkte parlayacak!
        float sizeWeight = Mathf.Pow(sizeNorm, 5);
        float speedWeight = Mathf.Pow(speedNorm, 5);
        float plantWeight = Mathf.Pow(plantNorm, 5);
        float meatWeight = Mathf.Pow(meatNorm, 5);
        float healWeight = Mathf.Pow(healNorm, 5);
        float poisonWeight = Mathf.Pow(poisonNorm, 5);

        // 3. ADIM: YENİ RENK PALETİ 
        Color sizeColor = Color.green;                     // İriler YEŞİL (Yürüyen çalı gibi)
        Color speedColor = Color.yellow;                   // Hızlılar SARI (Elektrik/Çıta gibi)
        Color plantColor = Color.cyan;                     // Otçullar TURKUAZ
        Color meatColor = new Color(0.7f, 0f, 0f);         // 🌟 ETÇİLLER KOYU KIRMIZI (Kan Rengi!)
        Color healColor = new Color(1f, 0.4f, 0.7f);       // İyileşmesi yüksek olanlar PEMBE
        Color poisonColor = new Color(0.6f, 0f, 1f);       // Zehir yiyiciler MOR

        float totalWeight = sizeWeight + speedWeight + plantWeight + meatWeight + healWeight + poisonWeight; 
        
        // Bölme hatasını (Divide by Zero) önlemek için güvenlik
        if (totalWeight <= 0.001f) totalWeight = 1f;

        // 4. ADIM: Renkleri Harmanla
        float r = (sizeColor.r * sizeWeight + speedColor.r * speedWeight + plantColor.r * plantWeight + meatColor.r * meatWeight + healColor.r * healWeight + poisonColor.r * poisonWeight) / totalWeight;
        float g = (sizeColor.g * sizeWeight + speedColor.g * speedWeight + plantColor.g * plantWeight + meatColor.g * meatWeight + healColor.g * healWeight + poisonColor.g * poisonWeight) / totalWeight;
        float b = (sizeColor.b * sizeWeight + speedColor.b * speedWeight + plantColor.b * plantWeight + meatColor.b * meatWeight + healColor.b * healWeight + poisonColor.b * poisonWeight) / totalWeight;

        Color rawColor = new Color(r, g, b);
        
        // 5. ADIM: Parlat ve Ata!
        Color.RGBToHSV(rawColor, out float h, out float s, out float v);
        
        // Doygunluğu (Saturation) yüksek tutuyoruz ki renkler soluklaşıp kahverengiye dönmesin
        float finalSaturation = Mathf.Clamp(s * 1.5f, 0.6f, 1f); 
        
        dna.skinColor = Color.HSVToRGB(h, finalSaturation, 0.9f);
    }
}