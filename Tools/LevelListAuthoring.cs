// Editor-only authoring utility. Run in the disposable validation project;
// copy the reviewed saved prefab back afterwards. Never runs during gameplay.
// Replaces the two ten-row World pages with one saved twenty-row level list.
using System;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using TapBlast;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class LevelListAuthoring
{
    const string CanvasPath = "Assets/Prefabs/UI/GameCanvas.prefab";
    const int Levels = 20;
    static string Renumber(string name, int from, int to) => new Regex(@"\bLevel " + from + @"\b").Replace(name, "Level " + to);
    public static void Run()
    {
        try
        {
            var root = PrefabUtility.LoadPrefabContents(CanvasPath);
            var data = new SerializedObject(root.GetComponent<GameHUD>());
            var buttons = data.FindProperty("levelButtons"); var texts = data.FindProperty("selectionTexts");
            var content = ((ScrollRect)data.FindProperty("selectionScroll").objectReferenceValue).content;
            var select = ((GameObject)data.FindProperty("levelSelect").objectReferenceValue).transform;
            int authored = buttons.arraySize;
            var first = (RectTransform)((Button)buttons.GetArrayElementAtIndex(0).objectReferenceValue).transform;
            var second = (RectTransform)((Button)buttons.GetArrayElementAtIndex(1).objectReferenceValue).transform;
            float pitch = first.anchoredPosition.y - second.anchoredPosition.y;
            if (Mathf.Abs(pitch - data.FindProperty("levelRowPitch").floatValue) > .01f) throw new Exception("Row pitch " + pitch + " differs from GameHUD.levelRowPitch");

            // Clone level N-10 for each new row so badge colors and artwork keep their five-row cycle.
            buttons.arraySize = texts.arraySize = Levels;
            for (int i = authored; i < Levels; i++)
            {
                int source = i % authored;
                var sourceRow = (RectTransform)((Button)buttons.GetArrayElementAtIndex(source).objectReferenceValue).transform;
                var sourceStatus = ((TMP_Text)texts.GetArrayElementAtIndex(source).objectReferenceValue).transform;
                string statusPath = Renumber(AnimationUtility.CalculateTransformPath(sourceStatus, sourceRow), source + 1, i + 1);
                var row = (RectTransform)Object.Instantiate(sourceRow.gameObject, content, false).transform;
                foreach (var t in row.GetComponentsInChildren<Transform>(true)) t.name = Renumber(t.name, source + 1, i + 1);
                row.name = "Select Level " + (i + 1);
                row.anchoredPosition = sourceRow.anchoredPosition + Vector2.down * pitch * (i - source);
                buttons.GetArrayElementAtIndex(i).objectReferenceValue = row.GetComponent<Button>();
                texts.GetArrayElementAtIndex(i).objectReferenceValue = row.Find(statusPath).GetComponent<TMP_Text>();
            }
            content.sizeDelta += Vector2.up * pitch * (Levels - authored);

            foreach (string name in new[] { "Previous world", "Next world" })
            {
                var button = select.Find(name);
                if (button != null) Object.DestroyImmediate(button.gameObject);
            }
            var world = (TMP_Text)data.FindProperty("selectionWorld").objectReferenceValue;
            world.text = "ALL LEVELS"; world.rectTransform.sizeDelta = new Vector2(300, world.rectTransform.sizeDelta.y);
            var worldPlate = (RectTransform)world.transform.parent.Find(world.name + " plate");
            if (worldPlate != null) worldPlate.sizeDelta = new Vector2(340, worldPlate.sizeDelta.y);
            ((TMP_Text)data.FindProperty("selectionTitle").objectReferenceValue).text = "LEVEL <color=#EAF8FF>SELECT</color>";
            ((TMP_Text)data.FindProperty("introWorld").objectReferenceValue).text = "LEVEL 01";
            ((TMP_Text)data.FindProperty("worldLabel").objectReferenceValue).text = "LEVEL 01   /   FIRST SPARK";
            foreach (var t in select.GetComponentsInChildren<Transform>(true))
                if (t.name == "Footer") t.GetComponent<TMP_Text>().text = "TWENTY CHALLENGES. KEEP YOUR STREAK.";

            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, CanvasPath); PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            File.WriteAllText("level-list-authoring.txt", "PASS Saved " + Levels + " level rows in one list (" + (Levels - authored) + " added); World navigation removed.\n");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("level-list-authoring.txt", "FAIL " + e); EditorApplication.Exit(1); }
    }
}
