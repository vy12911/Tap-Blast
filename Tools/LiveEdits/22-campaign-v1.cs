// Import the restored level assets; preserve the open scene and every UI/art asset.
UnityEditor.AssetDatabase.Refresh();
for (int id = 1; id <= 20; id++)
{
    var level = UnityEditor.AssetDatabase.LoadAssetAtPath<TapBlast.LevelConfig>("Assets/Data/Levels/Level" + id.ToString("00") + ".asset");
    if (level == null || level.DesignVersion != 1 || level.Scripted || level.V5Boss)
        throw new System.InvalidOperationException("V1 campaign asset mismatch at level " + id);
}
UnityEngine.Debug.Log("Restored GDD v1 campaign: all 20 levels imported; existing UI/art preserved.");
