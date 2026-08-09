using UnityEngine;

public enum EcologicalLineage
{
    Unassigned,
    Herbivore,
    Predator,
    Scavenger,
    Toxicovore
}

[CreateAssetMenu(fileName = "New Creature Data", menuName = "Evolution/Creature Data")]
public class CreatureData : ScriptableObject
{
    [Header("Görsel ve Fiziksel")]
    public Color skinColor = Color.green; // Deri Rengi
    public float baseSize = 1f; // Temel Büyüklük (Scale)

    [Header("Görsel Evrim Genleri")]
    [Range(0f, 1f)] public float lineageHue = 0.5f;
    [Range(0f, 1f)] public float patternSeed = 0.5f;
    [HideInInspector] public bool visualGenesInitialized;

    [Header("Ekolojik Soy")]
    [Tooltip("Kurucu ekolojik soyun, baskın gen paketi üzerinden nesiller boyunca izlenmesini sağlar.")]
    public EcologicalLineage ecologicalLineage = EcologicalLineage.Unassigned;

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

    public void EnsureVisualGenes()
    {
        if (visualGenesInitialized)
        {
            return;
        }

        Color.RGBToHSV(skinColor, out float sourceHue, out _, out _);
        lineageHue = Mathf.Repeat((sourceHue * 0.73f) + (baseSize * 0.11f) + (moveSpeed * 0.037f), 1f);
        patternSeed = Mathf.Repeat((sourceHue * 0.41f) + (maxHealth * 0.0031f) + (smellRadius * 0.071f), 1f);
        visualGenesInitialized = true;
    }

    public void UpdateSkinColorFromEcology()
    {
        EnsureVisualGenes();

        float plantScore = Mathf.Sqrt(Mathf.Clamp01(desirePlant) * Mathf.Clamp01(plantEfficiency * 0.5f));
        float meatScore = Mathf.Sqrt(Mathf.Clamp01(desireMeat) * Mathf.Clamp01(meatEfficiency * 0.5f));
        float poisonScore = Mathf.Sqrt(Mathf.Clamp01(desirePoison) * Mathf.Clamp01(poisonResistance));

        float ecologicalHue;
        float strongest = Mathf.Max(plantScore, Mathf.Max(meatScore, poisonScore));
        float weakest = Mathf.Min(plantScore, Mathf.Min(meatScore, poisonScore));
        float secondStrongest = plantScore + meatScore + poisonScore - strongest - weakest;
        bool hybrid = strongest > 0.001f && secondStrongest >= strongest * 0.78f;

        if (hybrid && plantScore >= poisonScore && meatScore >= poisonScore)
        {
            ecologicalHue = 0.11f; // Ot + et: altın/kehribar.
        }
        else if (hybrid && plantScore >= meatScore && poisonScore >= meatScore)
        {
            ecologicalHue = 0.53f; // Ot + zehir: mavi/turkuaz.
        }
        else if (hybrid)
        {
            ecologicalHue = 0.92f; // Et + zehir: koyu pembe.
        }
        else if (meatScore >= plantScore && meatScore >= poisonScore)
        {
            ecologicalHue = 0.015f; // Etçil: kırmızı.
        }
        else if (poisonScore >= plantScore)
        {
            ecologicalHue = 0.78f; // Zehir uyumu: mor.
        }
        else
        {
            ecologicalHue = 0.34f; // Otçul: yeşil.
        }

        float lineageOffset = Mathf.Lerp(-0.075f, 0.075f, lineageHue);
        float hue = Mathf.Repeat(ecologicalHue + lineageOffset, 1f);
        float saturation = Mathf.Lerp(0.78f, 1f, Mathf.Repeat(patternSeed * 1.91f, 1f));
        float value = Mathf.Lerp(0.74f, 0.98f, Mathf.Repeat((patternSeed * 1.37f) + 0.19f, 1f));
        skinColor = Color.HSVToRGB(hue, saturation, value);
    }

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
        parentA.EnsureVisualGenes();
        parentB.EnsureVisualGenes();

        // Yepyeni, boş bir DNA sarmalı oluşturuyoruz
        CreatureData newDNA = ScriptableObject.CreateInstance<CreatureData>();

        // Beslenme, duyu ve av silahları tek tek parçalanmak yerine ağırlıklı bir
        // ekolojik paket olarak aktarılır. İkinci ebeveynden gelen sınırlı gen akışı
        // melezleşmeyi ve uzun vadeli türleşmeyi açık tutar.
        CreatureData ecologicalPrimary = Random.value > 0.5f ? parentA : parentB;
        CreatureData ecologicalSecondary = ecologicalPrimary == parentA ? parentB : parentA;
        float secondaryGeneFlow = Random.Range(0.12f, 0.28f);
        newDNA.ecologicalLineage = ecologicalPrimary.ecologicalLineage != EcologicalLineage.Unassigned
            ? ecologicalPrimary.ecologicalLineage
            : ecologicalSecondary.ecologicalLineage;

        // 1. Fiziksel Özellikler (Boyut, Can, Enerji, Hız)
        newDNA.baseSize = (Random.value > 0.5f) ? parentA.baseSize : parentB.baseSize;
        newDNA.maxHealth = (Random.value > 0.5f) ? parentA.maxHealth : parentB.maxHealth;
        newDNA.maxEnergy = (Random.value > 0.5f) ? parentA.maxEnergy : parentB.maxEnergy;
        newDNA.moveSpeed = (Random.value > 0.5f) ? parentA.moveSpeed : parentB.moveSpeed;
        newDNA.lineageHue = Mathf.Repeat(Mathf.Lerp(ecologicalPrimary.lineageHue, ecologicalSecondary.lineageHue, secondaryGeneFlow), 1f);
        newDNA.patternSeed = Mathf.Repeat(Mathf.Lerp(ecologicalPrimary.patternSeed, ecologicalSecondary.patternSeed, secondaryGeneFlow), 1f);
        newDNA.visualGenesInitialized = true;

        // 2. Metabolizma ve Sindirim
        newDNA.baseIdleEnergyDrain = Mathf.Lerp(ecologicalPrimary.baseIdleEnergyDrain, ecologicalSecondary.baseIdleEnergyDrain, secondaryGeneFlow);
        newDNA.idleEnergyDrain = Mathf.Lerp(ecologicalPrimary.idleEnergyDrain, ecologicalSecondary.idleEnergyDrain, secondaryGeneFlow);
        newDNA.moveEnergyDrain = (Random.value > 0.5f) ? parentA.moveEnergyDrain : parentB.moveEnergyDrain;
        newDNA.plantEfficiency = Mathf.Lerp(ecologicalPrimary.plantEfficiency, ecologicalSecondary.plantEfficiency, secondaryGeneFlow);
        newDNA.meatEfficiency = Mathf.Lerp(ecologicalPrimary.meatEfficiency, ecologicalSecondary.meatEfficiency, secondaryGeneFlow);
        newDNA.eatDuration = (Random.value > 0.5f) ? parentA.eatDuration : parentB.eatDuration;
        
        // 3. Psikoloji ve Zehir
        newDNA.desirePlant = Mathf.Lerp(ecologicalPrimary.desirePlant, ecologicalSecondary.desirePlant, secondaryGeneFlow);
        newDNA.desirePoison = Mathf.Lerp(ecologicalPrimary.desirePoison, ecologicalSecondary.desirePoison, secondaryGeneFlow);
        newDNA.desireMeat = Mathf.Lerp(ecologicalPrimary.desireMeat, ecologicalSecondary.desireMeat, secondaryGeneFlow);
        float desireTotal = Mathf.Max(newDNA.desirePlant + newDNA.desirePoison + newDNA.desireMeat, 0.001f);
        newDNA.desirePlant /= desireTotal;
        newDNA.desirePoison /= desireTotal;
        newDNA.desireMeat /= desireTotal;
        newDNA.poisonResistance = Mathf.Lerp(ecologicalPrimary.poisonResistance, ecologicalSecondary.poisonResistance, secondaryGeneFlow);

        // 4. Duyular (Göz ve Burun)
        newDNA.visionRadius = Mathf.Lerp(ecologicalPrimary.visionRadius, ecologicalSecondary.visionRadius, secondaryGeneFlow);
        newDNA.visionAngle = Mathf.Lerp(ecologicalPrimary.visionAngle, ecologicalSecondary.visionAngle, secondaryGeneFlow);
        newDNA.visionEnergyTax = Mathf.Lerp(ecologicalPrimary.visionEnergyTax, ecologicalSecondary.visionEnergyTax, secondaryGeneFlow);
        newDNA.smellRadius = Mathf.Lerp(ecologicalPrimary.smellRadius, ecologicalSecondary.smellRadius, secondaryGeneFlow);

        // 5. İyileşme ve Üreme Sınırları
        newDNA.healingRate = Mathf.Lerp(ecologicalPrimary.healingRate, ecologicalSecondary.healingRate, secondaryGeneFlow);
        newDNA.healingEnergyCost = Mathf.Lerp(ecologicalPrimary.healingEnergyCost, ecologicalSecondary.healingEnergyCost, secondaryGeneFlow);
        newDNA.reproduceEnergyThreshold = Mathf.Lerp(ecologicalPrimary.reproduceEnergyThreshold, ecologicalSecondary.reproduceEnergyThreshold, secondaryGeneFlow);
        newDNA.reproductionEnergyCost = Mathf.Lerp(ecologicalPrimary.reproductionEnergyCost, ecologicalSecondary.reproductionEnergyCost, secondaryGeneFlow);

        // 6. Avcılık Genleri Mirası
        newDNA.attackDistance = Mathf.Lerp(ecologicalPrimary.attackDistance, ecologicalSecondary.attackDistance, secondaryGeneFlow);
        newDNA.attackCooldown = Mathf.Lerp(ecologicalPrimary.attackCooldown, ecologicalSecondary.attackCooldown, secondaryGeneFlow);
        newDNA.attackDamageMultiplier = Mathf.Lerp(ecologicalPrimary.attackDamageMultiplier, ecologicalSecondary.attackDamageMultiplier, secondaryGeneFlow);
        newDNA.attackEnergyCost = Mathf.Lerp(ecologicalPrimary.attackEnergyCost, ecologicalSecondary.attackEnergyCost, secondaryGeneFlow);

        // 7. Sosyallik ve Göç Genleri Mirası
        newDNA.homeWanderRadius = (Random.value > 0.5f) ? parentA.homeWanderRadius : parentB.homeWanderRadius;
        newDNA.migrationThreshold = (Random.value > 0.5f) ? parentA.migrationThreshold : parentB.migrationThreshold;
        newDNA.sociability = (Random.value > 0.5f) ? parentA.sociability : parentB.sociability;
        newDNA.flockTolerance = (Random.value > 0.5f) ? parentA.flockTolerance : parentB.flockTolerance;

        newDNA.UpdateSkinColorFromEcology();

        return newDNA;
    }
}
