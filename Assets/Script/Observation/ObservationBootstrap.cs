using UnityEngine;

public static class ObservationBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateObservationSystems()
    {
        EnsureMainCanvasLayout();

        if (Object.FindAnyObjectByType<SimulationEventLogger>() != null)
        {
            return;
        }

        GameObject systems = new GameObject("Observation Systems");
        Object.DontDestroyOnLoad(systems);
        systems.AddComponent<SimulationEventLogger>();
        systems.AddComponent<ObservationDashboard>();
    }

    private static void EnsureMainCanvasLayout()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.name == "--------------UI/CANVAS--------------" &&
                canvas.GetComponent<SimulationUILayoutController>() == null)
            {
                canvas.gameObject.AddComponent<SimulationUILayoutController>();
                return;
            }
        }
    }
}
