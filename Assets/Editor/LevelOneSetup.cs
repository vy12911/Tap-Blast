using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TapBlast.Editor
{
    [InitializeOnLoad]
    public static class LevelOneSetup
    {
        private const string ScenePath = "Assets/Scenes/Level01.unity";
        static LevelOneSetup() { EditorApplication.delayCall += ConfigurePlayScene; }
        private static void ConfigurePlayScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.playModeStartScene == null)
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }
        [MenuItem("Tap Blast/Open Level 1")]
        [MenuItem("Tap Blast/Open Campaign")]
        public static void OpenLevel()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }
        [MenuItem("Tap Blast/Select Level 1 Settings")]
        public static void SelectConfig()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/Data/Levels/Level01.asset");
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
        [MenuItem("Tap Blast/Select Level 2 Settings")]
        public static void SelectLevelTwoConfig()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/Data/Levels/Level02.asset");
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
    }
}
