using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldManager))]
public class WorldManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw standard fields (Seed, Chunk Size, Block Types array, etc.)
        DrawDefaultInspector();

        WorldManager worldManager = (WorldManager)target;

        GUILayout.Space(15);

        // Editor Button to trigger generation
        if (GUILayout.Button("Generate World in Editor", GUILayout.Height(30)))
        {
            worldManager.GenerateWorld();
        }

        // Editor Button to clear generation
        if (GUILayout.Button("Clear World", GUILayout.Height(25)))
        {
            worldManager.ClearWorld();
        }
    }
}