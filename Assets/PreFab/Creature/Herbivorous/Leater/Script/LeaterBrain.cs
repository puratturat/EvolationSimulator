using UnityEngine;

public class LeaterBrain : MonoBehaviour
{
    // YENİ DURUMLAR EKLENDİ (SeekingMate, Mating)
    public enum State { Idle, Wandering, ChasingFood, Eating, SeekingMate, Mating, ChasingPrey, Attacking }

    public State currentState;

    [Header("Zamanlayıcı Ayarları")]
    public float minActionTime = 1f; 
    public float maxActionTime = 3f; 

    [Header("Etkileşim Ayarları")]
    public float eatDistance = 1.2f; 
    public float mateDistance = 1.5f; 
    public float mateDuration = 3f; 

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

    private CreatureStats stats;

    void Start()
    {
        stats = GetComponent<CreatureStats>();

        ChooseNextAction();
    }

    void Update()
    {
        if (stats == null || stats.dna == null) return;

        EvaluatePriorities();
        ExecuteCurrentState();

        // Hareket durumlarını güncelliyoruz
        stats.isMoving = (currentState == State.Wandering || currentState == State.ChasingFood || currentState == State.SeekingMate || currentState == State.ChasingPrey);
        UpdateStateLabel();
    }

    // 🌟 İŞTE DÜZELTİLMİŞ O MUAZZAM KARAR MEKANİZMASI 🌟
    void EvaluatePriorities()
    {
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
        
        bool isFertile = stats.currentStage == LifeStage.Adult;
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
                if (food.type == FoodType.Plant || food.type == FoodType.PoisonousPlant) { desireMult = (food.type == FoodType.Plant) ? stats.dna.desirePlant : stats.dna.desirePoison; effMult = stats.dna.plantEfficiency; }
                else if (food.type == FoodType.Meat) { desireMult = stats.dna.desireMeat; effMult = stats.dna.meatEfficiency; }

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
                        // 🌟 CİNSEL SEÇİLİM (SEXUAL SELECTION) ALGORİTMASI 🌟
                        // 1. Temel cazibe: Yakında olmak her zaman ufak bir avantajdır (Enerji tasarrufu)
                        float mateScore = 10f / safeDist; 

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

                if (stats.dna.desireMeat > 0.3f && stats.dna.meatEfficiency > 0.2f)
                {
                    float courage = stats.dna.baseSize / otherCreature.dna.baseSize;
                    if (courage > 0.6f)
                    {
                        float score = (courage * stats.dna.desireMeat * stats.dna.meatEfficiency / safeDist) * 50f;
                        if (score > bestFoodScore) { bestFoodScore = score; bestFoodTarget = hit.transform; bestFoodData = null; bestPrey = otherCreature; }
                        continue;
                    }
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
            preyStats.currentHealth -= stats.currentAttackDamage;
            
            // 🌟 ARTIK DNA'DAN OKUYOR: Genetik Enerji Bedeli
            stats.currentEnergy = Mathf.Max(0f, stats.currentEnergy - attackEnergyCost);

            currentAttackCooldown = stats.dna.attackCooldown; // Soğuma süresini DNA'dan alıp sıfırla
        }
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
            float gainedEnergy = currentFoodData.Consume();
            
            if (gainedEnergy > 0)
            {
                // 🌟 MİDE KONTROLÜ: Acaba ot mu yedim et mi? 🌟
                float activeEfficiency = 0f;

                if (currentFoodData.type == FoodType.Plant || currentFoodData.type == FoodType.PoisonousPlant)
                    activeEfficiency = stats.dna.plantEfficiency;
                else if (currentFoodData.type == FoodType.Meat)
                    activeEfficiency = stats.dna.meatEfficiency;

                // Enerjiyi doğru sindirim katsayısıyla al! 
                stats.currentEnergy += gainedEnergy * activeEfficiency;

                // 🌟 YENİ: YEMEK YEDİĞİN YERİ EVİN YAP!
                homePoint = transform.position;
                hasHome = true;

                if (stats.currentEnergy > stats.currentMaxEnergy) stats.currentEnergy = stats.currentMaxEnergy;
                
                // Büyümeye de ne yediğini ve ne kadar sindirebildiğini yolla (HATA BURADAYDI, DÜZELTİLDİ)
                stats.AddGrowth(gainedEnergy, activeEfficiency); 

                // 🌟 ZEHİR HASARI HESAPLAMASI 🌟
                if (currentFoodData.type == FoodType.PoisonousPlant && currentFoodData.foodData != null)
                {
                    float actualDamage = currentFoodData.foodData.poisonDamage * (1f - stats.dna.poisonResistance);
                    stats.currentHealth -= actualDamage;
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
        if (mateBrain == null || mateBrain.currentState != State.SeekingMate)
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
            stats.currentEnergy -= stats.dna.reproductionEnergyCost;
            if (stats.currentEnergy > stats.currentMaxEnergy) stats.currentEnergy = stats.currentMaxEnergy;

            // Bebek sadece BİR kere doğsun diye, ID'si büyük olan taraf doğumu üstlenir
            if (gameObject.GetEntityId() > targetMate.gameObject.GetEntityId())
            {
                Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f);
                
                // Bebeğin fiziksel bedeni yaratılır
                GameObject baby = Instantiate(gameObject, spawnPosition, Quaternion.identity);
                CreatureStats babyStats = baby.GetComponent<CreatureStats>();
                
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
