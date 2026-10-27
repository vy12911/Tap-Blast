// Live edit: the Badge app icon (TAP arched over the target, BLAST under it).
// Default icon + Android legacy (rounded square), round (circle cut) and adaptive (sunburst background +
// badge foreground scaled into the 66 dp safe circle, so launcher masks never clip the name).
// Source art: Documentation/Previews/Logo/tap-blast-logo.html (#export-iconArc, -iconArcRound, -adaptiveBg, -adaptiveFg).
const string Dir = "Assets/Art/Brand/";
var sb = new System.Text.StringBuilder();
System.Func<string, UnityEngine.Texture2D> Load = name =>
{
    string path = Dir + name;
    UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    imp.textureType = UnityEditor.TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaIsTransparency = true;
    imp.mipmapEnabled = false; imp.npotScale = UnityEditor.TextureImporterNPOTScale.None; imp.maxTextureSize = 1024;
    imp.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed; imp.SaveAndReimport();
    return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
};
var icon = Load("AppIcon.png"); var round = Load("AppIcon-Round.png");
var back = Load("AppIcon-Adaptive-Background.png"); var fore = Load("AppIcon-Adaptive-Foreground.png");
if (icon == null || round == null || back == null || fore == null) return "FAIL: icon textures missing";

// Default icon (used by every platform without its own override).
UnityEditor.PlayerSettings.SetIconsForTargetGroup(UnityEditor.BuildTargetGroup.Unknown, new[] { icon });

// Android: fill every size of every icon kind.
foreach (var kind in UnityEditor.PlayerSettings.GetSupportedIconKindsForPlatform(UnityEditor.BuildTargetGroup.Android))
{
    var icons = UnityEditor.PlayerSettings.GetPlatformIcons(UnityEditor.BuildTargetGroup.Android, kind);
    string name = kind.ToString();
    foreach (var platformIcon in icons)
    {
        if (name.Contains("Adaptive")) platformIcon.SetTextures(back, fore);
        else if (name.Contains("Round")) platformIcon.SetTexture(round);
        else platformIcon.SetTexture(icon);
    }
    UnityEditor.PlayerSettings.SetPlatformIcons(UnityEditor.BuildTargetGroup.Android, kind, icons);
    sb.Append(name + " x" + icons.Length + " ");
}
UnityEditor.AssetDatabase.SaveAssets();
return "PASS app icon set: default + Android " + sb + "| product name: " + UnityEditor.PlayerSettings.productName;
