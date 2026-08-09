using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public enum SimulationDeathCause
{
    Unknown,
    Starvation,
    Poison,
    Predation
}

public class SimulationEventLogger : MonoBehaviour
{
    private sealed class EventCounters
    {
        public long spawns;
        public long births;
        public long deaths;
        public long starvationDeaths;
        public long poisonDeaths;
        public long predationDeaths;
        public long attacks;
        public long kills;
        public long plantsEaten;
        public long poisonPlantsEaten;
        public long meatEaten;
        public double digestedEnergy;

        public void Clear()
        {
            spawns = births = deaths = starvationDeaths = poisonDeaths = predationDeaths = 0;
            attacks = kills = plantsEaten = poisonPlantsEaten = meatEaten = 0;
            digestedEnergy = 0d;
        }
    }

    private static SimulationEventLogger instance;
    public static SimulationEventLogger Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<SimulationEventLogger>();
            }
            return instance;
        }
    }

    [Header("Özetleme")]
    [Tooltip("Oyun içi kaç saniyede bir ekosistem özeti yazılacağını belirler.")]
    [SerializeField, Min(10f)] private float snapshotIntervalSimulationSeconds = 60f;
    [SerializeField, Min(1f)] private float diskFlushIntervalRealSeconds = 5f;

    [Header("Dosya Sınırları")]
    [SerializeField, Min(1)] private int maxFileSizeMegabytes = 4;
    [SerializeField, Range(2, 50)] private int retainedLogFiles = 12;

    private readonly EventCounters interval = new EventCounters();
    private readonly EventCounters totals = new EventCounters();
    private readonly StringBuilder pending = new StringBuilder(16 * 1024);
    private readonly CultureInfo invariant = CultureInfo.InvariantCulture;

    private StreamWriter writer;
    private string logDirectory;
    private string sessionId;
    private string sessionFileStem;
    private string currentLogPath;
    private int partNumber;
    private long nextCreatureId = 1;
    private float simulationSeconds;
    private float nextSnapshotAt;
    private float flushTimer;
    private bool shuttingDown;
    private bool loggingAvailable = true;
    private bool predatorMilestoneWritten;
    private bool toxicovoreMilestoneWritten;

    public int SnapshotCount { get; private set; }
    public string CurrentLogPath => currentLogPath;
    public string CurrentLogFileName => string.IsNullOrEmpty(currentLogPath) ? "hazırlanıyor" : Path.GetFileName(currentLogPath);

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        sessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
        sessionFileStem = $"simulation_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{sessionId}";
        logDirectory = Path.Combine(Application.persistentDataPath, "SimulationLogs");
        nextSnapshotAt = snapshotIntervalSimulationSeconds;
        flushTimer = diskFlushIntervalRealSeconds;

        OpenWriter();
        AppendSessionRecord("session_start");
        FlushToDisk();
    }

    private void Update()
    {
        simulationSeconds += Time.deltaTime;
        flushTimer -= Time.unscaledDeltaTime;

        if (simulationSeconds >= nextSnapshotAt)
        {
            AppendSnapshot();
            nextSnapshotAt = simulationSeconds + snapshotIntervalSimulationSeconds;
        }

        if (flushTimer <= 0f || pending.Length >= 64 * 1024)
        {
            FlushToDisk();
            flushTimer = diskFlushIntervalRealSeconds;
        }
    }

    private void OnApplicationQuit()
    {
        Shutdown();
    }

    private void OnDisable()
    {
        Shutdown();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            Shutdown();
            instance = null;
        }
    }

    public static long RegisterCreature(CreatureStats creature)
    {
        if (Instance == null || creature == null)
        {
            return 0;
        }

        long id = Instance.nextCreatureId++;
        Instance.interval.spawns++;
        Instance.totals.spawns++;
        return id;
    }

    public static void RecordBirth(CreatureStats parentA, CreatureStats parentB)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.interval.births++;
        Instance.totals.births++;
    }

    public static void RecordAttack(CreatureStats attacker, CreatureStats prey, float damage)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.interval.attacks++;
        Instance.totals.attacks++;
    }

    public static void RecordDeath(CreatureStats creature, SimulationDeathCause cause, CreatureStats attacker)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.interval.deaths++;
        Instance.totals.deaths++;

        switch (cause)
        {
            case SimulationDeathCause.Starvation:
                Instance.interval.starvationDeaths++;
                Instance.totals.starvationDeaths++;
                break;
            case SimulationDeathCause.Poison:
                Instance.interval.poisonDeaths++;
                Instance.totals.poisonDeaths++;
                break;
            case SimulationDeathCause.Predation:
                Instance.interval.predationDeaths++;
                Instance.totals.predationDeaths++;
                Instance.interval.kills++;
                Instance.totals.kills++;
                break;
        }
    }

    public static void RecordFoodConsumed(CreatureStats creature, FoodType foodType, float digestedEnergy)
    {
        if (Instance == null)
        {
            return;
        }

        switch (foodType)
        {
            case FoodType.Plant:
                Instance.interval.plantsEaten++;
                Instance.totals.plantsEaten++;
                break;
            case FoodType.PoisonousPlant:
                Instance.interval.poisonPlantsEaten++;
                Instance.totals.poisonPlantsEaten++;
                break;
            case FoodType.Meat:
                Instance.interval.meatEaten++;
                Instance.totals.meatEaten++;
                break;
        }

        Instance.interval.digestedEnergy += digestedEnergy;
        Instance.totals.digestedEnergy += digestedEnergy;
    }

    public void OpenLogFolder()
    {
        if (!string.IsNullOrEmpty(logDirectory))
        {
            Application.OpenURL("file:///" + logDirectory.Replace('\\', '/'));
        }
    }

    private void AppendSnapshot()
    {
        List<CreatureStats> living = GetLivingCreatures();
        int plants = EcosystemManager.instance != null
            ? EcosystemManager.instance.allLivingPlants.Count(p => p != null)
            : 0;

        float generationSum = 0f;
        float ageSum = 0f;
        float meatDesireSum = 0f;
        float meatEfficiencySum = 0f;
        float poisonDesireSum = 0f;
        float poisonResistanceSum = 0f;
        int herbivores = 0;
        int omnivores = 0;
        int scavengers = 0;
        int predators = 0;
        int toxicovores = 0;
        int developing = 0;
        int herbivoreLineage = 0;
        int predatorLineage = 0;
        int scavengerLineage = 0;
        int toxicovoreLineage = 0;
        int unassignedLineage = 0;

        foreach (CreatureStats creature in living)
        {
            generationSum += creature.generation;
            ageSum += creature.age;
            meatDesireSum += creature.dna.desireMeat;
            meatEfficiencySum += creature.dna.meatEfficiency;
            poisonDesireSum += creature.dna.desirePoison;
            poisonResistanceSum += creature.dna.poisonResistance;

            switch (creature.dna.ecologicalLineage)
            {
                case EcologicalLineage.Herbivore: herbivoreLineage++; break;
                case EcologicalLineage.Predator: predatorLineage++; break;
                case EcologicalLineage.Scavenger: scavengerLineage++; break;
                case EcologicalLineage.Toxicovore: toxicovoreLineage++; break;
                default: unassignedLineage++; break;
            }

            switch (GetSimpleClass(creature))
            {
                case "herbivore": herbivores++; break;
                case "omnivore": omnivores++; break;
                case "scavenger": scavengers++; break;
                case "predator": predators++; break;
                case "toxicovore": toxicovores++; break;
                default: developing++; break;
            }
        }

        float divisor = Mathf.Max(1, living.Count);
        CreatureStats carnivoreChampion = FindChampion(living, c => Mathf.Sqrt(Mathf.Clamp01(c.dna.desireMeat) * Mathf.Clamp01(c.dna.meatEfficiency)));
        CreatureStats poisonChampion = FindChampion(living, c => c.lifetimePoisonPlantsEaten);
        CreatureStats hunterChampion = FindChampion(living, c => c.lifetimeKills);
        CreatureStats generationChampion = FindChampion(living, c => c.generation);

        StringBuilder json = new StringBuilder(2048);
        json.Append("{\"schema\":1,\"type\":\"snapshot\",\"session_id\":\"").Append(sessionId)
            .Append("\",\"utc\":\"").Append(DateTime.UtcNow.ToString("O", invariant))
            .Append("\",\"sim_seconds\":").Append(F(simulationSeconds))
            .Append(",\"time_scale\":").Append(F(Time.timeScale))
            .Append(",\"population\":").Append(living.Count)
            .Append(",\"plants\":").Append(plants)
            .Append(",\"interval\":");
        AppendCounters(json, interval);
        json.Append(",\"totals\":");
        AppendCounters(json, totals);
        json.Append(",\"profile\":{")
            .Append("\"avg_generation\":").Append(F(generationSum / divisor))
            .Append(",\"avg_age\":").Append(F(ageSum / divisor))
            .Append(",\"avg_meat_desire\":").Append(F(meatDesireSum / divisor))
            .Append(",\"avg_meat_efficiency\":").Append(F(meatEfficiencySum / divisor))
            .Append(",\"avg_poison_desire\":").Append(F(poisonDesireSum / divisor))
            .Append(",\"avg_poison_resistance\":").Append(F(poisonResistanceSum / divisor))
            .Append(",\"classes\":{")
            .Append("\"herbivore\":").Append(herbivores)
            .Append(",\"omnivore\":").Append(omnivores)
            .Append(",\"scavenger\":").Append(scavengers)
            .Append(",\"predator\":").Append(predators)
            .Append(",\"toxicovore\":").Append(toxicovores)
            .Append(",\"developing\":").Append(developing)
            .Append("},\"lineages\":{")
            .Append("\"herbivore\":").Append(herbivoreLineage)
            .Append(",\"predator\":").Append(predatorLineage)
            .Append(",\"scavenger\":").Append(scavengerLineage)
            .Append(",\"toxicovore\":").Append(toxicovoreLineage)
            .Append(",\"unassigned\":").Append(unassignedLineage)
            .Append("}},\"champions\":{");
        AppendChampion(json, "carnivore_genetics", carnivoreChampion, c => Mathf.Sqrt(Mathf.Clamp01(c.dna.desireMeat) * Mathf.Clamp01(c.dna.meatEfficiency)));
        json.Append(',');
        AppendChampion(json, "poison_consumer", poisonChampion, c => c.lifetimePoisonPlantsEaten);
        json.Append(',');
        AppendChampion(json, "hunter", hunterChampion, c => c.lifetimeKills);
        json.Append(',');
        AppendChampion(json, "generation", generationChampion, c => c.generation);
        json.Append("}}\n");

        pending.Append(json);
        SnapshotCount++;

        if (!predatorMilestoneWritten && predators > 0)
        {
            AppendMilestone("first_predator", carnivoreChampion);
            predatorMilestoneWritten = true;
        }

        if (!toxicovoreMilestoneWritten && toxicovores > 0)
        {
            AppendMilestone("first_toxicovore", poisonChampion);
            toxicovoreMilestoneWritten = true;
        }

        interval.Clear();
    }

    private void AppendSessionRecord(string type)
    {
        pending.Append("{\"schema\":1,\"type\":\"").Append(type)
            .Append("\",\"session_id\":\"").Append(sessionId)
            .Append("\",\"utc\":\"").Append(DateTime.UtcNow.ToString("O", invariant))
            .Append("\",\"sim_seconds\":").Append(F(simulationSeconds))
            .Append(",\"unity_version\":\"").Append(Escape(Application.unityVersion))
            .Append("\",\"game_version\":\"").Append(Escape(Application.version))
            .Append("\"}\n");
    }

    private void AppendMilestone(string milestone, CreatureStats creature)
    {
        pending.Append("{\"schema\":1,\"type\":\"milestone\",\"session_id\":\"").Append(sessionId)
            .Append("\",\"utc\":\"").Append(DateTime.UtcNow.ToString("O", invariant))
            .Append("\",\"sim_seconds\":").Append(F(simulationSeconds))
            .Append(",\"milestone\":\"").Append(milestone).Append("\",\"creature\":");
        AppendCreatureIdentity(pending, creature, creature == null ? 0f : creature.generation);
        pending.Append("}\n");
    }

    private void AppendCounters(StringBuilder json, EventCounters counters)
    {
        json.Append('{')
            .Append("\"spawns\":").Append(counters.spawns)
            .Append(",\"births\":").Append(counters.births)
            .Append(",\"deaths\":").Append(counters.deaths)
            .Append(",\"starvation_deaths\":").Append(counters.starvationDeaths)
            .Append(",\"poison_deaths\":").Append(counters.poisonDeaths)
            .Append(",\"predation_deaths\":").Append(counters.predationDeaths)
            .Append(",\"attacks\":").Append(counters.attacks)
            .Append(",\"kills\":").Append(counters.kills)
            .Append(",\"plants_eaten\":").Append(counters.plantsEaten)
            .Append(",\"poison_plants_eaten\":").Append(counters.poisonPlantsEaten)
            .Append(",\"meat_eaten\":").Append(counters.meatEaten)
            .Append(",\"digested_energy\":").Append(F(counters.digestedEnergy))
            .Append('}');
    }

    private void AppendChampion(StringBuilder json, string key, CreatureStats creature, Func<CreatureStats, float> score)
    {
        json.Append('\"').Append(key).Append("\":");
        AppendCreatureIdentity(json, creature, creature == null ? 0f : score(creature));
    }

    private void AppendCreatureIdentity(StringBuilder json, CreatureStats creature, float score)
    {
        if (creature == null)
        {
            json.Append("null");
            return;
        }

        json.Append("{\"id\":").Append(creature.observationId)
            .Append(",\"name\":\"").Append(Escape(creature.name))
            .Append("\",\"generation\":").Append(creature.generation)
            .Append(",\"score\":").Append(F(score))
            .Append('}');
    }

    private static CreatureStats FindChampion(List<CreatureStats> living, Func<CreatureStats, float> score)
    {
        CreatureStats champion = null;
        float best = float.NegativeInfinity;
        foreach (CreatureStats creature in living)
        {
            float candidate = score(creature);
            if (candidate > best)
            {
                best = candidate;
                champion = creature;
            }
        }
        return champion;
    }

    private static string GetSimpleClass(CreatureStats creature)
    {
        if (creature.dna.desirePoison >= 0.35f && creature.dna.poisonResistance >= 0.35f)
        {
            return "toxicovore";
        }

        if (creature.dna.desireMeat >= 0.40f && creature.dna.meatEfficiency >= 0.40f)
        {
            return creature.dna.attackDamageMultiplier >= 15f && creature.dna.attackDistance >= 1.2f
                ? "predator"
                : "scavenger";
        }

        if (creature.dna.plantEfficiency >= 0.35f && creature.dna.meatEfficiency >= 0.35f)
        {
            return "omnivore";
        }

        if (creature.dna.desirePlant >= 0.40f && creature.dna.plantEfficiency >= 0.40f)
        {
            return "herbivore";
        }

        return "developing";
    }

    private List<CreatureStats> GetLivingCreatures()
    {
        if (EcosystemManager.instance == null)
        {
            return new List<CreatureStats>();
        }

        EcosystemManager.instance.allLivingCreatures.RemoveAll(c => c == null || c.dna == null);
        return EcosystemManager.instance.allLivingCreatures;
    }

    private void OpenWriter()
    {
        if (!loggingAvailable)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(logDirectory);
            string partSuffix = partNumber == 0 ? string.Empty : $"_part{partNumber:D2}";
            currentLogPath = Path.Combine(logDirectory, sessionFileStem + partSuffix + ".jsonl");
            writer = new StreamWriter(currentLogPath, true, new UTF8Encoding(false), 4096);
            PruneOldFiles();
        }
        catch (Exception exception)
        {
            DisableLogging(exception);
        }
    }

    private void FlushToDisk()
    {
        if (!loggingAvailable || writer == null || pending.Length == 0)
        {
            return;
        }

        try
        {
            writer.Write(pending.ToString());
            pending.Length = 0;
            writer.Flush();

            long maximumBytes = (long)maxFileSizeMegabytes * 1024L * 1024L;
            if (writer.BaseStream.Length >= maximumBytes)
            {
                writer.Dispose();
                writer = null;
                partNumber++;
                OpenWriter();
            }
        }
        catch (Exception exception)
        {
            DisableLogging(exception);
        }
    }

    private void PruneOldFiles()
    {
        FileInfo[] files = new DirectoryInfo(logDirectory)
            .GetFiles("simulation_*.jsonl")
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToArray();

        for (int index = retainedLogFiles; index < files.Length; index++)
        {
            if (!string.Equals(files[index].FullName, currentLogPath, StringComparison.OrdinalIgnoreCase))
            {
                files[index].Delete();
            }
        }
    }

    private void Shutdown()
    {
        if (shuttingDown)
        {
            return;
        }

        shuttingDown = true;
        if (simulationSeconds > 0f)
        {
            AppendSnapshot();
        }
        AppendSessionRecord("session_end");
        FlushToDisk();
        writer?.Dispose();
        writer = null;
    }

    private void DisableLogging(Exception exception)
    {
        loggingAvailable = false;
        pending.Length = 0;
        writer?.Dispose();
        writer = null;
        Debug.LogWarning($"Simülasyon günlüğü yazılamadı: {exception.Message}");
    }

    private string F(float value)
    {
        return value.ToString("0.####", invariant);
    }

    private string F(double value)
    {
        return value.ToString("0.####", invariant);
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
    }
}
