using System;
using TapBlast;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class CampaignV3Validator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) { ValidateAll(); }
    [MenuItem("Tap Blast/Validate v3 campaign")]
    public static void ValidateAll()
    {
        for (int id=1;id<=20;id++)
        { var level=AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Data/Levels/Level{id:00}.asset"); if(level!=null&&level.DesignVersion>=5)CampaignV5Validator.Validate(level);else Validate(level); }
        Debug.Log("V3 FEASIBILITY PASS: all 20 required schedules fit 2.5 deliberate taps/sec plus 20% reaction margin.");
    }
    public static void Validate(LevelConfig level)
    {
        if(level==null)throw new BuildFailedException("Missing v3 level asset");
        if(!level.Scripted)return;
        string context="Level "+level.LevelId+": ";
        if(level.PressureLimit<=0)throw new BuildFailedException(context+"pressure must have a positive fail limit");
        float total=0;
        foreach(var wave in level.Waves)
        {
            var r=wave.rules;total+=r.duration;
            if(r.duration<=0||r.quota<1)throw new BuildFailedException(context+"invalid wave budget/quota");
            if(wave.maxActive<1||wave.maxActive>level.MaxTargets||wave.maxActive>(r.gate==GateKind.MegaChains?8:4))throw new BuildFailedException(context+"invalid active cap");
            float previous=-1,service=0;int credits=0,pairs=0,bombs=0;
            foreach(var cue in wave.schedule)
            {
                if(cue.at<previous||cue.at<0||cue.at+(cue.kind==TargetKind.LinkedPair ? 1f : .6f)>r.duration||cue.count<1||cue.count>wave.maxActive)throw new BuildFailedException(context+"unsorted, late or impossible reservation");
                previous=cue.at;if(cue.kind==TargetKind.Bomb){bombs++;continue;}
                if(cue.kind==TargetKind.LinkedPair&&cue.count!=2)throw new BuildFailedException(context+"pair needs exactly two nodes");
                if(cue.kind==TargetKind.Priority&&(!cue.critical||cue.life<1.8f||cue.life>3.0f))throw new BuildFailedException(context+"Priority must have a visible fair critical window");
                bool pair=cue.kind==TargetKind.LinkedPair;
                bool eligible=r.gate==GateKind.Taps||r.gate==GateKind.SafeTaps||r.gate==GateKind.Combo||
                    r.gate==GateKind.Small&&cue.kind==TargetKind.Small||r.gate==GateKind.Moving&&cue.moving||r.gate==GateKind.Priority&&cue.kind==TargetKind.Priority||
                    r.gate==GateKind.Golden&&cue.kind==TargetKind.Golden||r.gate==GateKind.Pairs&&pair||
                    (r.gate==GateKind.Chains||r.gate==GateKind.ChainPairs)&&(cue.kind==TargetKind.Chain||cue.kind==TargetKind.Conduit)&&cue.count>=3||
                    r.gate==GateKind.MegaChains&&cue.kind==TargetKind.Chain&&cue.count>=8;
                if(!eligible&&!(r.gate==GateKind.ChainPairs&&pair))continue;
                // .4 sec per deliberate tap, with the specified 20% reaction margin.
                float finish=Mathf.Max(service,cue.at)+(pair ? .96f : .48f);
                if(finish>cue.at+cue.life||finish>r.duration)continue;
                service=finish;
                if(pair)pairs++;
                if(eligible)credits++;
            }
            if(r.gate==GateKind.Shield)
            {if(level.ShieldCharges<1||bombs==0)throw new BuildFailedException(context+"Shield gate lacks a charge or guaranteed Bomb");}
            else if(credits<r.quota||pairs<r.pairs)throw new BuildFailedException(context+"mandatory quota cannot be completed at 2.5 taps/sec + 20% margin");
        }
        if(Mathf.Abs(total-level.Duration)>.01f)throw new BuildFailedException(context+"wave budgets do not equal the global timer");
        if(level.LevelId%10==0&&(level.Waves.Length!=5||Mathf.Abs(level.Waves[4].rules.duration-8)>.01f||level.Waves[4].rules.gate!=GateKind.Priority))throw new BuildFailedException(context+"boss needs five phases and a final eight-second direct check");
    }
}
