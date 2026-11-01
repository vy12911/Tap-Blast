using System;
using System.IO;
using System.Reflection;
using TapBlast;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad] public static class CampaignV3Smoke
{
    const string Key="TapBlast.V3Smoke";
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int checks;
    static double began;
    static string report;
    static CampaignV3Smoke(){if(SessionState.GetBool(Key,false)){began=EditorApplication.timeSinceStartup;EditorApplication.update+=Step;}}
    public static void Run(){Start(false);}
    public static void RunWorld1(){Start(true);}
    static void Start(bool world1)
    {
        PlayerSettings.companyName="TapBlastValidation";PlayerSettings.productName=world1?"IsolatedV3World1":"IsolatedV3Campaign20";
        PlayerPrefs.DeleteKey("TapBlast.v2.Progress");ProgressStore.Invalidate();
        SessionState.SetBool(Key+"World1",world1);SessionState.SetBool(Key,true);
        report=world1?"world1-results.txt":"campaign20-results.txt";File.WriteAllText(report,"");
        EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");EditorApplication.EnterPlaymode();
    }
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Private).GetValue(o);
    static void Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Private).Invoke(o,args);
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;File.AppendAllText(report,"PASS "+message+"\n");}
    static void Advance(GameManager game,GameHUD hud,float delta){Call(game,"TickCampaign",delta);Call(hud,"TickFeedback",delta);}
    static void Begin(GameManager game){game.BeginCountdown();typeof(GameManager).GetField("countdown",Private).SetValue(game,0f);Call(game,"AdvanceFrame");}
    static void Tap(TargetView t){t.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});}
    static void Step()
    {
        if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup-began<2)return;
        var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();var hud=UnityEngine.Object.FindFirstObjectByType<GameHUD>();
        if(game==null||hud==null||game.Session==null)return;
        bool world1=SessionState.GetBool(Key+"World1",false);report=world1?"world1-results.txt":"campaign20-results.txt";
        try
        {
            var pool=Field<TargetPool>(game,"pool");
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            Check(Field<Button>(hud,"ultimateButton")!=null&&Field<ArenaTapSurface>(hud,"tapSurface")!=null,"Ultimate and empty-tap surface are authored and wired");
            Check(Field<Image[]>(hud,"pressurePips").Length==4,"Pressure pips are authored Images, not unsupported text glyphs");
            int end=world1?10:20;
            for(int id=1;id<=end;id++)
            {
                Check(game.SelectLevel(id),"Select unlocked V3 level "+id);
                var config=game.CurrentLevel;
                CampaignV3Validator.Validate(config);
                if (config.DesignVersion >= 5) { CampaignV5Validator.Validate(config); Check(config.DesignVersion==5,"V5 authored level version "+id); }
                Check(config.Scripted&&config.Waves.Length==(id%10==0?5:3),"Level "+id+" has authored mandatory waves");
                float total=0;
                for(int w=0;w<config.Waves.Length;w++)
                {
                    var wave=config.Waves[w];total+=wave.rules.duration;
                    int credits=0,pairs=0;float last=-1;
                    foreach(var cue in wave.schedule)
                    {
                        Check(cue.at>=last&&cue.at+(cue.kind==TargetKind.LinkedPair ? 1f : .6f)<wave.rules.duration,"Feasible ordered cue in level "+id+" wave "+w);last=cue.at;
                        Check(cue.count<=wave.maxActive&&wave.maxActive<=(wave.rules.gate==GateKind.MegaChains?8:4),"Atomic reservation fits level "+id+" pool");
                        if(cue.kind==TargetKind.LinkedPair)pairs++;
                        if(wave.rules.gate==GateKind.Chains||wave.rules.gate==GateKind.ChainPairs){if((cue.kind==TargetKind.Chain||cue.kind==TargetKind.Conduit)&&cue.count>=3)credits++;}
                        else if(wave.rules.gate==GateKind.MegaChains){if(cue.kind==TargetKind.Chain&&cue.count>=8)credits++;}
                        else if(wave.rules.gate==GateKind.Priority){if(cue.kind==TargetKind.Priority)credits++;}
                        else if(wave.rules.gate==GateKind.Pairs){if(cue.kind==TargetKind.LinkedPair)credits++;}
                        else if(wave.rules.gate==GateKind.Small){if(cue.kind==TargetKind.Small)credits++;}
                        else if(wave.rules.gate==GateKind.Moving){if(cue.moving&&cue.kind!=TargetKind.Bomb)credits++;}
                        else if(wave.rules.gate==GateKind.Golden){if(cue.kind==TargetKind.Golden)credits++;}
                        else if(cue.kind!=TargetKind.Bomb)credits+=cue.count;
                    }
                    if(wave.rules.gate!=GateKind.Shield)Check(credits>=wave.rules.quota&&pairs>=wave.rules.pairs,"Required quota has sufficient authored opportunities in level "+id+" wave "+w);
                    float deliberate=wave.rules.gate==GateKind.MegaChains?1:wave.rules.gate==GateKind.Chains?wave.rules.quota:wave.rules.gate==GateKind.Pairs?wave.rules.quota*2:wave.rules.quota;
                    Check(deliberate/2.5f*1.2f<wave.rules.duration,"2.5-taps/sec plus 20% reaction margin fits level "+id+" wave "+w);
                }
                Check(Mathf.Abs(total-config.Duration)<.01f,"Level "+id+" global wave budget equals its timer");
                Begin(game);int objects=hud.GetComponentsInChildren<RectTransform>(true).Length;
                int beforeCoins=ProgressStore.Coins;float nextTap=.1f;float elapsed=0;bool shot=false,routed=false;
                for(int tick=0;tick<1700&&game.Session.State==RoundState.Playing;tick++)
                {
                    Advance(game,hud,.05f);elapsed+=.05f;if(game.Session.State!=RoundState.Playing)break;
                    if(elapsed<nextTap)continue;nextTap=elapsed+.4f;
                    TargetView best=null;
                    for(int i=0;i<pool.Capacity;i++)
                    {
                        var t=pool.At(i);if(!t.Active||t.BlastPending)continue;
                        if(t.Kind==TargetKind.Boss){if(!game.Session.CoreExposed)continue;best=t;break;}
                        if(t.Kind==TargetKind.Priority&&game.Session.Wave.priorityOrder>game.Session.WavePriorityOrder&&t.Order!=game.Session.WavePriorityOrder+1)continue;
                        if(t.Kind==TargetKind.Bomb)
                        {
                            if(game.Session.Wave.gate==GateKind.Shield&&game.Session.ShieldBlocks==0){game.ActivateShield();best=t;break;}continue;
                        }
                        if(t.Kind==TargetKind.LinkedPair&&t.Order!=(game.Session.ActivePair==t.SequenceId?2:1))continue;
                        if(best==null||t.Kind==TargetKind.Priority||t.Kind==TargetKind.LinkedPair)best=t;
                    }
                    if(best!=null)
                    {
                        if(config.V5Boss&&best.Kind==TargetKind.Boss){Check(Field<float>(hud,"progressTarget")<1,"V5 phase progress waits for the core hit");UnitySmoke.Capture(hud,"v5-boss-"+id+"-core-"+game.Session.WaveIndex);}
                        if(!routed)
                        {
                            foreach(var fade in hud.GetComponentsInChildren<GuiPanelFade>()) fade.GetComponent<CanvasGroup>().alpha=1;
                            UnitySmoke.Capture(hud,"v3-input-route-"+id);
                            Canvas.ForceUpdateCanvases();var canvas=hud.GetComponentInParent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                            var route=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,best.transform.position)};
                            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(route,hits);
                            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<TargetView>()!=best)
                            {
                                string names="";foreach(var hit in hits)names+=hit.gameObject.name+";";
                                var image=best.GetComponent<Image>();
                                foreach(var raycaster in UnityEngine.Object.FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None))
                                {
                                    var rc=raycaster.GetComponent<Canvas>();
                                    File.AppendAllText(report,"RAYCASTER "+raycaster.name+" canvas "+rc.renderMode+" camera "+(raycaster.eventCamera==null?"null":raycaster.eventCamera.name)+" registered "+GraphicRegistry.GetRaycastableGraphicsForCanvas(rc).Count+"\n");
                                }
                                File.AppendAllText(report,"TARGET canvas "+image.canvas.name+" root "+image.canvas.rootCanvas.name+" forward "+best.transform.forward+" contains "+RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform,route.position,camera)+"\n");
                                UnitySmoke.Capture(hud,"v3-input-failure");
                                throw new Exception("Raycast level "+id+" hits ["+names+"] point "+route.position+" depth "+image.depth+" culled "+image.canvasRenderer.cull+" targetFlag "+image.raycastTarget+" eligible "+best.IsRaycastLocationValid(route.position,camera)+" graphicEligible "+image.Raycast(route.position,camera)+" active "+best.Active+" interactable "+Field<bool>(best,"interactable"));
                            }
                            Check(true,"Level "+id+" visible target owns its nearest UI raycast");
                            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,route,ExecuteEvents.pointerDownHandler);routed=true;
                        }
                        else Tap(best);
                        if(!shot&&(id==8||id==13||id==14||id==20))
                        {
                            UnitySmoke.Capture(hud,"v3-level-"+id+"-wave-"+game.Session.WaveIndex);shot=true;
                        }
                    }
                }
                if(config.V5Boss&&game.Session.Won)
                {
                    Check(!Field<bool>(game,"roundFinalized"),"V5 boss waits for defeat animation before rewards");
                    Advance(game,hud,.65f);UnitySmoke.Capture(hud,"v5-boss-"+id+"-defeat-cascade");
                    Advance(game,hud,.5f);UnitySmoke.Capture(hud,"v5-boss-"+id+"-defeat-shockwave");
                    Advance(game,hud,1f);
                }
                Check(game.Session.Won,"V3 level "+id+" is solvable by direct play at 2.5 taps/sec ("+game.Session.FailureReason+")");
                Check(game.Session.ClearedWaves==config.Waves.Length&&game.Session.Stars==3,"Level "+id+" mandatory gates and mastery are attainable");
                Check(ProgressStore.Coins==beforeCoins+config.CoinReward,"Level "+id+" grants its reward exactly once");
                Call(game,"Finish");Check(ProgressStore.Coins==beforeCoins+config.CoinReward,"Repeated results cannot duplicate V3 rewards");
                Check(hud.GetComponentsInChildren<RectTransform>(true).Length==objects,"Level "+id+" does not construct runtime UI");
            }
            if(!world1 && game.CurrentLevel.DesignVersion>=5) V5Checks(game,hud,pool);
            if(world1)
            {
                ProgressStore.SelectLevel(10);
                File.AppendAllText(report,"PASS V3 World 1: all ten authored levels, boss phases, gates and rewards verified\n"+checks+" checks\n");
                SessionState.SetBool(Key,false);EditorApplication.update-=Step;EditorApplication.Exit(0);return;
            }
            game.SelectLevel(1);Begin(game);Advance(game,hud,.7f);Vector2 replayPosition=Vector2.zero;
            for(int i=0;i<pool.Capacity;i++)if(pool.At(i).Active){replayPosition=pool.At(i).Position;break;}
            Begin(game);Advance(game,hud,.7f);bool same=false;for(int i=0;i<pool.Capacity;i++)if(pool.At(i).Active){same=pool.At(i).Position==replayPosition;break;}
            Check(same,"Required spawn positions replay deterministically from the authored seed/queue");
            // Dedicated mechanics/accessibility checks after the authored campaign pass.
            Check(game.SelectLevel(14),"Ordered-pair regression selects its lesson");Begin(game);Advance(game,hud,.7f);
            TargetView first=null,second=null;
            for(int i=0;i<pool.Capacity;i++){var t=pool.At(i);if(t.Active&&t.Kind==TargetKind.LinkedPair){if(t.Order==1)first=t;else second=t;}}
            Check(first!=null&&second!=null,"Pair is a saved visible numbered target type");
            Tap(second);Check(game.Session.PairsCompleted==0&&game.Session.Pressure==0,"Wrong order gives no pair credit or World 2 pressure");
            Tap(first);game.Pause();float remaining=game.Session.PairRemaining;Advance(game,hud,0);Call(hud,"TickFeedback",.5f);Check(game.Session.PairRemaining==remaining,"Pause freezes the pair window");game.Resume();Advance(game,hud,2.6f);Check(game.Session.ActivePair<0&&game.Session.Pressure==0&&Field<TMP_Text>(hud,"hint").text=="PAIR WINDOW MISSED","Pair window expiry resets visibly without pressure");Tap(first);Advance(game,hud,1.6f);Check(second.Active,"A restarted pair retains its full 2.5-second direct window");Tap(second);Check(game.Session.PairsCompleted==1,"Ordered direct pair completes");
            game.SelectLevel(13);Begin(game);
            for(int i=0;i<3;i++)game.Session.Tap();Advance(game,hud,9);Advance(game,hud,.7f);
            var chainMembers=new TargetView[pool.Capacity];TargetView origin=null;
            for(int i=0;i<pool.Capacity;i++)if(pool.At(i).Active&&pool.At(i).Kind==TargetKind.Chain){origin=pool.At(i);break;}
            Check(origin!=null&&pool.FindChain(origin,chainMembers)==8,"Scripted Mega wave reserves eight connected targets");
            int directBefore=game.Session.SuccessfulDirectTaps;Tap(origin);
            Check(game.Session.MegaChains==1&&game.Session.SuccessfulDirectTaps==directBefore+1,"Mega credits one direct hit and one chain, not eight direct taps");
            Advance(game,hud,.1f);UnitySmoke.Capture(hud,"v3-mega-01-first-pop");
            game.Pause();float fxClock=Field<float>(hud.WorldEffects,"clock");Call(hud,"TickFeedback",.5f);
            Check(fxClock==Field<float>(hud.WorldEffects,"clock"),"Pause freezes the Mega wave");game.Resume();
            Advance(game,hud,.3f);UnitySmoke.Capture(hud,"v3-mega-02-cascade");
            Advance(game,hud,1f);UnitySmoke.Capture(hud,"v3-mega-03-central-shockwave");
            Advance(game,hud,3f);Check(hud.WorldEffects.ActiveEffectCount==0,"Mega animation drains after its last member");
            Begin(game);Check(hud.WorldEffects.ActiveEffectCount==0&&pool.ActiveCount==0,"Replay clears delayed Mega members and effects");
            for(int i=0;i<3;i++)game.Session.Tap();Advance(game,hud,9);Advance(game,hud,.7f);
            hud.Motion.onClick.Invoke();origin=null;for(int i=0;i<pool.Capacity;i++)if(pool.At(i).Active&&pool.At(i).Kind==TargetKind.Chain){origin=pool.At(i);break;}
            Tap(origin);Check(pool.ActiveCount==0&&hud.WorldEffects.ActiveEffectCount==0,"Reduced-motion Mega resolves without animated delay or particles");hud.Motion.onClick.Invoke();
            game.SelectLevel(13);Begin(game);Advance(game,hud,9); // failure without gates must not become a score pass.
            Check(!game.Session.Won,"Skipping a mandatory warm-up never passes by score");
            game.SelectLevel(1);Begin(game);int accuracy=game.Session.EmptyTaps;
            UnitySmoke.Capture(hud,"v3-empty-arena");
            var rootCanvas=hud.GetComponentInParent<Canvas>();var rootCamera=rootCanvas.renderMode==RenderMode.ScreenSpaceOverlay?null:rootCanvas.worldCamera;
            var emptyPointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(rootCamera,hud.Arena.position)};
            var emptyHits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(emptyPointer,emptyHits);
            Check(emptyHits.Count>0,"Empty arena has a visible input surface");ExecuteEvents.ExecuteHierarchy(emptyHits[0].gameObject,emptyPointer,ExecuteEvents.pointerDownHandler);
            Check(game.Session.EmptyTaps==accuracy+1,"An actual empty-arena raycast enters the accuracy denominator");
            for(int i=0;i<4;i++)game.Session.Tap();Advance(game,hud,.7f);TargetView fifth=null;for(int i=0;i<pool.Capacity;i++)if(pool.At(i).Active){fifth=pool.At(i);break;}Tap(fifth);
            bool nice=false;foreach(var popup in Field<Array>(hud,"popups"))if(((TMP_Text)popup.GetType().GetField("Label").GetValue(popup)).text=="NICE!")nice=true;
            Check(nice,"V3 direct combo milestone emits its required callout");
            game.SelectLevel(8);Begin(game);Advance(game,hud,.7f);game.ActivateUltimate();Check(game.Session.PriorityHits==0,"Uncharged Ultimate cannot satisfy Priority");
            for(int i=0;i<34;i++)game.Session.Tap();Call(game,"RefreshHUD");Check(Field<Button>(hud,"ultimateButton").interactable,"Ultimate button becomes usable at full charge");Field<Button>(hud,"ultimateButton").onClick.Invoke();
            Check(game.Session.PriorityHits==0&&game.Session.WaveProgress==0&&pool.ActiveCount>0,"Charged Ultimate leaves mandatory Priority visible and gives zero gate credit");
            TargetView priority=null;for(int i=0;i<pool.Capacity;i++)if(pool.At(i).Active&&pool.At(i).Kind==TargetKind.Priority){priority=pool.At(i);break;}
            Check(priority!=null,"Priority collection polish has a live required target");
            Advance(game,hud,.5f);var stableScale=priority.transform.localScale;Advance(game,hud,.2f);
            Check(priority.transform.localScale==stableScale,"Settled Priority keeps a stable tap boundary while its warning glow breathes");
            UnitySmoke.Capture(hud,"v3-priority-01-warning");Tap(priority);Advance(game,hud,.12f);UnitySmoke.Capture(hud,"v3-priority-02-collection");
            Check(hud.WorldEffects.ActiveEffectCount>0,"Priority collection emits its local focus burst");
            game.Session.AddPressure("TEST");game.Session.AddPressure("TEST");Call(game,"RefreshHUD");UnitySmoke.Capture(hud,"v3-pressure-warning");
            hud.SetSettings(true,true);hud.WorldEffects.Finale(Vector2.zero,UIArt.Gold);Check(hud.WorldEffects.ActiveEffectCount==0,"Reduced motion emits no Ultimate particles");hud.SetSettings(true,false);
            hud.WorldEffects.Finale(Vector2.zero,UIArt.Gold);Call(hud,"TickFeedback",3f);Check(hud.WorldEffects.ActiveEffectCount==0,"V3 feedback drains the fixed pool");
            hud.WorldEffects.Clear();hud.WorldEffects.SetPerformanceBudget(false);hud.WorldEffects.Burst(Vector2.zero,UIArt.Gold,75,TargetKind.Golden);int fullEffects=hud.WorldEffects.ActiveEffectCount;
            hud.WorldEffects.Clear();hud.WorldEffects.SetPerformanceBudget(true);hud.WorldEffects.Burst(Vector2.zero,UIArt.Gold,75,TargetKind.Golden);
            Check(hud.WorldEffects.ActiveEffectCount<fullEffects,"Slow frames trim optional extras without changing required target lifetime or gates");hud.WorldEffects.Clear();hud.WorldEffects.SetPerformanceBudget(false);
            for(int i=0;i<3;i++)ProgressStore.RecordFailure(8);int coins=ProgressStore.Coins;Field<Button>(hud,"practiceButton").onClick.Invoke();Call(game,"AdvanceFrame");
            Check(game.Session.Practice&&Field<TMP_Text>(hud,"levelChip").text=="PRACTICE","Optional Practice is explicitly marked without crowding the wave counter");game.ShowLevels();Check(ProgressStore.Coins==coins,"Abandoning Practice grants no rewards");
            ProgressStore.SelectLevel(world1?10:13);
            File.AppendAllText(report,"PASS V3 scripted campaign, skill gates, pressure, ordered input, replay and feedback verified\n"+checks+" checks\n");
            SessionState.SetBool(Key,false);EditorApplication.update-=Step;EditorApplication.Exit(0);
        }
        catch(Exception e){File.AppendAllText(report,"FAIL "+e+"\n"+checks+" checks\n");SessionState.SetBool(Key,false);EditorApplication.update-=Step;EditorApplication.Exit(1);}
    }
    public static void VerifyPersistence()
    {
        report="campaign20-persistence.txt";File.WriteAllText(report,"");
        try
        {
            PlayerSettings.companyName="TapBlastValidation";PlayerSettings.productName="IsolatedV3Campaign20";ProgressStore.Invalidate();
            int coins=0,xp=0;
            for(int id=1;id<=20;id++){var c=AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Data/Levels/Level{id:00}.asset");coins+=c.CoinReward;xp+=c.XPReward;Check(ProgressStore.GetBestStars(id)==3&&ProgressStore.GetBestScore(id)>0,"V3 progress survives process for level "+id);}
            int passes=AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/Data/Levels/Level01.asset").DesignVersion>=5?4:1;
            Check(ProgressStore.Coins==coins*passes&&ProgressStore.XP==xp*passes,"V3 wallet survives a fresh process including refresh-rate replays");
            Check(ProgressStore.SelectedLevel==13&&ProgressStore.SelectLevel(1),"V3 selected level survives and World 1 remains accessible");
            File.AppendAllText(report,"PASS V3 persistence verified\n");EditorApplication.Exit(0);
        }catch(Exception e){File.AppendAllText(report,"FAIL "+e);EditorApplication.Exit(1);}
    }
    static void V5Checks(GameManager game,GameHUD hud,TargetPool pool)
    {
        Check(game.SelectLevel(5),"V5 lane lesson selects");Begin(game);Advance(game,hud,.7f);
        TargetView lane=null;for(int i=0;i<pool.Capacity;i++)if(pool.At(i).Active&&pool.At(i).LaneShift){lane=pool.At(i);break;}
        Check(lane!=null&&lane.LaneWarning,"V5 moving target starts with visible directional preview");
        Check(lane.transform.localScale==Vector3.one,"V5 spawn animation preserves the full input region");
        var initial=lane.Position;Advance(game,hud,.3f);Check(lane.Position==initial,"V5 preview retains original hit position");
        UnitySmoke.Capture(hud,"v5-lane-warning");Advance(game,hud,.3f);Check(lane.Position!=initial,"V5 collider follows lane transit");
        UnitySmoke.Capture(hud,"v5-lane-transit");game.Pause();var frozen=lane.Position;Advance(game,hud,.5f);Check(lane.Position==frozen,"V5 pause freezes lane position");game.Resume();
        var touch=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
        try
        {
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch,new UnityEngine.InputSystem.LowLevel.TouchState { touchId=11,phase=UnityEngine.InputSystem.TouchPhase.Began,position=new Vector2(200,300) });
            UnityEngine.InputSystem.InputSystem.Update();
            var primary=new UnityEngine.InputSystem.UI.ExtendedPointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pointerType=UnityEngine.InputSystem.UI.UIPointerType.Touch,touchId=11};
            var secondary=new UnityEngine.InputSystem.UI.ExtendedPointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pointerType=UnityEngine.InputSystem.UI.UIPointerType.Touch,touchId=12};
            Check(TapInput.Accept(primary)&&!TapInput.Accept(secondary),"V5 accepts primary finger and rejects a second finger");
        }
        finally { UnityEngine.InputSystem.InputSystem.RemoveDevice(touch); }
        game.SelectLevel(6);Begin(game);Advance(game,hud,.7f);
        for(int i=0;i<pool.Capacity;i++)if(pool.At(i).Active&&pool.At(i).Kind==TargetKind.Golden)Tap(pool.At(i));
        for(int i=0;i<70;i++)Advance(game,hud,.05f);
        TargetView conduit=null;for(int i=0;i<pool.Capacity;i++)if(pool.At(i).Active&&pool.At(i).Kind==TargetKind.Conduit)conduit=pool.At(i);
        Check(conduit!=null,"V5 Golden lesson includes a safe optional conduit");
        var members=new TargetView[pool.Capacity];int linked=pool.FindChain(conduit,members);
        Check(linked==3,"V5 conduit snapshots its linked graph");int chains=game.Session.Chains;Tap(conduit);Tap(conduit);
        Check(game.Session.Chains==chains+1,"V5 conduit is single-use during cascade");
        UnitySmoke.Capture(hud,"v5-conduit-routing");Advance(game,hud,2f);Check(hud.WorldEffects.ActiveEffectCount==0,"V5 conduit effects drain");
        Check(Mathf.Abs(TargetView.ChainStepSeconds-.07f)<.0001f,"V5 chain links use 70 ms timing");
        pool.Clear();
        pool.Spawn(new Vector2(-180,0),156,10,UIArt.Cyan,false,false,TargetKind.Conduit,default,1,false,false,1,0,true);
        pool.Spawn(Vector2.zero,156,10,UIArt.Cyan,false,false,TargetKind.Chain,default,1,false,false,2,0,true);
        pool.Spawn(new Vector2(0,180),156,10,UIArt.Cyan,false,false,TargetKind.Chain,default,1,false,false,3,0,true);
        conduit=pool.At(0);Check(pool.FindChain(conduit,members)==3,"V5 conduit BFS visits a cyclic triangle once");
        pool.Spawn(new Vector2(-90,0),156,10,UIArt.Cyan,false,false,TargetKind.Priority,default,1,true,true,4,0,true);
        Check(pool.FindChain(conduit,members)==3,"V5 conduit excludes direct-only Priority targets");
        pool.Clear();
        pool.Spawn(new Vector2(-180,0),156,10,UIArt.Cyan,false,false,TargetKind.Conduit,default,1,false,false,1,0,true);
        pool.Spawn(Vector2.zero,156,10,UIArt.Cyan,false,false,TargetKind.Chain,default,1,false,false,2,0,true);
        pool.Spawn(new Vector2(180,0),156,10,UIArt.Cyan,false,false,TargetKind.Chain,default,1,false,false,3,0,true);
        pool.Spawn(new Vector2(-90,0),156,10,UIArt.Warning,false,false,TargetKind.Bomb,default,1,false,false,4,0,true);
        Check(pool.FindChain(pool.At(0),members)==0,"V5 Bomb boundary blocks conduit propagation");
        pool.Clear();hud.ClearFeedback();hud.SetSettings(true,false);hud.WorldEffects.SetPerformanceBudget(false);
        hud.WorldEffects.BossDefeat(Vector2.zero);hud.Callout("CORE DESTROYED!",UIArt.Gold);
        Check(hud.WorldEffects.ActiveEffectCount>=16,"V5 full-budget defeat emits core cracks, cascades and finale");
        Call(hud,"TickFeedback",.35f);UnitySmoke.Capture(hud,"v5-defeat-full-01-crack");
        Call(hud,"TickFeedback",.45f);UnitySmoke.Capture(hud,"v5-defeat-full-02-cascade");
        Call(hud,"TickFeedback",.35f);UnitySmoke.Capture(hud,"v5-defeat-full-03-finale");
        Call(hud,"TickFeedback",1f);Check(hud.WorldEffects.ActiveEffectCount==0,"V5 full-budget defeat drains within its result delay");
        hud.SetSettings(true,true);hud.WorldEffects.BossDefeat(Vector2.zero);Check(hud.WorldEffects.ActiveEffectCount==0,"V5 reduced-motion defeat suppresses all animated effects");hud.SetSettings(true,false);
        foreach(int hz in new[]{30,60,120})
            for(int id=1;id<=20;id++)
            {
                game.SelectLevel(id);Begin(game);float time=0,nextTap=.1f,dt=1f/hz;
                for(int frame=0;frame<hz*75&&game.Session.State==RoundState.Playing;frame++)
                {
                    Advance(game,hud,dt);time+=dt;if(game.Session.State!=RoundState.Playing||time<nextTap)continue;nextTap=time+.4f;
                    TargetView best=null;
                    for(int i=0;i<pool.Capacity;i++)
                    {
                        var t=pool.At(i);if(!t.Active||t.BlastPending)continue;
                        if(t.Kind==TargetKind.Bomb){if(game.Session.Wave.gate==GateKind.Shield&&game.Session.ShieldBlocks==0){game.ActivateShield();best=t;break;}continue;}
                        if(t.Kind==TargetKind.Boss){if(!game.Session.CoreExposed)continue;best=t;break;}
                        if(t.Kind==TargetKind.Priority&&game.Session.Wave.priorityOrder>game.Session.WavePriorityOrder&&t.Order!=game.Session.WavePriorityOrder+1)continue;
                        if(t.Kind==TargetKind.LinkedPair&&t.Order!=(game.Session.ActivePair==t.SequenceId?2:1))continue;
                        if(best==null||t.Kind==TargetKind.Priority||t.Kind==TargetKind.LinkedPair)best=t;
                    }
                    if(best!=null)Tap(best);
                }
                if(game.Session.Won&&game.CurrentLevel.V5Boss)Advance(game,hud,2.1f);
                Check(game.Session.Won,"V5 level "+id+" completes at "+hz+" Hz ("+game.Session.FailureReason+")");
            }
    }
}
