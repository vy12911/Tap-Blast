using System;
using System.Collections.Generic;
using TapBlast;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CampaignV5Authoring
{
    [Serializable] sealed class Data
    {
        public int designVersion = 5;
        public ScriptedWave[] waves;
        public BossPhaseDefinition[] bossPhases = new BossPhaseDefinition[0];
        public string displayName, mechanicHint, scriptedGoal;
        public bool isBossLevel;
    }
    static SpawnCue Optional(float at, TargetKind kind, int count = 1)
        => new SpawnCue { at = at, kind = kind, count = count, life = 3.8f, size = 156,
            required = false, position = kind == TargetKind.Bomb ? new Vector2(.82f, .2f) : Vector2.zero };
    static void Add(ScriptedWave wave, SpawnCue cue)
    { var list = new List<SpawnCue>(wave.schedule); list.Add(cue); list.Sort((a,b) => a.at.CompareTo(b.at)); wave.schedule = list.ToArray(); }
    static void Folder(string parent, string name) { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }
    [MenuItem("Tap Blast/Author GDD v5 levels 1-20")]
    public static void Author()
    {
        CampaignV3Authoring.Author();
        Folder("Assets/Data", "Bosses");
        for (int id = 1; id <= 20; id++)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Data/Levels/Level{id:00}.asset");
            var data = new Data { waves = level.Waves, displayName = level.DisplayName, mechanicHint = level.MechanicHint, scriptedGoal = level.Goal, isBossLevel = id % 10 == 0 };
            if (id == 8)
            {
                var a = level.Waves[0]; a.rules.duration = 9; a.schedule = new[] {
                    new SpawnCue { at = .65f, kind = TargetKind.Priority, critical = true, life = 3, size = 156 },
                    new SpawnCue { at = 4.1f, kind = TargetKind.Priority, critical = true, life = 3, size = 156, position = new Vector2(-.5f,.3f) } };
                level.Waves[1].rules.duration = 22.5f; level.Waves[2].rules.duration = 13.5f;
            }
            if (id == 3) foreach (var cue in level.Waves[2].schedule) cue.moving = true;
            if (id == 6) Add(level.Waves[0], Optional(4, TargetKind.Conduit, 3));
            if (id == 16) Add(level.Waves[1], Optional(16.5f, TargetKind.Conduit, 3));
            if (id == 12) foreach (var wave in level.Waves) foreach (var cue in wave.schedule)
                if (cue.kind == TargetKind.Chain) cue.kind = TargetKind.Conduit;
            if (id == 4 || id == 14)
            {
                // A visible three-target choice: mandatory small/pair, optional gold.
                var wave = level.Waves[0];
                wave.schedule[0].count = id == 4 ? 2 : 2;
                Add(wave, Optional(.7f, TargetKind.Golden));
            }
            if (id == 7)
            { Add(level.Waves[1], Optional(0, TargetKind.Bomb)); Add(level.Waves[2], Optional(0, TargetKind.Bomb)); }
            if (id == 10) Add(level.Waves[3], Optional(0, TargetKind.Bomb));
            if (id == 9 || id == 19)
            {
                var wave = level.Waves[2];
                wave.rules.priorityOrder = 2;
                Add(wave, new SpawnCue { at = 8.5f, kind = TargetKind.Priority, order = 1, critical = true, life = 2.7f, size = 156, position = new Vector2(-.6f,.4f) });
                Add(wave, new SpawnCue { at = 8.6f, kind = TargetKind.Priority, order = 2, critical = true, life = 2.7f, size = 156, position = new Vector2(.6f,-.4f) });
                Add(wave, new SpawnCue { at = 11.2f, kind = TargetKind.Priority, order = 1, critical = true, life = 1.8f, size = 156, position = new Vector2(-.6f,.4f) });
                Add(wave, new SpawnCue { at = 11.3f, kind = TargetKind.Priority, order = 2, critical = true, life = 1.8f, size = 156, position = new Vector2(.6f,-.4f) });
                Add(wave, Optional(5, TargetKind.Multiplier));
                data.scriptedGoal = id == 9 ? "Hit 15 in a row, then tap !1 followed by !2" : "Make 2 Megas, 1 pair, then tap !1 followed by !2";
                data.mechanicHint = id == 9 ? "Keep your streak, then tap !1 before !2. Blasts cannot clear the order." : "Make 8-target Megas, then tap !1 before !2. Tap pairs 1 then 2.";
            }
            int theme = (id - 1) % 10;
            for (int w = 0; w < level.Waves.Length; w++)
            {
                var wave = level.Waves[w];
                var kind = theme == 0 ? ArenaEventKind.SafeOpening : theme == 1 ? ArenaEventKind.TwoLaneRhythm : theme == 5 ? ArenaEventKind.ConduitPreview : theme == 8 ? ArenaEventKind.PriorityFinale : theme == 6 || id == 17 ? ArenaEventKind.SafeCorridor : theme == 3 ? ArenaEventKind.PulsePreview : theme == 7 ? ArenaEventKind.TempoFakeout : ArenaEventKind.LanePreview;
                wave.encounters = new[] { new EncounterEvent { id = $"L{id:00}-P{w+1}-encounter", kind = kind, at = 0, active = kind == ArenaEventKind.SafeCorridor ? wave.rules.duration - .45f : 2 } };
                bool corridor = false;
                foreach (var cue in wave.schedule) if (cue.kind == TargetKind.Bomb) corridor = true;
                for (int n = 0; n < wave.schedule.Length; n++)
                {
                    var cue = wave.schedule[n]; cue.id = $"L{id:00}-W{w+1}-S{n+1:00}"; cue.gateId = cue.required ? $"L{id:00}-W{w+1}-gate" : "";
                    cue.telegraph = .45f; cue.transit = .22f; cue.cadence = 1.0f + (id % 4) * .2f;
                    cue.laneId = n % 2;
                    if ((id == 1 || id == 2) && cue.count == 1) cue.position = new Vector2((n % 2 == 0 ? -.55f : .55f), ((n / 2) % 3 - 1) * .45f);
                    if (id == 1 && w == 0 && n == 0) cue.position = Vector2.zero;
                    if (corridor)
                    {
                        if (cue.kind == TargetKind.Bomb) { cue.position = new Vector2(.82f,.25f); cue.moving = false; }
                        else if (cue.count == 1) cue.position.x = -.5f;
                    }
                }
                if (corridor) wave.encounters[0].kind = ArenaEventKind.SafeCorridor;
            }
            if (id == 10 || id == 20)
            {
                data.bossPhases = new BossPhaseDefinition[5];
                for (int p = 0; p < 5; p++)
                {
                    string name = (id == 10 ? "Sentinel" : "Architect") + "Phase" + (p + 1).ToString("00");
                    string path = "Assets/Data/Bosses/" + name + ".asset";
                    var phase = AssetDatabase.LoadAssetAtPath<BossPhaseDefinition>(path);
                    if (phase == null) { phase = ScriptableObject.CreateInstance<BossPhaseDefinition>(); AssetDatabase.CreateAsset(phase, path); }
                    phase.phaseId = name; phase.signature = id == 10 ? BossSignature.Sentinel : BossSignature.Architect;
                    phase.safeTeaching = p == 0; phase.coreHits = 1;
                    phase.corePosition = new Vector2(0, .95f);
                    phase.teachingHint = id == 10 ? "Clear the phase gate. Tap the core when the rotating shield opens." : "Route the chains and numbered pairs to expose the gold core.";
                    phase.warning = .45f; phase.exposedSeconds = 1.6f; phase.shieldSeconds = 1.2f;
                    var rules = level.Waves[p].rules; rules.coreHits = phase.coreHits; rules.coreWarning = phase.warning; rules.coreOpen = phase.exposedSeconds; rules.coreClosed = phase.shieldSeconds;
                    data.bossPhases[p] = phase; EditorUtility.SetDirty(phase);
                }
                data.displayName = id == 10 ? "Sentinel" : "Architect";
                data.mechanicHint = "Clear each phase gate, then tap the gold core while its shield is open.";
                data.scriptedGoal = id == 10 ? "Clear 5 Sentinel phases and direct-hit each exposed core" : "Make 3 chains, 2 pairs and direct-hit all 5 Architect cores";
            }
            // Apply scalar/reference edits without overwriting the nested schedule edits above.
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(data), level); EditorUtility.SetDirty(level);
            CampaignV5Validator.Validate(level);
        }
        AuthorTarget();
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects()) if (root.name == "Tap Blast UI") AuthorUI(root);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        const string uiPath = "Assets/Prefabs/UI/GameCanvas.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(uiPath); AuthorUI(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, uiPath); PrefabUtility.UnloadPrefabContents(prefab);
        AssetDatabase.SaveAssets(); Debug.Log("V5 AUTHOR PASS: 20 levels, 10 boss phases, scene and prefabs saved.");
    }
    static void AuthorUI(GameObject root)
    {
        CampaignV3Authoring.AuthorUI(root);
        var responsive = root.GetComponent<ResponsiveCanvas>();
        if (responsive != null)
        {
            // The component default remains off; this v5 campaign explicitly opts in.
            var so = new SerializedObject(responsive); so.FindProperty("respectSafeArea").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(responsive);
        }
    }
    static void AuthorTarget()
    {
        const string path = "Assets/Prefabs/Targets/NormalTarget.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var view = root.GetComponent<TargetView>(); var so = new SerializedObject(view);
        var visual = root.transform.Find("Visuals") as RectTransform;
        if (visual == null)
        {
            visual = new GameObject("Visuals", typeof(RectTransform)).GetComponent<RectTransform>();
            visual.SetParent(root.transform,false); visual.anchorMin=Vector2.zero;visual.anchorMax=Vector2.one;visual.offsetMin=visual.offsetMax=Vector2.zero;
            var source = root.GetComponent<Image>();
            var body = new GameObject("Target body",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            body.transform.SetParent(visual,false);EditorUtility.CopySerialized(source,body);body.raycastTarget=false;
            body.rectTransform.anchorMin=Vector2.zero;body.rectTransform.anchorMax=Vector2.one;body.rectTransform.offsetMin=body.rectTransform.offsetMax=Vector2.zero;
            for (int i=root.transform.childCount-1;i>=0;i--)
            {var child=root.transform.GetChild(i);if(child!=visual)child.SetParent(visual,false);}
            body.transform.SetAsFirstSibling();source.color=Color.clear;source.sprite=null;source.raycastTarget=true;source.preserveAspect=true;
            so.FindProperty("hitImage").objectReferenceValue=body;
        }
        so.FindProperty("visualRoot").objectReferenceValue=visual;so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
    }
}
