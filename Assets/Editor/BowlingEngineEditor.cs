using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BowlingEngine))]
public class BowlingEngineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BowlingEngine engine = (BowlingEngine)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Log Coordinates for Script Copy-Paste"))
        {
            engine.LogPointsForScript();
        }

        if (GUILayout.Button("Simulate DecidePoint"))
        {
            engine.DecidePoint(false);
        }

        if (GUILayout.Button("Reset to Defaults"))
        {
            if (EditorUtility.DisplayDialog("Reset Points", "Overwrite manual moves with defaults?", "Yes", "No"))
                engine.InitializePoints();
        }
    }

    private void OnSceneGUI()
    {
        BowlingEngine engine = (BowlingEngine)target;
        if (engine.deliveryPoints == null) return;

        for (int i = 0; i < engine.deliveryPoints.Count; i++)
        {
            BowlingPoint bp = engine.deliveryPoints[i];

            // Set handle color
            Handles.color = (bp.length == DeliveryLength.GoodLength) ? Color.green :
                            (bp.length == DeliveryLength.Yorker) ? Color.red :
                            (bp.length == DeliveryLength.Short) ? Color.blue : Color.yellow;

            // Display Label with Name and Coordinates
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontStyle = FontStyle.Bold;
            string labelText = $"{bp.name}\n({bp.point.x:F1}, {bp.point.z:F1})";
            Handles.Label(bp.point + Vector3.up * 0.6f, labelText, labelStyle);

            // Create Position Handle
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(bp.point, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(engine, "Move Bowling Point");
                newPos.y = bp.point.y; // Lock to ground
                engine.UpdatePoint(i, newPos);
                EditorUtility.SetDirty(engine);
            }
        }
    }
}