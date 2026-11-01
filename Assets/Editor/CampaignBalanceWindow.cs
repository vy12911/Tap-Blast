using System;
using System.IO;
using TapBlast;
using UnityEditor;
using UnityEngine;

public sealed class CampaignBalanceWindow : EditorWindow
{
    [Serializable] sealed class Batch { public CampaignTelemetry.Entry[] events; public int frameP95ms; }
    struct Row { public int attempts, wins, failures, quits, firstWins, p95, taps; public float accuracy, latency; public string reason; }
    readonly Row[] rows = new Row[21];
    Vector2 scroll;
    string path, error;
    [MenuItem("Tap Blast/Campaign balance dashboard")]
    static void Open() { GetWindow<CampaignBalanceWindow>("Campaign balance"); }
    void OnEnable() { path = Path.Combine(Application.persistentDataPath, "campaign-v3-events.jsonl"); }
    void Load()
    {
        Array.Clear(rows,0,rows.Length);error="";
        try
        {
            foreach(string line in File.ReadLines(path))
            {
                var batch=JsonUtility.FromJson<Batch>(line);if(batch==null||batch.events==null)continue;
                int level=0,attempt=0;
                foreach(var e in batch.events) if(e.name=="level_attempt"){level=e.level;attempt=e.value;break;}
                if(level<1||level>20)continue;var row=rows[level];row.p95=Math.Max(row.p95,batch.frameP95ms);
                foreach(var e in batch.events)
                {
                    if(e.name=="level_attempt")row.attempts++;
                    if(e.name=="level_complete"){row.wins++;if(attempt==1)row.firstWins++;}
                    if(e.name=="level_fail"){row.failures++;row.reason=e.reason;}
                    if(e.name=="level_quit")row.quits++;
                    if(e.name=="direct_accuracy_percent")row.accuracy+=e.value;
                    if(e.name=="pointer_snapshot"){row.taps++;row.latency+=e.latencyMs;}
                }
                rows[level]=row;
            }
        }
        catch(Exception ex){error=ex.Message;}
    }
    void OnGUI()
    {
        EditorGUILayout.LabelField("Local playtest telemetry",EditorStyles.boldLabel);
        path=EditorGUILayout.TextField("JSONL file",path);
        if(GUILayout.Button("Refresh local report"))Load();
        if(!string.IsNullOrEmpty(error))EditorGUILayout.HelpBox(error,MessageType.Info);
        EditorGUILayout.LabelField("Level   Attempts   Wins   Failed   Quit   First-win   Accuracy   Input ms   P95 ms");
        scroll=EditorGUILayout.BeginScrollView(scroll);
        for(int i=1;i<=20;i++)
        {
            var r=rows[i];int finished=r.wins+r.failures;
            EditorGUILayout.LabelField($"{i:00}       {r.attempts}          {r.wins}        {r.failures}       {r.quits}        {r.firstWins}          {(finished>0?r.accuracy/finished:0):0}%        {(r.taps>0?r.latency/r.taps:0):0.0}        {r.p95}");
            if(!string.IsNullOrEmpty(r.reason))EditorGUILayout.LabelField("    Last failure: "+r.reason,EditorStyles.miniLabel);
        }
        EditorGUILayout.EndScrollView();
    }
}
