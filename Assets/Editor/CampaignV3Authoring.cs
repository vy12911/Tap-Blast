using System;
using System.Collections.Generic;
using TapBlast;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CampaignV3Authoring
{
    [Serializable] private sealed class Data
    {
        public bool scripted = true, firstTapTutorial = false, isBossLevel = false, movingTargets = false, noBombs;
        public string displayName, scriptedGoal, mechanicHint;
        public int seed, pressureLimit, priorityMissLimit = 99, maxTargets = 8, shieldCharges, requiredTaps = 20, twoStarScore = 1000;
        public float duration = 45, targetLifetime = 3.8f, spawnInterval = 1.4f, goldenChance, timeChance, bombChance;
        public ScriptedWave[] waves;
    }
    private static SpawnCue Cue(float at, TargetKind kind, int count = 1, bool moving = false, int index = 0, bool required = true)
    {
        return new SpawnCue { at = at, kind = kind, count = count, moving = moving, required = required,
            critical = kind == TargetKind.Priority, life = kind == TargetKind.Priority ? 2.7f : 3.8f,
            size = kind == TargetKind.Small ? 132 : 156,
            color = index % 3, position = count > 1 ? Vector2.zero : new Vector2((index % 3 - 1) * .62f, ((index / 3) % 3 - 1) * .55f) };
    }
    private static ScriptedWave Make(string title, float seconds, GateKind gate, int quota, TargetKind kind, int opportunities, float interval, bool moving = false, int group = 1, int pairs = 0)
    {
        var cues = new List<SpawnCue>();
        for (int i = 0; i < opportunities; i++) cues.Add(Cue(.65f + i * interval, kind, group, moving, i));
        return new ScriptedWave { rules = new WaveRules { title = title, duration = seconds, gate = gate, quota = quota, pairs = pairs },
            maxActive = group >= 8 ? 8 : 4, schedule = cues.ToArray() };
    }
    private static void Add(ScriptedWave wave, SpawnCue cue)
    {
        var list = new List<SpawnCue>(wave.schedule); list.Add(cue); list.Sort((a,b) => a.at.CompareTo(b.at)); wave.schedule = list.ToArray();
    }
    public static void Author()
    {
        for (int id = 1; id <= 20; id++)
        {
            var config = AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Data/Levels/Level{id:00}.asset");
            if (config == null) throw new Exception("Missing level " + id);
            string[] names = { "First Spark", "Quick Fingers", "Side Step", "Pulse Check", "New Twist", "Combo Craft", "Danger Read", "Beat Shift", "Skill Trial", "World Boss" };
            var d = new Data { displayName = names[(id - 1) % 10], seed = 30000 + id * 97, pressureLimit = id <= 10 ? 4 : 3, shieldCharges = id >= 17 ? 1 : 0 };
            var a = Make("WARM UP · TAP 4", 9, GateKind.Taps, 4, TargetKind.Normal, 5, 1.4f);
            var b = Make("DIRECT HITS · TAP 10", 22.5f, GateKind.Taps, 10, TargetKind.Normal, 12, 1.5f);
            var c = Make("FINAL WAVE · TAP 6", 13.5f, GateKind.Taps, 6, TargetKind.Normal, 7, 1.5f);
            d.scriptedGoal = "Tap 20 targets across 3 waves"; d.mechanicHint = "Tap each wave's targets before its time ends. Watch the wave counter.";
            switch (id)
            {
                case 2: case 4:
                    int n = id == 2 ? 8 : 12;
                    a = Make("SMALL CIRCLES",9,GateKind.Small,2,TargetKind.Small,4,1.8f);
                    b = Make("SMALL TARGET TEST",22.5f,GateKind.Small,n-4,TargetKind.Small,n,1.6f);
                    c = Make("PRECISION FINISH",13.5f,GateKind.Small,2,TargetKind.Small,5,1.8f);
                    d.scriptedGoal = "Direct-tap " + n + " Small targets"; d.mechanicHint = "Tap the small circles. Blasts cannot clear this direct-tap gate."; break;
                case 3: case 9: case 15:
                    int combo = id == 3 ? 5 : id == 9 ? 15 : 20;
                    a = Make("BUILD A RHYTHM",9,GateKind.Taps,4,TargetKind.Normal,5,1.3f);
                    b = Make("HIT " + combo + " IN A ROW",22.5f,GateKind.Combo,combo,TargetKind.Normal,combo+2,combo >= 20 ? .9f : 1.15f,id >= 9);
                    c = Make("KEEP YOUR FOCUS",13.5f,GateKind.Taps,4,TargetKind.Normal,7,1.5f,id >= 9);
                    d.scriptedGoal = "Clear 3 waves and hit " + combo + " in a row"; d.mechanicHint = "Tap every circle to keep your streak. A missed circle resets it."; break;
                case 5:
                    a=Make("MOVEMENT PREVIEW",9,GateKind.Moving,2,TargetKind.Small,4,1.7f,true);
                    b=Make("MOVING TARGET TEST",22.5f,GateKind.Moving,6,TargetKind.Small,9,1.8f,true);
                    c=Make("MOVING FINISH",13.5f,GateKind.Moving,2,TargetKind.Small,5,1.8f,true);
                    d.scriptedGoal="Direct-tap 10 moving targets"; d.mechanicHint="Lead the moving circles and tap inside their rings."; break;
                case 6:
                    a=Make("GOLDEN PREVIEW",9,GateKind.Golden,1,TargetKind.Golden,2,3.8f);
                    b=Make("COLLECT GOLD",22.5f,GateKind.Golden,1,TargetKind.Golden,3,5.5f);
                    c=Make("GOLDEN FINISH",13.5f,GateKind.Golden,1,TargetKind.Golden,2,4.5f);
                    d.scriptedGoal="Collect 3 Golden targets across 3 waves"; d.mechanicHint="Tap GOLD circles first. Optional circles build score between them.";
                    Add(b,Cue(3,TargetKind.Normal)); Add(b,Cue(9,TargetKind.Normal)); break;
                case 7:
                    a=Make("RUSH WARM UP",9,GateKind.Taps,3,TargetKind.Normal,5,1.4f,true);
                    b=Make("RUSH · 15 DIRECT HITS",22.5f,GateKind.Taps,15,TargetKind.Normal,20,1.0f,true);
                    c=Make("ACCURATE CLEANUP",13.5f,GateKind.Taps,3,TargetKind.Normal,6,1.7f,true);
                    d.scriptedGoal="Make 15 direct hits in the rush wave"; d.mechanicHint="Keep tapping moving circles during the rush. Clear all three waves."; break;
                case 8:
                    a=Make("PRIORITY · TAP !",10,GateKind.Priority,1,TargetKind.Priority,3,3.3f);
                    foreach(var cue in a.schedule) cue.life=3;
                    b=Make("PRIORITY TEST",20,GateKind.Priority,3,TargetKind.Priority,5,3.7f);
                    c=Make("PRIORITY FINISH",15,GateKind.Priority,1,TargetKind.Priority,2,5.5f);
                    Add(c,Cue(3.5f,TargetKind.Normal,1,true,1,false)); Add(c,Cue(10,TargetKind.Normal,1,true,2,false));
                    d.priorityMissLimit=2; d.scriptedGoal="Direct-tap 5 Priority targets; miss at most 2"; d.mechanicHint="Tap ! targets first. A missed ! adds pressure; three misses fail."; break;
                case 11: case 12: case 18:
                    a=Make("CHAIN INTRO",9,GateKind.Chains,1,TargetKind.Chain,2,4.5f,false,3);
                    b=Make("LINKED CHAIN TEST",22.5f,GateKind.Chains,1,TargetKind.Chain,3,6,false,3);
                    c=id==11 ? Make("DIRECT FINISH",13.5f,GateKind.Taps,3,TargetKind.Normal,5,1.8f) : Make("CHAIN FINISH",13.5f,GateKind.Chains,1,TargetKind.Chain,2,5,false,3);
                    if(id==18) { b.rules.gate=GateKind.ChainPairs; b.rules.pairs=1; b.schedule=new[]{Cue(1,TargetKind.Chain,3),Cue(6,TargetKind.LinkedPair,2),Cue(12,TargetKind.Chain,3)}; Add(c,Cue(8,TargetKind.Bomb,1,true,2,false)); d.noBombs=true; }
                    d.scriptedGoal=id==11 ? "Make 2 chains and clear the final wave" : id==18 ? "Make 3 chains and 1 pair with zero Bomb hits" : "Make 3 chains across 3 waves";
                    d.mechanicHint="Tap a linked group of 3+ matching shapes. Bombs stop chain links."; break;
                case 13: case 19:
                    a=id==19 ? Make("PAIR WARM UP",9,GateKind.Pairs,1,TargetKind.LinkedPair,2,4.5f,false,2) : Make("LANE WARM UP",9,GateKind.Taps,3,TargetKind.Normal,5,1.4f);
                    b=Make("MEGA · 8 LINKED CIRCLES",22.5f,GateKind.MegaChains,1,TargetKind.Chain,3,6,false,8);
                    c=id==19 ? Make("SECOND MEGA",13.5f,GateKind.MegaChains,1,TargetKind.Chain,2,5,false,8) : Make("SAFE FINISH",13.5f,GateKind.Taps,4,TargetKind.Normal,6,1.7f);
                    d.scriptedGoal=id==13?"Make 1 Mega Chain and clear every wave":"Make 2 Mega Chains and 1 ordered pair";
                    d.mechanicHint="Tap one circle when 8 matching shapes form a linked group."; break;
                case 14:
                    a=Make("PAIR · TAP 1 THEN 2",9,GateKind.Pairs,1,TargetKind.LinkedPair,2,4.5f,false,2);
                    b=Make("MOVING PAIR TEST",22.5f,GateKind.Pairs,1,TargetKind.LinkedPair,3,6,true,2);
                    c=Make("ORDERED FINISH",13.5f,GateKind.Pairs,1,TargetKind.LinkedPair,2,5,true,2);
                    d.scriptedGoal="Complete 3 numbered pairs"; d.mechanicHint="Tap 1 then 2 within 2.5 seconds. Wrong order resets only the pair."; break;
                case 16:
                    a=Make("SAFE WARM UP",9,GateKind.Taps,3,TargetKind.Normal,5,1.4f);
                    b=Make("LEAVE BOMBS ALONE",22.5f,GateKind.SafeTaps,8,TargetKind.Normal,12,1.5f,true);
                    c=Make("SAFE FINISH",13.5f,GateKind.SafeTaps,3,TargetKind.Normal,5,1.8f,true);
                    Add(b,Cue(0,TargetKind.Bomb,1,true,2,false)); Add(c,Cue(0,TargetKind.Bomb,1,true,1,false)); d.noBombs=true;
                    d.scriptedGoal="Clear all waves without tapping any Bomb";d.mechanicHint="Tap valid circles and leave X Bombs alone. One Bomb tap fails.";break;
                case 17:
                    a=Make("PAIR WARM UP",9,GateKind.Pairs,1,TargetKind.LinkedPair,2,4.5f,false,2);
                    b=Make("SHIELD · BLOCK A BOMB",22.5f,GateKind.Shield,1,TargetKind.Normal,8,2.1f);
                    Add(b,Cue(0,TargetKind.Bomb,1,true,2,false)); Add(b,Cue(6,TargetKind.Bomb,1,true,2,false)); Add(b,Cue(12,TargetKind.Bomb,1,true,2,false));
                    c=Make("ORDERED FINISH",13.5f,GateKind.Pairs,1,TargetKind.LinkedPair,2,5,false,2);
                    d.scriptedGoal="Block 1 Bomb and complete both pair waves";d.mechanicHint="Press USE SHIELD, then tap a Bomb. Pair targets need 1 then 2.";break;
            }
            if(id==1) a.schedule[0].position=Vector2.zero;
            d.waves=new[]{a,b,c};
            if(id==10 || id==20)
            {
                d.duration=60;
                if(id==10) d.waves=new[]{Make("CORE SKILL",10,GateKind.Taps,5,TargetKind.Normal,6,1.3f),Make("MOVEMENT REVIEW",12,GateKind.Moving,4,TargetKind.Small,6,1.5f,true),Make("COMBO GATE · 5 IN A ROW",15,GateKind.Combo,5,TargetKind.Normal,9,1.3f),Make("PRESSURE WAVE · TAP !",15,GateKind.Priority,3,TargetKind.Priority,4,3.3f),Make("FINAL DIRECT CHECK",8,GateKind.Priority,1,TargetKind.Priority,2,4.1f)};
                else
                {
                    var comboWave=Make("CHAIN + ORDER",15,GateKind.ChainPairs,2,TargetKind.Chain,2,4.5f,false,3,1);
                    Add(comboWave,Cue(10,TargetKind.LinkedPair,2));
                    var hazardWave=Make("PRESSURE WAVE · SAFE HITS",15,GateKind.SafeTaps,5,TargetKind.Normal,8,1.5f,true);
                    Add(hazardWave,Cue(0,TargetKind.Bomb,1,true,2,false));
                    d.waves=new[]{Make("CHAIN REVIEW",10,GateKind.Chains,1,TargetKind.Chain,2,5,false,3),Make("PAIR REVIEW",12,GateKind.Pairs,1,TargetKind.LinkedPair,2,5,false,2),comboWave,hazardWave,Make("FINAL DIRECT CHECK",8,GateKind.Priority,1,TargetKind.Priority,2,4.1f)};
                }
                d.scriptedGoal=id==10?"Clear all 5 boss phases and the final ! check":"Clear 5 phases with 3 chains, 2 pairs and the final ! check";
                d.mechanicHint="Clear the displayed phase gate. The final 8 seconds require a direct ! tap.";
            }
            if(id>=11) foreach(var phase in d.waves) Add(phase,Cue(phase.rules.duration-2.5f,TargetKind.Normal,1,false,1,false));
            // Optional Time rewards never gate a level; +6 seconds maximum is enforced in pure rules.
            if(id==8) Add(b,Cue(18,TargetKind.Time,1,false,2,false));
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(d),config); CampaignV3Validator.Validate(config); EditorUtility.SetDirty(config);
        }
        AuthorTarget();
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Level01.unity") scene=EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
        GameObject ui=null;foreach(var root in scene.GetRootGameObjects())if(root.name=="Tap Blast UI")ui=root;
        if(ui==null)throw new Exception("Saved UI root missing");
        AuthorUI(ui);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        const string prefabPath="Assets/Prefabs/UI/GameCanvas.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);AuthorUI(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);PrefabUtility.UnloadPrefabContents(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log("V3 AUTHOR PASS: 20 scripted levels, scene, Canvas template and target symbols saved.");
    }
    private static void AuthorTarget()
    {
        const string path="Assets/Prefabs/Targets/NormalTarget.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);var view=root.GetComponent<TargetView>();var so=new SerializedObject(view);
        var template=((GameObject)so.FindProperty("goldenSymbol").objectReferenceValue).GetComponent<TMP_Text>();
        var existing=root.transform.Find("Campaign symbol"); if(existing==null)existing=root.transform.Find("Visuals/Campaign symbol");
        var label=existing==null?UnityEngine.Object.Instantiate(template,root.transform):existing.GetComponent<TMP_Text>();
        label.name="Campaign symbol";label.text="!";label.fontSize=64;label.fontStyle=FontStyles.Bold;label.raycastTarget=false;
        label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
        label.alignment=TextAlignmentOptions.Center;label.gameObject.SetActive(false);
        so.FindProperty("campaignSymbol").objectReferenceValue=label;so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
    }
    private static TMP_Text Text(Transform parent,string name,TMP_Text template,Vector2 size,Vector2 position,float font)
    {
        var found=parent.Find(name);var label=found==null?UnityEngine.Object.Instantiate(template,parent):found.GetComponent<TMP_Text>();
        label.name=name;var rect=label.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=size;rect.anchoredPosition=position;
        label.fontSize=font;label.text="";label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.Normal;label.alignment=TextAlignmentOptions.Center;return label;
    }
    private static void Bind(SerializedObject so,string name,UnityEngine.Object value){so.FindProperty(name).objectReferenceValue=value;}
    public static void AuthorUI(GameObject root)
    {
        var hud=root.GetComponent<GameHUD>();var so=new SerializedObject(hud);
        var mechanics=((TMP_Text)so.FindProperty("mechanicStatus").objectReferenceValue).transform;
        var template=(TMP_Text)so.FindProperty("mechanicStatus").objectReferenceValue;
        var oldPressure=so.FindProperty("pressureStatus").objectReferenceValue as TMP_Text;if(oldPressure!=null&&oldPressure.transform.parent!=mechanics)oldPressure.transform.SetParent(mechanics,false);
        var pressure=Text(mechanics,"Pressure pips",template,new Vector2(220,36),new Vector2(130,-30),28);
        pressure.rectTransform.anchorMin=pressure.rectTransform.anchorMax=new Vector2(0,.5f);pressure.rectTransform.pivot=new Vector2(.5f,.5f); pressure.text="PRESSURE 0/4";Bind(so,"pressureStatus",pressure);
        var wave=(TMP_Text)so.FindProperty("levelLabel").objectReferenceValue;Bind(so,"waveStatus",wave);
        foreach(var label in wave.transform.parent.GetComponentsInChildren<TMP_Text>(true))if(label!=wave&&label.text=="Level")label.text="Wave";
        var shield=(Button)so.FindProperty("<Shield>k__BackingField").objectReferenceValue;
        var found=mechanics.Find("Ultimate control"); if(found==null)found=shield.transform.parent.Find("Ultimate control");var ultimate=found==null?UnityEngine.Object.Instantiate(shield,shield.transform.parent):found.GetComponent<Button>();ultimate.name="Ultimate control";
                ultimate.transform.SetParent(mechanics,false);shield.transform.SetParent(mechanics,false);
        var sr=(RectTransform)shield.transform;sr.anchorMin=sr.anchorMax=new Vector2(0,.5f);sr.pivot=new Vector2(.5f,.5f);sr.anchoredPosition=new Vector2(490,-30);sr.sizeDelta=new Vector2(210,50);
        var rect=(RectTransform)ultimate.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(810,-30);rect.sizeDelta=new Vector2(290,50);
        var ul=ultimate.GetComponentInChildren<TMP_Text>(true);ul.text="ULTIMATE 0%";ul.fontSize=23;Bind(so,"ultimateStatus",ul);Bind(so,"ultimateButton",ultimate);
        var fillObject=ultimate.transform.Find("Charge fill");Image fill;
                if(fillObject==null){var go=new GameObject("Charge fill",typeof(RectTransform),typeof(RoundedProgressImage));go.transform.SetParent(ultimate.transform,false);fill=go.GetComponent<Image>();}
        else
        {
            var rounded=fillObject.GetComponent<RoundedProgressImage>();
            if(rounded==null){var prior=fillObject.GetComponent<Image>();if(prior!=null)UnityEngine.Object.DestroyImmediate(prior);rounded=fillObject.gameObject.AddComponent<RoundedProgressImage>();}
            fill=rounded;
        }
        var fr=fill.rectTransform;fr.anchorMin=fr.anchorMax=new Vector2(.5f,0);fr.pivot=new Vector2(.5f,0);fr.sizeDelta=new Vector2(260,5);fr.anchoredPosition=new Vector2(0,4);
        fill.sprite=((Image)so.FindProperty("progressFill").objectReferenceValue).sprite;fill.preserveAspect=true;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillAmount=0;fill.color=UIArt.Gold;fill.raycastTarget=false;Bind(so,"ultimateFill",fill);
        var pipArray=so.FindProperty("pressurePips");pipArray.arraySize=4;
        for(int n=0;n<4;n++)
        {
            string pipName="Pressure pip "+n;var child=mechanics.Find(pipName);Image dot;
            if(child==null){var go=new GameObject(pipName,typeof(RectTransform),typeof(Image));go.transform.SetParent(mechanics,false);dot=go.GetComponent<Image>();}else dot=child.GetComponent<Image>();
            dot.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Modern/Circle.png");dot.preserveAspect=true;dot.raycastTarget=false;dot.color=new Color32(196,192,232,255);
            var dr=dot.rectTransform;dr.anchorMin=dr.anchorMax=new Vector2(0,.5f);dr.pivot=new Vector2(.5f,.5f);dr.sizeDelta=new Vector2(16,16);dr.anchoredPosition=new Vector2(260+n*20,-30);
            pipArray.GetArrayElementAtIndex(n).objectReferenceValue=dot;
        }
        var arena=(RectTransform)so.FindProperty("<Arena>k__BackingField").objectReferenceValue;
        var surface=arena.GetComponent<ArenaTapSurface>();if(surface==null)surface=arena.gameObject.AddComponent<ArenaTapSurface>();Bind(so,"tapSurface",surface);
        var arenaImage=arena.GetComponent<Image>();if(arenaImage==null){arenaImage=arena.gameObject.AddComponent<Image>();arenaImage.color=Color.clear;}arenaImage.raycastTarget=true;arenaImage.preserveAspect=true;
        var next=(Button)so.FindProperty("<Next>k__BackingField").objectReferenceValue;var p=next.transform.parent.Find("Practice control");
        var practice=p==null?UnityEngine.Object.Instantiate(next,next.transform.parent):p.GetComponent<Button>();practice.name="Practice control";practice.GetComponentInChildren<TMP_Text>(true).text="Practice";Bind(so,"practiceButton",practice);practice.gameObject.SetActive(false);
        Bind(so,"campaignResult",so.FindProperty("resultHint").objectReferenceValue);
        var motion=(Button)so.FindProperty("<Motion>k__BackingField").objectReferenceValue;var i=motion.transform.parent.Find("Shape cue setting");
        var icon=i==null?UnityEngine.Object.Instantiate(motion,motion.transform.parent):i.GetComponent<Button>();icon.name="Shape cue setting";
        var ir=(RectTransform)icon.transform;ir.anchoredPosition=((RectTransform)motion.transform).anchoredPosition+Vector2.down*95;
        icon.GetComponentInChildren<TMP_Text>(true).text="Shape cues     On";Bind(so,"iconModeButton",icon);
        var outcome=(TMP_Text)so.FindProperty("resultSubtitle").objectReferenceValue;outcome.enableAutoSizing=true;outcome.fontSizeMin=20;outcome.fontSizeMax=28;
        var introCaption=(TMP_Text)so.FindProperty("introWorld").objectReferenceValue;introCaption.enableAutoSizing=true;introCaption.fontSizeMin=20;introCaption.fontSizeMax=28;
        var statArray=so.FindProperty("resultStatTiles");for(int n=0;n<statArray.arraySize;n++){var label=statArray.GetArrayElementAtIndex(n).objectReferenceValue as TMP_Text;if(label!=null){label.fontSize=22;label.enableAutoSizing=true;label.fontSizeMin=19;label.fontSizeMax=22;label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,110);label.rectTransform.anchoredPosition=new Vector2(label.rectTransform.anchoredPosition.x,-27);}}
        var summary=(TMP_Text)so.FindProperty("resultHint").objectReferenceValue;summary.fontSize=23;summary.enableAutoSizing=true;summary.fontSizeMin=19;summary.fontSizeMax=23;summary.rectTransform.sizeDelta=new Vector2(summary.rectTransform.sizeDelta.x,66);
        foreach(var stat in hud.GetComponentsInChildren<TMP_Text>(true)) if(stat.name=="Stat 0"||stat.name=="Stat 1"||stat.name=="Stat 2") {stat.enableAutoSizing=true;stat.fontSizeMin=20;stat.fontSizeMax=27;}
        // Preserve the owner's unpacked root; only saved children and bindings are changed.
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(hud);
    }
}
