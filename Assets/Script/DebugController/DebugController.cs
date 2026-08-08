using UnityEngine;
using TMPro; 
using UnityEngine.EventSystems;

public class DebugController : MonoBehaviour
{
    public static DebugController instance;

    [Header("Arayüz (UI) Referansları")]
    public GameObject statsPanel; 
    public TextMeshProUGUI statsText; 

    public CreatureStats selectedCreature;

    void Awake()
    {
        instance = this; 
    }

    void Start()
    {
        statsPanel.SetActive(false);
    }

    void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                // 🌟 ÇÖZÜMÜN KALBİ BURASI 🌟
                // Eğer Yaratıcı Modundaysak (Elimizde tıklayıp spawnlamak için bir canlı tutuyorsak),
                // Işın atıp canlıyı seçme (Raycast) işlemini İPTAL ET!
                bool isCreatureSpawning = CreatureSpawnController.instance != null && CreatureSpawnController.instance.IsSpawning();
                bool isFoodSpawning = FoodSpawnController.instance != null && FoodSpawnController.instance.IsSpawning();
                if (isCreatureSpawning || isFoodSpawning)
                {
                    return; // Aşağıdaki CheckClick kodunu hiç okumadan Update döngüsünden çık
                }

                CheckClick();
            }

            // Eğer bir canlı seçiliyse ve panelimiz açıksa, değerleri ekranda SÜREKLİ GÜNCELLE
            if (selectedCreature != null && statsPanel.activeSelf)
            {
                UpdatePanelInfo();
            }
        }

    void CheckClick()
    {
        if (Camera.main == null) return;

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

        if (hit.collider != null)
        {
            CreatureStats clickedStats = hit.collider.GetComponent<CreatureStats>();

            if (clickedStats != null)
            {
                SelectCreature(clickedStats);
                return; 
            }
        }

       
        Deselect();
    }

    // 🌟 YENİ: Hem boşluğa tıklayınca hem de kamerayı WASD ile hareket ettirince seçimi sıfırlayacak ortak fonksiyon
    public void Deselect()
    {
        selectedCreature = null;
        statsPanel.SetActive(false);
    }

    public void SelectCreature(CreatureStats creature)
    {
        if (creature == null)
        {
            Deselect();
            return;
        }

        selectedCreature = creature;
        statsPanel.SetActive(true);
        UpdatePanelInfo();
    }

void UpdatePanelInfo()
    {
        string durum = selectedCreature.currentStateName;

        string evre = "Bilinmiyor";
        if (selectedCreature.currentStage == LifeStage.Young) evre = "Genç (0-5)";
        else if (selectedCreature.currentStage == LifeStage.Adult) evre = "Yetişkin (6-40)";
        else if (selectedCreature.currentStage == LifeStage.Old) evre = "Yaşlı (41+)";

        float odedigiVergi = selectedCreature.dna.visionRadius * selectedCreature.dna.visionEnergyTax;
        float uremeSiniri = selectedCreature.currentMaxEnergy * (selectedCreature.dna.reproduceEnergyThreshold / 100f);
        float gercekHasar = selectedCreature.currentAttackDamage;

        // 🌟 YENİ: BEYİNDEKİ AKTİF SOSYAL DEĞERLERİ ALALIM 🌟
        LeaterBrain brain = selectedCreature.GetComponent<LeaterBrain>();
        string evDurumu = "<color=#708090>Göçebe (Evsiz)</color>";
        string suruDurumu = "<color=#708090>Yalnız Kurt</color>";

        if (brain != null)
        {
            if (brain.hasHome)
            {
                float distToHome = Vector2.Distance(selectedCreature.transform.position, brain.homePoint);
                evDurumu = $"<color=#0080FF>Yuvada (Uzaklık: {distToHome:F1} br)</color>";
            }
            
            if (brain.targetFlock != null)
            {
                suruDurumu = $"<color=#00C8C8>Sürü Üyesi (Lider: {brain.targetFlock.name})</color>";
            }
        }

        statsText.text = 
            "<color=#111111>" + 

            "<size=110%><b><color=#8B4500>--- GENEL BİLGİLER ---</color></b></size>\n" +
            "<b>Canlı:</b> " + selectedCreature.gameObject.name + "\n" +
            "<b>Nesil (Soy Ağacı):</b> <color=#00008B><b>V" + selectedCreature.generation + "</b></color>\n" +
            "<b>Biyolojik Sınıf:</b> " + selectedCreature.GetCreatureClass() + "\n" +
            "<b>Anlık Durum:</b> <i>" + durum + "</i>\n\n" +

            // 🌟 YENİ SIRA: SOSYALLİK VE BÖLGE DURUMU 🌟
            "<size=110%><b><color=#005b96>--- SOSYALLİK & BÖLGE ---</color></b></size>\n" +
            "<b>Ev Durumu:</b> " + evDurumu + "\n" +
            "<b>Sürü Durumu:</b> " + suruDurumu + "\n" +
            "<b>Sosyallik Derecesi:</b> %" + (selectedCreature.dna.sociability * 100f).ToString("F0") + " <i>(Sürüye Katılma Şansı)</i>\n" +
            "<b>Ev Çapı (Territory):</b> " + selectedCreature.dna.homeWanderRadius.ToString("F1") + " br\n" +
            "<b>Göç Eşiği:</b> < " + Mathf.Round(selectedCreature.currentMaxEnergy * selectedCreature.dna.migrationThreshold) + " E <i>(%" + (selectedCreature.dna.migrationThreshold * 100f).ToString("F0") + ")</i>\n" +
            "<b>Sürü Toleransı:</b> " + selectedCreature.dna.flockTolerance.ToString("F2") + "x <i>(Boyut Farkı)</i>\n\n" +

            "<size=110%><b><color=#B22222>--- YAŞ AND BÜYÜME ---</color></b></size>\n" +
            "<b>Yaş:</b> " + selectedCreature.age + " <i>(" + evre + ")</i>\n" +
            "<b>Büyüme:</b> %" + Mathf.Round(selectedCreature.growthProgress) + "\n\n" +
            
            "<size=110%><b><color=#006400>--- HAYATİ DEĞERLER ---</color></b></size>\n" +
            "<b>Can:</b> " + Mathf.Round(selectedCreature.currentHealth) + " / " + Mathf.Round(selectedCreature.currentMaxHealth) + "\n" +
            "<b>Enerji:</b> " + Mathf.Round(selectedCreature.currentEnergy) + " / " + Mathf.Round(selectedCreature.currentMaxEnergy) + "\n" +
            "<b>Üreme Sınırı:</b> > " + Mathf.Round(uremeSiniri) + " E\n" +
            "<b>İyileşme:</b> " + selectedCreature.currentHealingRate.ToString("F1") + " HP/sn\n\n" +
            
            "<size=110%><b><color=#8B0000>--- AVCILIK & SALDIRI ---</color></b></size>\n" +
            "<b>Saldırı Hasarı:</b> <color=#FF0000><b>" + gercekHasar.ToString("F1") + " HP</b></color>\n" +
            "<b>Saldırı Menzili:</b> " + selectedCreature.dna.attackDistance.ToString("F1") + " br\n" +
            "<b>Saldırı Hızı:</b> " + selectedCreature.dna.attackCooldown.ToString("F1") + " sn\n\n" +

            "<size=110%><b><color=#4B0082>--- PSİKOLOJİ & ARZU ---</color></b></size>\n" +
            "<b>Ota İlgi:</b> %" + (selectedCreature.dna.desirePlant * 100f).ToString("F0") + " | <b>Ete İlgi:</b> %" + (selectedCreature.dna.desireMeat * 100f).ToString("F0") + "\n" +
            "<b>Zehirli Ota İlgi:</b> %" + (selectedCreature.dna.desirePoison * 100f).ToString("F0") + " | <b>Zehir Direnci:</b> %" + (selectedCreature.dna.poisonResistance * 100f).ToString("F0") + "\n\n" +

            "<size=110%><b><color=#D2691E>--- SİNDİRİM & METABOLİZMA ---</color></b></size>\n" +
            "<b>Ot Sindirimi:</b> x" + selectedCreature.dna.plantEfficiency.ToString("F2") + " | <b>Et Sindirimi:</b> x" + selectedCreature.dna.meatEfficiency.ToString("F2") + "\n" +
            "<b>Sabit Acıkma:</b> " + selectedCreature.dna.idleEnergyDrain.ToString("F2") + " E/sn\n\n" +

            "<size=110%><b><color=#0055AA>--- FİZİKSEL & DUYULAR ---</color></b></size>\n" +
            "<b>Boyut:</b> " + selectedCreature.dna.baseSize.ToString("F2") + "x | <b>Hız:</b> " + selectedCreature.currentSpeed.ToString("F1") + "\n" +
            "<b>Görüş:</b> " + selectedCreature.dna.visionRadius.ToString("F1") + " br <i>(" + selectedCreature.dna.visionAngle.ToString("F0") + "°)</i>\n" +
            "<b>Koku:</b> " + selectedCreature.dna.smellRadius.ToString("F1") + " br\n" +

            "</color>";
    }
}
