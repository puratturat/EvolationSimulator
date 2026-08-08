using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObservationDashboard : MonoBehaviour
{
    private sealed class ChampionRow
    {
        public string title;
        public Func<CreatureStats, float> score;
        public Func<CreatureStats, string> value;
        public CreatureStats creature;
        public TextMeshProUGUI text;
    }

    private readonly List<ChampionRow> rows = new List<ChampionRow>();
    private GameObject panel;
    private TextMeshProUGUI populationText;
    private TextMeshProUGUI logText;
    private float refreshTimer;

    private static readonly Color PanelColor = new Color(0.035f, 0.05f, 0.07f, 0.94f);
    private static readonly Color RowColor = new Color(0.10f, 0.14f, 0.18f, 0.96f);
    private static readonly Color AccentColor = new Color(0.20f, 0.78f, 0.68f, 1f);

    private void Start()
    {
        BuildInterface();
        SetPanelVisible(false);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetPanelVisible(false);
            return;
        }

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            RefreshChampions();
            refreshTimer = 1f;
        }
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject("Observation Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Button toggle = CreateButton(canvasObject.transform, "Enler Toggle", "ENLER", new Vector2(126f, 42f), AccentColor);
        RectTransform toggleRect = toggle.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1f, 1f);
        toggleRect.anchorMax = new Vector2(1f, 1f);
        toggleRect.pivot = new Vector2(1f, 1f);
        toggleRect.anchoredPosition = new Vector2(-22f, -22f);
        toggle.onClick.AddListener(() => SetPanelVisible(true));

        panel = CreatePanel(canvasObject.transform, "Enler Panel", PanelColor);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.sizeDelta = new Vector2(430f, 760f);
        panelRect.anchoredPosition = new Vector2(-22f, 0f);

        TextMeshProUGUI title = CreateText(panel.transform, "Title", "EKOSİSTEM REKORLARI", 24f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(18f, -54f);
        titleRect.offsetMax = new Vector2(-58f, -10f);
        title.color = AccentColor;

        Button close = CreateButton(panel.transform, "Close", "×", new Vector2(38f, 38f), new Color(0.55f, 0.18f, 0.18f, 1f));
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-10f, -10f);
        close.onClick.AddListener(() => SetPanelVisible(false));

        populationText = CreateText(panel.transform, "Population", "Canlı: 0", 14f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        RectTransform popRect = populationText.rectTransform;
        popRect.anchorMin = new Vector2(0f, 1f);
        popRect.anchorMax = new Vector2(1f, 1f);
        popRect.pivot = new Vector2(0.5f, 1f);
        popRect.offsetMin = new Vector2(18f, -82f);
        popRect.offsetMax = new Vector2(-18f, -57f);
        populationText.color = new Color(0.74f, 0.82f, 0.86f, 1f);

        GameObject viewport = CreatePanel(panel.transform, "Viewport", new Color(0f, 0f, 0f, 0.12f));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(12f, 84f);
        viewportRect.offsetMax = new Vector2(-12f, -88f);
        viewport.AddComponent<RectMask2D>();

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.spacing = 6f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = viewport.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 32f;

        BuildChampionButtons(content.transform);

        Button logButton = CreateButton(panel.transform, "Log Folder", "LOG KLASÖRÜNÜ AÇ", new Vector2(190f, 36f), new Color(0.18f, 0.34f, 0.42f, 1f));
        RectTransform logButtonRect = logButton.GetComponent<RectTransform>();
        logButtonRect.anchorMin = new Vector2(0f, 0f);
        logButtonRect.anchorMax = new Vector2(0f, 0f);
        logButtonRect.pivot = new Vector2(0f, 0f);
        logButtonRect.anchoredPosition = new Vector2(12f, 12f);
        logButton.onClick.AddListener(OpenLogFolder);

        logText = CreateText(panel.transform, "Log State", "Log hazırlanıyor...", 12f, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
        RectTransform logRect = logText.rectTransform;
        logRect.anchorMin = new Vector2(0f, 0f);
        logRect.anchorMax = new Vector2(1f, 0f);
        logRect.pivot = new Vector2(0.5f, 0f);
        logRect.offsetMin = new Vector2(210f, 12f);
        logRect.offsetMax = new Vector2(-12f, 48f);
        logText.color = new Color(0.60f, 0.70f, 0.74f, 1f);
    }

    private void BuildChampionButtons(Transform content)
    {
        AddRow(content, "En Etçil (Davranış)", c => c.lifetimeMeatEaten + (c.lifetimeKills * 3f), c => $"{c.lifetimeMeatEaten} et • {c.lifetimeKills} av");
        AddRow(content, "En Etçil (Genetik)", c => Mathf.Sqrt(Mathf.Clamp01(c.dna.desireMeat) * Mathf.Clamp01(c.dna.meatEfficiency)), c => $"ilgi %{c.dna.desireMeat * 100f:F0} • sindirim x{c.dna.meatEfficiency:F2}");
        AddRow(content, "En Başarılı Avcı", c => c.lifetimeKills, c => $"{c.lifetimeKills} av");
        AddRow(content, "En Saldırgan", c => c.lifetimeDamageDealt, c => $"{c.lifetimeAttacks} ısırık • {c.lifetimeDamageDealt:F0} hasar");
        AddRow(content, "En Zehir Tüketen", c => c.lifetimePoisonPlantsEaten, c => $"{c.lifetimePoisonPlantsEaten} zehirli öğün");
        AddRow(content, "En Zehir Uyumlu", c => Mathf.Sqrt(Mathf.Clamp01(c.dna.desirePoison) * Mathf.Clamp01(c.dna.poisonResistance)), c => $"ilgi %{c.dna.desirePoison * 100f:F0} • direnç %{c.dna.poisonResistance * 100f:F0}");
        AddRow(content, "En Otçul", c => c.lifetimePlantsEaten + (c.dna.desirePlant * c.dna.plantEfficiency), c => $"{c.lifetimePlantsEaten} ot • sindirim x{c.dna.plantEfficiency:F2}");
        AddRow(content, "En Güçlü Isırık", c => c.currentAttackDamage, c => $"{c.currentAttackDamage:F1} HP");
        AddRow(content, "En Hızlı", c => c.currentSpeed, c => $"{c.currentSpeed:F1} br/sn");
        AddRow(content, "En Büyük", c => c.dna.baseSize, c => $"{c.dna.baseSize:F2}x");
        AddRow(content, "En Dayanıklı", c => c.currentMaxHealth, c => $"{c.currentMaxHealth:F0} HP");
        AddRow(content, "En Enerjik", c => c.currentMaxEnergy, c => $"{c.currentMaxEnergy:F0} E");
        AddRow(content, "En İyi İyileşen", c => c.currentHealingRate, c => $"{c.currentHealingRate:F1} HP/sn");
        AddRow(content, "En Sosyal", c => c.dna.sociability, c => $"%{c.dna.sociability * 100f:F0}");
        AddRow(content, "En İyi Görüş", c => c.dna.visionRadius, c => $"{c.dna.visionRadius:F1} br • {c.dna.visionAngle:F0}°");
        AddRow(content, "En İyi Koku", c => c.dna.smellRadius, c => $"{c.dna.smellRadius:F1} br");
        AddRow(content, "En Yaşlı", c => c.age, c => $"yaş {c.age}");
        AddRow(content, "En İleri Nesil", c => c.generation, c => $"V{c.generation}");
        AddRow(content, "En Çok Yavru", c => c.lifetimeOffspring, c => $"{c.lifetimeOffspring} yavru");
    }

    private void AddRow(Transform content, string title, Func<CreatureStats, float> score, Func<CreatureStats, string> value)
    {
        Button button = CreateButton(content, title, title, new Vector2(0f, 51f), RowColor);
        LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 51f;
        element.minHeight = 51f;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        label.fontSize = 14f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.margin = new Vector4(12f, 2f, 8f, 2f);

        ChampionRow row = new ChampionRow { title = title, score = score, value = value, text = label };
        rows.Add(row);
        button.onClick.AddListener(() => SelectChampion(row));
    }

    private void RefreshChampions()
    {
        List<CreatureStats> living = EcosystemManager.instance != null
            ? EcosystemManager.instance.allLivingCreatures
            : null;

        int livingCount = 0;
        if (living != null)
        {
            living.RemoveAll(c => c == null);
            livingCount = living.Count;
        }

        populationText.text = $"Yaşayan canlı: {livingCount}  •  Satıra tıklayarak canlıyı izle";

        foreach (ChampionRow row in rows)
        {
            row.creature = null;
            float bestScore = float.NegativeInfinity;

            if (living != null)
            {
                foreach (CreatureStats candidate in living)
                {
                    if (candidate == null || candidate.dna == null)
                    {
                        continue;
                    }

                    float candidateScore = row.score(candidate);
                    if (candidateScore > bestScore)
                    {
                        bestScore = candidateScore;
                        row.creature = candidate;
                    }
                }
            }

            if (row.creature == null)
            {
                row.text.text = $"<b>{row.title}</b>\n<color=#829099>Henüz canlı yok</color>";
            }
            else
            {
                row.text.text = $"<b>{row.title}</b>  <color=#55D8C3>› {row.creature.name}</color>\n<color=#AAB8BF>{row.value(row.creature)}</color>";
            }
        }

        if (SimulationEventLogger.Instance != null)
        {
            logText.text = $"Özet: {SimulationEventLogger.Instance.SnapshotCount} • {SimulationEventLogger.Instance.CurrentLogFileName}";
        }
    }

    private void SelectChampion(ChampionRow row)
    {
        if (row.creature == null)
        {
            return;
        }

        if (DebugController.instance != null)
        {
            DebugController.instance.SelectCreature(row.creature);
        }

        SetPanelVisible(false);
    }

    private void SetPanelVisible(bool visible)
    {
        if (panel == null)
        {
            return;
        }

        panel.SetActive(visible);
        if (visible)
        {
            refreshTimer = 0f;
            RefreshChampions();
        }
    }

    private void OpenLogFolder()
    {
        if (SimulationEventLogger.Instance != null)
        {
            SimulationEventLogger.Instance.OpenLogFolder();
        }
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        panelObject.GetComponent<Image>().color = color;
        return panelObject;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 size, Color color)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<RectTransform>().sizeDelta = size;
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        button.colors = colors;

        TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, 16f, FontStyles.Bold, TextAlignmentOptions.Center);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.raycastTarget = false;
        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }
}
