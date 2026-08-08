using System;
using System.Collections.Generic;
using UnityEngine;

public class BiomeGenerator : MonoBehaviour
{
    [Serializable]
    public class BiomeDefinition
    {
        [Tooltip("Inspector ve Hierarchy'de görünecek biyom adı.")]
        public string biomeName = "Yeni Biyom";

        [Min(0)]
        [Tooltip("Haritada bu biyomdan kaç ayrı alan oluşturulacak?")]
        public int areaCount = 1;

        [Min(0.1f)]
        public float minRadius = 12f;

        [Min(0.1f)]
        public float maxRadius = 20f;

        [Header("Bitki Hedefleri (Her Biyom Alanı İçin)")]
        [Min(0)] public int normalPlantCount = 8;
        [Min(0)] public int poisonousPlantCount = 2;
        public GameObject normalPlantPrefab;
        public GameObject poisonousPlantPrefab;

        [Min(0f)]
        [Tooltip("Ağaç merkezleri arasında bırakılacak minimum mesafe.")]
        public float minimumPlantSpacing = 2f;

        [Tooltip("Scene görünümündeki biyom çemberinin rengi.")]
        public Color gizmoColor = new Color(0.25f, 0.8f, 0.35f, 0.8f);
    }

    private sealed class GeneratedBiome
    {
        public BiomeDefinition definition;
        public Vector2 center;
        public float radius;
        public Transform container;
        public readonly List<PlantHub> normalPlants = new List<PlantHub>();
        public readonly List<PlantHub> poisonousPlants = new List<PlantHub>();
    }

    [Header("Referanslar")]
    [SerializeField] private EcosystemManager ecosystemManager;
    [SerializeField] private Transform generatedBiomesParent;

    [Header("Biyom Tanımları")]
    [SerializeField] private List<BiomeDefinition> biomes = new List<BiomeDefinition>();

    [Header("Üretim Ayarları")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int fixedSeed = 12345;
    [SerializeField, Min(0f)] private float worldEdgePadding = 2f;
    [SerializeField, Min(0f)] private float minimumBiomeGap = 4f;
    [SerializeField, Min(1)] private int biomePlacementAttempts = 100;
    [SerializeField, Min(1)] private int plantPlacementAttempts = 30;

    [Header("Nüfus Koruma")]
    [SerializeField] private bool maintainPlantTargets = true;
    [SerializeField, Min(0.1f)] private float populationCheckInterval = 5f;

    private readonly List<GeneratedBiome> generatedBiomes = new List<GeneratedBiome>();
    private float populationCheckTimer;

    private void Awake()
    {
        if (ecosystemManager == null)
        {
            ecosystemManager = GetComponent<EcosystemManager>();
        }

        if (ecosystemManager == null)
        {
            ecosystemManager = EcosystemManager.instance;
        }
    }

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateBiomes();
        }

        populationCheckTimer = populationCheckInterval;
    }

    private void Update()
    {
        if (!maintainPlantTargets || generatedBiomes.Count == 0)
        {
            return;
        }

        populationCheckTimer -= Time.deltaTime;
        if (populationCheckTimer <= 0f)
        {
            MaintainPlantPopulations();
            populationCheckTimer = populationCheckInterval;
        }
    }

    [ContextMenu("Biyomları Yeniden Üret (Play Mode)")]
    public void GenerateBiomes()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Biyom üretimi yalnızca Play Mode sırasında çalıştırılabilir.", this);
            return;
        }

        if (ecosystemManager == null)
        {
            Debug.LogError("BiomeGenerator için EcosystemManager referansı eksik.", this);
            return;
        }

        ClearGeneratedBiomes();

        UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
        if (!useRandomSeed)
        {
            UnityEngine.Random.InitState(fixedSeed);
        }

        for (int definitionIndex = 0; definitionIndex < biomes.Count; definitionIndex++)
        {
            BiomeDefinition definition = biomes[definitionIndex];
            if (!IsDefinitionUsable(definition))
            {
                continue;
            }

            for (int areaIndex = 0; areaIndex < definition.areaCount; areaIndex++)
            {
                if (TryCreateBiome(definition, areaIndex, out GeneratedBiome generatedBiome))
                {
                    generatedBiomes.Add(generatedBiome);
                    FillBiome(generatedBiome);
                }
                else
                {
                    Debug.LogWarning(
                        definition.biomeName + " biyomu için haritada çakışmayan bir alan bulunamadı.",
                        this);
                }
            }
        }

        if (!useRandomSeed)
        {
            UnityEngine.Random.state = previousRandomState;
        }

        Debug.Log(generatedBiomes.Count + " biyom alanı üretildi.", this);
    }

    public string GetBiomeNameAtPosition(Vector2 worldPosition)
    {
        for (int i = 0; i < generatedBiomes.Count; i++)
        {
            GeneratedBiome biome = generatedBiomes[i];
            if (Vector2.SqrMagnitude(worldPosition - biome.center) <= biome.radius * biome.radius)
            {
                return biome.definition.biomeName;
            }
        }

        return string.Empty;
    }

    private bool IsDefinitionUsable(BiomeDefinition definition)
    {
        if (definition == null || definition.areaCount <= 0)
        {
            return false;
        }

        bool needsNormalPrefab = definition.normalPlantCount > 0 && definition.normalPlantPrefab == null;
        bool needsPoisonPrefab = definition.poisonousPlantCount > 0 && definition.poisonousPlantPrefab == null;
        if (needsNormalPrefab || needsPoisonPrefab)
        {
            Debug.LogWarning(definition.biomeName + " biyomunda gerekli bitki prefabı eksik.", this);
            return false;
        }

        return true;
    }

    private bool TryCreateBiome(
        BiomeDefinition definition,
        int areaIndex,
        out GeneratedBiome generatedBiome)
    {
        generatedBiome = null;

        float halfWidth = (ecosystemManager.maxX - ecosystemManager.minX) * 0.5f;
        float halfHeight = (ecosystemManager.maxY - ecosystemManager.minY) * 0.5f;
        float maximumAllowedRadius = Mathf.Min(halfWidth, halfHeight) - worldEdgePadding;
        if (maximumAllowedRadius <= 0f)
        {
            Debug.LogError("Dünya sınırları biyom üretmek için çok küçük.", this);
            return false;
        }

        float minimumRadius = Mathf.Min(definition.minRadius, definition.maxRadius);
        float maximumRadius = Mathf.Max(definition.minRadius, definition.maxRadius);
        maximumRadius = Mathf.Min(maximumRadius, maximumAllowedRadius);
        minimumRadius = Mathf.Min(minimumRadius, maximumRadius);

        for (int attempt = 0; attempt < biomePlacementAttempts; attempt++)
        {
            float radius = UnityEngine.Random.Range(minimumRadius, maximumRadius);
            float minCenterX = ecosystemManager.minX + radius + worldEdgePadding;
            float maxCenterX = ecosystemManager.maxX - radius - worldEdgePadding;
            float minCenterY = ecosystemManager.minY + radius + worldEdgePadding;
            float maxCenterY = ecosystemManager.maxY - radius - worldEdgePadding;

            Vector2 center = new Vector2(
                UnityEngine.Random.Range(minCenterX, maxCenterX),
                UnityEngine.Random.Range(minCenterY, maxCenterY));

            if (OverlapsExistingBiome(center, radius))
            {
                continue;
            }

            GameObject containerObject = new GameObject(
                definition.biomeName + " " + (areaIndex + 1));
            containerObject.transform.SetParent(generatedBiomesParent != null
                ? generatedBiomesParent
                : transform);
            containerObject.transform.position = center;

            generatedBiome = new GeneratedBiome
            {
                definition = definition,
                center = center,
                radius = radius,
                container = containerObject.transform
            };

            return true;
        }

        return false;
    }

    private bool OverlapsExistingBiome(Vector2 center, float radius)
    {
        for (int i = 0; i < generatedBiomes.Count; i++)
        {
            GeneratedBiome other = generatedBiomes[i];
            float requiredDistance = radius + other.radius + minimumBiomeGap;
            if (Vector2.SqrMagnitude(center - other.center) < requiredDistance * requiredDistance)
            {
                return true;
            }
        }

        return false;
    }

    private void FillBiome(GeneratedBiome biome)
    {
        for (int i = 0; i < biome.definition.normalPlantCount; i++)
        {
            SpawnPlantInBiome(biome, false);
        }

        for (int i = 0; i < biome.definition.poisonousPlantCount; i++)
        {
            SpawnPlantInBiome(biome, true);
        }
    }

    private void MaintainPlantPopulations()
    {
        for (int i = 0; i < generatedBiomes.Count; i++)
        {
            GeneratedBiome biome = generatedBiomes[i];
            biome.normalPlants.RemoveAll(plant => plant == null);
            biome.poisonousPlants.RemoveAll(plant => plant == null);

            int missingNormalPlants = biome.definition.normalPlantCount - biome.normalPlants.Count;
            int missingPoisonousPlants = biome.definition.poisonousPlantCount - biome.poisonousPlants.Count;

            for (int plantIndex = 0; plantIndex < missingNormalPlants; plantIndex++)
            {
                SpawnPlantInBiome(biome, false);
            }

            for (int plantIndex = 0; plantIndex < missingPoisonousPlants; plantIndex++)
            {
                SpawnPlantInBiome(biome, true);
            }
        }
    }

    private bool SpawnPlantInBiome(GeneratedBiome biome, bool poisonous)
    {
        GameObject prefab = poisonous
            ? biome.definition.poisonousPlantPrefab
            : biome.definition.normalPlantPrefab;

        if (prefab == null || !TryFindPlantPosition(biome, out Vector2 position))
        {
            return false;
        }

        GameObject plantObject = Instantiate(prefab, position, Quaternion.identity, biome.container);
        PlantHub plant = plantObject.GetComponentInChildren<PlantHub>();
        if (plant == null)
        {
            Debug.LogError(prefab.name + " prefabında PlantHub bileşeni bulunamadı.", prefab);
            Destroy(plantObject);
            return false;
        }

        ecosystemManager.RegisterPlant(plant);
        if (poisonous)
        {
            biome.poisonousPlants.Add(plant);
        }
        else
        {
            biome.normalPlants.Add(plant);
        }

        return true;
    }

    private bool TryFindPlantPosition(GeneratedBiome biome, out Vector2 position)
    {
        float usableRadius = Mathf.Max(0.1f, biome.radius - worldEdgePadding);

        for (int attempt = 0; attempt < plantPlacementAttempts; attempt++)
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float distance = Mathf.Sqrt(UnityEngine.Random.value) * usableRadius;
            Vector2 candidate = biome.center + new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance);

            if (ecosystemManager.IsInsideWorld(candidate, worldEdgePadding) &&
                IsPlantPositionClear(candidate, biome.definition.minimumPlantSpacing))
            {
                position = candidate;
                return true;
            }
        }

        position = Vector2.zero;
        return false;
    }

    private bool IsPlantPositionClear(Vector2 candidate, float minimumSpacing)
    {
        float minimumSpacingSquared = minimumSpacing * minimumSpacing;
        List<PlantHub> livingPlants = ecosystemManager.allLivingPlants;

        for (int i = livingPlants.Count - 1; i >= 0; i--)
        {
            PlantHub plant = livingPlants[i];
            if (plant == null)
            {
                livingPlants.RemoveAt(i);
                continue;
            }

            if (Vector2.SqrMagnitude(candidate - (Vector2)plant.transform.position) < minimumSpacingSquared)
            {
                return false;
            }
        }

        return true;
    }

    private void ClearGeneratedBiomes()
    {
        for (int i = 0; i < generatedBiomes.Count; i++)
        {
            GeneratedBiome biome = generatedBiomes[i];

            for (int plantIndex = 0; plantIndex < biome.normalPlants.Count; plantIndex++)
            {
                ecosystemManager.UnregisterPlant(biome.normalPlants[plantIndex]);
            }

            for (int plantIndex = 0; plantIndex < biome.poisonousPlants.Count; plantIndex++)
            {
                ecosystemManager.UnregisterPlant(biome.poisonousPlants[plantIndex]);
            }

            if (biome.container != null)
            {
                Destroy(biome.container.gameObject);
            }
        }

        generatedBiomes.Clear();
    }

    private void OnValidate()
    {
        populationCheckInterval = Mathf.Max(0.1f, populationCheckInterval);
        biomePlacementAttempts = Mathf.Max(1, biomePlacementAttempts);
        plantPlacementAttempts = Mathf.Max(1, plantPlacementAttempts);

        for (int i = 0; i < biomes.Count; i++)
        {
            BiomeDefinition definition = biomes[i];
            if (definition == null)
            {
                continue;
            }

            definition.minRadius = Mathf.Max(0.1f, definition.minRadius);
            definition.maxRadius = Mathf.Max(definition.minRadius, definition.maxRadius);
        }
    }

    private void OnDrawGizmosSelected()
    {
        for (int i = 0; i < generatedBiomes.Count; i++)
        {
            GeneratedBiome biome = generatedBiomes[i];
            Gizmos.color = biome.definition.gizmoColor;
            Gizmos.DrawWireSphere(biome.center, biome.radius);
        }
    }
}
