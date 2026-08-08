using System;
using UnityEngine;

public enum UIAnchorPreset
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    Center,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

[Serializable]
public class UIElementLayout
{
    [InspectorName("Yerleşimi Uygula")]
    public bool apply = true;

    [InspectorName("Ekran Sabitleme Noktası")]
    public UIAnchorPreset anchor = UIAnchorPreset.Center;

    [InspectorName("Konum (X / Y)")]
    public Vector2 position;

    [InspectorName("Boyut (Genişlik / Yükseklik)")]
    public Vector2 size = new Vector2(200f, 60f);

    [InspectorName("Ölçek")]
    public Vector2 scale = Vector2.one;

    public UIElementLayout()
    {
    }

    public UIElementLayout(UIAnchorPreset anchor, Vector2 position, Vector2 size, Vector2 scale)
    {
        this.anchor = anchor;
        this.position = position;
        this.size = size;
        this.scale = scale;
    }
}

[CreateAssetMenu(fileName = "SimulationUILayoutSettings", menuName = "Evolution Simulator/UI Yerleşim Ayarları")]
public class SimulationUILayoutSettings : ScriptableObject
{
    public const string ResourceName = "SimulationUILayoutSettings";

    [Header("Genel Canvas")]
    [InspectorName("Referans Çözünürlük")]
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [InspectorName("Genişlik / Yükseklik Eşleşmesi")]
    [Range(0f, 1f)] public float matchWidthOrHeight = 0.5f;

    [Header("Dar Ekran / Kompakt Düzen")]
    [InspectorName("Kompakt Düzeni Etkinleştir")]
    public bool compactLayoutEnabled = true;

    [InspectorName("Kompakt Düzen En-Boy Eşiği")]
    [Tooltip("Ekran en-boy oranı bu değerin altına indiğinde sağ üst kontroller ikinci satıra geçer.")]
    [Range(1f, 2f)] public float compactAspectThreshold = 1.55f;

    [InspectorName("Üst Kontrol İkinci Satır Mesafesi")]
    [Min(0f)] public float compactHeaderRowOffset = 68f;

    [InspectorName("ENLER Ek Dikey Mesafesi")]
    [Min(0f)] public float compactObservationOffset = 68f;

    [Header("Üst Kontrol Alanı")]
    [InspectorName("Popülasyon Yazısı")]
    public UIElementLayout population = new UIElementLayout(
        UIAnchorPreset.TopRight,
        new Vector2(-410f, -18f),
        new Vector2(380f, 58f),
        Vector2.one);

    [InspectorName("Canlı Paneli Düğmesi")]
    public UIElementLayout creatureButton = new UIElementLayout(
        UIAnchorPreset.TopRight,
        new Vector2(-210f, -18f),
        new Vector2(160f, 48f),
        Vector2.one);

    [InspectorName("Yemek Paneli Düğmesi")]
    public UIElementLayout foodButton = new UIElementLayout(
        UIAnchorPreset.TopRight,
        new Vector2(-20f, -18f),
        new Vector2(160f, 48f),
        Vector2.one);

    [InspectorName("Hız Düğmeleri Grubu")]
    public UIElementLayout timerButtons = new UIElementLayout(
        UIAnchorPreset.TopCenter,
        new Vector2(0f, -18f),
        new Vector2(620f, 58f),
        new Vector2(1.35f, 1.35f));

    [InspectorName("Tek Hız Düğmesi Boyutu")]
    public Vector2 timerButtonSize = new Vector2(58f, 48f);

    [InspectorName("Hız Düğmeleri Aralığı")]
    [Min(0f)] public float timerButtonSpacing = 10f;

    [Header("Yazı Boyutları")]
    [InspectorName("Popülasyon Yazı Boyutu")]
    [Min(8f)] public float populationFontSize = 24f;

    [InspectorName("Üst Panel Düğme Yazı Boyutu")]
    [Min(8f)] public float topButtonFontSize = 20f;

    [InspectorName("Hız Düğmesi Yazı Boyutu")]
    [Min(8f)] public float timerButtonFontSize = 22f;

    [InspectorName("Süre / Sürüm Yazı Boyutu")]
    [Min(8f)] public float simulationInfoFontSize = 20f;

    [Header("Paneller")]
    [InspectorName("Canlı İstatistik Paneli")]
    public UIElementLayout statsPanel = new UIElementLayout(
        UIAnchorPreset.TopLeft,
        new Vector2(16f, -16f),
        new Vector2(540f, 1048f),
        Vector2.one);

    [InspectorName("Canlı Oluşturma Paneli")]
    public UIElementLayout creaturePanel = new UIElementLayout(
        UIAnchorPreset.BottomRight,
        new Vector2(-16f, 16f),
        new Vector2(620f, 150f),
        Vector2.one);

    [InspectorName("Yemek Oluşturma Paneli")]
    public UIElementLayout foodPanel = new UIElementLayout(
        UIAnchorPreset.BottomRight,
        new Vector2(-16f, 16f),
        new Vector2(620f, 150f),
        Vector2.one);

    [InspectorName("Geçen Süre / Sürüm Yazısı")]
    public UIElementLayout simulationInfo = new UIElementLayout(
        UIAnchorPreset.BottomCenter,
        new Vector2(0f, 16f),
        new Vector2(500f, 42f),
        Vector2.one);

    [Header("Gözlem / Enler")]
    [InspectorName("ENLER Düğmesi")]
    public UIElementLayout observationToggle = new UIElementLayout(
        UIAnchorPreset.TopRight,
        new Vector2(-20f, -82f),
        new Vector2(150f, 48f),
        Vector2.one);

    [InspectorName("ENLER Paneli")]
    public UIElementLayout observationPanel = new UIElementLayout(
        UIAnchorPreset.MiddleRight,
        new Vector2(-20f, -28f),
        new Vector2(430f, 760f),
        Vector2.one);

    public static SimulationUILayoutSettings Load()
    {
        return Resources.Load<SimulationUILayoutSettings>(ResourceName);
    }
}

public static class SimulationUILayoutUtility
{
    public static void Apply(RectTransform rect, UIElementLayout layout)
    {
        if (rect == null || layout == null || !layout.apply)
        {
            return;
        }

        GetAnchor(layout.anchor, out Vector2 anchor, out Vector2 pivot);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = layout.position;
        rect.sizeDelta = layout.size;
        rect.localScale = new Vector3(layout.scale.x, layout.scale.y, 1f);
    }

    public static bool ShouldUseCompactLayout(SimulationUILayoutSettings settings, float aspectRatio)
    {
        return settings != null &&
               settings.compactLayoutEnabled &&
               aspectRatio > 0f &&
               aspectRatio < settings.compactAspectThreshold;
    }

    private static void GetAnchor(UIAnchorPreset preset, out Vector2 anchor, out Vector2 pivot)
    {
        switch (preset)
        {
            case UIAnchorPreset.TopLeft: anchor = pivot = new Vector2(0f, 1f); break;
            case UIAnchorPreset.TopCenter: anchor = pivot = new Vector2(0.5f, 1f); break;
            case UIAnchorPreset.TopRight: anchor = pivot = new Vector2(1f, 1f); break;
            case UIAnchorPreset.MiddleLeft: anchor = pivot = new Vector2(0f, 0.5f); break;
            case UIAnchorPreset.MiddleRight: anchor = pivot = new Vector2(1f, 0.5f); break;
            case UIAnchorPreset.BottomLeft: anchor = pivot = new Vector2(0f, 0f); break;
            case UIAnchorPreset.BottomCenter: anchor = pivot = new Vector2(0.5f, 0f); break;
            case UIAnchorPreset.BottomRight: anchor = pivot = new Vector2(1f, 0f); break;
            default: anchor = pivot = new Vector2(0.5f, 0.5f); break;
        }
    }
}
