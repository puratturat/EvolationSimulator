using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EcosystemManager : MonoBehaviour
{
    public static EcosystemManager instance;

    [Header("Arayüz (UI) Referansları")]
    public TextMeshProUGUI populationText;

    [Header("Simülasyon Kayıtları")]
    public List<CreatureStats> allLivingCreatures = new List<CreatureStats>();
    public List<PlantHub> allLivingPlants = new List<PlantHub>();

    [Header("Dünya Sınırları (Harita Büyüklüğü)")]
    public float minX = -15f;
    public float maxX = 15f;
    public float minY = -10f;
    public float maxY = 10f;

    private float uiUpdateTimer;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Sahnede birden fazla EcosystemManager bulundu.", this);
        }

        instance = this;
    }

    private void Update()
    {
        if (populationText == null)
        {
            return;
        }

        uiUpdateTimer -= Time.deltaTime;
        if (uiUpdateTimer <= 0f)
        {
            UpdatePopulationCount();
            uiUpdateTimer = 0.5f;
        }
    }

    public void RegisterPlant(PlantHub plant)
    {
        if (plant != null && !allLivingPlants.Contains(plant))
        {
            allLivingPlants.Add(plant);
        }
    }

    public void UnregisterPlant(PlantHub plant)
    {
        if (plant != null)
        {
            allLivingPlants.Remove(plant);
        }
    }

    public bool IsInsideWorld(Vector2 position, float margin = 0f)
    {
        return position.x >= minX + margin &&
               position.x <= maxX - margin &&
               position.y >= minY + margin &&
               position.y <= maxY - margin;
    }

    private void UpdatePopulationCount()
    {
        allLivingCreatures.RemoveAll(item => item == null);
        allLivingPlants.RemoveAll(item => item == null);
        populationText.text = "Leater Popülasyonu: " + allLivingCreatures.Count;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}
