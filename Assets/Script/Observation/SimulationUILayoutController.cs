using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
[DisallowMultipleComponent]
public class SimulationUILayoutController : MonoBehaviour
{
    [Header("Merkezi Yerleşim Ayarı")]
    [Tooltip("Assets/Resources/SimulationUILayoutSettings varlığı. Boş bırakılırsa Resources klasöründen otomatik bulunur.")]
    public SimulationUILayoutSettings settings;

    [Header("Sahne UI Referansları")]
    public RectTransform populationText;
    public RectTransform creatureButton;
    public RectTransform foodButton;
    public RectTransform timerButtons;
    public RectTransform statsPanel;
    public RectTransform creaturePanel;
    public RectTransform foodPanel;
    public RectTransform simulationInfoText;

    [Header("Editör Önizlemesi")]
    [Tooltip("Ayar varlığını değiştirirken sahne görünümünü otomatik günceller.")]
    public bool livePreviewInEditor = true;

    private CanvasScaler canvasScaler;
    private TextMeshProUGUI populationLabel;
    private TextMeshProUGUI creatureButtonLabel;
    private TextMeshProUGUI foodButtonLabel;
    private TextMeshProUGUI simulationInfoLabel;
    private TextMeshProUGUI[] timerButtonLabels;
    private Vector2Int lastScreenSize;
    private float nextEditorRefresh;
    private float nextRuntimeRefresh;

    private void OnEnable()
    {
        ResolveReferences();
        ApplyLayout();
    }

    private void OnValidate()
    {
        ResolveReferences();
        nextEditorRefresh = 0f;
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            bool shouldRefresh = screenSize != lastScreenSize;
#if UNITY_EDITOR
            shouldRefresh |= Time.unscaledTime >= nextRuntimeRefresh;
#endif
            if (shouldRefresh)
            {
                lastScreenSize = screenSize;
                nextRuntimeRefresh = Time.unscaledTime + 0.5f;
                ApplyLayout();
            }
            return;
        }

        if (livePreviewInEditor && Time.realtimeSinceStartup >= nextEditorRefresh)
        {
            nextEditorRefresh = Time.realtimeSinceStartup + 0.25f;
            ApplyLayout();
        }
    }

    [ContextMenu("UI Yerleşimini Şimdi Uygula")]
    public void ApplyLayout()
    {
        if (settings == null)
        {
            settings = SimulationUILayoutSettings.Load();
        }

        if (settings == null)
        {
            return;
        }

        if (canvasScaler == null)
        {
            canvasScaler = GetComponent<CanvasScaler>();
        }

        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = settings.referenceResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = settings.matchWidthOrHeight;
        }

        SimulationUILayoutUtility.Apply(populationText, settings.population);
        SimulationUILayoutUtility.Apply(creatureButton, settings.creatureButton);
        SimulationUILayoutUtility.Apply(foodButton, settings.foodButton);
        SimulationUILayoutUtility.Apply(timerButtons, settings.timerButtons);
        SimulationUILayoutUtility.Apply(statsPanel, settings.statsPanel);
        SimulationUILayoutUtility.Apply(creaturePanel, settings.creaturePanel);
        SimulationUILayoutUtility.Apply(foodPanel, settings.foodPanel);
        SimulationUILayoutUtility.Apply(simulationInfoText, settings.simulationInfo);

        float height = Mathf.Max(1f, ((RectTransform)transform).rect.height);
        float aspectRatio = ((RectTransform)transform).rect.width / height;
        if (SimulationUILayoutUtility.ShouldUseCompactLayout(settings, aspectRatio))
        {
            OffsetDown(populationText, settings.compactHeaderRowOffset);
            OffsetDown(creatureButton, settings.compactHeaderRowOffset);
            OffsetDown(foodButton, settings.compactHeaderRowOffset);
        }

        ArrangeTimerButtons();
        ApplyTextSizes();
    }

    [ContextMenu("UI Referanslarını İsimden Bul")]
    public void ResolveReferences()
    {
        if (settings == null)
        {
            settings = SimulationUILayoutSettings.Load();
        }

        if (populationText == null) populationText = FindRect("PopulationText");
        if (creatureButton == null) creatureButton = FindRect("CreatureButton");
        if (foodButton == null) foodButton = FindRect("FoodButton");
        if (timerButtons == null) timerButtons = FindRect("TimerButtons");
        if (statsPanel == null) statsPanel = FindRect("StatsPanelBG");
        if (creaturePanel == null) creaturePanel = FindRect("CreaturePanelBG");
        if (foodPanel == null) foodPanel = FindRect("FoodPanelBG");
        if (simulationInfoText == null) simulationInfoText = FindRect("SimulationInfoText");

        CacheTextReferences();
    }

    private RectTransform FindRect(string objectName)
    {
        RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform rect in rects)
        {
            if (rect.name == objectName)
            {
                return rect;
            }
        }
        return null;
    }

    private void ArrangeTimerButtons()
    {
        if (timerButtons == null)
        {
            return;
        }

        int count = timerButtons.childCount;
        float step = settings.timerButtonSize.x + settings.timerButtonSpacing;
        float startX = -((count - 1) * step) * 0.5f;

        for (int index = 0; index < count; index++)
        {
            RectTransform child = timerButtons.GetChild(index) as RectTransform;
            if (child == null)
            {
                continue;
            }

            child.anchorMin = child.anchorMax = child.pivot = new Vector2(0.5f, 0.5f);
            child.anchoredPosition = new Vector2(startX + (index * step), 0f);
            child.sizeDelta = settings.timerButtonSize;
            child.localScale = Vector3.one;
        }
    }

    private void ApplyTextSizes()
    {
        CacheTextReferences();
        if (populationLabel != null) populationLabel.fontSize = settings.populationFontSize;
        if (creatureButtonLabel != null) creatureButtonLabel.fontSize = settings.topButtonFontSize;
        if (foodButtonLabel != null) foodButtonLabel.fontSize = settings.topButtonFontSize;
        if (simulationInfoLabel != null) simulationInfoLabel.fontSize = settings.simulationInfoFontSize;

        if (timerButtonLabels != null)
        {
            foreach (TextMeshProUGUI label in timerButtonLabels)
            {
                if (label != null)
                {
                    label.fontSize = settings.timerButtonFontSize;
                }
            }
        }
    }

    private void CacheTextReferences()
    {
        if (populationLabel == null && populationText != null) populationLabel = populationText.GetComponentInChildren<TextMeshProUGUI>(true);
        if (creatureButtonLabel == null && creatureButton != null) creatureButtonLabel = creatureButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (foodButtonLabel == null && foodButton != null) foodButtonLabel = foodButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (simulationInfoLabel == null && simulationInfoText != null) simulationInfoLabel = simulationInfoText.GetComponentInChildren<TextMeshProUGUI>(true);
        if ((timerButtonLabels == null || timerButtonLabels.Length == 0) && timerButtons != null)
            timerButtonLabels = timerButtons.GetComponentsInChildren<TextMeshProUGUI>(true);
    }

    private static void OffsetDown(RectTransform rect, float amount)
    {
        if (rect != null)
        {
            rect.anchoredPosition += Vector2.down * amount;
        }
    }
}
