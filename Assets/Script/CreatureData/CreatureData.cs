using UnityEngine;

[CreateAssetMenu(fileName = "New Creature Data", menuName = "Evolution/Creature Data")]
public class CreatureData : ScriptableObject
{
    [Header("Görsel ve Fiziksel")]
    public Color skinColor = Color.green; // Deri Rengi
    public float baseSize = 1f; // Temel Büyüklük (Scale)

    [Header("Hayati Değerler")]
    public float maxHealth = 100f;
    public float maxEnergy = 100f;

    [Header("Bölgecilik ve Sürü Genleri")]
    public float homeWanderRadius = 5f;    // Yuvanın etrafında ne kadar geniş bir çapta gezecek?
    public float migrationThreshold = 0.4f;// % Kaç enerjinin altına düşünce evi terk edip göçe başlayacak?
    public float sociability = 0.5f;       // 0 = Yalnız Kurt, 1 = Tam bir sürü üyesi (Sürüye katılma ihtimali)
    public float flockTolerance = 0.4f;    // Boyut olarak kendinden ne kadar farklı olanları "kardeş/sürü" sayacak?

    [Header("İyileşme")]
    public float healingRate = 2f; // Saniyede kaç can yenileyecek?
    public float healingEnergyCost = 1f; // 1 Can yenilemek için kaç birim Enerji harcayacak?

    [Header("Fiziksel Adaptasyonlar")]
    // 0 = Tamamen Savunmasız (Anında hasar yer), 1 = Tam Dirençli (Zehir ona etki etmez)
    public float poisonResistance = 0f; 

    [Header("Avcılık ve Saldırı Genleri")]
    public float attackDistance = 1.5f;        // Ne kadar uzaktan ısırabilir? (Boyun/Pençe uzunluğu)
    public float attackCooldown = 1.0f;        // İki ısırık arasındaki bekleme süresi (Saldırı hızı)
    public float attackDamageMultiplier = 25f; // Boyutla çarpılacak olan temel hasar çarpanı
    public float attackEnergyCost = 3f;        // Her ısırığın bedeli

    [Header("Besin Arzuları (Zihinsel Tercihler)")]
    [Range(0f, 1f)] public float desirePlant = 1f;  // Normal ota duyulan istek
    [Range(0f, 1f)] public float desirePoison = 0f; // Zehirli besine duyulan istek
    [Range(0f, 1f)] public float desireMeat = 0f;   // Ete duyulan istek (Placeholder - İleride kullanacağız)
    // İleride buraya yeni arzular eklemek çok kolay olacak:
    // [Range(0f, 1f)] public float desireZehirliEt = 0f; gibi.

    [Header("Hareket Özellikleri")]
    public float moveSpeed = 2f;

    [Header("Sindirim ve Beslenme")]
    public float plantEfficiency = 1f; // V1 atalarımız %100 otçul başlar
    public float meatEfficiency = 0f;  // V1 atalarımız eti hiç sindiremez
    public float eatDuration = 2f; // Yemek yeme süresi (Verim artarsa bu da uzayacak)

    [Header("Enerji Tüketimi (Metabolizma)")]
    public float idleEnergyDrain = 2f; 
    public float moveEnergyDrain = 1.5f; 
    [HideInInspector] public float baseIdleEnergyDrain = -1f;

    [Header("Algı ve Duyular")]
    //görüş
    public float visionRadius = 5f; // Görüş mesafesi
    public float visionAngle = 120f; 
    public float visionEnergyTax = 0.1f; // Alan başına ödeyeceği vergi
    //koku
    public float smellRadius = 7f; 

    [Header("Üreme Ayarları")]
    public float reproduceEnergyThreshold = 80f; 
    public float reproductionEnergyCost = 40f; 

    public void EnsureBaseMetabolism()
    {
        if (baseIdleEnergyDrain < 0f)
        {
            baseIdleEnergyDrain = Mathf.Max(0.01f, idleEnergyDrain);
        }
    }

    // 🌟 EŞEYLİ ÜREME: Anne ve babanın genlerini %50 şansla çocuğa aktaran sistem
    public static CreatureData CreateMix(CreatureData parentA, CreatureData parentB)
    {
        parentA.EnsureBaseMetabolism();
        parentB.EnsureBaseMetabolism();

        // Yepyeni, boş bir DNA sarmalı oluşturuyoruz
        CreatureData newDNA = ScriptableObject.CreateInstance<CreatureData>();

        // 1. Fiziksel Özellikler (Boyut, Can, Enerji, Hız)
        newDNA.baseSize = (Random.value > 0.5f) ? parentA.baseSize : parentB.baseSize;
        newDNA.maxHealth = (Random.value > 0.5f) ? parentA.maxHealth : parentB.maxHealth;
        newDNA.maxEnergy = (Random.value > 0.5f) ? parentA.maxEnergy : parentB.maxEnergy;
        newDNA.moveSpeed = (Random.value > 0.5f) ? parentA.moveSpeed : parentB.moveSpeed;

        // 2. Metabolizma ve Sindirim
        newDNA.baseIdleEnergyDrain = (Random.value > 0.5f) ? parentA.baseIdleEnergyDrain : parentB.baseIdleEnergyDrain;
        newDNA.idleEnergyDrain = (Random.value > 0.5f) ? parentA.idleEnergyDrain : parentB.idleEnergyDrain;
        newDNA.moveEnergyDrain = (Random.value > 0.5f) ? parentA.moveEnergyDrain : parentB.moveEnergyDrain;
        newDNA.plantEfficiency = (Random.value > 0.5f) ? parentA.plantEfficiency : parentB.plantEfficiency;
        newDNA.meatEfficiency = (Random.value > 0.5f) ? parentA.meatEfficiency : parentB.meatEfficiency;
        newDNA.eatDuration = (Random.value > 0.5f) ? parentA.eatDuration : parentB.eatDuration;
        
        // 3. Psikoloji ve Zehir
        newDNA.desirePlant = (Random.value > 0.5f) ? parentA.desirePlant : parentB.desirePlant;
        newDNA.desirePoison = (Random.value > 0.5f) ? parentA.desirePoison : parentB.desirePoison;
        newDNA.desireMeat = (Random.value > 0.5f) ? parentA.desireMeat : parentB.desireMeat;
        newDNA.poisonResistance = (Random.value > 0.5f) ? parentA.poisonResistance : parentB.poisonResistance;

        // 4. Duyular (Göz ve Burun)
        newDNA.visionRadius = (Random.value > 0.5f) ? parentA.visionRadius : parentB.visionRadius;
        newDNA.visionAngle = (Random.value > 0.5f) ? parentA.visionAngle : parentB.visionAngle;
        newDNA.visionEnergyTax = (Random.value > 0.5f) ? parentA.visionEnergyTax : parentB.visionEnergyTax;
        newDNA.smellRadius = (Random.value > 0.5f) ? parentA.smellRadius : parentB.smellRadius;

        // 5. İyileşme ve Üreme Sınırları
        newDNA.healingRate = (Random.value > 0.5f) ? parentA.healingRate : parentB.healingRate;
        newDNA.healingEnergyCost = (Random.value > 0.5f) ? parentA.healingEnergyCost : parentB.healingEnergyCost;
        newDNA.reproduceEnergyThreshold = (Random.value > 0.5f) ? parentA.reproduceEnergyThreshold : parentB.reproduceEnergyThreshold;
        newDNA.reproductionEnergyCost = (Random.value > 0.5f) ? parentA.reproductionEnergyCost : parentB.reproductionEnergyCost;

        // 6. Avcılık Genleri Mirası
        newDNA.attackDistance = (Random.value > 0.5f) ? parentA.attackDistance : parentB.attackDistance;
        newDNA.attackCooldown = (Random.value > 0.5f) ? parentA.attackCooldown : parentB.attackCooldown;
        newDNA.attackDamageMultiplier = (Random.value > 0.5f) ? parentA.attackDamageMultiplier : parentB.attackDamageMultiplier;
        newDNA.attackEnergyCost = (Random.value > 0.5f) ? parentA.attackEnergyCost : parentB.attackEnergyCost;

        // 7. Sosyallik ve Göç Genleri Mirası
        newDNA.homeWanderRadius = (Random.value > 0.5f) ? parentA.homeWanderRadius : parentB.homeWanderRadius;
        newDNA.migrationThreshold = (Random.value > 0.5f) ? parentA.migrationThreshold : parentB.migrationThreshold;
        newDNA.sociability = (Random.value > 0.5f) ? parentA.sociability : parentB.sociability;
        newDNA.flockTolerance = (Random.value > 0.5f) ? parentA.flockTolerance : parentB.flockTolerance;

        return newDNA;
    }
}
