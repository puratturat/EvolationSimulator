using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CreatureSpawnController : MonoBehaviour
{
    public enum ObservationArchetype { Herbivore, Predator, Scavenger, Toxicovore }

    public static CreatureSpawnController instance;

    [Header("Arayüz (UI) Referansları")]
    public GameObject spawnPanel;

    [Header("Yaratılacak Objeler (Prefablar)")]
    public GameObject LeaterPrefab;
    public GameObject PretorPrefab;

    private GameObject selectedCreatureToSpawn;
    private ObservationArchetype selectedArchetype;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        BuildObservationButtons();
        if (spawnPanel != null) spawnPanel.SetActive(false);
        selectedCreatureToSpawn = null;
    }

    void Update()
    {
        if (selectedCreatureToSpawn != null && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                SpawnCreature();
            }
        }
    }

    void BuildObservationButtons()
    {
        if (spawnPanel == null) return;

        Button herbivoreButton = FindButton("LeaterButton");
        Button predatorButton = FindButton("PretorButton");
        Button template = herbivoreButton != null ? herbivoreButton : predatorButton;
        if (template == null)
        {
            Debug.LogWarning("Canlı panelinde düğme şablonu bulunamadı.");
            return;
        }

        Button scavengerButton = FindOrCloneButton("ScavengerButton", template);
        Button toxicButton = FindOrCloneButton("ToxicovoreButton", template);

        ConfigureButton(herbivoreButton, ObservationArchetype.Herbivore, "OTÇUL", new Color(0.20f, 0.72f, 0.28f), -225f);
        ConfigureButton(predatorButton, ObservationArchetype.Predator, "PREDATÖR", new Color(0.86f, 0.18f, 0.14f), -75f);
        ConfigureButton(scavengerButton, ObservationArchetype.Scavenger, "LEŞÇİL", new Color(0.82f, 0.48f, 0.12f), 75f);
        ConfigureButton(toxicButton, ObservationArchetype.Toxicovore, "ZEHİRCİ", new Color(0.58f, 0.20f, 0.78f), 225f);
    }

    Button FindButton(string objectName)
    {
        Transform child = spawnPanel.transform.Find(objectName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    Button FindOrCloneButton(string objectName, Button template)
    {
        Button existing = FindButton(objectName);
        if (existing != null) return existing;

        GameObject clone = Instantiate(template.gameObject, spawnPanel.transform);
        clone.name = objectName;
        return clone.GetComponent<Button>();
    }

    void ConfigureButton(Button button, ObservationArchetype archetype, string label, Color color, float x)
    {
        if (button == null) return;

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(132f, 88f);
        rect.localScale = Vector3.one;

        Image image = button.GetComponent<Image>();
        if (image != null) image.color = color;

        TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpLabel != null)
        {
            tmpLabel.text = label;
            tmpLabel.fontSize = 20f;
            tmpLabel.alignment = TextAlignmentOptions.Center;
            tmpLabel.color = Color.white;
        }
        else
        {
            Text legacyLabel = button.GetComponentInChildren<Text>(true);
            if (legacyLabel != null) legacyLabel.text = label;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectArchetype(archetype));
    }

    void SpawnCreature()
    {
        if (Camera.main == null || selectedCreatureToSpawn == null) return;

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        GameObject creature = Instantiate(selectedCreatureToSpawn, mousePosition, Quaternion.identity);
        ConfigureObservationArchetype(creature, selectedArchetype);
    }

    void ConfigureObservationArchetype(GameObject creature, ObservationArchetype archetype)
    {
        CreatureStats creatureStats = creature.GetComponent<CreatureStats>();
        if (creatureStats == null || creatureStats.dna == null) return;

        CreatureData dna = creatureStats.dna;
        dna.EnsureBaseMetabolism();
        dna.visualGenesInitialized = true;
        dna.lineageHue = Random.value;
        dna.patternSeed = Random.value;

        switch (archetype)
        {
            case ObservationArchetype.Predator:
                ApplyDiet(dna, 0.15f, 0.07f, 0.78f, 0.45f, 0.10f, 1.50f);
                dna.baseSize = 1.10f;
                dna.moveSpeed = 5.8f;
                dna.attackDamageMultiplier = 38f;
                dna.attackDistance = 2.2f;
                dna.attackCooldown = 0.8f;
                creature.name = "Gözlem-Predatör";
                break;

            case ObservationArchetype.Scavenger:
                ApplyDiet(dna, 0.30f, 0.08f, 0.62f, 0.75f, 0.10f, 1.35f);
                dna.baseSize = 1f;
                dna.moveSpeed = 3f;
                dna.attackDamageMultiplier = 4f;
                dna.attackDistance = 0.9f;
                dna.attackCooldown = 1.3f;
                creature.name = "Gözlem-Leşçil";
                break;

            case ObservationArchetype.Toxicovore:
                ApplyDiet(dna, 0.23f, 0.72f, 0.05f, 1.15f, 0.82f, 0.15f);
                dna.baseSize = 0.95f;
                dna.moveSpeed = 2.7f;
                dna.healingRate = 4.5f;
                dna.attackDamageMultiplier = 10f;
                creature.name = "Gözlem-Zehirci";
                break;

            default:
                ApplyDiet(dna, 0.88f, 0.07f, 0.05f, 1.45f, 0.08f, 0.12f);
                dna.baseSize = 1.05f;
                dna.moveSpeed = 2.2f;
                dna.attackDamageMultiplier = 10f;
                dna.attackDistance = 0.9f;
                creature.name = "Gözlem-Otçul";
                break;
        }

        dna.UpdateSkinColorFromEcology();
        creatureStats.age = 8;
        creatureStats.generation = 1;
    }

    static void ApplyDiet(CreatureData dna, float plantDesire, float poisonDesire, float meatDesire,
        float plantEfficiency, float poisonResistance, float meatEfficiency)
    {
        float total = Mathf.Max(plantDesire + poisonDesire + meatDesire, 0.001f);
        dna.desirePlant = plantDesire / total;
        dna.desirePoison = poisonDesire / total;
        dna.desireMeat = meatDesire / total;
        dna.plantEfficiency = plantEfficiency;
        dna.poisonResistance = poisonResistance;
        dna.meatEfficiency = meatEfficiency;
    }

    public void ToggleSpawnPanel()
    {
        if (spawnPanel == null) return;
        spawnPanel.SetActive(!spawnPanel.activeSelf);
        if (!spawnPanel.activeSelf) CancelSelection();
    }

    public void SelectLeater() => SelectArchetype(ObservationArchetype.Herbivore);
    public void SelectPretor() => SelectArchetype(ObservationArchetype.Predator);
    public void SelectScavenger() => SelectArchetype(ObservationArchetype.Scavenger);
    public void SelectToxicovore() => SelectArchetype(ObservationArchetype.Toxicovore);

    public void SelectArchetype(ObservationArchetype archetype)
    {
        if (FoodSpawnController.instance != null) FoodSpawnController.instance.CancelSelection();
        selectedArchetype = archetype;
        selectedCreatureToSpawn = LeaterPrefab != null ? LeaterPrefab : PretorPrefab;
        Debug.Log(archetype + " gözlem canlısı seçildi. Haritaya tıklayarak yerleştirebilirsin.");
    }

    public void CancelSelection()
    {
        selectedCreatureToSpawn = null;
    }

    public bool IsSpawning() => selectedCreatureToSpawn != null;
}
