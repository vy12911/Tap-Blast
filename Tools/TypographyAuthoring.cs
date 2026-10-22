// Editor-only migration. Every font and material assignment is saved in the prefabs.
using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class TypographyAuthoring
{
    const string Folder = "Assets/Fonts/RobotoCondensed/";
    static TMP_FontAsset black, bold, body;
    static Material title;

    static TMP_FontAsset Font(string weight)
    {
        string path = Folder + "RobotoCondensed-" + weight + " SDF.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (asset != null) return asset;
        var source = AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(Folder + "RobotoCondensed-" + weight + ".ttf");
        if (source == null) throw new Exception("Missing supplied font: " + weight);
        asset = TMP_FontAsset.CreateFontAsset(source, 100, 14, GlyphRenderMode.SDFAA, 2048, 2048);
        asset.name = "RobotoCondensed-" + weight + " SDF";
        var characters = new StringBuilder();
        for (int c = 32; c <= 126; c++) characters.Append((char)c);
        characters.Append("\u00a0\u00d7\u2013\u2014\u2018\u2019\u201c\u201d\u2026");
        if (!asset.TryAddCharacters(characters.ToString(), out string missing))
            throw new Exception("Roboto Condensed is missing required UI glyphs: " + missing);
        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(asset, path);
        foreach (var atlas in asset.atlasTextures) { atlas.filterMode = FilterMode.Bilinear; AssetDatabase.AddObjectToAsset(atlas, asset); }
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    static void Prepare()
    {
        if (bold != null) return;
        black = Font("Black"); bold = Font("Bold"); body = Font("SemiBold");
        title = AssetDatabase.LoadAssetAtPath<Material>(Folder + "RobotoCondensed-Title.mat");
        if (title == null) { title = new Material(black.material); AssetDatabase.CreateAsset(title, Folder + "RobotoCondensed-Title.mat"); }
        title.name = "Roboto Condensed Title";
        title.CopyPropertiesFromMaterial(black.material);
        title.SetFloat("_OutlineWidth", .055f);
        title.SetColor("_OutlineColor", new Color32(0, 70, 139, 255));
        title.SetFloat("_FaceDilate", 0);
        title.EnableKeyword("OUTLINE_ON"); title.EnableKeyword("UNDERLAY_ON");
        title.SetColor("_UnderlayColor", new Color32(0, 28, 82, 255));
        title.SetFloat("_UnderlayOffsetY", -.45f);
        title.SetFloat("_UnderlayDilate", .07f);
        EditorUtility.SetDirty(title);
    }

    public static void Apply(GameObject root)
    {
        Prepare();
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            string path = AnimationUtility.CalculateTransformPath(label.transform, root.transform);
            bool outlined = label.fontSharedMaterial != null && label.fontSharedMaterial.name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0;
            bool numeric = path.EndsWith("/Level number") || path.EndsWith("/Value") || path.EndsWith("/Combo value") || path.EndsWith("/Multiplier");
            bool instruction = label.name == "Description" || label.name == "Detail" || label.name == "Subhint" || label.name == "Wallet details" || label.name.Contains("requirement") || label.name == "Star rules";
            label.font = outlined || numeric ? black : instruction ? body : bold;
            label.fontSharedMaterial = outlined ? title : label.font.material;
            label.fontStyle = FontStyles.Normal;
            label.fontWeight = FontWeight.Regular; // Actual static weight, no synthetic bold.
            label.characterSpacing = 0;
            label.wordSpacing = 0;
            label.enableKerning = true;
            label.extraPadding = outlined;
            // Retain the measured screen hierarchy and role sizes; narrower glyphs
            // gain space without allowing oversized one-word labels.
            label.fontSize = label.fontSizeMax;
            label.enableAutoSizing = true;
            label.ForceMeshUpdate(true);
            EditorUtility.SetDirty(label);
        }
    }

    public static void Run()
    {
        try
        {
            AssetDatabase.Refresh(); Prepare();
            int labels = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                labels += root.GetComponentsInChildren<TMP_Text>(true).Length;
                Apply(root);
                PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
            }
            var settings = new SerializedObject(AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset"));
            settings.FindProperty("m_defaultFontAsset").objectReferenceValue = bold;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            File.WriteAllText("typography-authoring-results.txt", "PASS Roboto Condensed assigned to " + labels + " saved labels and the TMP default.\n");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("typography-authoring-results.txt", "FAIL " + e); EditorApplication.Exit(1); }
    }
}
