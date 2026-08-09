using UnityEngine;

public class LeaterBrain : MonoBehaviour
{
    // YENİ DURUMLAR EKLENDİ (SeekingMate, Mating)
    public enum State { Idle, Wandering, ChasingFood, Eating, SeekingMate, Mating, ChasingPrey, Attacking, Fleeing, Defending }

    public State currentState;

    [Header("Zamanlayıcı Ayarları")]
    public float minActionTime = 1f; 
    public float maxActionTime = 3f; 

    [Header("Etkileşim Ayarları")]
    public float eatDistance = 1.2f; 
    public float mateDistance = 1.5f; 
    public float mateDuration = 3f; 
    [Tooltip("Çiftleşme maliyeti ödendikten sonra korunacak asgari enerji oranı. Soyların üreyerek kendini açlıktan silmesini engeller.")]
    [Range(0.25f, 0.50f)] public float minimumPostMatingEnergyRatio = 0.34f;

    [Header("Zehirli Besin Dengesi")]
    [InspectorName("Uyumsuz Canlı Acil Açlık Eşiği")]
    [Tooltip("Zehre uyum sağlamamış canlılar yalnızca enerjileri bu oranın altına düştüğünde zehirli besini düşünür.")]
    [Range(0f, 1f)] public float unadaptedPoisonEmergencyThreshold = 0.08f;
    [InspectorName("Uyumlu Canlı Beslenme Eşiği")]
    [Tooltip("Tam uyumlu canlılar enerjileri bu oranın altındayken zehirli besini normal bir kaynak olarak kullanabilir.")]
    [Range(0f, 1f)] public float adaptedPoisonEnergyThreshold = 0.50f;
    [InspectorName("Tam Uyum Gen Eşiği")]
    [Tooltip("Zehir isteği ve direncinin geometrik ortalaması bu değere ulaştığında canlı tam uyumlu sayılır.")]
    [Range(0.05f, 1f)] public float fullPoisonAdaptationThreshold = 0.35f;
    [InspectorName("Zehir Hasarı / Maksimum Can")]
    [Tooltip("Direnç uygulanmadan önce tek zehirli öğünün vereceği asgari hasarın maksimum cana oranı.")]
    [Range(0f, 1f)] public float poisonMaxHealthDamageRatio = 0.45f;
    [InspectorName("Dirençsiz Besin Kazancı")]
    [Tooltip("Hiç zehir direnci olmayan canlının zehirli bitkiden alabileceği besin oranı.")]
    [Range(0f, 1f)] public float minimumPoisonNutrition = 0.12f;
    [InspectorName("Tam Dirençli Besin Kazancı")]
    [Tooltip("Tam dirençli canlının zehirli bitkiden alacağı besin çarpanı.")]
    [Range(1f, 2f)] public float maximumPoisonNutrition = 1.10f;

    [Header("Et ve Avcılık Evrimi")]
    [InspectorName("Uyumsuz Canlı Leş Eşiği")]
    [Tooltip("Etçil uyumu olmayan canlılar yalnızca enerjileri bu oranın altındayken yerdeki eti son çare olarak yer.")]
    [Range(0f, 1f)] public float unadaptedCarrionEmergencyThreshold = 0.12f;
    [InspectorName("Uyumsuz Canlı Av Eşiği")]
    [Tooltip("Etçil uyumu olmayan canlılar yalnızca enerjileri bu oranın altındayken daha küçük bir canlıyı avlamayı dener.")]
    [Range(0f, 1f)] public float unadaptedHuntEmergencyThreshold = 0.05f;
    [InspectorName("Uyumlu Etçil Beslenme Eşiği")]
    [Tooltip("Tam uyumlu etçiller enerjileri bu oranın altındayken leş ve canlı av arar.")]
    [Range(0f, 1f)] public float adaptedCarnivoreEnergyThreshold = 0.68f;
    [InspectorName("Tam Etçil Uyum Gen Eşiği")]
    [Tooltip("Et isteği ve et sindiriminin geometrik ortalaması bu değere ulaştığında davranış tam etçil kabul edilir.")]
    [Range(0.05f, 1f)] public float fullCarnivoreAdaptationThreshold = 0.35f;
    [InspectorName("Fırsatçı Asgari Boyut Üstünlüğü")]
    [Tooltip("Etçil uyumu olmayan bir canlının avından kaç kat büyük olması gerektiği.")]
    [Range(1f, 3f)] public float opportunistRequiredSizeRatio = 1.25f;
    [InspectorName("Predatör Asgari Boyut Oranı")]
    [Tooltip("Tam uyumlu bir predatörün saldırabileceği avlara göre asgari boyut oranı.")]
    [Range(0.25f, 1f)] public float predatorRequiredSizeRatio = 0.88f;
    [Tooltip("Aynı ekolojik soydan bir canlıya yalnızca gerçek ölüm kalım açlığında saldırılabilir.")]
    [Range(0f, 0.20f)] public float sameLineageCannibalEmergencyThreshold = 0.06f;
    [Tooltip("Avı öldüren canlıya, leş yere düşmeden önce verilen taze et payının av enerjisine oranı.")]
    [Range(0.05f, 0.30f)] public float freshKillEnergyRatio = 0.15f;
    [Tooltip("Av sırasında can bu oranın altına düşerse avcı dövüşü bırakıp hayatta kalmayı seçer.")]
    [Range(0.15f, 0.60f)] public float huntRetreatHealthThreshold = 0.40f;
    [Tooltip("Bu nesle kadar kurucu soylar canlı av olarak hedeflenmez; kaynak rekabeti ve diğer ölüm nedenleri devam eder.")]
    [Range(0, 5)] public int protectedFounderGenerations = 2;
    [Tooltip("Etçil uyumu bu değerin üzerindeyse üreme için yakın zamanda et tüketmiş olması gerekir.")]
    [Range(0.25f, 0.90f)] public float meatRequiredForReproductionAdaptation = 0.55f;
    [Tooltip("Bir et öğününün etçil üremesine izin verdiği oyun saniyesi.")]
    [Range(30f, 300f)] public float carnivoreMealReproductionWindow = 120f;

    [Header("Kaçış ve Savunma")]
    [Tooltip("Bir avcının takibi kesildikten sonra tehdidin kaç saniye hatırlanacağı.")]
    [Range(0.5f, 8f)] public float threatMemoryDuration = 2.5f;
    [Tooltip("Kaçarken normal hareket hızına uygulanan kısa süreli çarpan.")]
    [Range(1f, 2f)] public float fleeSpeedMultiplier = 1.18f;
    [Tooltip("Kendisinden küçük bir saldırgana karşı savunmayı seçme taban olasılığı.")]
    [Range(0f, 1f)] public float smallerThreatDefenseChance = 0.68f;
    [Tooltip("Daha güçlü bir saldırgana karşı köşeye sıkışınca son kez savunma olasılığı.")]
    [Range(0f, 1f)] public float lastStandDefenseChance = 0.22f;

    [Header("Ekolojik Eş Seçimi")]
    [Tooltip("Bu benzerliğin altındaki çok farklı beslenme tipleri yalnızca nadir çeşitlilik köprüsüyle eşleşebilir.")]
    [Range(0f, 1f)] public float minimumMateSimilarity = 0.40f;
    [Tooltip("Çok farklı iki soyun nadiren gen akışı kurabilme olasılığı.")]
    [Range(0f, 0.25f)] public float diversityBridgeChance = 0.07f;
    [Range(0f, 160f)] public float ecologicalMateWeight = 90f;

    [Header("Engel Aşma (Bıyık Sistemi)")]
    public LayerMask obstacleLayer; // Hangi objelerden kaçacak?
    public float avoidDistance = 1.0f; // Bıyıkların uzunluğu

    [Header("Bölgecilik ve Sürü")]
    public Vector2 homePoint;        // Son yemek yediği evin koordinatı
    public bool hasHome = false;       // Evi var mı? (Yoksa göçer)
    public Transform targetFlock;      // Peşine takılacağı sürü (kardeş) lideri

    // Zihinsel değişkenler
    private Vector2 moveDirection;
    private float stateTimer;
    private float eatTimer;
    private float mateTimer; 
    
    private float radarTimer = 0f;
    private float radarCooldown = 0.5f;

    // 🌟 ÇÖZÜM: AÇLIK HAFIZASI 🌟
    private bool isHungryMode = false; 
    private bool isMatingMode = false;

    // Hedefler
    private Transform targetFood;
    private FoodObject currentFoodData;
    
    // Eş hedefi değişkenleri
    private Transform targetMate; 
    private CreatureStats mateStats;

    private Transform targetPrey; 
    private CreatureStats preyStats;
    private float currentAttackCooldown = 0f;
    private float timeSinceMeatMeal = float.PositiveInfinity;

    private CreatureStats threatStats;
    private Transform threatSource;
    private float threatTimer;
    private State selectedThreatResponse = State.Fleeing;

    private CreatureStats stats;

    void Start()
    {
        stats = GetComponent<CreatureStats>();

        ChooseNextAction();
    }

    void Update()
    {
        if (stats == null || stats.dna == null) return;

        if (!float.IsPositiveInfinity(timeSinceMeatMeal))
            timeSinceMeatMeal += Time.deltaTime;

        EvaluatePriorities();
        ExecuteCurrentState();

        // Hareket durumlarını güncelliyoruz
        stats.isMoving = (currentState == State.Wandering || currentState == State.ChasingFood || currentState == State.SeekingMate || currentState == State.ChasingPrey || currentState == State.Fleeing);
        UpdateStateLabel();
    }

    // 🌟 İŞTE DÜZELTİLMİŞ O MUAZZAM KARAR MEKANİZMASI 🌟
    void EvaluatePriorities()
    {
        threatTimer = Mathf.Max(0f, threatTimer - Time.deltaTime);
        if (HasActiveThreat())
        {
            currentState = selectedThreatResponse;
            isMatingMode = false;
            return;
        }

        if (currentState == State.Fleeing || currentState == State.Defending)
        {
            ClearThreat();
            ChooseNextAction();
        }

        if (currentState == State.Eating || currentState == State.Mating || currentState == State.Attacking) return;

        radarTimer -= Time.deltaTime;
        if (radarTimer <= 0f)
        {
            ScanEnvironment(); // TEK BİR RADAR ATAR, HER ŞEYİ BULUR!
            radarTimer = radarCooldown; 
        }

        if (stats.currentEnergy < (stats.currentMaxEnergy * 0.5f)) isHungryMode = true;
        else if (stats.currentEnergy >= (stats.currentMaxEnergy * 0.9f)) isHungryMode = false;

        bool isCriticallyHungry = stats.currentEnergy < (stats.currentMaxEnergy * 0.3f);
        
        float rawCarnivoreAdaptation = Mathf.Sqrt(
            Mathf.Clamp01(stats.dna.desireMeat) * Mathf.Clamp01(stats.dna.meatEfficiency));
        bool hasReproductionFuel = stats.generation <= protectedFounderGenerations
            || rawCarnivoreAdaptation < meatRequiredForReproductionAdaptation
            || timeSinceMeatMeal <= carnivoreMealReproductionWindow;
        bool isFertile = stats.currentStage == LifeStage.Adult && hasReproductionFuel;
        float dynamicMateThreshold = stats.currentMaxEnergy * (stats.dna.reproduceEnergyThreshold / 100f);
        
        if (isFertile && stats.currentEnergy >= dynamicMateThreshold) isMatingMode = true; 
        else if (!isFertile || stats.currentEnergy < (dynamicMateThreshold * 0.7f)) isMatingMode = false; 

        if (stats.currentEnergy < (stats.currentMaxEnergy * stats.dna.migrationThreshold) && targetFood == null && targetPrey == null)
        {
            hasHome = false; 
        }

        // --- HEDEF KARARLARI ---
        if (isCriticallyHungry)
        {
            isMatingMode = false; 
            // 🌟 YENİ: Açlıktan ölürken radar yemek veya av bulduysa ona koş!
            if (targetPrey != null) { currentState = State.ChasingPrey; }
            else if (targetFood != null) { currentState = State.ChasingFood; }
            else if (currentState != State.Wandering) { ForceWander(); }
            return; 
        }

        if (isMatingMode)
        {
            if (currentState != State.SeekingMate) 
            { 
                currentState = State.SeekingMate; 
                // 🌟 FİX 1 (ALZHEİMER ÇÖZÜMÜ): Buradaki "targetMate = null;" komutunu SİLDİK! Artık hafızası silinmeyecek.
            }
            return; 
        }

        if (isHungryMode)
        {
            // 🌟 YENİ: Karnı açsa radarın bulduğu yemeğe veya ava koş!
            if (targetPrey != null) { currentState = State.ChasingPrey; }
            else if (targetFood != null) { currentState = State.ChasingFood; }
            else if (currentState != State.Wandering) { ForceWander(); }
            return;
        }

        if (currentState == State.ChasingFood || currentState == State.SeekingMate || currentState == State.ChasingPrey)
        {
            targetFood = null; targetMate = null; targetPrey = null;
            ChooseNextAction(); 
        }
    }

    void ForceWander()
    {
        currentState = State.Wandering;
        SetWanderDirection();
        stateTimer = maxActionTime;
    }

    void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case State.Idle:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0) ChooseNextAction();
                IdleLookAround(); 
                break;
                
            case State.Wandering:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0) ChooseNextAction();
                MoveAndRotate(); 
                break;
                
            case State.ChasingFood: ChaseFood(); break;
            case State.Eating: EatFood(); break;
            case State.SeekingMate: ChaseMate(); break; 
            case State.Mating: Mate(); break;
            
            // 🌟 YENİ EKLENEN AVCILIK DURUMLARI 🌟
            case State.ChasingPrey: ChasePrey(); break;
            case State.Attacking: AttackPrey(); break;
            case State.Fleeing: FleeFromThreat(); break;
            case State.Defending: DefendAgainstThreat(); break;
        }
    }

    void ChooseNextAction()
    {
        currentState = (Random.value > 0.5f) ? State.Wandering : State.Idle;
        SetWanderDirection();
        stateTimer = Random.Range(minActionTime, maxActionTime);
    }

    // 🌟 YENİ: GEZİNME MOTORU (Göç, Bölgecilik ve Sürü Mantığı)
    void SetWanderDirection()
    {
        // 1. SÜRÜ PSİKOLOJİSİ: Eğer etrafta kanka varsa, SOSYALLİK GENİNE göre karar ver!
        if (targetFlock != null && currentState == State.Wandering)
        {
            // Eğer Sosyallik Geni %80 (0.8f) ise, %80 ihtimalle sürünün peşine takılır, %20 kendi başına takılır.
            if (Random.value <= stats.dna.sociability)
            {
                moveDirection = (targetFlock.position - transform.position).normalized;
                return;
            }
        }

        // 2. BÖLGECİLİK: Eğer yemek yediğimiz bir evimiz varsa, DNA'daki çapa göre ev etrafında turluyoruz.
        if (hasHome)
        {
            Vector2 randomOffset = Random.insideUnitCircle * stats.dna.homeWanderRadius;
            moveDirection = ((homePoint + randomOffset) - (Vector2)transform.position).normalized;
        }
        // 3. GÖÇ (MIGRATION): Eğer ev yoksa ufka doğru yepyeni bir maceraya çık!
        else
        {
            moveDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }
    }

    void MoveAndRotate()
    {
        Vector2 finalDirection = moveDirection;

        if (moveDirection != Vector2.zero)
        {
            // 🌟 YENİ: 3 BIYIKLI ANTEN SİSTEMİ 🌟
            Vector2 forward = transform.up;
            // Sağ ve sol antenler için 30 derecelik açılar oluşturuyoruz
            Vector2 rightAngle = Quaternion.Euler(0, 0, -30) * forward;
            Vector2 leftAngle = Quaternion.Euler(0, 0, 30) * forward;

            bool isAvoiding = false;
            Vector2 avoidanceForce = Vector2.zero;

            // 1. Orta Bıyık (Tam ileri)
            RaycastHit2D hitCenter = Physics2D.Raycast(transform.position, forward, avoidDistance, obstacleLayer);
            if (hitCenter.collider != null) 
            { 
                isAvoiding = true; 
                // 🌟 FİX 2: Sadece geri sekme! Canlıyı kendi "Sağına" doğru it (transform.right).
                // Bu sayede ağaca takılıp titremez, ağacın sağından yumuşakça bir kavis çizerek dolaşır!
                avoidanceForce += (Vector2)transform.right * 2.5f + hitCenter.normal; 
            }

            // 2. Sağ Bıyık
            RaycastHit2D hitRight = Physics2D.Raycast(transform.position, rightAngle, avoidDistance * 0.8f, obstacleLayer);
            if (hitRight.collider != null) { isAvoiding = true; avoidanceForce += hitRight.normal * 1.5f; }

            // 3. Sol Bıyık
            RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, leftAngle, avoidDistance * 0.8f, obstacleLayer);
            if (hitLeft.collider != null) { isAvoiding = true; avoidanceForce += hitLeft.normal * 1.5f; }

            // Eğer herhangi bir bıyık ağaca değdiyse, hedefi boşver ve engelden dışarı doğru (Normal vektörü) kay!
            if (isAvoiding)
            {
                finalDirection = (finalDirection + avoidanceForce).normalized;
            }

            // Dönüşü uygula
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, finalDirection);
            
            // Eğer engelden kaçıyorsa, daha kıvrak dönsün ki ağaca yapışmasın (Çarpanı 2.5f yaptık)
            float turnSpeed = isAvoiding ? stats.currentSpeed * 2.5f : stats.currentSpeed * 1.5f; 
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // Fiziksel olarak ilerle
        transform.Translate(Vector3.up * stats.currentSpeed * Time.deltaTime, Space.Self);

        // ... (Alttaki sınır kontrolü "EcosystemManager isOutOfBounds" kısmı sende nasılsa aynen kalsın, oraya dokunmuyoruz) ...
        if (EcosystemManager.instance != null)
        {
            Vector3 pos = transform.position;
            bool isOutOfBounds = false;

            if (pos.x < EcosystemManager.instance.minX || pos.x > EcosystemManager.instance.maxX ||
                pos.y < EcosystemManager.instance.minY || pos.y > EcosystemManager.instance.maxY)
            {
                isOutOfBounds = true;
                pos.x = Mathf.Clamp(pos.x, EcosystemManager.instance.minX, EcosystemManager.instance.maxX);
                pos.y = Mathf.Clamp(pos.y, EcosystemManager.instance.minY, EcosystemManager.instance.maxY);
                transform.position = pos;
            }

            if (isOutOfBounds && currentState == State.Wandering)
            {
                Vector2 worldCenter = new Vector2((EcosystemManager.instance.minX + EcosystemManager.instance.maxX) / 2, (EcosystemManager.instance.minY + EcosystemManager.instance.maxY) / 2);
                moveDirection = (worldCenter - (Vector2)transform.position).normalized;
            }
        }
    }

    void IdleLookAround()
    {
        // Eğer bakacak bir yönümüz varsa, o yöne doğru yavaşça kafamızı çeviriyoruz
        if (moveDirection != Vector2.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, moveDirection);
            
            // Dönüş hızını normal yürüme dönüşünden biraz daha yavaş yapıyoruz ki etrafı sindire sindire tarasın
            float lookSpeed = stats.currentSpeed * 0.8f; 
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookSpeed * Time.deltaTime);
        }
        // NOT: Burada transform.Translate YOK! Yani canlı adım atmıyor, sadece olduğu yerde dönüyor.
    }

    float GetPoisonAdaptation()
    {
        float desire = Mathf.Clamp01(stats.dna.desirePoison);
        float resistance = Mathf.Clamp01(stats.dna.poisonResistance);
        return Mathf.Sqrt(desire * resistance);
    }

    float GetPoisonNutritionMultiplier()
    {
        return Mathf.Lerp(minimumPoisonNutrition, maximumPoisonNutrition, Mathf.Clamp01(stats.dna.poisonResistance));
    }

    float CalculatePoisonDamage(FoodObject food)
    {
        if (food == null || food.foodData == null) return 0f;

        float resistance = Mathf.Clamp01(stats.dna.poisonResistance);
        float scaledBaseDamage = Mathf.Max(food.foodData.poisonDamage, stats.currentMaxHealth * poisonMaxHealthDamageRatio);
        return scaledBaseDamage * Mathf.Pow(1f - resistance, 1.5f);
    }

    bool CanTargetPoison(FoodObject food)
    {
        float energyRatio = stats.currentMaxEnergy > 0f
            ? stats.currentEnergy / stats.currentMaxEnergy
            : 0f;
        float adaptation = Mathf.InverseLerp(0f, fullPoisonAdaptationThreshold, GetPoisonAdaptation());
        float allowedEnergyThreshold = Mathf.Lerp(
            unadaptedPoisonEmergencyThreshold,
            adaptedPoisonEnergyThreshold,
            adaptation);

        if (energyRatio > allowedEnergyThreshold) return false;

        float expectedDamage = CalculatePoisonDamage(food);
        bool wouldBeImmediatelyFatal = expectedDamage >= stats.currentHealth;
        return !wouldBeImmediatelyFatal;
    }

    float GetCarnivoreAdaptation()
    {
        float desire = Mathf.Clamp01(stats.dna.desireMeat);
        float digestion = Mathf.Clamp01(stats.dna.meatEfficiency);
        float rawAdaptation = Mathf.Sqrt(desire * digestion);
        return Mathf.InverseLerp(0f, fullCarnivoreAdaptationThreshold, rawAdaptation);
    }

    float GetEnergyRatio()
    {
        return stats.currentMaxEnergy > 0f
            ? Mathf.Clamp01(stats.currentEnergy / stats.currentMaxEnergy)
            : 0f;
    }

    float GetPhysicalSize(CreatureStats creature)
    {
        if (creature == null) return 0f;
        Vector3 scale = creature.transform.localScale;
        return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
    }

    public static float CalculateEcologicalSimilarity(CreatureData first, CreatureData second)
    {
        if (first == null || second == null) return 0f;

        Vector3 firstProfile = GetEcologicalProfile(first);
        Vector3 secondProfile = GetEcologicalProfile(second);
        float distance = Mathf.Abs(firstProfile.x - secondProfile.x)
            + Mathf.Abs(firstProfile.y - secondProfile.y)
            + Mathf.Abs(firstProfile.z - secondProfile.z);
        return Mathf.Clamp01(1f - (distance * 0.5f));
    }

    static Vector3 GetEcologicalProfile(CreatureData dna)
    {
        float plant = Mathf.Sqrt(Mathf.Clamp01(dna.desirePlant) * Mathf.Clamp01(dna.plantEfficiency * 0.5f));
        float poison = Mathf.Sqrt(Mathf.Clamp01(dna.desirePoison) * Mathf.Clamp01(dna.poisonResistance));
        float meat = Mathf.Sqrt(Mathf.Clamp01(dna.desireMeat) * Mathf.Clamp01(dna.meatEfficiency * 0.5f));
        float total = Mathf.Max(plant + poison + meat, 0.001f);
        return new Vector3(plant / total, poison / total, meat / total);
    }

    bool IsMateCompatible(CreatureStats candidate, float similarity)
    {
        if (candidate == null) return false;
        if (similarity >= minimumMateSimilarity) return true;

        long lowId = System.Math.Min(stats.observationId, candidate.observationId);
        long highId = System.Math.Max(stats.observationId, candidate.observationId);
        unchecked
        {
            uint hash = (uint)(lowId * 73856093L) ^ (uint)(highId * 19349663L)
                ^ (uint)((stats.generation + candidate.generation) * 83492791);
            float stableRoll = (hash & 0x00FFFFFF) / 16777215f;
            float socialBridge = diversityBridgeChance * Mathf.Lerp(0.75f, 1.25f,
                (stats.dna.sociability + candidate.dna.sociability) * 0.5f);
            return stableRoll <= socialBridge;
        }
    }

    public void RegisterThreat(CreatureStats attacker)
    {
        if (stats == null) stats = GetComponent<CreatureStats>();
        if (stats == null || stats.dna == null || attacker == null || attacker.dna == null
            || attacker == stats || attacker.currentHealth <= 0f) return;

        bool isNewThreat = threatStats != attacker || threatTimer <= 0f;
        threatStats = attacker;
        threatSource = attacker.transform;
        threatTimer = threatMemoryDuration;

        if (!isNewThreat) return;

        float ownSize = Mathf.Max(GetPhysicalSize(stats), 0.01f);
        float attackerSize = Mathf.Max(GetPhysicalSize(attacker), 0.01f);
        float sizeRatio = ownSize / attackerSize;
        float healthRatio = stats.currentMaxHealth > 0f ? Mathf.Clamp01(stats.currentHealth / stats.currentMaxHealth) : 0f;
        float defenseChance;

        if (sizeRatio >= 1.15f)
            defenseChance = smallerThreatDefenseChance;
        else if (sizeRatio >= 0.85f)
            defenseChance = Mathf.Lerp(lastStandDefenseChance, smallerThreatDefenseChance, 0.45f);
        else
            defenseChance = lastStandDefenseChance * Mathf.Lerp(1.5f, 0.35f, healthRatio);

        float attackCost = Mathf.Max(stats.dna.baseSize * stats.dna.attackEnergyCost, 0.01f);
        bool canDefend = stats.currentEnergy >= attackCost && stats.currentAttackDamage > 0f;
        selectedThreatResponse = canDefend && Random.value <= Mathf.Clamp01(defenseChance)
            ? State.Defending
            : State.Fleeing;

        CancelMatingForThreat();
    }

    bool HasActiveThreat()
    {
        return threatTimer > 0f && threatSource != null && threatStats != null && threatStats.currentHealth > 0f;
    }

    void ClearThreat()
    {
        threatStats = null;
        threatSource = null;
        threatTimer = 0f;
    }

    void CancelMatingForThreat()
    {
        if (targetMate != null)
        {
            LeaterBrain partner = targetMate.GetComponent<LeaterBrain>();
            if (partner != null && partner.targetMate == transform)
            {
                partner.targetMate = null;
                partner.mateStats = null;
                if (partner.currentState == State.Mating) partner.currentState = State.Idle;
            }
        }

        targetMate = null;
        mateStats = null;
        isMatingMode = false;
    }

    void FleeFromThreat()
    {
        if (!HasActiveThreat()) return;

        moveDirection = ((Vector2)transform.position - (Vector2)threatSource.position).normalized;
        if (moveDirection == Vector2.zero) moveDirection = Random.insideUnitCircle.normalized;

        float normalSpeed = stats.currentSpeed;
        stats.currentSpeed *= fleeSpeedMultiplier;
        MoveAndRotate();
        stats.currentSpeed = normalSpeed;
    }

    void DefendAgainstThreat()
    {
        if (!HasActiveThreat()) return;

        Vector2 direction = (threatSource.position - transform.position).normalized;
        if (direction != Vector2.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, stats.currentSpeed * 2f * Time.deltaTime);
        }

        float attackDistance = stats.dna.attackDistance + ((stats.dna.baseSize + threatStats.dna.baseSize) * 0.4f);
        if (Vector2.Distance(transform.position, threatSource.position) > attackDistance) return;

        currentAttackCooldown -= Time.deltaTime;
        float attackEnergyCost = Mathf.Max(stats.dna.baseSize * stats.dna.attackEnergyCost, 0.01f);
        if (currentAttackCooldown > 0f || stats.currentEnergy < attackEnergyCost) return;

        float damage = stats.currentAttackDamage;
        threatStats.TakeDamage(damage, SimulationDeathCause.Predation, stats);
        stats.currentEnergy = Mathf.Max(0f, stats.currentEnergy - attackEnergyCost);
        stats.lifetimeAttacks++;
        stats.lifetimeDamageDealt += damage;
        SimulationEventLogger.RecordAttack(stats, threatStats, damage);
        currentAttackCooldown = stats.dna.attackCooldown;
    }

    bool CanTargetCarrion()
    {
        float allowedEnergyThreshold = Mathf.Lerp(
            unadaptedCarrionEmergencyThreshold,
            adaptedCarnivoreEnergyThreshold,
            GetCarnivoreAdaptation());
        return GetEnergyRatio() <= allowedEnergyThreshold;
    }

    bool CanHuntLivingPrey(CreatureStats candidate)
    {
        if (candidate == null || candidate.dna == null || candidate.currentHealth <= 0f) return false;

        bool protectedFounder = candidate.dna.ecologicalLineage != EcologicalLineage.Unassigned
            && candidate.generation <= protectedFounderGenerations;
        if (protectedFounder) return false;

        bool sameTrackedLineage = stats.dna.ecologicalLineage != EcologicalLineage.Unassigned
            && stats.dna.ecologicalLineage == candidate.dna.ecologicalLineage;
        if (sameTrackedLineage && GetEnergyRatio() > sameLineageCannibalEmergencyThreshold) return false;

        float adaptation = GetCarnivoreAdaptation();
        float allowedEnergyThreshold = Mathf.Lerp(
            unadaptedHuntEmergencyThreshold,
            adaptedCarnivoreEnergyThreshold,
            adaptation);
        if (GetEnergyRatio() > allowedEnergyThreshold) return false;

        float ownSize = GetPhysicalSize(stats);
        float preySize = Mathf.Max(GetPhysicalSize(candidate), 0.01f);
        float requiredSizeRatio = Mathf.Lerp(
            opportunistRequiredSizeRatio,
            predatorRequiredSizeRatio,
            adaptation);
        if ((ownSize / preySize) < requiredSizeRatio) return false;

        float healingBetweenBites = candidate.currentHealingRate * Mathf.Max(stats.dna.attackCooldown, 0.2f);
        float damagePerBite = stats.currentAttackDamage - healingBetweenBites;
        if (damagePerBite <= 0f) return false;

        float attackEnergyCost = Mathf.Max(stats.dna.baseSize * stats.dna.attackEnergyCost, 0.01f);
        float expectedBites = Mathf.Ceil(candidate.currentHealth / damagePerBite);
        float expectedHuntCost = expectedBites * attackEnergyCost;
        return expectedHuntCost <= stats.currentEnergy;
    }


    // 🌟 4'Ü 1 ARADA RADAR SİSTEMİ (Yemek, Av, Eş ve Sürü)
    void ScanEnvironment()
    {
        float currentScanRadius = Mathf.Max(stats.dna.visionRadius, stats.dna.smellRadius);
        if (isMatingMode) currentScanRadius *= 3f;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, currentScanRadius);
        
        float bestFoodScore = -1f; Transform bestFoodTarget = null; FoodObject bestFoodData = null; CreatureStats bestPrey = null;
        float bestMateScore = -1f; Transform bestMateTarget = null; CreatureStats bestMateStats = null;
        float closestFlockDist = Mathf.Infinity; Transform bestFlockTarget = null;

        foreach (var hit in hitColliders)
        {
            if (hit.gameObject == this.gameObject) continue;

            float dist = Vector2.Distance(transform.position, hit.transform.position);
            float safeDist = Mathf.Max(dist, 0.1f); 
            Vector2 dirToTarget = (hit.transform.position - transform.position).normalized;
            float angleToTarget = Vector2.Angle(transform.up, dirToTarget);

            bool canSee = (dist <= stats.dna.visionRadius && angleToTarget <= stats.dna.visionAngle / 2f);
            bool canSmell = (dist <= (isMatingMode ? currentScanRadius : stats.dna.smellRadius));

            if (!canSee && !canSmell) continue;

            FoodObject food = hit.GetComponent<FoodObject>();
            CreatureStats otherCreature = hit.GetComponent<CreatureStats>();
            LeaterBrain otherBrain = hit.GetComponent<LeaterBrain>();

            if (food != null)
            {
                float desireMult = 0f; float effMult = 0f;
                if (food.type == FoodType.Plant)
                {
                    desireMult = stats.dna.desirePlant;
                    effMult = stats.dna.plantEfficiency;
                }
                else if (food.type == FoodType.PoisonousPlant)
                {
                    if (!CanTargetPoison(food)) continue;

                    desireMult = stats.dna.desirePoison;
                    effMult = stats.dna.plantEfficiency * GetPoisonNutritionMultiplier();
                }
                else if (food.type == FoodType.Meat)
                {
                    if (!CanTargetCarrion()) continue;

                    desireMult = stats.dna.desireMeat;
                    effMult = stats.dna.meatEfficiency;
                }

                float score = (food.currentEnergy * effMult / safeDist) * desireMult;
                if (score > bestFoodScore) { bestFoodScore = score; bestFoodTarget = hit.transform; bestFoodData = food; bestPrey = null; }
            }
            else if (otherCreature != null && otherBrain != null)
            {
                // A) EŞ ADAYI MI?
                if (isMatingMode && otherBrain.currentState == State.SeekingMate)
                {
                    bool isOtherFertile = otherCreature.currentStage == LifeStage.Adult;
                    if (isOtherFertile)
                    {
                        float ecologicalSimilarity = CalculateEcologicalSimilarity(stats.dna, otherCreature.dna);
                        if (!IsMateCompatible(otherCreature, ecologicalSimilarity)) continue;

                        // 🌟 CİNSEL SEÇİLİM (SEXUAL SELECTION) ALGORİTMASI 🌟
                        // 1. Temel cazibe: Yakında olmak her zaman ufak bir avantajdır (Enerji tasarrufu)
                        float mateScore = (10f / safeDist) + (ecologicalSimilarity * ecologicalMateWeight);

                        // 2. ZEHİR TÜKETİCİLERİ: Kendilerinden daha dayanıklı olanlara aşık olurlar!
                        if (stats.dna.desirePoison >= 0.3f) 
                            mateScore += (otherCreature.dna.poisonResistance * 50f);

                        // 3. YIRTICILAR: Güçlü çenesi (yüksek hasarı) ve eti iyi sindirebilenlere ilgi duyar!
                        if (stats.dna.desireMeat >= 0.35f) 
                            mateScore += (otherCreature.dna.attackDamageMultiplier) + (otherCreature.dna.meatEfficiency * 20f);

                        // 4. OTÇULLAR: İri kıyım (hayatta kalma şansı yüksek) ve iyi ot sindirenlere ilgi duyar!
                        if (stats.dna.desirePlant >= 0.4f) 
                            mateScore += (otherCreature.dna.plantEfficiency * 30f) + (otherCreature.dna.baseSize * 15f);

                        // 5. TÜR (BOYUT) BENZERLİĞİ: Sürü toleransına uygun olanlara (kendi türüne) devasa bir bonus!
                        // Bu özellik, farenin fille çiftleşmesini engeller ve türlerin kendi içinde "safkan" kalmasını sağlar.
                        float sizeDiff = Mathf.Abs(stats.dna.baseSize - otherCreature.dna.baseSize);
                        if (sizeDiff <= stats.dna.flockTolerance) 
                            mateScore += 40f; 

                        // Puanı en yüksek olan (En cazip olan) eş adayını hafızaya kazı!
                        if (mateScore > bestMateScore)
                        {
                            bestMateScore = mateScore; 
                            bestMateTarget = hit.transform; 
                            bestMateStats = otherCreature;
                        }
                        continue; 
                    }
                }

                if (CanHuntLivingPrey(otherCreature))
                {
                    float sizeAdvantage = GetPhysicalSize(stats) / Mathf.Max(GetPhysicalSize(otherCreature), 0.01f);
                    float vulnerability = stats.currentAttackDamage / Mathf.Max(otherCreature.currentHealth, 1f);
                    float score = (sizeAdvantage * vulnerability * stats.dna.desireMeat * stats.dna.meatEfficiency / safeDist) * 100f;
                    if (score > bestFoodScore) { bestFoodScore = score; bestFoodTarget = hit.transform; bestFoodData = null; bestPrey = otherCreature; }
                    continue;
                }

                if (!isMatingMode && !isHungryMode && canSee) 
                {
                    float sizeDiff = Mathf.Abs(stats.dna.baseSize - otherCreature.dna.baseSize);
                    if (sizeDiff <= stats.dna.flockTolerance && dist < closestFlockDist) 
                    {
                        closestFlockDist = dist; bestFlockTarget = hit.transform;
                    }
                }
            }
        }

        // 🌟 FİX 2: SONUÇLARI KAYDET (ASLA currentState DEĞİŞTİRME!) 🌟
        // Radar sadece hedefleri bulur, beyni (State) karıştırmaz. Kararı üstteki EvaluatePriorities verir.
        if (bestFoodTarget != null) 
        { 
            if (bestPrey != null) { targetPrey = bestFoodTarget; preyStats = bestPrey; targetFood = null; }
            else { targetFood = bestFoodTarget; currentFoodData = bestFoodData; targetPrey = null; }
        }
        
        // ÖNEMLİ: Sadece YENİ bir eş bulduysa hafızayı günceller, bulamadıysa (null ise) eskisine koşmaya devam eder.
        if (bestMateTarget != null) { targetMate = bestMateTarget; mateStats = bestMateStats; }
        
        targetFlock = bestFlockTarget; 
    }

    // --- AVCILIK FONKSİYONLARI ---
    void ChasePrey()
    {
        if (targetPrey == null || preyStats == null || preyStats.currentHealth <= 0)
        {
            targetPrey = null;
            preyStats = null;
            currentState = State.Idle;
            return;
        }

        NotifyPreyOfThreat();
        
        // 🌟 FİX: Dinamik Avlanma Mesafesi 🌟
        float dynamicAttackDistance = stats.dna.attackDistance + ((stats.dna.baseSize + preyStats.dna.baseSize) * 0.4f);

        if (Vector2.Distance(transform.position, targetPrey.position) <= dynamicAttackDistance)
        {
            currentState = State.Attacking;
            return;
        }

        moveDirection = (targetPrey.position - transform.position).normalized;
        MoveAndRotate();
    }

    void AttackPrey()
    {
        if (targetPrey == null || preyStats == null || preyStats.currentHealth <= 0) 
        { 
            targetPrey = null;
            preyStats = null;
            currentState = State.Idle; 
            return; 
        }

        NotifyPreyOfThreat();

        float healthRatio = stats.currentMaxHealth > 0f
            ? stats.currentHealth / stats.currentMaxHealth
            : 0f;
        if (healthRatio <= huntRetreatHealthThreshold)
        {
            CreatureStats dangerousPrey = preyStats;
            RegisterThreat(dangerousPrey);
            selectedThreatResponse = State.Fleeing;
            targetPrey = null;
            preyStats = null;
            currentState = State.Fleeing;
            return;
        }

        float dynamicAttackDistance = stats.dna.attackDistance + ((stats.dna.baseSize + preyStats.dna.baseSize) * 0.4f);
        if (Vector2.Distance(transform.position, targetPrey.position) > dynamicAttackDistance)
        {
            currentState = State.ChasingPrey; 
            return;
        }

        float attackEnergyCost = stats.dna.baseSize * stats.dna.attackEnergyCost;
        if (stats.currentEnergy < attackEnergyCost)
        {
            targetPrey = null;
            preyStats = null;
            currentState = State.Idle;
            return;
        }

        currentAttackCooldown -= Time.deltaTime;
        
        if (currentAttackCooldown <= 0)
        {
            float damage = stats.currentAttackDamage;
            float preyHealthBeforeAttack = preyStats.currentHealth;
            preyStats.TakeDamage(damage, SimulationDeathCause.Predation, stats);
            stats.lifetimeAttacks++;
            stats.lifetimeDamageDealt += damage;
            SimulationEventLogger.RecordAttack(stats, preyStats, damage);

            if (preyHealthBeforeAttack > 0f && preyStats.currentHealth <= 0f)
            {
                ConsumeFreshKill(preyStats);
            }
            
            // 🌟 ARTIK DNA'DAN OKUYOR: Genetik Enerji Bedeli
            stats.currentEnergy = Mathf.Max(0f, stats.currentEnergy - attackEnergyCost);

            currentAttackCooldown = stats.dna.attackCooldown; // Soğuma süresini DNA'dan alıp sıfırla
        }
    }

    void ConsumeFreshKill(CreatureStats defeatedPrey)
    {
        if (defeatedPrey == null || stats.dna.meatEfficiency <= 0f) return;

        float rawEnergy = Mathf.Max(25f, defeatedPrey.currentMaxEnergy * freshKillEnergyRatio);
        float digestedEnergy = rawEnergy * stats.dna.meatEfficiency;
        stats.currentEnergy = Mathf.Min(stats.currentMaxEnergy, stats.currentEnergy + digestedEnergy);
        stats.lifetimeMeatEaten++;
        timeSinceMeatMeal = 0f;
        stats.AddGrowth(rawEnergy, stats.dna.meatEfficiency);
        homePoint = transform.position;
        hasHome = true;
        SimulationEventLogger.RecordFoodConsumed(stats, FoodType.Meat, digestedEnergy);
    }

    void NotifyPreyOfThreat()
    {
        if (targetPrey == null || preyStats == null) return;
        LeaterBrain preyBrain = targetPrey.GetComponent<LeaterBrain>();
        if (preyBrain != null) preyBrain.RegisterThreat(stats);
    }

    void ChaseFood()
    {
        if (targetFood == null) { currentState = State.Idle; return; }
        
        // 🌟 FİX: Dinamik Yemek Mesafesi 🌟
        float dynamicEatDistance = eatDistance + (stats.dna.baseSize * 0.4f);

        if (Vector2.Distance(transform.position, targetFood.position) <= dynamicEatDistance)
        {
            currentState = State.Eating;
            eatTimer = stats.dna.eatDuration; 
            return;
        }

        moveDirection = (targetFood.position - transform.position).normalized;
        MoveAndRotate();
    }

    void EatFood()
    {
        if (targetFood == null || currentFoodData == null) { currentState = State.Idle; return; }
        
        eatTimer -= Time.deltaTime;
        if (eatTimer <= 0)
        {
            FoodType eatenFoodType = currentFoodData.type;
            FoodData eatenFoodData = currentFoodData.foodData;
            float poisonDamage = eatenFoodType == FoodType.PoisonousPlant
                ? CalculatePoisonDamage(currentFoodData)
                : 0f;
            float gainedEnergy = currentFoodData.Consume();
            
            if (gainedEnergy > 0)
            {
                // 🌟 MİDE KONTROLÜ: Acaba ot mu yedim et mi? 🌟
                float activeEfficiency = 0f;

                if (eatenFoodType == FoodType.Plant)
                    activeEfficiency = stats.dna.plantEfficiency;
                else if (eatenFoodType == FoodType.PoisonousPlant)
                    activeEfficiency = stats.dna.plantEfficiency * GetPoisonNutritionMultiplier();
                else if (eatenFoodType == FoodType.Meat)
                {
                    activeEfficiency = stats.dna.meatEfficiency;
                    timeSinceMeatMeal = 0f;
                }

                // Enerjiyi doğru sindirim katsayısıyla al! 
                float digestedEnergy = gainedEnergy * activeEfficiency;
                stats.currentEnergy += digestedEnergy;

                if (eatenFoodType == FoodType.Plant) stats.lifetimePlantsEaten++;
                else if (eatenFoodType == FoodType.PoisonousPlant) stats.lifetimePoisonPlantsEaten++;
                else if (eatenFoodType == FoodType.Meat) stats.lifetimeMeatEaten++;
                SimulationEventLogger.RecordFoodConsumed(stats, eatenFoodType, digestedEnergy);

                // 🌟 YENİ: YEMEK YEDİĞİN YERİ EVİN YAP!
                homePoint = transform.position;
                hasHome = true;

                if (stats.currentEnergy > stats.currentMaxEnergy) stats.currentEnergy = stats.currentMaxEnergy;
                
                // Büyümeye de ne yediğini ve ne kadar sindirebildiğini yolla (HATA BURADAYDI, DÜZELTİLDİ)
                stats.AddGrowth(gainedEnergy, activeEfficiency); 

                // 🌟 ZEHİR HASARI HESAPLAMASI 🌟
                if (eatenFoodType == FoodType.PoisonousPlant && eatenFoodData != null)
                {
                    stats.TakeDamage(poisonDamage, SimulationDeathCause.Poison);
                }
            }

            targetFood = null;
            currentFoodData = null;
            currentState = State.Idle;
        }
    }


    void ChaseMate()
    {
        if (targetMate == null || mateStats == null)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0)
            {
                moveDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                stateTimer = Random.Range(minActionTime, maxActionTime);
            }
            MoveAndRotate();
            return;
        }

        LeaterBrain mateBrain = targetMate.GetComponent<LeaterBrain>();
        float ecologicalSimilarity = CalculateEcologicalSimilarity(stats.dna, mateStats.dna);
        if (mateBrain == null || mateBrain.currentState != State.SeekingMate || !IsMateCompatible(mateStats, ecologicalSimilarity))
        {
            targetMate = null; mateStats = null; return;
        }

        // 🌟 FİX: Çarpışma Engelleyici (Dinamik Mesafe) 🌟
        // İki canlının boyutunun yarısını mesafeye ekliyoruz, böylece göbekleri devasa bile olsa rahatça eşleşiyorlar!
        float dynamicMateDistance = mateDistance + ((stats.dna.baseSize + mateStats.dna.baseSize) * 0.4f);

        if (Vector2.Distance(transform.position, targetMate.position) <= dynamicMateDistance)
        {
            currentState = State.Mating;
            mateTimer = mateDuration; 
            
            mateBrain.currentState = State.Mating;
            mateBrain.mateTimer = mateDuration;
            mateBrain.targetMate = this.transform; 
            mateBrain.mateStats = this.stats;
            return;
        }

        moveDirection = (targetMate.position - transform.position).normalized;
        MoveAndRotate();
    }

    void Mate()
    {
        // 1. GÜVENLİK KONTROLÜ: Çiftleşirken eş aniden silinirse/ölürse işlemi iptal et
        if (targetMate == null || mateStats == null)
        {
            currentState = State.Idle;
            return;
        }

        // Dans (Çiftleşme) süresini geriye say
        mateTimer -= Time.deltaTime;
        
        if (mateTimer <= 0)
        {
            // Anne ve Baba enerjilerini harcar
            float protectedReserve = stats.currentMaxEnergy * minimumPostMatingEnergyRatio;
            float affordableCost = Mathf.Max(0f, stats.currentEnergy - protectedReserve);
            stats.currentEnergy -= Mathf.Min(stats.dna.reproductionEnergyCost, affordableCost);
            if (stats.currentEnergy > stats.currentMaxEnergy) stats.currentEnergy = stats.currentMaxEnergy;

            // Bebek sadece BİR kere doğsun diye, ID'si büyük olan taraf doğumu üstlenir
            if (gameObject.GetEntityId() > targetMate.gameObject.GetEntityId())
            {
                Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f);
                
                // Bebeğin fiziksel bedeni yaratılır
                GameObject baby = Instantiate(gameObject, spawnPosition, Quaternion.identity);
                CreatureStats babyStats = baby.GetComponent<CreatureStats>();
                babyStats.ResetLifetimeObservationStats();
                stats.lifetimeOffspring++;
                mateStats.lifetimeOffspring++;
                SimulationEventLogger.RecordBirth(stats, mateStats);
                
                // 🌟 MUCİZE BURADA GERÇEKLEŞİYOR! 🌟
                babyStats.dna = CreatureData.CreateMix(this.stats.dna, this.mateStats.dna);
                babyStats.ApplyMutation();
                
                babyStats.age = 0;
                babyStats.growthProgress = 0;
                
                // 🌟 BENJAMİN BUTTON ÇÖZÜMÜ 🌟
                // Bebeği zorla "Genç (Young)" statüsüne ve küçük boyutuna sokuyoruz!
                babyStats.UpdateLifeStage(); 
                babyStats.currentHealth = babyStats.currentMaxHealth; // Bebek canı full doğar
                babyStats.currentEnergy = babyStats.currentMaxEnergy; // Bebek enerjisi full doğar
                
                // Nesil (Generation) hesabı: Hangi ebeveyn daha yaşlı bir nesilse, onun bir fazlası olur
                babyStats.generation = Mathf.Max(this.stats.generation, this.mateStats.generation) + 1; 
                babyStats.dna.name = "DNA_Gen_" + babyStats.generation;
                baby.name = "Leater-V" + babyStats.generation; 
                
                // Bebeğin rengini, mutasyona uğramış YENİ MELEZ GENETİĞİNDEKİ renk yapıyoruz!
                SpriteRenderer babySr = baby.GetComponent<SpriteRenderer>();
                if (babySr != null) babySr.color = babyStats.dna.skinColor;
                
                LeaterBrain babyBrain = baby.GetComponent<LeaterBrain>();
                babyBrain.currentState = State.Idle;
                
                // 🌟 YENİ: SOY TAKİBİ (KAMERA VE PANEL ODAĞI) 🌟
                if (DebugController.instance != null)
                {
                    // Eğer oyuncunun kamerası şu an anneyi veya babayı izliyorsa...
                    if (DebugController.instance.selectedCreature == this.stats || DebugController.instance.selectedCreature == this.mateStats)
                    {
                        // Odağı ve paneli anında yeni doğan bebeğe geçir!
                        DebugController.instance.selectedCreature = babyStats;
                    }
                }
            }

            // İŞLEM BİTTİ, NORMAL HAYATA DÖN!
            targetMate = null;
            mateStats = null;
            currentState = State.Idle;
        }
    }

    // --- GÖRSEL HATA AYIKLAMA (UI ve Gizmos) ---
    void UpdateStateLabel()
        {
            if (currentState == State.Idle) stats.currentStateName = "Bekliyor (Idle)";
            else if (currentState == State.Wandering) stats.currentStateName = "Geziniyor (Wandering)";
            else if (currentState == State.ChasingFood) stats.currentStateName = "<color=#FF8C00>Yemeğe Koşuyor!</color>";
            else if (currentState == State.Eating) stats.currentStateName = "<color=#32CD32>Yemek Yiyor...</color>";
            else if (currentState == State.Mating) stats.currentStateName = "<color=#FF00FF>Çiftleşiyor! </color>";
            else if (currentState == State.ChasingPrey) stats.currentStateName = "<color=#8B0000><b>AV KOVALIYOR!</b></color>";
            else if (currentState == State.Attacking) stats.currentStateName = "<color=#FF0000><b>SALDIRIYOR!</b></color>";
            else if (currentState == State.Fleeing) stats.currentStateName = "<color=#00BFFF><b>TEHDİTTEN KAÇIYOR!</b></color>";
            else if (currentState == State.Defending) stats.currentStateName = "<color=#FFB000><b>KENDİNİ SAVUNUYOR!</b></color>";
            else if (currentState == State.SeekingMate) 
            {
                // Ekranda gerçekten eşi bulup bulmadığını daha net anlaman için ufak bir detay
                if (targetMate == null) stats.currentStateName = "<color=#FF69B4>Eş Arıyor (Geziniyor)</color>";
                else stats.currentStateName = "<color=#FF1493>Eşine Koşuyor! </color>";
            }
        }
        
void OnDrawGizmos()
    {
        CreatureStats myStats = GetComponent<CreatureStats>();
        if (myStats == null || DebugController.instance == null || DebugController.instance.selectedCreature != myStats) return;
        
        if (myStats.dna != null)
        {
            // 🟢 KOKU ÇEMBERİ (Yarı saydam yeşil)
            Gizmos.color = new Color(0f, 1f, 0f, 0.12f); 
            Gizmos.DrawWireSphere(transform.position, myStats.dna.smellRadius);

            // 🟡 GÖRÜŞ HUNİSİ (Sarı)
            Gizmos.color = Color.yellow;
            Vector3 rightLimit = Quaternion.Euler(0, 0, -myStats.dna.visionAngle / 2f) * transform.up;
            Vector3 leftLimit = Quaternion.Euler(0, 0, myStats.dna.visionAngle / 2f) * transform.up;

            Gizmos.DrawLine(transform.position, transform.position + rightLimit * myStats.dna.visionRadius);
            Gizmos.DrawLine(transform.position, transform.position + leftLimit * myStats.dna.visionRadius);

            // 🔴 SALDIRI MENZİLİ (Yarı saydam kırmızı çember)
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, myStats.dna.attackDistance);

            // 🌟 1. MAVİ EV BÖLGESİ (Bölgecilik Görselleştirmesi) 🌟
            if (hasHome)
            {
                Vector3 homeV3 = new Vector3(homePoint.x, homePoint.y, 0f);

                // Evin sınır çapını yarı saydam mavi bir daireyle çiziyoruz
                Gizmos.color = new Color(0f, 0.6f, 1f, 0.20f);
                Gizmos.DrawWireSphere(homeV3, myStats.dna.homeWanderRadius);

                // Evin tam merkezine küçük bir mavi çekirdek koyuyoruz
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(homeV3, 0.25f);

                // Canlıdan evine doğru ince, kesik hissi veren mavi bir bağ çizgisi çekiyoruz
                Gizmos.color = new Color(0f, 0.4f, 1f, 0.5f);
                Gizmos.DrawLine(transform.position, homeV3);
            }
        }

        if (targetFood != null) { Gizmos.color = Color.red; Gizmos.DrawLine(transform.position, targetFood.position); }
        if (targetMate != null) { Gizmos.color = Color.magenta; Gizmos.DrawLine(transform.position, targetMate.position); }
        if (targetPrey != null) { Gizmos.color = new Color(0.6f, 0f, 0f); Gizmos.DrawLine(transform.position, targetPrey.position); }
        if (threatSource != null) { Gizmos.color = Color.cyan; Gizmos.DrawLine(transform.position, threatSource.position); }

        // 🌟 2. SÜRÜ LİDERİ TAKİP ÇİZGİSİ (Turkuaz Çizgi) 🌟
        if (targetFlock != null)
        {
            // Takip ettiği arkadaşına/liderine turkuaz bir çizgi çeker
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetFlock.position);

            // Sürü liderinin etrafına küçük bir halka çizerek onu "Lider" olarak işaretler
            Gizmos.DrawWireSphere(targetFlock.position, 0.45f);
        }

        // 🔵 BIYIKLAR (Mavi Çizgiler)
        Gizmos.color = Color.cyan;
        Vector3 fwd = transform.up;
        Vector3 r = Quaternion.Euler(0, 0, -30) * fwd;
        Vector3 l = Quaternion.Euler(0, 0, 30) * fwd;
        Gizmos.DrawRay(transform.position, fwd * avoidDistance);
        Gizmos.DrawRay(transform.position, r * (avoidDistance * 0.8f));
        Gizmos.DrawRay(transform.position, l * (avoidDistance * 0.8f));
    }
}
