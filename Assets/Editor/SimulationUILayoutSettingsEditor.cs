using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimulationUILayoutSettings))]
public class SimulationUILayoutSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Bu varlık oyundaki tüm ana UI öğelerinin konum, boyut, anchor ve ölçeğini yönetir. " +
            "Play Mode sırasında yaptığınız değişiklikler yaklaşık yarım saniye içinde canlı olarak uygulanır.",
            MessageType.Info);

        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Açık Sahneye Şimdi Uygula", GUILayout.Height(32f)))
        {
            SimulationUILayoutController[] controllers = FindObjectsByType<SimulationUILayoutController>();
            foreach (SimulationUILayoutController controller in controllers)
            {
                controller.settings = (SimulationUILayoutSettings)target;
                controller.ResolveReferences();
                controller.ApplyLayout();
                EditorUtility.SetDirty(controller);
            }

            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Varsayılan Ayar Varlığını Seç"))
        {
            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }
    }
}
