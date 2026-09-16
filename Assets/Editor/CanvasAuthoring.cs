using UnityEditor;
using UnityEngine;

namespace TapBlast.Editor
{
    public static class CanvasAuthoring
    {
        [MenuItem("Tap Blast/Select Game Canvas")]
        public static void SelectCanvas()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GameCanvas.prefab");
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
        [MenuItem("Tap Blast/Select Normal Target Prefab")]
        public static void SelectTarget()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Targets/NormalTarget.prefab");
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
    }
}
