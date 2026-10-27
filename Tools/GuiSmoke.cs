using System;
using System.IO;
using System.Reflection;
using TapBlast;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class GuiSmoke
{
    const string Key="TapBlast.GuiSmoke";
    static double started;
    static GuiSmoke() { if(SessionState.GetBool(Key,false)) { started=EditorApplication.timeSinceStartup; EditorApplication.update+=Step; } }
    public static void Run()
    {
        PlayerSettings.companyName="TapBlastValidation"; PlayerSettings.productName="IsolatedGui";
        EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity"); SessionState.SetBool(Key,true); EditorApplication.EnterPlaymode();
    }
    static T Field<T>(object target,string field) => (T)target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
    // Section CanvasGroups hide whole HUD areas; multiply alpha up the hierarchy.
    static float VisibleAlpha(Transform t)
    {
        float alpha=1;
        for(;t!=null;t=t.parent) {var group=t.GetComponent<CanvasGroup>(); if(group==null) continue; alpha*=group.alpha; if(group.ignoreParentGroups) break;}
        return alpha;
    }
    static void Check(bool pass,string message) { if(!pass) throw new Exception(message); File.AppendAllText("gui-smoke-results.txt","PASS "+message+"\n"); }
    static void Layout(GameHUD hud)
    {
        var corners=new Vector3[4]; ((RectTransform)hud.transform.Find("Device safe area/Portrait frame")).GetWorldCorners(corners);
        float left=corners[0].x, bottom=corners[0].y, right=corners[2].x, top=corners[2].y;
        foreach(var button in hud.GetComponentsInChildren<Button>())
        {
            var scroll=button.GetComponentInParent<ScrollRect>();
            if(scroll!=null && button.transform.IsChildOf(scroll.content)) continue;
            ((RectTransform)button.transform).GetWorldCorners(corners);
            Check(corners[0].x >= left-.01f && corners[0].y >= bottom-.01f && corners[2].x <= right+.01f && corners[2].y <= top+.01f,"Control stays inside portrait safe frame: "+button.name);
        }
    }
    static void Shot(GameHUD hud,string name)
    {
        foreach(var fade in hud.GetComponentsInChildren<GuiPanelFade>()) fade.GetComponent<CanvasGroup>().alpha=1;
        hud.GetComponent<GuiScreenVisibility>()?.Refresh();
        foreach(var text in hud.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true);
        foreach(var button in hud.GetComponentsInChildren<Button>()) button.targetGraphic.CrossFadeColor(button.interactable ? button.colors.normalColor : button.colors.disabledColor,0,true,true);
        UnitySmoke.Capture(hud,"gui-"+name,validateLayout:()=>Layout(hud));
    }
    // Mirrors GameHUD.Refresh: glow and shine stretch with the fill, the head rides its leading edge.
    static void ShowProgress(GameHUD hud,float amount)
    {
        foreach(var follower in Field<RectTransform[]>(hud,"progressFollowers")) {follower.anchorMax=new Vector2(amount,follower.anchorMax.y);follower.gameObject.SetActive(amount>0);}
        var head=Field<RectTransform>(hud,"progressHead");head.anchorMin=new Vector2(amount,.5f);head.anchorMax=new Vector2(amount,.5f);head.gameObject.SetActive(amount>0);
    }
    static void RoundedFill(GameHUD hud,Image progress)
    {
        Check(progress is RoundedProgressImage,"Progress uses a rounded-cap Image");
        float previous=progress.fillAmount;
        try {
            foreach(float amount in new[]{0f,.01f,.35f,1f}) {
                progress.fillAmount=amount; ShowProgress(hud,amount);
                using(var mesh=new VertexHelper()) {
                    typeof(RoundedProgressImage).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Invoke(progress,new object[]{mesh});
                    if(amount==0) Check(mesh.currentVertCount==0,"Zero progress has no green geometry");
                    else {
                        var vertices=new System.Collections.Generic.List<UIVertex>();mesh.GetUIVertexStream(vertices);
                        float left=float.MaxValue,right=float.MinValue;
                        foreach(var vertex in vertices) {left=Mathf.Min(left,vertex.position.x);right=Mathf.Max(right,vertex.position.x);}
                        bool caps=true;
                        foreach(var vertex in vertices) if(Mathf.Abs(vertex.position.x-left)<.001f || Mathf.Abs(vertex.position.x-right)<.001f) caps &= Mathf.Abs(vertex.position.y-progress.rectTransform.rect.center.y)<.01f;
                        Check(caps && Mathf.Abs(right-left-progress.rectTransform.rect.width*amount)<.01f,"Both fill ends are round at "+amount);
                    }
                }
                Check(Field<RectTransform>(hud,"progressHead").gameObject.activeSelf==(amount>0),"Progress head shows only with progress at "+amount);
                UnitySmoke.Capture(hud,"gui-progress-"+Mathf.RoundToInt(amount*100),captureArea:(RectTransform)hud.transform.Find("Device safe area/Portrait frame/HUD/Objective/Objective card"));
            }
        } finally {progress.fillAmount=previous; ShowProgress(hud,previous);}
    }
    [Serializable] sealed class SelectionFixture
    {
        public int coins=2020, xp=985;
        public int[] scores=new int[20], stars=new int[20], bestCombos=new int[20];
        public int selectedLevel=1, activeLevel=1;
        public string activeRound="";
    }
    static void SelectionLayout(GameHUD hud)
    {
        Layout(hud);
        var card=(RectTransform)hud.transform.Find("Device safe area/Portrait frame/Screens/Level select/Level select card");
        var scroll=Field<ScrollRect>(hud,"selectionScroll");
        Check(scroll.viewport.GetComponent<RectMask2D>()!=null && scroll.vertical && !scroll.horizontal && scroll.content.rect.height>scroll.viewport.rect.height,"Saved scroll viewport clips the taller vertical list");
        Check(scroll.verticalScrollbar!=null,"Scroll position has a visible indicator");
        var wallet=(RectTransform)card.Find("Wallet/Wallet card"); var local=new Vector3[4];
        foreach(string chip in new[]{"Wallet/Coins chip","Wallet/XP chip"}) {
            var chipRect=(RectTransform)card.Find(chip); chipRect.GetWorldCorners(local);
            Check(chipRect.GetSiblingIndex()>wallet.GetSiblingIndex(),chip+" renders above the wallet card");
            for(int i=0;i<4;i++) Check(wallet.rect.Contains(wallet.InverseTransformPoint(local[i])),chip+" corner inside wallet card "+i);
        }
        foreach(var row in Field<Button[]>(hud,"levelButtons")) {
              var rowRect=(RectTransform)row.transform;
              var rowImage=row.GetComponent<Image>();
              Check(rowImage.type==Image.Type.Sliced && rowImage.preserveAspect && rowRect.rect.height==144,"Taller row uses fixed panel corners");
            var name=row.GetComponentInChildren<TMP_Text>(); name.ForceMeshUpdate(true);
            Check(!name.isTextOverflowing && name.fontSize>=32,"Readable row title: "+name.text);
            var titleRect=name.rectTransform;
            Check(Mathf.Abs(rowRect.rect.yMax-titleRect.anchoredPosition.y-titleRect.rect.height*.5f-22)<.01f,"Consistent title top padding");
            var corners=new Vector3[4];row.transform.Find("Level landscape").GetComponent<RectTransform>().GetWorldCorners(corners);
            float thumbnailRight=corners[2].x;name.rectTransform.GetWorldCorners(corners);
            Check(thumbnailRight<=corners[0].x,"Thumbnail clears row title: "+name.text);
              foreach(string child in new[]{"Level landscape","Number badge","Earned star 1","Earned star 2","Earned star 3"}) {
                  var content=(RectTransform)row.transform.Find(child);
                  content.GetWorldCorners(corners);
                  bool contained=true;
                  for(int c=0;c<4;c++) contained &= rowRect.rect.Contains(rowRect.InverseTransformPoint(corners[c]));
                  Check(contained,"Row contains artwork with padding: "+child);
              }
        }
        foreach(var text in Field<TMP_Text[]>(hud,"selectionTexts")) {
            text.ForceMeshUpdate(true);Check(!text.isTextOverflowing && text.fontSize>=23,"Readable row status: "+text.text);
            var rect=text.rectTransform;var rowRect=(RectTransform)rect.parent;
            Check(Mathf.Abs(rect.anchoredPosition.y-rect.rect.height*.5f-rowRect.rect.yMin-22)<.01f,"Consistent status bottom padding");
        }
        var xp=Field<TMP_Text>(hud,"selectionExperience");xp.ForceMeshUpdate(true);Check(!xp.isTextOverflowing,"XP and player level fit wallet");
    }
    static void ReferenceSelection(GameManager game,GameHUD hud)
    {
        // Disposable IsolatedGui save only; restore it after rendering fixture progress.
        const string key="TapBlast.v2.Progress";
        bool existed=PlayerPrefs.HasKey(key);string previous=PlayerPrefs.GetString(key,"");
        try {
            var fixture=new SelectionFixture();
            int[] scores={275,1465,1040,1001,1080,5511,8529,3350,1675,3354,5005,7164,12779,4954,4260,2830,2090};
            for(int i=0;i<scores.Length;i++){fixture.scores[i]=scores[i];fixture.stars[i]=3;}
            PlayerPrefs.SetString(key,JsonUtility.ToJson(fixture)); ProgressStore.Invalidate();
            game.ShowLevels();Shot(hud,"09-reference-levels");
            UnitySmoke.Capture(hud,"gui-levels-narrow",343,728,validateLayout:()=>SelectionLayout(hud));
            UnitySmoke.Capture(hud,"gui-levels-reference",1080,1920,validateLayout:()=>SelectionLayout(hud));
            UnitySmoke.Capture(hud,"gui-row-reference-scale",1800,3200,captureArea:(RectTransform)Field<Button[]>(hud,"levelButtons")[0].transform);
            fixture.coins=4980;fixture.xp=2495;PlayerPrefs.SetString(key,JsonUtility.ToJson(fixture)); ProgressStore.Invalidate();
            game.ShowLevels();
            Check(Field<TMP_Text>(hud,"selectionWorld").text=="Tap Blast","Level selection has one page without worlds");
            var rows=Field<Button[]>(hud,"levelButtons");
            Check(rows.Length==20,"One saved list holds all twenty level rows");
            for(int i=0;i<rows.Length;i++) Check(Field<TMP_Text>(rows[i].GetComponent<GuiLevelRow>(),"number").text==(i+1).ToString("00"),"List row binds level "+(i+1));
            var select=hud.transform.Find("Device safe area/Portrait frame/Screens/Level select");
            Check(select.Find("Previous world")==null && select.Find("Next world")==null,"World navigation buttons are removed");
            UnitySmoke.Capture(hud,"gui-levels-tablet-reference",1536,2048,validateLayout:()=>SelectionLayout(hud));
            var scroll=Field<ScrollRect>(hud,"selectionScroll");
            Canvas.ForceUpdateCanvases();
            var drag=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) {position=new Vector2(500,800),button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
            scroll.OnBeginDrag(drag);drag.position=new Vector2(500,1000);scroll.OnDrag(drag);scroll.OnEndDrag(drag);scroll.StopMovement();
            Check(scroll.content.anchoredPosition.y>0,"Pointer drag scrolls the level list");
            var wheel=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) {scrollDelta=new Vector2(0,-30)};
            scroll.OnScroll(wheel);
            Check(scroll.content.anchoredPosition.y>400,"Level list scrolls toward the later rows");
            UnitySmoke.Capture(hud,"gui-levels-scrolled",1080,1920,validateLayout:()=>SelectionLayout(hud));
            game.ShowLevels();
            Check(scroll.content.anchoredPosition==Vector2.zero,"Reopening the list returns to the current level 1 row");
        } finally {if(existed)PlayerPrefs.SetString(key,previous);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();ProgressStore.Invalidate();game.ShowLevels();}
    }
    static void Step()
    {
        if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup-started<3) return;
        try
        {
            var game=UnityEngine.Object.FindFirstObjectByType<GameManager>(); if(game==null || game.Session==null) return;
            var hud=UnityEngine.Object.FindFirstObjectByType<GameHUD>();
            File.WriteAllText("gui-smoke-results.txt","");
            foreach(var component in hud.GetComponents<MonoBehaviour>())
            {
                if(!component.GetType().Name.StartsWith("Gui")) continue;
                var serialized=new SerializedObject(component); var property=serialized.GetIterator();
                while(property.Next(true)) if(property.propertyType==SerializedPropertyType.ObjectReference && !property.propertyPath.StartsWith("m_")) Check(property.objectReferenceValue!=null,component.GetType().Name+" binding "+property.propertyPath);
            }
            int fontLabels=0;
            foreach(var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                if(text.font==null || text.font.faceInfo.familyName!="Poppins") throw new Exception("Wrong font on "+text.name);
                if(text.font.atlasPopulationMode!=AtlasPopulationMode.Static || text.fontSharedMaterial.GetTexture("_MainTex")!=text.font.atlasTextures[0]) throw new Exception("Wrong atlas/material on "+text.name);
                fontLabels++;
            }
            Check(fontLabels>0,"All "+fontLabels+" live/pooled/hidden labels use static Poppins with matching materials");
              int aspectImages=0;
              foreach(var image in UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                  if(image.name=="Background" || image.name=="Target arena" || image.name=="Arena scenery" || image.name=="Scenery") continue;
                  // Full-stretch tints and overlays fill their parent; they have no artwork aspect to keep.
                  if(image.rectTransform.anchorMin==Vector2.zero && image.rectTransform.anchorMax==Vector2.one) continue;
                  if(!image.preserveAspect) throw new Exception("Preserve Aspect disabled: "+image.name);
                  aspectImages++;
              }
              Check(aspectImages>0,"All "+aspectImages+" non-background images preserve aspect, including hidden and pooled images");
            var nav=hud.GetComponent<GuiNavigation>();
            game.SelectLevel(1); Shot(hud,"07-briefing");
            game.BeginCountdown(); hud.SetCountdown(3); Shot(hud,"01-countdown");
              Check(VisibleAlpha(hud.Arena)==0,"Countdown hides arena artwork and targets beneath the modal");
            typeof(GameManager).GetField("countdown",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,0f);
            typeof(GameManager).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,null);
            Shot(hud,"05-tutorial");
              Check(VisibleAlpha(hud.Arena)==1,"Gameplay restores arena visibility");
              var progress=Field<Image>(hud,"progressFill");
              Check(progress.sprite!=null && progress.type==Image.Type.Filled && progress.fillAmount==0 && progress.rectTransform.rect.height==20,"Empty objective uses a fixed-height zero fill with a real sprite");
              RoundedFill(hud,progress);
              var hintPlate=Field<GameObject>(hud,"hintPlate").transform;
              Check(Field<TMP_Text>(hud,"hint").transform.GetSiblingIndex()>hintPlate.GetSiblingIndex() && Field<TMP_Text>(hud,"subhint").transform.GetSiblingIndex()>hintPlate.GetSiblingIndex(),"Both hint lines render above their shared background");
            Field<Button>(nav,"walletButton").onClick.Invoke();
            Check(Field<GameObject>(nav,"walletPanel").activeSelf && game.Session.State==RoundState.Paused,"Wallet opens and pauses the round"); Shot(hud,"wallet");
            Field<Button>(nav,"walletClose").onClick.Invoke(); Check(game.Session.State==RoundState.Playing,"Wallet close resumes its paused round");
            Field<Button>(nav,"walletButton").onClick.Invoke(); Check(nav.TryCloseWallet() && !nav.TryCloseWallet() && game.Session.State==RoundState.Playing,"Back action closes wallet once and resumes play");
            hud.HideTapHand(); hud.SetHint("READY?","Tap the glowing targets before they disappear."); Shot(hud,"04-ready");
            hud.SetHint("TARGET MISSED!","Your combo reset. Start a new streak!"); Shot(hud,"06-missed");
            hud.Pause.onClick.Invoke(); Check(game.Session.State==RoundState.Paused,"Single header pause button is connected"); Shot(hud,"02-paused");
            game.ShowLevels(); Shot(hud,"09-levels");
            UnitySmoke.Capture(hud,"gui-levels-tall",1080,2400,new Rect(0,84,1080,2196),()=>Layout(hud));
            UnitySmoke.Capture(hud,"gui-levels-tablet",1536,2048,validateLayout:()=>Layout(hud));
            ReferenceSelection(game,hud);
            Field<Button>(nav,"selectionClose").onClick.Invoke(); Check(game.Session.State==RoundState.Ready,"Level selection close returns to briefing");
            var result=new RoundSession(new RoundRules(45,20,10,240,20)); result.Reset(); result.StartPlaying(); for(int i=0;i<20;i++) result.Tap();
            hud.SetSettings(true, true); hud.ShowResults(result,true,result.Score,new ProgressStore.RewardReceipt(50,25,50,25));
            Check(hud.ResultRevealComplete,"Reduced-motion results are immediately readable"); Shot(hud,"results-complete");
            File.AppendAllText("gui-smoke-results.txt","PASS Reference-state screenshots rendered from the saved Canvas.\n");
            SessionState.SetBool(Key,false); EditorApplication.Exit(0);
        }
        catch(Exception e) { File.WriteAllText("gui-smoke-results.txt","FAIL "+e); SessionState.SetBool(Key,false); EditorApplication.Exit(1); }
    }
}
