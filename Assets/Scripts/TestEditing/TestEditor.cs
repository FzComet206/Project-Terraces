using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Generator))]
public class TestEditor: Editor
{
    public override void OnInspectorGUI()
    {
        Generator generator = (Generator) target;
        DrawDefaultInspector();

        if (GUILayout.Button("Generate"))
        {
            generator.Generate();
        }
        if (GUILayout.Button("Delete"))
        {
            generator.DeleteMesh();
        }
    }
}