// Editor-only utility. Run in the disposable validation project; copy the reviewed scene back afterwards.
// Report: lists every difference between the scene's unpacked "Tap Blast UI" and the prefab it came from.
// Relink: replaces the unpacked copy with a linked GameCanvas instance, keeping the designer's scene edits
// (found by Report) as ordinary prefab overrides and rebinding scene references to the new instance.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SceneCanvasRelink
{
    const string ScenePath = "Assets/Scenes/Level01.unity";
    const string FlatPath = "Assets/Prefabs/UI/GameCanvasFlat.prefab";
    const string UiName = "Tap Blast UI";
    static readonly HashSet<string> Ignored = new HashSet<string> {
        "m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset", "m_GameObject", "m_Father",
        "m_LocalEulerAnglesHint", "m_TextStyleHashCode", "m_havePropertiesChanged", "m_isInputParsingRequired", "m_EditorClassIdentifier" };

    static GameObject FindRoot(string name) =>
        EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == name) ?? throw new Exception("Scene has no root " + name);
    static string PathOf(Transform t, Transform root) => t == root ? "" : AnimationUtility.CalculateTransformPath(t, root);
    static string Describe(Object value, Transform root)
    {
        if (value == null) return "null";
        var t = value is Component c ? c.transform : value is GameObject g ? g.transform : null;
        if (t != null && (t == root || t.IsChildOf(root))) return "<" + value.GetType().Name + ":" + PathOf(t, root) + ">";
        string asset = AssetDatabase.GetAssetPath(value);
        return asset.Length > 0 ? asset + "#" + value.name : value.GetType().Name + ":" + value.name;
    }
    static string Value(SerializedProperty p, Transform root)
    {
        switch (p.propertyType)
        {
            case SerializedPropertyType.ObjectReference: return Describe(p.objectReferenceValue, root);
            case SerializedPropertyType.Float: return p.floatValue.ToString("0.####");
            case SerializedPropertyType.Integer: case SerializedPropertyType.LayerMask: case SerializedPropertyType.ArraySize: return p.longValue.ToString();
            case SerializedPropertyType.Boolean: return p.boolValue.ToString();
            case SerializedPropertyType.String: return "\"" + p.stringValue + "\"";
            case SerializedPropertyType.Enum: return p.enumValueIndex.ToString();
            case SerializedPropertyType.Color: return p.colorValue.ToString();
            case SerializedPropertyType.Vector2: return p.vector2Value.ToString("0.####");
            case SerializedPropertyType.Vector3: return p.vector3Value.ToString("0.####");
            case SerializedPropertyType.Vector4: return p.vector4Value.ToString("0.####");
            case SerializedPropertyType.Quaternion: return p.quaternionValue.eulerAngles.ToString("0.##");
            case SerializedPropertyType.Rect: return p.rectValue.ToString();
            default: return null; // Compared through their leaf children.
        }
    }
    static void CompareComponent(Component scene, Component prefab, Transform sceneRoot, Transform prefabRoot, string where, StringBuilder report)
    {
        var a = new SerializedObject(scene); var b = new SerializedObject(prefab);
        var p = a.GetIterator();
        while (p.Next(true))
        {
            // Object references are compared by target path; their raw file ids always differ.
            if (Ignored.Contains(p.name) || p.name == "m_FileID" || p.name == "m_PathID" || p.propertyPath.StartsWith("m_Children") || p.propertyPath.StartsWith("m_Component")) continue;
            string left = Value(p, sceneRoot); if (left == null) continue;
            var other = b.FindProperty(p.propertyPath);
            string right = other == null ? "(missing)" : Value(other, prefabRoot);
            if (left != right) report.AppendLine("  CHANGED " + where + " [" + scene.GetType().Name + "] " + p.propertyPath + ": prefab " + right + " -> scene " + left);
        }
    }
    static void Compare(Transform scene, Transform prefab, Transform sceneRoot, Transform prefabRoot, StringBuilder report)
    {
        string where = PathOf(scene, sceneRoot); if (where == "") where = "(root)";
        if (scene.gameObject.activeSelf != prefab.gameObject.activeSelf) report.AppendLine("  ACTIVE " + where + ": prefab " + prefab.gameObject.activeSelf + " -> scene " + scene.gameObject.activeSelf);
        if (scene != sceneRoot && scene.GetSiblingIndex() != prefab.GetSiblingIndex()) report.AppendLine("  ORDER " + where + ": prefab index " + prefab.GetSiblingIndex() + " -> scene " + scene.GetSiblingIndex());
        var sceneComponents = scene.GetComponents<Component>(); var prefabComponents = prefab.GetComponents<Component>().ToList();
        foreach (var component in sceneComponents)
        {
            if (component == null) { report.AppendLine("  MISSING-SCRIPT " + where); continue; }
            int index = sceneComponents.Where(c => c != null && c.GetType() == component.GetType()).ToList().IndexOf(component);
            var match = prefabComponents.Where(c => c != null && c.GetType() == component.GetType()).ElementAtOrDefault(index);
            if (match == null) { report.AppendLine("  ADDED-COMPONENT " + where + " [" + component.GetType().Name + "]"); continue; }
            CompareComponent(component, match, sceneRoot, prefabRoot, where, report);
        }
        foreach (var component in prefabComponents)
            if (component != null && !sceneComponents.Any(c => c != null && c.GetType() == component.GetType()))
                report.AppendLine("  REMOVED-COMPONENT " + where + " [" + component.GetType().Name + "]");
        var prefabChildren = prefab.Cast<Transform>().ToList();
        foreach (Transform child in scene)
        {
            int occurrence = scene.Cast<Transform>().Where(c => c.name == child.name).ToList().IndexOf(child);
            var match = prefabChildren.Where(c => c.name == child.name).ElementAtOrDefault(occurrence);
            if (match == null) report.AppendLine("  ADDED-OBJECT " + PathOf(child, sceneRoot) + " (" + string.Join(",", child.GetComponents<Component>().Select(c => c == null ? "?" : c.GetType().Name)) + ")");
            else Compare(child, match, sceneRoot, prefabRoot, report);
        }
        foreach (var child in prefabChildren)
            if (!scene.Cast<Transform>().Any(c => c.name == child.name)) report.AppendLine("  REMOVED-OBJECT " + PathOf(child, prefabRoot));
    }

    const string CanvasPath = "Assets/Prefabs/UI/GameCanvas.prefab";
    static Transform Need(Transform root, string path) => root.Find(path) ?? throw new Exception("Missing " + root.name + "/" + path);
    // Scene-only objects become added objects under the matching parent of the new instance.
    static void Carry(Transform oldRoot, string oldPath, Transform newRoot, string newParent, StringBuilder log)
    {
        var moved = Need(oldRoot, oldPath); int index = moved.GetSiblingIndex();
        moved.SetParent(Need(newRoot, newParent), false); moved.SetSiblingIndex(index);
        log.AppendLine("Kept scene object " + oldPath + " under " + newParent + " at index " + index);
    }
    public static void Relink()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var old = FindRoot(UiName); var log = new StringBuilder();
            if (PrefabUtility.IsPartOfPrefabInstance(old)) throw new Exception(UiName + " is already a prefab instance.");
            var ui = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath), scene);
            ui.name = UiName; ui.transform.SetSiblingIndex(old.transform.GetSiblingIndex());
            Transform o = old.transform, n = ui.transform;

            Carry(o, "Device safe area/Portrait frame/Target arena/Arena scenery/Panel", n, "Device safe area/Portrait frame/HUD/Arena/Target arena/Arena scenery", log);
            Carry(o, "Device safe area/Portrait frame/Level briefing/Level briefing card/Panel", n, "Device safe area/Portrait frame/Screens/Level briefing/Level briefing card", log);
            var card = Need(n, "Device safe area/Portrait frame/Screens/Level select/Level select card").GetComponent<UnityEngine.UI.Image>();
            card.color = Need(o, "Device safe area/Portrait frame/Level select/Level select card").GetComponent<UnityEngine.UI.Image>().color;
            PrefabUtility.RecordPrefabInstancePropertyModifications(card); log.AppendLine("Kept Level select card tint " + card.color);
            var scroll = Need(n, "Device safe area/Portrait frame/Screens/Level select/Level select card/Level list/Level list viewport").GetComponent<UnityEngine.UI.ScrollRect>();
            scroll.movementType = Need(o, "Device safe area/Portrait frame/Level select/Level select card/Level list viewport").GetComponent<UnityEngine.UI.ScrollRect>().movementType;
            PrefabUtility.RecordPrefabInstancePropertyModifications(scroll); log.AppendLine("Kept level list movement " + scroll.movementType);

            // Rebind references from outside the UI: root components map to the new root's component of the same type.
            foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null || component.transform.IsChildOf(o) || component.transform.IsChildOf(n)) continue;
                    var data = new SerializedObject(component); var p = data.GetIterator(); bool changed = false;
                    while (p.Next(true))
                    {
                        if (p.propertyType != SerializedPropertyType.ObjectReference || p.name == "m_GameObject" || !(p.objectReferenceValue is Component target) || !target.transform.IsChildOf(o)) continue;
                        if (target.transform != o) throw new Exception("Unmapped scene reference " + component.name + "." + p.propertyPath + " -> " + Describe(target, o));
                        p.objectReferenceValue = ui.GetComponent(target.GetType()); changed = true;
                        log.AppendLine("Rebound " + component.name + " [" + component.GetType().Name + "] " + p.propertyPath + " to the new " + target.GetType().Name);
                    }
                    if (changed) data.ApplyModifiedPropertiesWithoutUndo();
                }
            Object.DestroyImmediate(old);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText("scene-canvas-relink.txt", "PASS " + UiName + " is a GameCanvas prefab instance again.\n" + log);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("scene-canvas-relink.txt", "FAIL " + e); EditorApplication.Exit(1); }
    }

    // Unpack: bakes the linked instance into plain scene objects (the designer wants to edit the
    // scene directly). Scene-only tweaks made for the previous look are dropped first: added
    // objects and property overrides, except the root's name and transform.
    public static void Unpack()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var ui = FindRoot(UiName); var log = new StringBuilder();
            if (!PrefabUtility.IsPartOfPrefabInstance(ui)) throw new Exception(UiName + " is not a prefab instance.");
            foreach (var added in PrefabUtility.GetAddedGameObjects(ui))
            { log.AppendLine("Removed scene-only object " + PathOf(added.instanceGameObject.transform, ui.transform)); Object.DestroyImmediate(added.instanceGameObject); }
            foreach (var changed in PrefabUtility.GetObjectOverrides(ui))
            {
                var target = changed.instanceObject;
                var t = target is Component c ? c.transform : target is GameObject g ? g.transform : null;
                if (t == ui.transform) continue; // Keep the root's name, canvas and transform.
                log.AppendLine("Reverted override on " + PathOf(t, ui.transform) + " [" + target.GetType().Name + "]");
                changed.Revert(InteractionMode.AutomatedAction);
            }
            PrefabUtility.UnpackPrefabInstance(ui, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText("scene-canvas-unpack.txt", "PASS " + UiName + " unpacked into the scene.\n" + log);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("scene-canvas-unpack.txt", "FAIL " + e); EditorApplication.Exit(1); }
    }

    public static void Report()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath);
            var ui = FindRoot(UiName);
            var flat = PrefabUtility.LoadPrefabContents(FlatPath);
            var report = new StringBuilder("Scene '" + UiName + "' vs " + FlatPath + "\n");
            Compare(ui.transform, flat.transform, ui.transform, flat.transform, report);
            // Scene objects outside the UI that reference into it must be rebound on relink.
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null || component.transform.IsChildOf(ui.transform)) continue;
                    var p = new SerializedObject(component).GetIterator();
                    while (p.Next(true))
                        if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue != null && p.name != "m_GameObject" && p.name != "m_Father" && !p.propertyPath.StartsWith("m_Children"))
                        {
                            var value = p.objectReferenceValue; var t = value is Component c ? c.transform : value is GameObject g ? g.transform : null;
                            if (t != null && t.IsChildOf(ui.transform))
                                report.AppendLine("  EXTERNAL-REF " + component.name + " [" + component.GetType().Name + "] " + p.propertyPath + " -> " + Describe(value, ui.transform));
                        }
                }
            PrefabUtility.UnloadPrefabContents(flat);
            File.WriteAllText("scene-canvas-diff.txt", report.ToString());
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("scene-canvas-diff.txt", "FAIL " + e); EditorApplication.Exit(1); }
    }
}
