using UnityEngine;
using TMPro; 

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
                // 🌟 ÇÖZÜMÜN KALBİ BURASI 🌟
                // Eğer Yaratıcı Modundaysak (Elimizde tıklayıp spawnlamak için bir canlı tutuyorsak),
                // Işın atıp canlıyı seçme (Raycast) işlemini İPTAL ET!
                if (CreatureSpawnController.instance != null && CreatureSpawnController.instance.IsSpawning())
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
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

        if (hit.collider != null)
        {
            CreatureStats clickedStats = hit.collider.GetComponent<CreatureStats>();

            if (clickedStats != null)
            {
                selectedCreature = clickedStats;
                statsPanel.SetActive(true);
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

    void UpdatePanelInfo()
    {
        string durum = selectedCreature.currentStateName;

        string evre = "Bilinmiyor";
        if (selectedCreature.currentStage == LifeStage.Young) evre = "Genç (0-2)";
        else if (selectedCreature.currentStage == LifeStage.Adult) evre = "Yetişkin (3-14)";
        else if (selectedCreature.currentStage == LifeStage.Old) evre = "Yaşlı (15+)";

        float odedigiVergi = selectedCreature.dna.visionRadius * selectedCreature.dna.visionEnergyTax;
        float uremeSiniri = selectedCreature.currentMaxEnergy * (selectedCreature.dna.reproduceEnergyThreshold / 100f);

        // Tüm metni siyah (#111111) bir blok içine alıyoruz, başlıkları koyulaştırılmış pastel tonlar yapıyoruz
        statsText.text = 
            "<color=#111111>" + 

            "<size=110%><b><color=#8B4500>--- GENEL BİLGİLER ---</color></b></size>\n" +
            "<b>Canlı:</b> " + selectedCreature.gameObject.name + "\n" +
            "<b>Nesil (Soy Ağacı):</b> <color=#00008B><b>V" + selectedCreature.generation + "</b></color>\n" +
            "<b>Anlık Durum:</b> <i>" + durum + "</i>\n\n" +

            "<size=110%><b><color=#B22222>--- YAŞ VE BÜYÜME ---</color></b></size>\n" +
            "<b>Yaş:</b> " + selectedCreature.age + " <i>(" + evre + ")</i>\n" +
            "<b>Büyüme:</b> %" + Mathf.Round(selectedCreature.growthProgress) + "\n\n" +
            
            "<size=110%><b><color=#006400>--- HAYATİ DEĞERLER ---</color></b></size>\n" +
            "<b>Can:</b> " + Mathf.Round(selectedCreature.currentHealth) + " / " + Mathf.Round(selectedCreature.currentMaxHealth) + "\n" +
            "<b>Enerji:</b> " + Mathf.Round(selectedCreature.currentEnergy) + " / " + Mathf.Round(selectedCreature.currentMaxEnergy) + "\n" +
            "<b>Üreme Sınırı:</b> > " + Mathf.Round(uremeSiniri) + " E\n" +
            "<b>İyileşme:</b> " + selectedCreature.dna.healingRate.ToString("F1") + " HP/sn <i>(Mlyt: " + selectedCreature.dna.healingEnergyCost.ToString("F1") + " E)</i>\n\n" +
            
            // 🌟 YENİDEN TASARLANAN ARZU SİSTEMİ 🌟
            "<size=110%><b><color=#4B0082>--- PSİKOLOJİ & ARZU ---</color></b></size>\n" +
            "<b>Ota İlgi:</b> %" + (selectedCreature.dna.desirePlant * 100f).ToString("F0") + " | <b>Ete İlgi:</b> <color=#8B0000><b>%" + (selectedCreature.dna.desireMeat * 100f).ToString("F0") + "</b></color>\n" +
            "<b>Zehirli Ota İlgi:</b> <color=#8B008B><b>%" + (selectedCreature.dna.desirePoison * 100f).ToString("F0") + "</b></color>\n" +
            "<b>Zehir Direnci:</b> <color=#4B0082><b>%" + (selectedCreature.dna.poisonResistance * 100f).ToString("F0") + "</b></color>\n\n" +

            // 🌟 YENİDEN TASARLANAN SİNDİRİM SİSTEMİ 🌟
            "<size=110%><b><color=#D2691E>--- SİNDİRİM & METABOLİZMA ---</color></b></size>\n" +
            "<b>Ot Sindirimi:</b> x" + selectedCreature.dna.plantEfficiency.ToString("F2") + "\n" +
            "<b>Et Sindirimi:</b> <color=#8B0000><b>x" + selectedCreature.dna.meatEfficiency.ToString("F2") + "</b></color>\n" +
            "<b>Yemek Süresi:</b> " + selectedCreature.dna.eatDuration.ToString("F1") + " sn\n" +
            "<b>Sabit Acıkma:</b> " + selectedCreature.dna.idleEnergyDrain.ToString("F2") + " E/sn\n" +
            "<b>Hareket Acıkması:</b> " + selectedCreature.dna.moveEnergyDrain.ToString("F2") + " E/sn\n\n" +

            "<size=110%><b><color=#0055AA>--- FİZİKSEL & DUYULAR ---</color></b></size>\n" +
            "<b>Boyut:</b> " + selectedCreature.dna.baseSize.ToString("F2") + "x | <b>Hız:</b> " + selectedCreature.currentSpeed.ToString("F1") + "\n" +
            "<b>Görüş:</b> " + selectedCreature.dna.visionRadius.ToString("F1") + " br <i>(" + selectedCreature.dna.visionAngle.ToString("F0") + "°)</i>\n" +
            "<b>Koku:</b> " + selectedCreature.dna.smellRadius.ToString("F1") + " br\n" +
            "<b>Görüş Vergisi:</b> <color=#A52A2A>+" + odedigiVergi.ToString("F2") + " E/sn</color>\n" +

            "</color>";
    }
}