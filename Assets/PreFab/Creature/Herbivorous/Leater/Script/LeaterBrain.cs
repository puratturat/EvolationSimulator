using UnityEngine;

public class LeaterBrain : MonoBehaviour
{
    // YENİ DURUMLAR EKLENDİ (SeekingMate, Mating)
    public enum State { Idle, Wandering, ChasingFood, Eating, SeekingMate, Mating }

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

    private CreatureStats stats;

    void Start()
    {
        stats = GetComponent<CreatureStats>();

        ChooseNextAction();
    }

    void Update()
    {
        EvaluatePriorities();
        ExecuteCurrentState();

        // Hareket durumlarını güncelliyoruz
        stats.isMoving = (currentState == State.Wandering || currentState == State.ChasingFood || currentState == State.SeekingMate);
        UpdateStateLabel();
    }

    // 🌟 İŞTE DÜZELTİLMİŞ O MUAZZAM KARAR MEKANİZMASI 🌟
    void EvaluatePriorities()
    {
        if (currentState == State.Eating || currentState == State.Mating) return;

        if (stats.currentEnergy < (stats.currentMaxEnergy * 0.5f)) isHungryMode = true;
        else if (stats.currentEnergy >= (stats.currentMaxEnergy * 0.9f)) isHungryMode = false;

        bool isCriticallyHungry = stats.currentEnergy < (stats.currentMaxEnergy * 0.3f);
        
        bool isFertile = (stats.currentStage == LifeStage.Adult) || (stats.currentStage == LifeStage.Old && stats.age <= 17);
        float dynamicMateThreshold = stats.currentMaxEnergy * (stats.dna.reproduceEnergyThreshold / 100f);
        
        // 🌟 KARIŞIK GENLER İÇİN ÜREME HAFIZASI (TITREME ÇÖZÜMÜ) 🌟
        if (isFertile && stats.currentEnergy >= dynamicMateThreshold) 
        {
            isMatingMode = true; // Eş arama moduna kilitlendi
        }
        else if (!isFertile || stats.currentEnergy < (dynamicMateThreshold * 0.5f)) 
        {
            isMatingMode = false; // Enerjisi çok düşerse vazgeçer
        }

        if (isCriticallyHungry)
        {
            isMatingMode = false; 
            radarTimer -= Time.deltaTime;
            if(radarTimer <= 0f) { SearchForFood(); radarTimer = radarCooldown; }
            if (currentState != State.ChasingFood && currentState != State.Wandering) ForceWander();
            return; 
        }

        if (isMatingMode)
        {
            if (currentState != State.SeekingMate)
            {
                currentState = State.SeekingMate; 
                targetMate = null; 
            }
            radarTimer -= Time.deltaTime;
            if(radarTimer <= 0f) { SearchForMate(); radarTimer = radarCooldown; }
            return; 
        }

        if (isHungryMode)
        {
            radarTimer -= Time.deltaTime;
            if(radarTimer <= 0f) { SearchForFood(); radarTimer = radarCooldown; }
            if (currentState != State.ChasingFood && currentState != State.Wandering) ForceWander();
            return;
        }

        if (currentState == State.ChasingFood || currentState == State.SeekingMate)
        {
            targetFood = null;
            targetMate = null;
            ChooseNextAction(); 
        }
    }

    void ForceWander()
    {
        currentState = State.Wandering;
        moveDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        stateTimer = maxActionTime;
    }

    void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case State.Idle:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0) ChooseNextAction();
                IdleLookAround(); // 🌟 YENİ: Olduğun yerde dur ve etrafı tara!
                break;
                
            case State.Wandering:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0) ChooseNextAction();
                MoveAndRotate(); // Hem dön hem yürü
                break;
                
            case State.ChasingFood: ChaseFood(); break;
            case State.Eating: EatFood(); break;
            case State.SeekingMate: ChaseMate(); break; 
            case State.Mating: Mate(); break;           
        }
    }

    void ChooseNextAction()
    {
        // %50 ihtimalle gezinmeye, %50 ihtimalle etrafı izlemeye (Idle) karar ver
        currentState = (Random.value > 0.5f) ? State.Wandering : State.Idle;
        
        // 🌟 FİX: Artık Idle durumunda da rastgele bir yöne dönme (tarama) emri veriyoruz!
        moveDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        
        stateTimer = Random.Range(minActionTime, maxActionTime);
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

    // --- YEMEK FONKSİYONLARI ---
    void SearchForFood()
    {
        // En geniş duyumuz hangisiyse o kadar büyük bir çember tarıyoruz
        float maxRadius = Mathf.Max(stats.dna.visionRadius, stats.dna.smellRadius);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, maxRadius);
        
        // 🌟 DEĞİŞİM 1: Artık "closestDistance" (En Yakın) yerine "bestScore" (En Yüksek Puan) arıyoruz!
        float bestSeenScore = -1f;
        Transform bestSeenTarget = null;
        FoodObject bestSeenFoodData = null;

        float bestSmelledScore = -1f;
        Transform bestSmelledTarget = null;

        foreach (var hit in hitColliders)
        {
            FoodObject food = hit.GetComponent<FoodObject>();
            if (food != null)
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                float safeDist = Mathf.Max(dist, 0.1f); // 0'a bölme hatasını engellemek için güvenlik
                
                Vector2 dirToFood = (hit.transform.position - transform.position).normalized;
                float angleToFood = Vector2.Angle(transform.up, dirToFood);

                // --- 🌟 YENİ: ARZU VE SİNDİRİM ÇARPANLARI 🌟 ---
                float desireMultiplier = 0f;
                float efficiencyMultiplier = 0f;

                if (food.type == FoodType.Plant) 
                {
                    desireMultiplier = stats.dna.desirePlant;
                    efficiencyMultiplier = stats.dna.plantEfficiency;
                }
                else if (food.type == FoodType.PoisonousPlant) 
                {
                    desireMultiplier = stats.dna.desirePoison;
                    efficiencyMultiplier = stats.dna.plantEfficiency; 
                }
                else if (food.type == FoodType.Meat) 
                {
                    desireMultiplier = stats.dna.desireMeat;
                    efficiencyMultiplier = stats.dna.meatEfficiency;
                }

                // --- 🌟 YENİ: YAPAY ZEKA KARAR FORMÜLÜ 🌟 ---
                // Puan = (Enerji * Sindirme Yeteneği / Mesafe) * Psikolojik Arzu
                float foodScore = (food.currentEnergy * efficiencyMultiplier / safeDist) * desireMultiplier;

                // Eğer canlının bu yemeğe ilgisi (veya sindirimi) 0 ise, bu yemeği tamamen yok say!
                if (foodScore <= 0) continue; 

                // 1. GÖZLERLE GÖRME KONTROLÜ (Mesafede ve Açıdaysa)
                if (dist <= stats.dna.visionRadius && angleToFood <= stats.dna.visionAngle / 2f)
                {
                    // 🌟 DEĞİŞİM 2: Mesafe kısa mı diye değil, Puanı daha mı yüksek diye bakıyoruz!
                    if (foodScore > bestSeenScore)
                    {
                        bestSeenScore = foodScore; // Yeni en iyi puanı kaydet
                        bestSeenTarget = hit.transform;
                        bestSeenFoodData = food;
                    }
                }
                // 2. BURUNLA KOKU KONTROLÜ (Göremiyorsak ama koku mesafesindeyse)
                else if (dist <= stats.dna.smellRadius)
                {
                    if (foodScore > bestSmelledScore)
                    {
                        bestSmelledScore = foodScore; // Yeni en iyi koku puanını kaydet
                        bestSmelledTarget = hit.transform;
                    }
                }
            }
        }

        // --- KARAR MEKANİZMASI (Senin harika Fix'in dahil, birebir aynı!) ---
        if (bestSeenTarget != null) 
        { 
            targetFood = bestSeenTarget; 
            currentFoodData = bestSeenFoodData; 
            currentState = State.ChasingFood; 
        }
        else if (bestSmelledTarget != null && currentState != State.ChasingFood)
        {
            moveDirection = (bestSmelledTarget.position - transform.position).normalized;
            currentState = State.Wandering;
            
            // 🌟 FİX 1: Kokuyu aldığı sürece "Gezinme Süresini" sürekli fulle! 
            stateTimer = maxActionTime; 
        }
    }

    void ChaseFood()
        {
            // GÜVENLİK: Eğer yemeğe koşarken yemek aniden yok olduysa (başkası yutmuş veya çürümüşse)
            if (targetFood == null) 
            { 
                currentState = State.Idle; // Aramayı bırak ve bekleme moduna geç
                return; 
            }
            
            // Eğer yemeğe yeterince yaklaştıysak yemeye başla
            if (Vector2.Distance(transform.position, targetFood.position) <= eatDistance)
            {
                currentState = State.Eating;
                eatTimer = stats.dna.eatDuration; // Yemek çiğneme süresini DNA'dan alıyoruz
                return;
            }

            // Yakın değilsek yemeğe doğru yürümeye devam et
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

    // --- ÜREME FONKSİYONLARI ---
    void SearchForMate()
    {
        float maxRadius = Mathf.Max(stats.dna.visionRadius, stats.dna.smellRadius);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, maxRadius);
        
        float closestSeenDistance = Mathf.Infinity;
        Transform bestSeenTarget = null;
        CreatureStats bestSeenStats = null;

        float closestSmelledDistance = Mathf.Infinity;
        Transform bestSmelledTarget = null;

        foreach (var hit in hitColliders)
        {
            if (hit.gameObject == this.gameObject) continue;
            
            CreatureStats otherStats = hit.GetComponent<CreatureStats>();
            LeaterBrain otherBrain = hit.GetComponent<LeaterBrain>();

            if (otherStats != null && otherBrain != null)
            {
                bool isOtherFertile = (otherStats.currentStage == LifeStage.Adult) || (otherStats.currentStage == LifeStage.Old && otherStats.age <= 17);
                if (!isOtherFertile || otherBrain.currentState != State.SeekingMate) continue;

                float dist = Vector2.Distance(transform.position, hit.transform.position);
                Vector2 dirToMate = (hit.transform.position - transform.position).normalized;
                float angleToMate = Vector2.Angle(transform.up, dirToMate);

                // 1. GÖRME KONTROLÜ
                if (dist <= stats.dna.visionRadius && angleToMate <= stats.dna.visionAngle / 2f)
                {
                    if (dist < closestSeenDistance) 
                    { 
                        closestSeenDistance = dist; 
                        bestSeenTarget = hit.transform; 
                        bestSeenStats = otherStats; 
                    }
                }
                // 2. KOKU KONTROLÜ (Görmüyor ama feromon kokusu alıyorsa)
                else if (dist <= stats.dna.smellRadius)
                {
                    if (dist < closestSmelledDistance)
                    {
                        closestSmelledDistance = dist;
                        bestSmelledTarget = hit.transform;
                    }
                }
            }
        }
        
        // --- KARAR MEKANİZMASI ---
        if (bestSeenTarget != null) 
        { 
            targetMate = bestSeenTarget; 
            mateStats = bestSeenStats; 
        }
        else if (bestSmelledTarget != null && targetMate == null)
        {
            // Eşi görmedi ama kokusunu aldı, o yöne doğru dön ve yürümeye başla!
            moveDirection = (bestSmelledTarget.position - transform.position).normalized;
            currentState = State.Wandering;
            
            // 🌟 FİX 1 (ALZHEİMER ÇÖZÜMÜ BURADA) 🌟
            // Eşin kokusunu (feromonları) aldığı sürece süreyi fullüyoruz ki 
            // aniden "ben nereye gidiyordum ya" diyip dönmesin.
            stateTimer = maxActionTime; 
        }
    }

    void ChaseMate()
        {
            // 🌟 ÇÖZÜM: HEDEF YOKSA BİLE RENGİNİ BOZMADAN RASTGELE GEZİNEREK EŞ ARA
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
            
            // Eş aniden açlıktan dolayı modu kapatırsa, hedefi sil ve üstteki "arama" döngüsüne dön
            if (mateBrain == null || mateBrain.currentState != State.SeekingMate)
            {
                targetMate = null;
                mateStats = null;
                return;
            }

            if (Vector2.Distance(transform.position, targetMate.position) <= mateDistance)
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
            if (gameObject.GetHashCode() > targetMate.gameObject.GetHashCode())
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
            // 🌟 YENİ: KOKU ÇEMBERİ (Yarı saydam yeşil)
            Gizmos.color = new Color(0f, 1f, 0f, 0.15f); 
            Gizmos.DrawWireSphere(transform.position, myStats.dna.smellRadius);

            // GÖRÜŞ HUNİSİ (Sarı)
            Gizmos.color = Color.yellow;
            Vector3 rightLimit = Quaternion.Euler(0, 0, -myStats.dna.visionAngle / 2f) * transform.up;
            Vector3 leftLimit = Quaternion.Euler(0, 0, myStats.dna.visionAngle / 2f) * transform.up;

            Gizmos.DrawLine(transform.position, transform.position + rightLimit * myStats.dna.visionRadius);
            Gizmos.DrawLine(transform.position, transform.position + leftLimit * myStats.dna.visionRadius);
            // Mesafenin ucunu da yay şeklinde belli etmek istersen ufak bir radar çizgisi atabiliriz ama bu kadarı yeterli
        }

        if (targetFood != null) { Gizmos.color = Color.red; Gizmos.DrawLine(transform.position, targetFood.position); }
        if (targetMate != null) { Gizmos.color = Color.magenta; Gizmos.DrawLine(transform.position, targetMate.position); }

        // 🌟 BIYIKLARI ÇİZME (Mavi Çizgiler)
        Gizmos.color = Color.cyan;
        Vector3 fwd = transform.up;
        Vector3 r = Quaternion.Euler(0, 0, -30) * fwd;
        Vector3 l = Quaternion.Euler(0, 0, 30) * fwd;
        Gizmos.DrawRay(transform.position, fwd * avoidDistance);
        Gizmos.DrawRay(transform.position, r * (avoidDistance * 0.8f));
        Gizmos.DrawRay(transform.position, l * (avoidDistance * 0.8f));
    }
}