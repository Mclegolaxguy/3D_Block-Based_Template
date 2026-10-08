#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldManager))]
public class WorldManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WorldManager worldManager = (WorldManager)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Generate World"))
        {
            worldManager.GenerateWorld();
            EditorUtility.SetDirty(worldManager);
        }

        if (GUILayout.Button("Clear World"))
        {
            worldManager.ClearWorld();
            EditorUtility.SetDirty(worldManager);
        }
    }
}
#endif