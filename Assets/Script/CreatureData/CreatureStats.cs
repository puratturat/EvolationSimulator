using UnityEngine;

public enum LifeStage { Young, Adult, Old }

public class CreatureStats : MonoBehaviour
{
    [Header("Gözlem Kayıtları")]
    public long observationId;
    public int lifetimePlantsEaten;
    public int lifetimePoisonPlantsEaten;
    public int lifetimeMeatEaten;
    public int lifetimeAttacks;
    public int lifetimeKills;
    public int lifetimeOffspring;
    public float lifetimeDamageDealt;

    [Header("Genetik Veri")]
    public CreatureData dna; 

    [Header("Soy Ağacı")]
    public int generation = 1; 

    [Header("Anlık Değerler (Gözlem İçin)")]
    public float currentHealth;
    public float currentEnergy;
    public float currentSpeed;
    
    public float currentAttackDamage; 
    public float currentHealingRate;  

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

    private SimulationDeathCause lastDamageCause = SimulationDeathCause.Unknown;
    private CreatureStats lastAttacker;
    private bool isDead;

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
            dna.EnsureVisualGenes();
            dna.UpdateSkinColorFromEcology();

            // 🌟 2. KRİTİK FİX: Evrimleşen Renk Genini canlının fiziksel bedenine uygula
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = dna.skinColor;

            UpdateLifeStage(); 
            currentHealth = currentMaxHealth;
            currentEnergy = currentMaxEnergy;
        }
        else
        {
            Debug.LogWarning(gameObject.name + " objesinin DNA'sı (CreatureData) atanmamış!");
        }

        if (EcosystemManager.instance != null)
        {
            EcosystemManager.instance.allLivingCreatures.Add(this);
        }

        observationId = SimulationEventLogger.RegisterCreature(this);

        CreatureVisualEvolution visualEvolution = GetComponent<CreatureVisualEvolution>();
        if (visualEvolution == null)
        {
            visualEvolution = gameObject.AddComponent<CreatureVisualEvolution>();
        }
        visualEvolution.Initialize(this);
    }

    void Update()
        {
            if (dna == null) return; 

            if (currentEnergy > 0)
            {
                currentSpeed = CalculateCurrentSpeed();
                float metabolismTax = dna.visionRadius * dna.visionEnergyTax;

                // 1. Standart Enerji Harcaması (Metabolizma)
                if (isMoving) currentEnergy -= ((dna.moveEnergyDrain * dna.moveSpeed) + metabolismTax) * ageEnergyDrainMultiplier * Time.deltaTime;
                else currentEnergy -= (dna.idleEnergyDrain + metabolismTax) * ageEnergyDrainMultiplier * Time.deltaTime;

                // 🌟 2. YENİ: CAN YENİLEME SİSTEMİ 🌟
                if (currentHealth < currentMaxHealth)
                {
                    // Saniyede ne kadar can iyileşecek?
                    float healAmount = currentHealingRate * Time.deltaTime;
                    
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
                currentSpeed = CalculateCurrentSpeed() / 2f; 
                TakeDamage(starvationDamage * Time.deltaTime, SimulationDeathCause.Starvation);
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

    public void ResetLifetimeObservationStats()
    {
        observationId = 0;
        lifetimePlantsEaten = 0;
        lifetimePoisonPlantsEaten = 0;
        lifetimeMeatEaten = 0;
        lifetimeAttacks = 0;
        lifetimeKills = 0;
        lifetimeOffspring = 0;
        lifetimeDamageDealt = 0f;
    }

    // 🌟 YENİ NESİL BİYOLOJİK YAŞLANMA VE ÖMÜR MOTORU 🌟
    public void UpdateLifeStage()
    {
        // 1. ÖMÜR AYARLARI: Yetişkinlik dönemi genişletildi, yaşlılık sınırı 40'a çekildi!
        if (age < 6) currentStage = LifeStage.Young;
        else if (age >= 6 && age <= 40) currentStage = LifeStage.Adult;
        else currentStage = LifeStage.Old; 

        float ageMultiplier = 1f;
        float sizeMultiplier = 1f; 

        if (currentStage == LifeStage.Young)
        {
            sizeMultiplier = 0.5f + (age * 0.1f); // 5 yaşında 1.0f tam boyuta ulaşır
            ageMultiplier = 0.5f + (age * 0.1f);
            ageEnergyDrainMultiplier = 0.6f + (age * 0.08f);
        }
        else if (currentStage == LifeStage.Adult)
        {
            sizeMultiplier = 1.0f;
            if (age >= 15 && age <= 30) ageMultiplier = 1.2f; // Prime dönem gücü
            else ageMultiplier = 1.0f;
            ageEnergyDrainMultiplier = 1.0f;
        }
        else // LifeStage.Old
        {
            sizeMultiplier = 1.0f;
            int yearsIntoOldAge = age - 40;
            float decay = yearsIntoOldAge * 0.025f; // Her 10 yaşta bir %25 çöküş
            ageMultiplier = Mathf.Max(0.15f, 1.0f - decay);
            ageEnergyDrainMultiplier = 1.2f + (yearsIntoOldAge * 0.05f); // Dedelerin metabolizma penaltısı
        }

        // =========================================================================
        // 🌟 2. ARKETİP / SINIF SİNERJİ SİSTEMİ (TÜRLEŞME KATALİZÖRÜ) 🌟
        // =========================================================================
        
        // A) 🌿 OTÇUL SİNERJİSİ: Ota ilgi ve sindirim birleşirse can, enerji ve boyut tavan yapar!
        float herbivoreFocus = dna.desirePlant * dna.plantEfficiency; // Maks 2.0
        float herbivoreHealthBonus = herbivoreFocus * 0.40f;          // Maks +%80 Can bonusu
        float herbivoreEnergyBonus = herbivoreFocus * 0.30f;          // Maks +%60 Enerji bonusu
        float herbivoreSizeBonus = herbivoreFocus * 0.20f;            // Maks +%40 Devleşme bonusu

        // =========================================================================
        // 3. STATLARIN AKTİF ATANMASI
        // =========================================================================
        currentMaxHealth = (dna.maxHealth * (1f + herbivoreHealthBonus)) * ageMultiplier;
        currentMaxEnergy = (dna.maxEnergy * (1f + herbivoreEnergyBonus)) * ageMultiplier;

        // Boyutlandırma: Otçullara ekstra devleşme payı veriliyor
        float finalSize = sizeMultiplier * dna.baseSize * (1f + herbivoreSizeBonus);
        transform.localScale = new Vector3(finalSize, finalSize, finalSize);

        // Dinamik Sinerji Statlarını Güncelleme
        currentSpeed = CalculateCurrentSpeed();
        currentAttackDamage = CalculateCurrentAttackDamage();
        currentHealingRate = CalculateCurrentHealingRate();

        // Güvenlik taşma kontrolleri
        if (currentHealth > currentMaxHealth) currentHealth = currentMaxHealth;
        if (currentEnergy > currentMaxEnergy) currentEnergy = currentMaxEnergy;
    }

    float CalculateCurrentSpeed()
    {
        float predatorFocus = dna.desireMeat * dna.meatEfficiency;
        return dna.moveSpeed * (1f + (predatorFocus * 0.35f));
    }

    float CalculateCurrentAttackDamage()
    {
        float predatorFocus = dna.desireMeat * dna.meatEfficiency;
        float physicalSize = Mathf.Max(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y));
        return (physicalSize * dna.attackDamageMultiplier) * (1f + (predatorFocus * 0.50f));
    }

    float CalculateCurrentHealingRate()
    {
        float toxicovoreFocus = dna.desirePoison * dna.poisonResistance;
        return dna.healingRate * (1f + (toxicovoreFocus * 0.80f));
    }

    public void TakeDamage(float amount, SimulationDeathCause cause, CreatureStats attacker = null)
    {
        if (isDead || amount <= 0f)
        {
            return;
        }

        currentHealth -= amount;
        lastDamageCause = cause;
        lastAttacker = attacker;
    }

    void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        if (lastDamageCause == SimulationDeathCause.Predation && lastAttacker != null)
        {
            lastAttacker.lifetimeKills++;
        }
        SimulationEventLogger.RecordDeath(this, lastDamageCause, lastAttacker);

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


    // 🌟 EVRİM MOTORU: Yavru doğduğu an çalışır ve melez genlerini saptırır 🌟
    public void ApplyMutation()
    {
        dna.EnsureBaseMetabolism();
        dna.EnsureVisualGenes();

        dna.lineageHue = Mathf.Repeat(dna.lineageHue + Random.Range(-0.045f, 0.045f), 1f);
        if (Random.value < 0.035f)
        {
            dna.lineageHue = Mathf.Repeat(dna.lineageHue + Random.Range(0.12f, 0.32f), 1f);
        }
        dna.patternSeed = Mathf.Repeat(dna.patternSeed + Random.Range(-0.075f, 0.075f), 1f);

        // 🌟 BİYOLOJİK YÖNELİM BİASLARI (Zar Hileleri) 🌟
        // Ebeveynin yönelimi ne kadar güçlüyse, sonraki neslin o sınıfa ait mutasyon şansı o kadar katlanır!
        float meatBias = (dna.desireMeat * dna.meatEfficiency) * 0.06f;
        float plantBias = (dna.desirePlant * dna.plantEfficiency) * 0.02f;
        float poisonBias = (dna.desirePoison * dna.poisonResistance) * 0.05f;// Maks +0.05 mutasyon kıyağı

        // 1. BOYUT VE GÜÇ MUTASYONU (Otçulların devleşme eğilimi vardır)
        float sizeMut = Random.Range(-0.05f, 0.05f) + plantBias; 
        dna.baseSize += dna.baseSize * sizeMut;
        dna.maxHealth += dna.maxHealth * sizeMut;
        dna.maxEnergy += dna.maxEnergy * sizeMut;
        dna.moveSpeed -= dna.moveSpeed * (sizeMut * 0.5f); 
        dna.baseIdleEnergyDrain += dna.baseIdleEnergyDrain * sizeMut; 

        // 2. SİNDİRİM VERİMLİLİĞİ (KATI MİDE KAPASİTESİ VE TAHTEREVALLİ KURALI)
        dna.plantEfficiency += Random.Range(-0.05f, 0.05f) + plantBias;
        
        if (dna.meatEfficiency <= 0.12f && Random.value < 0.25f)
            dna.meatEfficiency += Random.Range(0.03f, 0.085f);
        else 
            dna.meatEfficiency += Random.Range(-0.05f, 0.05f) + meatBias;

        dna.plantEfficiency = Mathf.Max(0f, dna.plantEfficiency);
        dna.meatEfficiency = Mathf.Max(0.01f, dna.meatEfficiency);

        // TAHETEREVALLİ: Mide enzimleri toplamı 2.0'yi aşamaz!
        float totalEfficiency = dna.plantEfficiency + dna.meatEfficiency;
        if (totalEfficiency > 2f)
        {
            dna.plantEfficiency = (dna.plantEfficiency / totalEfficiency) * 2f;
            dna.meatEfficiency = (dna.meatEfficiency / totalEfficiency) * 2f;
        }

        dna.eatDuration += dna.eatDuration * (Random.Range(-0.05f, 0.05f));

        // 3. GÖRÜŞ VE KOKU MUTASYONU (Etçillerin duyuları keskinleşir)
        float visionMut = Random.Range(-0.1f, 0.1f) + meatBias;
        dna.visionRadius += dna.visionRadius * visionMut;
        dna.visionAngle -= dna.visionAngle * (visionMut * 0.8f); 
        dna.visionAngle += Random.Range(-10f, 10f); 

        float totalVisionArea = dna.visionRadius * (dna.visionAngle / 360f); 
        dna.visionEnergyTax = totalVisionArea * 0.01f;

        float smellMut = Random.Range(-0.08f, 0.08f) + meatBias;
        dna.smellRadius += dna.smellRadius * smellMut;
        
        // 4. İYİLEŞME MUTASYONU (Zehirciler inanılmaz bir yenilenme kazanır)
        float healMut = Random.Range(-0.05f, 0.05f) + poisonBias;
        dna.healingRate += dna.healingRate * healMut;
        dna.healingEnergyCost += dna.healingEnergyCost * (healMut * 1.5f);

        // 5. ÜÇLÜ ARZU SİSTEMİ (PASTA DİLİMİ MANTIĞI)
        dna.desirePlant += Random.Range(-0.05f, 0.05f);
        dna.desirePoison += Random.Range(-0.05f, 0.05f);
        dna.desireMeat += Random.Range(-0.045f, 0.05f) + meatBias;
        if (Random.value < 0.08f)
            dna.desireMeat += Random.Range(0.02f, 0.07f);

        dna.desirePlant = Mathf.Max(0.01f, dna.desirePlant);
        dna.desirePoison = Mathf.Max(0.01f, dna.desirePoison);
        dna.desireMeat = Mathf.Max(0.01f, dna.desireMeat);

        float totalDesire = dna.desirePlant + dna.desirePoison + dna.desireMeat;
        dna.desirePlant /= totalDesire;
        dna.desirePoison /= totalDesire;
        dna.desireMeat /= totalDesire;

        dna.poisonResistance += Random.Range(-0.05f, 0.05f) + poisonBias; 
        dna.poisonResistance = Mathf.Clamp(dna.poisonResistance, 0f, 1f);

        // 6. AVCILIK MUTASYONU (Pençeler, Dişler ve Hız)
        float predatorFocus = Mathf.Clamp01(dna.desireMeat * dna.meatEfficiency);
        float unusedWeaponBias = (1f - predatorFocus) * 0.02f;
        float huntMut = Random.Range(-0.05f, 0.05f) + meatBias - unusedWeaponBias;
        dna.attackDistance += dna.attackDistance * huntMut;
        dna.attackDamageMultiplier += dna.attackDamageMultiplier * huntMut;
        dna.attackEnergyCost += dna.attackEnergyCost * huntMut;
        dna.attackCooldown -= dna.attackCooldown * huntMut; 

        // --- SINIRLANDIRMALAR (Güvenlik) ---
        dna.baseSize = Mathf.Clamp(dna.baseSize, 0.3f, 3f);
        dna.moveSpeed = Mathf.Clamp(dna.moveSpeed, 0.5f, 10f);
        dna.eatDuration = Mathf.Clamp(dna.eatDuration, 0.5f, 10f);
        dna.visionRadius = Mathf.Clamp(dna.visionRadius, 2f, 20f);
        dna.healingRate = Mathf.Clamp(dna.healingRate, 0.1f, 15f);
        dna.healingEnergyCost = Mathf.Clamp(dna.healingEnergyCost, 0.1f, 10f);
        dna.visionAngle = Mathf.Clamp(dna.visionAngle, 30f, 360f); 
        dna.smellRadius = Mathf.Clamp(dna.smellRadius, 2f, 25f);
        dna.attackDistance = Mathf.Clamp(dna.attackDistance, 0.5f, 4f);
        dna.attackCooldown = Mathf.Clamp(dna.attackCooldown, 0.2f, 3f);
        dna.attackDamageMultiplier = Mathf.Clamp(dna.attackDamageMultiplier, 5f, 50f);
        dna.attackEnergyCost = Mathf.Clamp(dna.attackEnergyCost, 0.5f, 10f);
        dna.homeWanderRadius = Mathf.Clamp(dna.homeWanderRadius, 2f, 15f);
        dna.migrationThreshold = Mathf.Clamp(dna.migrationThreshold, 0.1f, 0.8f); 
        dna.sociability = Mathf.Clamp(dna.sociability, 0f, 1f);
        dna.flockTolerance = Mathf.Clamp(dna.flockTolerance, 0.1f, 1.5f);

        float omnivoreTax = (dna.plantEfficiency * dna.meatEfficiency) * 0.05f;
        float smellTax = dna.smellRadius * 0.005f;
        float poisonTax = dna.poisonResistance * 0.02f;
        float biteStrengthTax = Mathf.InverseLerp(5f, 50f, dna.attackDamageMultiplier) * 0.12f;
        float biteReachTax = Mathf.InverseLerp(0.5f, 4f, dna.attackDistance) * 0.05f;
        dna.idleEnergyDrain = Mathf.Max(0.01f, dna.baseIdleEnergyDrain + omnivoreTax + smellTax + poisonTax + biteStrengthTax + biteReachTax);

        // --- EKOLOJİK RENK VE GEOMETRİK FENOTİP MOTORU ---
        dna.UpdateSkinColorFromEcology();

        CreatureVisualEvolution visualEvolution = GetComponent<CreatureVisualEvolution>();
        if (visualEvolution != null)
        {
            visualEvolution.RefreshVisuals();
        }
    }

    // 🌟 DİNAMİK TAKSONOMİ SİSTEMİ (Canlının Biyolojik Sınıfını Hesaplar)
    public string GetCreatureClass()
    {
        if (dna == null) return "<color=#708090><b>Bilinmeyen Tür??</b></color>";

        // Kolaylık olsun diye DNA verilerini lokal değişkenlere alıyoruz
        float plantEff = dna.plantEfficiency;
        float meatEff = dna.meatEfficiency;
        float plantDes = dna.desirePlant;
        float meatDes = dna.desireMeat;
        float poisonDes = dna.desirePoison;
        float poisonRes = dna.poisonResistance;
        float size = dna.baseSize;

        // 1. ZEHİR TÜKETİCİSİ (Toxicovore)
        // Zehirli ota ilgisi %35'i geçmiş ve zehir direnci %35'in üzerindeyse
        if (poisonDes >= 0.35f && poisonRes >= 0.35f)
        {
            return "<color=#9400D3><b>Zehir Tüketicisi </b></color>";
        }

        // 2. ETÇİL SINIFLARI (Predator & Scavenger)
        if (meatDes >= 0.40f && meatEff >= 0.40f)
        {
            // Eğer saldırma genleri (hasarı ve mesafesi) gelişmişse gerçek bir yırtıcıdır!
            if (dna.attackDamageMultiplier >= 15f && dna.attackDistance >= 1.2f)
            {
                // Boyutu da devasaysa o bir Apex Predatördür!
                if (size >= 1.6f)
                {
                    return "<color=#800000><b>Devasa Predatör</b></color>";
                }
                return "<color=#FF0000><b> Predatör </b></color>";
            }
            else
            {
                // Eti sindiriyor ve seviyor ama saldıramıyorsa o bir Leşçildir
                return "<color=#D2691E><b> Leşçil </b></color>";
            }
        }

        // 3. HEPÇİL (Omnivore)
        // Hem otu hem eti %35'in üzerinde verimle sindirebiliyorsa dengeli bir hepçildir
        if (plantEff >= 0.35f && meatEff >= 0.35f)
        {
            return "<color=#8B4513><b> Hepçil </b></color>";
        }

        // 4. OTÇUL SINIFLARI (Herbivore)
        if (plantDes >= 0.40f && plantEff >= 0.40f)
        {
            // Boyutu devasaysa Megafauna (Dev Otçul) sınıfına girer
            if (size >= 1.7f)
            {
                return "<color=#005500><b> Devasa Otçul </b></color>";
            }
            return "<color=#008000><b> Otçul </b></color>";
        }

        // 5. BAŞLANGIÇ / ARA FORM
        return "<color=#708090><b>Gelişmekte Olan Tür</b></color>";
    }
}
