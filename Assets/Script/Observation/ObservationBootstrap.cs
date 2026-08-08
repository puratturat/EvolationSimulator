using UnityEngine;

public static class ObservationBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateObservationSystems()
    {
        if (Object.FindAnyObjectByType<SimulationEventLogger>() != null)
        {
            return;
        }

        GameObject systems = new GameObject("Observation Systems");
        Object.DontDestroyOnLoad(systems);
        systems.AddComponent<SimulationEventLogger>();
        systems.AddComponent<ObservationDashboard>();
    }
}
