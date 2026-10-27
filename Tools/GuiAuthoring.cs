// One-time editor authoring for the original flat Canvas. The resulting prefab, font and sprite
// metadata are saved assets. Superseded by CanvasLayoutAuthoring, which owns the HUD / Screens layout.
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using TapBlast;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class GuiAuthoring
{
    const string Art = "Assets/Art/GUI/";
    static readonly Color Ink = new Color32(13, 53, 100, 255);
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    static TMP_FontAsset font, displayFont;
    static Transform frame;
    static SerializedObject data;
    static Sprite Sprite(string relative, bool slice = false)
    {
        if (sprites.TryGetValue(relative, out var cached)) return cached;
        string path = Art + relative + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new Exception("Missing GUI artwork: " + path);
        var pixels = new Texture2D(2, 2); pixels.LoadImage(File.ReadAllBytes(path));
        int x0 = pixels.width, y0 = pixels.height, x1 = 0, y1 = 0;
        var colors = pixels.GetPixels32();
        for (int y = 0; y < pixels.height; y++) for (int x = 0; x < pixels.width; x++)
            if (colors[y * pixels.width + x].a > 20) { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
        var rect = new Rect(Math.Max(0, x0 - 2), Math.Max(0, y0 - 2), Math.Min(pixels.width, x1 + 3) - Math.Max(0, x0 - 2), Math.Min(pixels.height, y1 + 3) - Math.Max(0, y0 - 2));
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritesheet = new[] { new SpriteMetaData { name = Path.GetFileNameWithoutExtension(path), rect = rect, alignment = 9, pivot = new Vector2(.5f,.5f), border = slice ? new Vector4(rect.height * .42f, rect.height * .42f, rect.height * .42f, rect.height * .42f) : Vector4.zero } };
        importer.SaveAndReimport(); Object.DestroyImmediate(pixels);
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Sprite s) { sprites[relative] = s; return s; }
        throw new Exception("Sprite import failed " + path);
    }
    static T Ensure<T>(GameObject go) where T : Component { var component=go.GetComponent<T>(); return component != null ? component : go.AddComponent<T>(); }
    static RectTransform Container(Transform parent,string name)
    {
        var existing=parent.Find(name);
        if(existing!=null) return (RectTransform)existing;
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return (RectTransform)go.transform;
    }
    static void Place(Transform t, float x, float y, float w, float h)
    {
        var r = (RectTransform)t; r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f,.5f);
        r.anchoredPosition = new Vector2(x,y); r.sizeDelta = new Vector2(w,h); r.localScale = Vector3.one;
    }
    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    static Image Picture(Transform parent, string name, string art, float x, float y, float w, float h, bool slice = false)
    {
        var existing = parent.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false); Place(go.transform,x,y,w,h);
        var image = go.GetComponent<Image>(); image.sprite = Sprite(art,slice); image.color = Color.white; image.raycastTarget = false;
        image.type = slice ? Image.Type.Sliced : Image.Type.Simple; image.pixelsPerUnitMultiplier = slice ? 3 : 1;
        return image;
    }
    static void Skin(Transform t, string art, bool slice = true)
    {
        var image = t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>();
        image.sprite = Sprite(art,slice); image.type = slice ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = slice ? 3 : 1; image.color = Color.white;
    }
    static void PreserveArtwork(GameObject root)
    {
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            // Nine-sliced panel backgrounds retain fixed corner geometry. Simple
            // illustrations must fit uniformly, including hidden and pooled art.
            bool background = image.name == "Background" || image.name == "Target arena" || image.name == "Arena scenery" || image.name == "Scenery";
            image.preserveAspect = !background;
            var icon=image.transform.Find("Button icon");
            if(image.type==Image.Type.Simple && image.sprite!=null && icon!=null) {
                var rect=image.rectTransform.rect;
                float visibleWidth=Mathf.Min(rect.width,rect.height*image.sprite.rect.width/image.sprite.rect.height);
                var iconRect=(RectTransform)icon; iconRect.anchoredPosition=new Vector2(visibleWidth*.34f*(image.name=="Start level"?1:-1),iconRect.anchoredPosition.y);
                var label=image.GetComponentInChildren<TMP_Text>(true);
                if(label!=null) label.rectTransform.sizeDelta=new Vector2(visibleWidth-145,label.rectTransform.sizeDelta.y);
            }
        }
    }
    static Sprite StatusCapsule()
    {
        const string path="Assets/Art/UI/StatusCapsule.asset";
        var capsule=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(capsule!=null) return capsule;
        var disc=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Disc.asset");
        var rect=disc.rect;
        capsule=UnityEngine.Sprite.Create(disc.texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,
            new Vector4(rect.width*.5f,rect.height*.5f,rect.width*.5f,rect.height*.5f));
        capsule.name="StatusCapsule";AssetDatabase.CreateAsset(capsule,path);
        return capsule;
    }
    static Sprite ProgressSprite()
    {
        const string path="Assets/Art/UI/ProgressFill.asset";
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(sprite!=null) return sprite;
        // Reuse an opaque central strip of the existing disc texture. The 30:1
        // sprite matches the authored 900x30 fill, so Preserve Aspect stays on.
        var disc=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Disc.asset");
        sprite=UnityEngine.Sprite.Create(disc.texture,new Rect(34,63,60,2),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
        sprite.name="ProgressFill";AssetDatabase.CreateAsset(sprite,path);return sprite;
    }
    static TMP_Text Label(Transform parent, string name, string text, float x,float y,float w,float h,float size)
    {
        var existing = parent.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));
        go.transform.SetParent(parent,false); Place(go.transform,x,y,w,h);
        var label = go.GetComponent<TMP_Text>(); label.font = font; label.text = text; label.color = Ink;
        label.fontSize = size; label.enableAutoSizing = true; label.fontSizeMin = 18; label.fontSizeMax = size;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false; return label;
    }
    static Transform Find(string path) { var t = frame.Find(path); if (t == null) throw new Exception("Missing " + path); return t; }
    static void Move(string path,float x,float y,float w,float h) => Place(Find(path),x,y,w,h);
    static TMP_Text Text(string field,float x,float y,float w,float h,float size)
    {
        var t = (TMP_Text)data.FindProperty(field).objectReferenceValue; Place(t.transform,x,y,w,h);
        t.fontSize = size; t.fontSizeMax = size; t.fontSizeMin = 20; return t;
    }
    static void PlateBehind(TMP_Text text, string art = "09-Panels-and-Effects/objective-plate", float padding = 20)
    {
        var r = text.rectTransform;
        var bg = Picture(r.parent,text.name + " plate",art,r.anchoredPosition.x,r.anchoredPosition.y,r.sizeDelta.x+padding,r.sizeDelta.y+padding,true);
        bg.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
        text.transform.SetSiblingIndex(bg.transform.GetSiblingIndex()+1);
    }
    static void ButtonSkin(Transform t,string color,string icon = null)
    {
        Skin(t,"05-Buttons/Blank-Skins/button-" + color + "-blank-skin",true); t.GetComponent<Image>().type=Image.Type.Simple;
        var b = t.GetComponent<Button>(); var cs = b.colors; cs.normalColor = Color.white; cs.highlightedColor = Color.white;
        cs.pressedColor = new Color(.8f,.88f,1); cs.disabledColor = new Color(.6f,.66f,.74f,.8f); cs.fadeDuration = .08f; b.colors = cs;
        var label = t.GetComponentInChildren<TMP_Text>(true); label.color = Color.white; label.fontSize = label.fontSizeMax = 42;
        Place(label.transform,icon == null ? 0 : 35,4,((RectTransform)t).sizeDelta.x - (icon == null ? 32 : 110),((RectTransform)t).sizeDelta.y - 18);
        if (icon != null) Picture(t,"Button icon","09-Panels-and-Effects/icon-" + icon,-((RectTransform)t).sizeDelta.x*.34f,3,66,66);
    }
    static Transform Panel(string root, string card,float x,float y,float w,float h)
    {
        var overlay = Find(root); overlay.GetComponent<Image>().color = new Color(0.015f,.08f,.16f,.42f);
        if(root=="Level briefing" || root=="Level select" || root=="Results") overlay.GetComponent<Image>().color=Color.clear;
        var panel = overlay.Find(card); Place(panel,x,y,w,h);
        Skin(panel,"10-Individual-Parts/hud-score/hud-score-01-plate");
        panel.GetComponent<Image>().color = new Color(.72f,.95f,1,.84f);
        var rim = Picture(panel,"Illustrated outer rim","09-Panels-and-Effects/white-rim",0,0,w+20,h+20,true);
        rim.transform.SetAsFirstSibling();
        if(panel.GetComponent<CanvasGroup>()==null) panel.gameObject.AddComponent<CanvasGroup>(); if(panel.GetComponent<GuiPanelFade>()==null) panel.gameObject.AddComponent<GuiPanelFade>();
        return panel;
    }
    static void ScenicCard(Transform parent,float width,float height)
    {
        var mask=Picture(parent,"Scenery mask","10-Individual-Parts/hud-score/hud-score-01-plate",0,0,width,height,true);
        mask.transform.SetAsFirstSibling(); Ensure<Mask>(mask.gameObject).showMaskGraphic=false;
        Picture(mask.transform,"Scenery","02-Backgrounds/valley-reference",0,0,width,height);
    }
    static void RefineReferenceLayout(GameObject root, Transform brief, Material titleMaterial)
    {
        // Coordinates measured in the 1080 x 1920 reference frame. These are saved,
        // editable prefab objects; no screen is assembled during gameplay.
        Move("Level briefing/Level briefing card/Eyebrow",0,630,320,45);
        var eyebrow=brief.Find("Eyebrow").GetComponent<TMP_Text>(); eyebrow.fontSizeMax=35;
        PlateBehind(eyebrow); brief.Find("Eyebrow plate").GetComponent<Image>().color=new Color32(0,153,232,255);
        Place(brief.Find("Back to levels"),-395,725,110,110);
        var heading=Text("introName",0,530,710,125,110); heading.font=displayFont; heading.fontSharedMaterial=titleMaterial;
        Picture(brief,"Title star left","09-Panels-and-Effects/star-gold",-352,515,64,64);
        Picture(brief,"Title star right","09-Panels-and-Effects/star-gold",392,515,64,64);
        Place(((GameObject)data.FindProperty("introHero").objectReferenceValue).transform,0,315,250,250);
        Picture(brief,"Hero star left","09-Panels-and-Effects/star-gold",-218,380,57,57);
        Picture(brief,"Hero star low","09-Panels-and-Effects/star-gold",-185,270,57,57);
        Picture(brief,"Hero star right","09-Panels-and-Effects/star-gold",210,330,57,57);
        var goal=Text("introGoal",0,142,795,80,44); PlateBehind(goal);
        Text("description",0,43,795,88,32).color=new Color32(78,139,169,255);
        Place(brief.Find("Mastery card"),0,-137,870,238);
        foreach(Transform child in brief) if(child.name.StartsWith("Mastery star")) {var r=(RectTransform)child;r.anchoredPosition+=new Vector2(0,55);}
        foreach(string name in new[]{"Star rules","Two star requirement","Three star requirement"}) {var t=brief.Find(name); if(t!=null) {var r=(RectTransform)t;r.anchoredPosition+=new Vector2(0,55);}}
        for(int i=0;i<2;i++) {
            var divider=Picture(brief,"Mastery divider "+i,"09-Panels-and-Effects/row-panel",-145+i*280,-165,2,155);
            divider.color=new Color32(104,187,221,180);
        }
        var rewardBg=Picture(brief,"Reward background","09-Panels-and-Effects/row-panel",0,-355,870,137,true); rewardBg.color=new Color(0.6f,.87f,.96f,.42f); rewardBg.transform.SetAsFirstSibling();
        Place(brief.Find("Reward coin"),-330,-365,95,95);
        Picture(brief,"Reward coin two","03-Logo-and-Icons/Illustrated/coin",-292,-370,95,95);
        Picture(brief,"Reward coin three","03-Logo-and-Icons/Illustrated/coin",-310,-326,95,95);
        Label(brief,"Reward caption","COMPLETE:",-112,-320,275,42,32).color=new Color32(0,115,182,255);
        var reward=Text("rewardPreview",-110,-370,285,65,43); reward.font=displayFont; reward.fontSharedMaterial=titleMaterial; reward.color=new Color32(255,207,42,255);
        var xp=brief.Find("XP reward").GetComponent<TMP_Text>(); xp.font=displayFont; xp.fontSharedMaterial=titleMaterial; xp.color=reward.color;
        Place(brief.Find("Reward XP"),155,-350,110,110); Place(xp.transform,310,-355,205,75);
        var start=brief.Find("Start level"); Place(start,0,-495,600,110);
        Place(start.Find("Button icon"),213,3,52,52);
        var startText=start.GetComponentInChildren<TMP_Text>(true); Place(startText.transform,-35,4,445,90);startText.fontSizeMax=56;
        Text("introBest",0,-720,740,42,29).color=new Color32(78,139,169,255);
        var wallet=brief.Find("Best plate"); if(wallet==null) wallet=brief.Find(((TMP_Text)data.FindProperty("introBest").objectReferenceValue).name+" plate");
        Place(wallet,0,-675,815,150);
        Picture(brief,"Wallet coin","03-Logo-and-Icons/Illustrated/coin",-310,-650,72,72);
        Picture(brief,"Wallet XP","03-Logo-and-Icons/Illustrated/xp",115,-650,78,78);
        var bd=new SerializedObject(root.GetComponent<GuiBriefing>());
        bd.FindProperty("walletCoins").objectReferenceValue=Label(brief,"Wallet coins","",-120,-647,280,80,32);
        bd.FindProperty("walletXP").objectReferenceValue=Label(brief,"Wallet experience","",270,-647,230,80,31); bd.ApplyModifiedPropertiesWithoutUndo();
        foreach(string path in new[]{"Paused/Paused card/Title","Countdown/Countdown card/Caption"}) {var label=Find(path).GetComponent<TMP_Text>();label.font=displayFont;label.fontSharedMaterial=titleMaterial;}
        Move("Paused/Paused card/Eyebrow",0,438,420,53); PlateBehind(Find("Paused/Paused card/Eyebrow").GetComponent<TMP_Text>());
        Find("Paused/Paused card/Eyebrow plate").GetComponent<Image>().color=new Color32(0,153,232,255);
        var pause=Find("Paused/Paused card"); Place(pause,0,-100,880,1100);
        Place(pause.Find("Illustrated outer rim"),0,0,900,1120);
        Place(pause.Find("Title"),0,470,730,140); pause.Find("Title").GetComponent<TMP_Text>().fontSizeMax=136;
        Place(pause.Find("Eyebrow"),0,370,340,45); PlateBehind(pause.Find("Eyebrow").GetComponent<TMP_Text>());
        pause.Find("Eyebrow plate").GetComponent<Image>().color=new Color32(0,153,232,255);
        Place(pause.Find("Detail"),0,295,655,65); PlateBehind(pause.Find("Detail").GetComponent<TMP_Text>());
        string[] pauseNames={"Resume","Restart","Sound","Motion","Vibration","Exit to levels"};
        for(int i=0;i<6;i++) {var b=pause.Find(pauseNames[i]); Place(b,0,185-i*126,700,115); var t=b.GetComponentInChildren<TMP_Text>(true);Place(t.transform,40,3,540,90);}
        pause.Find("Restart").GetComponentInChildren<TMP_Text>(true).color=new Color32(126,66,0,255);
        ScenicCard(pause,880,1100);
        Picture(pause,"Pause leaves left","09-Panels-and-Effects/leaves-left",-245,495,130,120).transform.SetSiblingIndex(2);
        Picture(pause,"Pause leaves right","09-Panels-and-Effects/leaves-right",245,495,130,120).transform.SetSiblingIndex(2);
        Picture(pause,"Pause star left","09-Panels-and-Effects/star-gold",-305,410,82,82);
        Picture(pause,"Pause star right","09-Panels-and-Effects/star-gold",305,410,82,82);
        ScenicCard(Find("Countdown/Countdown card"),930,760);
        var buttons=data.FindProperty("levelButtons");
        var select=Find("Level select/Level select card"); Place(select,0,34,960,1680); Place(select.Find("Illustrated outer rim"),0,0,980,1700);
        Place(select.Find("Selection logo"),0,825,390,190);
        var world=Text("selectionWorld",0,700,240,42,34); PlateBehind(world); select.Find("World plate").GetComponent<Image>().color=new Color32(0,78,170,255);
        var title=Text("selectionTitle",0,620,760,100,87); title.font=displayFont;title.fontSharedMaterial=titleMaterial;title.color=new Color32(255,226,56,255);
        Place(select.Find("Title plate"),0,615,875,166); select.Find("Title plate").SetAsFirstSibling();
        Place(select.Find("Description"),0,567,820,44); select.Find("Description").GetComponent<TMP_Text>().color=Color.white;
        Picture(select,"Selection star left","09-Panels-and-Effects/star-gold",-365,625,55,55);
        Picture(select,"Selection star right","09-Panels-and-Effects/star-gold",365,625,55,55);
        string[] colors={"blue","green","purple","orange","red","blue","green","purple","orange","red"};
        for(int i=0;i<buttons.arraySize;i++) {
            var row=((Button)buttons.GetArrayElementAtIndex(i).objectReferenceValue).transform;
            Place(row,0,462-i*116,900,104);
            Skin(row.Find("Number badge"),"05-Buttons/Blank-Skins/button-"+colors[i%colors.Length]+"-blank-skin",true);
            row.Find("Number badge").GetComponent<Image>().pixelsPerUnitMultiplier=6;
            var status=((TMP_Text)data.FindProperty("selectionTexts").GetArrayElementAtIndex(i).objectReferenceValue);
            Place(status.transform,25,-23,360,32); PlateBehind(status,"09-Panels-and-Effects/objective-plate",10);
            row.Find(status.name+" plate").GetComponent<Image>().color=new Color32(146,215,240,255);
            var number=row.Find("Level number").GetComponent<TMP_Text>(); number.font=displayFont;number.fontSizeMax=54;
            var landscape=row.Find("Level landscape").GetComponent<Image>(); landscape.color=Color.clear;
            Place(landscape.transform,-240,0,120,88); Ensure<RectMask2D>(landscape.gameObject);
            Picture(landscape.transform,"Landscape crop","02-Backgrounds/valley-reference",0,66,120,213);
        }
    }
    static TMP_Text PositionedLabel(string path,float x,float y,float w,float h,float size)
    {
        var label=Find(path).GetComponent<TMP_Text>();Place(label.transform,x,y,w,h);
        label.fontSize=label.fontSizeMax=size;return label;
    }
    static void RefineScreenSpacing(GameObject root)
    {
        // Shared HUD coordinates measured against the supplied 9:16 screens.
        Move("Score card",-305,531,390,144);Move("Level card",26,531,250,144);
        PositionedLabel("Score card/Caption",55,40,245,44,34);
        PositionedLabel("Score card/Value",55,-19,245,82,74);
        var level=(TMP_Text)data.FindProperty("levelLabel").objectReferenceValue;
        Place(level.transform,0,-19,215,82);level.fontSize=level.fontSizeMax=74;level.color=new Color32(10,174,216,255);
        var levelCaption=Find("Level card").GetComponentsInChildren<TMP_Text>(true);
        foreach(var label in levelCaption) if(label!=level) {Place(label.transform,0,40,215,44);label.fontSize=label.fontSizeMax=34;}
        Move("Timer card",287,535,210,210);Move("Pause button",458,535,90,90);
        Text("timer",0,20,165,87,78);
        var timerLabel=(TMP_Text)data.FindProperty("timer").objectReferenceValue;
        foreach(var label in Find("Timer card").GetComponentsInChildren<TMP_Text>(true)) if(label!=timerLabel) {Place(label.transform,0,-48,150,44);label.fontSize=label.fontSizeMax=34;}
        var world=Text("worldLabel",0,630,530,48,34);world.color=Color.white;
        PlateBehind(world,"09-Panels-and-Effects/objective-plate",12);
        var worldPlate=world.transform.parent.Find(world.name+" plate").GetComponent<Image>();worldPlate.sprite=StatusCapsule();worldPlate.pixelsPerUnitMultiplier=128f/60;worldPlate.color=new Color32(4,120,174,235);
        Text("objective",0,418,340,58,37);var objective=(TMP_Text)data.FindProperty("objective").objectReferenceValue;
        Place(objective.transform.parent.Find(objective.name+" plate"),0,418,360,66);
        Move("Objective track",0,360,940,58);
        var progress=(Image)data.FindProperty("progressFill").objectReferenceValue;
        if(!(progress is RoundedProgressImage)) {
            var fillObject=progress.gameObject;Object.DestroyImmediate(progress);
            progress=fillObject.AddComponent<RoundedProgressImage>();progress.raycastTarget=false;
            data.FindProperty("progressFill").objectReferenceValue=progress;
        }
        Place(progress.transform,0,0,900,30);progress.sprite=ProgressSprite();progress.type=Image.Type.Filled;progress.fillMethod=Image.FillMethod.Horizontal;progress.fillOrigin=0;progress.fillAmount=0;progress.color=new Color32(48,232,5,255);
        var track=Find("Objective track");
        var trackColor=Picture(track,"Track color","09-Panels-and-Effects/row-panel",0,0,908,30,true);
        trackColor.sprite=StatusCapsule();trackColor.pixelsPerUnitMultiplier=128f/30;trackColor.color=new Color32(12,160,224,255);trackColor.transform.SetAsFirstSibling();
        for(int i=0;i<3;i++) Place(((Image)data.FindProperty("objectiveStars").GetArrayElementAtIndex(i).objectReferenceValue).transform,-150+i*290,360,60,60);
        var mechanics=PositionedLabel("World 2 progress",-110,310,690,36,30);
        mechanics.fontSizeMin=26;mechanics.textWrappingMode=TextWrappingModes.NoWrap;mechanics.alignment=TextAlignmentOptions.Center;
        mechanics.color=new Color32(5,32,108,255);
        Move("Shield",375,310,200,36);
        var shieldText=Find("Shield").GetComponentInChildren<TMP_Text>(true);
        Place(shieldText.transform,0,0,184,32);shieldText.fontSize=shieldText.fontSizeMax=26;shieldText.fontSizeMin=20;
        Move("Target arena",0,-162,970,880);Move("Arena rim",0,-162,1000,900);Move("Pooled feedback",0,-162,970,880);
        var arena=Find("Target arena");var arenaMask=arena.GetComponent<Image>();
        arenaMask.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Panel.asset");arenaMask.type=Image.Type.Sliced;arenaMask.pixelsPerUnitMultiplier=.4f;arenaMask.color=Color.white;
        Ensure<Mask>(arena.gameObject).showMaskGraphic=false;
        var scenery=Picture(arena,"Arena scenery","02-Backgrounds/city",0,0,970,880);scenery.transform.SetAsFirstSibling();
        Skin(Find("Arena rim"),"09-Panels-and-Effects/white-rim",true);
        var theme=new SerializedObject(root.GetComponent<GuiArenaTheme>());theme.FindProperty("arena").objectReferenceValue=scenery;theme.ApplyModifiedPropertiesWithoutUndo();
        var hint=Text("hint",0,-653,760,52,43);Text("subhint",0,-700,760,44,32).color=new Color32(52,123,166,255);
        var hintBg=hint.transform.parent.Find(hint.name+" plate");Place(hintBg,0,-678,800,126);
        hintBg.SetSiblingIndex(hint.transform.GetSiblingIndex());
        hint.transform.SetSiblingIndex(hintBg.GetSiblingIndex()+1);
        ((TMP_Text)data.FindProperty("subhint").objectReferenceValue).transform.SetSiblingIndex(hint.transform.GetSiblingIndex()+1);
        data.FindProperty("hintPlate").objectReferenceValue=hintBg.gameObject;
        Move("Combo card",0,-830,990,174);
        PositionedLabel("Combo card/Combo caption",-370,50,190,44,36);
        PositionedLabel("Combo card/Combo value",-370,-20,190,90,82);
        Text("fever",22,0,460,70,38);Text("multiplier",416,0,125,112,80).color=new Color32(255,189,18,255);
        Move("Combo card/Combo flame",300,0,100,125);
        ((Image)data.FindProperty("feverLine").objectReferenceValue).gameObject.SetActive(false);
        var comboDivider=Picture(Find("Combo card"),"Combo divider","09-Panels-and-Effects/row-panel",-250,0,2,110);comboDivider.sprite=null;comboDivider.color=new Color32(135,202,228,200);
        Move("Header pause",-440,880,125,125);
        Move("Wallet shortcut",355,886,285,80);Find("Wallet shortcut").GetComponent<Image>().color=new Color32(5,88,155,255);
        var walletNumber=Find("Wallet shortcut").GetComponentInChildren<TMP_Text>(true);walletNumber.fontSize=walletNumber.fontSizeMax=48;walletNumber.color=Color.white;

        var pause=Find("Paused/Paused card");
        PositionedLabel("Paused/Paused card/Title",0,470,730,160,148);
        PositionedLabel("Paused/Paused card/Eyebrow",0,370,340,48,36);
        PositionedLabel("Paused/Paused card/Detail",0,295,655,65,36);
        string[] actions={"Resume","Restart","Sound","Motion","Vibration","Exit to levels"};
        for(int i=0;i<actions.Length;i++) {
            var button=pause.Find(actions[i]);Place(button,0,185-i*130,700,130);
            var label=button.GetComponentInChildren<TMP_Text>(true);Place(label.transform,45,3,555,90);label.fontSize=label.fontSizeMax=48;
            Place(button.Find("Button icon"),-230,3,58,58);
        }
        var countdown=Find("Countdown/Countdown card");Place(countdown,0,-110,930,790);
        Place(countdown.Find("Illustrated outer rim"),0,0,950,810);ScenicCard(countdown,930,790);
        var caption=PositionedLabel("Countdown/Countdown card/Caption",0,270,730,140,118);caption.text="GET <color=#EAF8FF>READY!</color>";
        Place(countdown.Find("Countdown bubble"),0,-5,320,320);
        var digit=Text("countdownText",0,-5,300,280,225);digit.color=new Color32(255,210,30,255);digit.fontSharedMaterial=caption.fontSharedMaterial;
        var countGoal=Text("countdownGoal",0,-278,780,88,44);Place(countdown.Find(countGoal.name+" plate"),0,-278,800,110);
        Picture(countdown,"Countdown star left","09-Panels-and-Effects/star-gold",-355,225,78,78);
        Picture(countdown,"Countdown star right","09-Panels-and-Effects/star-gold",355,225,78,78);
        Picture(countdown,"Countdown star middle left","09-Panels-and-Effects/star-gold",-245,65,82,82);
        Picture(countdown,"Countdown star middle right","09-Panels-and-Effects/star-gold",230,85,82,82);
        Picture(countdown,"Countdown star low","09-Panels-and-Effects/star-gold",-210,-95,58,58);

        var brief=Find("Level briefing/Level briefing card");
        foreach(string name in new[]{"Star rules","Two star requirement","Three star requirement"}) {
            var label=brief.Find(name).GetComponent<TMP_Text>();var r=label.rectTransform;
            Place(r,r.anchoredPosition.x,-192,265,126);label.alignment=TextAlignmentOptions.Top;label.fontSize=label.fontSizeMax=35;
        }
        foreach(var image in root.GetComponentsInChildren<Image>(true)) if(image.name.IndexOf("divider",StringComparison.OrdinalIgnoreCase)>=0) image.sprite=null;
        for(int i=0;i<2;i++) Place(brief.Find("Mastery divider "+i),-145+i*280,-155,2,130);
        var rewardDivider=Picture(brief,"Reward divider","09-Panels-and-Effects/row-panel",50,-355,2,85);rewardDivider.sprite=null;rewardDivider.color=new Color32(104,187,221,180);
        foreach(string name in new[]{"Reward coin","Reward coin two","Reward coin three"}) {
            var r=(RectTransform)brief.Find(name);Place(r,r.anchoredPosition.x,r.anchoredPosition.y,75,75);
        }
        // Keep decorative foreground objects below overlays and bind visibility
        // explicitly. Modal screens hide only the arena/hint graphics beneath them.
        var visibility=new SerializedObject(root.GetComponent<GuiScreenVisibility>());
        visibility.FindProperty("paused").objectReferenceValue=Find("Paused").gameObject;
        visibility.FindProperty("countdown").objectReferenceValue=Find("Countdown").gameObject;
        var groups=new List<CanvasGroup>();
        foreach(Transform child in frame) {
            if(child.name=="Level briefing" || child.name=="Level select" || child.name=="Results" || child.name=="Paused" || child.name=="Countdown" || child.name=="Wallet information") continue;
            groups.Add(Ensure<CanvasGroup>(child.gameObject));
        }
        foreach(var group in groups) group.transform.SetSiblingIndex(Mathf.Min(group.transform.GetSiblingIndex(),Find("Level briefing").GetSiblingIndex()));
        var gameplay=visibility.FindProperty("gameplay");gameplay.arraySize=groups.Count;
        for(int i=0;i<groups.Count;i++) gameplay.GetArrayElementAtIndex(i).objectReferenceValue=groups[i];
        var obscured=new[]{Find("Target arena"),Find("Arena rim"),Find("Pooled feedback"),Find("World 2 progress"),Find("Shield"),hint.transform,((TMP_Text)data.FindProperty("subhint").objectReferenceValue).transform,hintBg};
        var hidden=visibility.FindProperty("modalObscured");hidden.arraySize=obscured.Length;
        for(int i=0;i<obscured.Length;i++) hidden.GetArrayElementAtIndex(i).objectReferenceValue=Ensure<CanvasGroup>(obscured[i].gameObject);
        visibility.ApplyModifiedPropertiesWithoutUndo();
    }
    // Editor-only layout pass; all objects and bindings are saved to the prefab.
    public static void RunLevelSelect()
    {
        try {
            AssetDatabase.Refresh();
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Black SDF.asset");
            displayFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/LilitaOne SDF.asset");
            var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/GameCanvas.prefab");
            data=new SerializedObject(root.GetComponent<GameHUD>());
            frame=root.transform.Find("Device safe area/Portrait frame");
            if(frame.Find("HUD")!=null) throw new Exception("GuiAuthoring targets the legacy flat Canvas; the HUD / Screens layout is owned by CanvasLayoutAuthoring.");
            RefineSelectionReference(AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/LilitaOne-Title.mat"));
            RefineScreenSpacing(root);
            data.ApplyModifiedPropertiesWithoutUndo();
            TypographyAuthoring.Apply(root); PreserveArtwork(root); PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/UI/GameCanvas.prefab"); PrefabUtility.UnloadPrefabContents(root);
            var target = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Targets/NormalTarget.prefab");
            PreserveArtwork(target); PrefabUtility.SaveAsPrefabAsset(target,"Assets/Prefabs/Targets/NormalTarget.prefab"); PrefabUtility.UnloadPrefabContents(target);
            AssetDatabase.SaveAssets(); File.WriteAllText("gui-authoring-results.txt","PASS Level selection reference layout saved.\n"); EditorApplication.Exit(0);
        } catch(Exception e) {Debug.LogException(e);File.WriteAllText("gui-authoring-results.txt","FAIL "+e);EditorApplication.Exit(1);}
    }
    static void RefineSelectionReference(Material titleMaterial)
    {
        var card=Find("Level select/Level select card"); Place(card,0,35,960,1680);
        card.GetComponent<Image>().color=new Color(.76f,.97f,.98f,.9f);
        Place(card.Find("Illustrated outer rim"),0,0,980,1700);
        Place(card.Find("Selection logo"),0,825,400,195);
        var world=Text("selectionWorld",0,704,300,42,34); world.font=font;world.fontSharedMaterial=font.material;
        Place(card.Find("World plate"),0,704,340,56);
        var worldPlate=card.Find("World plate").GetComponent<Image>();
        worldPlate.sprite=StatusCapsule();worldPlate.type=Image.Type.Sliced;
        worldPlate.pixelsPerUnitMultiplier=128f/56;worldPlate.color=new Color32(0,78,170,255);
        var title=Text("selectionTitle",0,640,690,80,96); title.font=displayFont;title.fontSharedMaterial=titleMaterial;
        Place(card.Find("Title plate"),0,614,875,165);
        var desc=card.Find("Description").GetComponent<TMP_Text>();Place(desc.transform,0,569,800,42);desc.fontSize=desc.fontSizeMax=38;
        var starsLeft=card.Find("Selection star left");Place(starsLeft,-360,625,60,60);Place(card.Find("Selection star right"),375,625,55,55);
        var buttons=data.FindProperty("levelButtons");
        var viewport=Container(card,"Level list viewport");Place(viewport,0,-70,940,1190);
        Ensure<RectMask2D>(viewport.gameObject);
        var touchSurface=Ensure<Image>(viewport.gameObject);touchSurface.color=Color.clear;touchSurface.raycastTarget=true;
        var content=Container(viewport,"Level list content");
        content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);
        content.sizeDelta=new Vector2(0,16+buttons.arraySize*160);content.anchoredPosition=Vector2.zero;
        var scroll=Ensure<ScrollRect>(viewport.gameObject);scroll.viewport=viewport;scroll.content=content;
        scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        scroll.inertia=true;scroll.decelerationRate=.135f;scroll.scrollSensitivity=45;
        var scrollRail=Container(card,"Level list scrollbar");Place(scrollRail,470,-70,6,1158);
        var rail=Ensure<Image>(scrollRail.gameObject);rail.sprite=StatusCapsule();rail.type=Image.Type.Sliced;rail.pixelsPerUnitMultiplier=22;rail.color=new Color32(56,158,196,75);
        var handle=Container(scrollRail,"Scroll handle");Stretch(handle);
        var handleImage=Ensure<Image>(handle.gameObject);handleImage.sprite=StatusCapsule();handleImage.type=Image.Type.Sliced;handleImage.pixelsPerUnitMultiplier=22;handleImage.color=new Color32(0,144,202,200);
        var scrollbar=Ensure<Scrollbar>(scrollRail.gameObject);scrollbar.handleRect=handle;scrollbar.targetGraphic=handleImage;scrollbar.direction=Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar=scrollbar;
        data.FindProperty("selectionScroll").objectReferenceValue=scroll;
        for(int i=0;i<buttons.arraySize;i++) {
            var row=((Button)buttons.GetArrayElementAtIndex(i).objectReferenceValue).transform;
            row.SetParent(content,false);Place(row,0,-88-i*160,900,144);
            ((RectTransform)row).anchorMin=((RectTransform)row).anchorMax=new Vector2(.5f,1);
            // Resizable panel background uses fixed corners; foreground artwork
            // fits uniformly. 16-pixel list padding/gaps and equal text insets.
            row.GetComponent<Image>().type=Image.Type.Sliced;
            row.GetComponent<Image>().pixelsPerUnitMultiplier=3;
            row.GetComponent<Image>().preserveAspect=true;
            var name=row.GetComponentInChildren<TMP_Text>(true);Place(name.transform,39,28,380,44);
            name.font=font;name.fontSharedMaterial=font.material;name.fontSize=name.fontSizeMax=40;name.fontSizeMin=32;name.textWrappingMode=TextWrappingModes.NoWrap;
            name.color=new Color32(5,32,108,255);
            var status=(TMP_Text)data.FindProperty("selectionTexts").GetArrayElementAtIndex(i).objectReferenceValue;
            Place(status.transform,25,-28,330,44); status.fontSize=status.fontSizeMax=28;status.fontSizeMin=23;status.textWrappingMode=TextWrappingModes.NoWrap;
            status.color=new Color32(0,144,202,255);
            var plate=row.Find(status.name+" plate").GetComponent<Image>();Place(plate.transform,15,-28,355,44);
            plate.sprite=StatusCapsule();plate.type=Image.Type.Sliced;plate.pixelsPerUnitMultiplier=128f/44;plate.color=new Color32(178,231,248,255);
            Place(row.Find("Number badge"),-360,0,160,110);
            var number=row.Find("Level number").GetComponent<TMP_Text>();Place(number.transform,-360,0,140,96);number.font=font;number.fontSharedMaterial=font.material;number.fontSize=number.fontSizeMax=70;
            var landscape=row.Find("Level landscape");Place(landscape,-237,0,144,110);landscape.SetAsFirstSibling();
            Place(landscape.Find("Landscape crop"),0,74,144,256);
            for(int j=0;j<3;j++) Place(row.Find("Earned star "+(j+1)),280+j*60,0,56,56);
        }
        var wallet=card.Find("Wallet plate");Place(wallet,0,-760,860,155);
        var coins=Text("selectionWallet",-100,-714,280,70,53); coins.font=font;coins.fontSharedMaterial=font.material;coins.color=new Color32(5,32,108,255);
        var experience=Label(card,"Selection experience","",290,-718,260,92,44);experience.color=coins.color;experience.fontSharedMaterial=font.material;
        data.FindProperty("selectionExperience").objectReferenceValue=experience;
        Picture(card,"Selection coins left","03-Logo-and-Icons/Illustrated/coin",-335,-735,65,65);
        Picture(card,"Selection coins right","03-Logo-and-Icons/Illustrated/coin",-295,-741,65,65);
        Picture(card,"Selection coins top","03-Logo-and-Icons/Illustrated/coin",-310,-699,65,65);
        Picture(card,"Selection XP icon","03-Logo-and-Icons/Illustrated/xp",120,-720,95,95);
        var divider=Picture(card,"Selection wallet divider","09-Panels-and-Effects/row-panel",35,-715,2,70);divider.sprite=null;divider.color=new Color32(99,181,215,180);
        var footer=card.Find("Footer"); footer.SetAsLastSibling(); Place(footer,0,-791,800,42);var footText=footer.GetComponent<TMP_Text>();footText.fontSize=footText.fontSizeMax=33;footText.color=new Color32(18,153,195,255);
        Place(Find("Level select/Close level selection"),0,-866,130,130);
        foreach (string child in new[]{"Selection coins left","Selection coins right","Selection coins top","Selection XP icon","Selection wallet divider","Footer","Selection experience"})
            ((RectTransform)card.Find(child)).anchoredPosition += Vector2.down*19;
        coins.rectTransform.anchoredPosition += Vector2.down*19;
        ((RectTransform)footer).anchoredPosition+=Vector2.up*8;
    }
    public static void Run()
    {
        try
        {
            AssetDatabase.Refresh();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito-Black SDF.asset");
            if (font == null)
            {
                font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Nunito-Black.ttf"), 90, 9, GlyphRenderMode.SDFAA, 2048, 2048);
                font.name = "Nunito-Black SDF";
                font.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~ז��");
                font.atlasPopulationMode = AtlasPopulationMode.Static;
                AssetDatabase.CreateAsset(font,"Assets/Fonts/Nunito-Black SDF.asset");
                foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas,font);
                AssetDatabase.AddObjectToAsset(font.material,font); EditorUtility.SetDirty(font); AssetDatabase.SaveAssets();
            }
            displayFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/LilitaOne SDF.asset");
            if(displayFont == null) {
                displayFont = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/LilitaOne-Regular.ttf"), 100, 18, GlyphRenderMode.SDFAA, 2048, 2048);
                displayFont.name="LilitaOne SDF";
                displayFont.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u00d7\u2013\u2019");
                displayFont.atlasPopulationMode=AtlasPopulationMode.Static;
                AssetDatabase.CreateAsset(displayFont,"Assets/Fonts/LilitaOne SDF.asset");
                foreach(var atlas in displayFont.atlasTextures) AssetDatabase.AddObjectToAsset(atlas,displayFont);
                AssetDatabase.AddObjectToAsset(displayFont.material,displayFont);
                EditorUtility.SetDirty(displayFont);
            }
            const string path = "Assets/Prefabs/UI/GameCanvas.prefab";
            var root = PrefabUtility.LoadPrefabContents(path); data = new SerializedObject(root.GetComponent<GameHUD>());
            frame = root.transform.Find("Device safe area/Portrait frame");
            if(frame.Find("HUD")!=null) throw new Exception("GuiAuthoring targets the legacy flat Canvas; the HUD / Screens layout is owned by CanvasLayoutAuthoring.");
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                // Feedback popups keep their contrast over moving targets.
                label.font = font; label.fontSharedMaterial = font.material;
                if (!label.transform.IsChildOf(frame.Find("Pooled feedback"))) label.color = Ink;
                label.overflowMode=TextOverflowModes.Overflow; label.enableAutoSizing=true; label.fontSizeMin=16; label.fontSizeMax=label.fontSize;
                label.characterSpacing = 0; label.fontStyle = FontStyles.Normal;
            }
            Skin(root.transform.Find("Background"),"02-Backgrounds/valley-reference",false);
            Find("Wordmark").gameObject.SetActive(false);
            Find("Teal atmosphere").gameObject.SetActive(false); Find("Blue atmosphere").gameObject.SetActive(false);
            Picture(frame,"Illustrated logo","03-Logo-and-Icons/Illustrated/logo",0,777,550,280).transform.SetSiblingIndex(0);
            Move("World",0,626,800,44);
            Move("Score card",-300,505,390,144); Skin(Find("Score card"),"10-Individual-Parts/hud-score/hud-score-01-plate");
            Move("Score card/Caption",35,39,250,38); Move("Score card/Value",35,-22,260,76);
            Picture(Find("Score card"),"Trophy","03-Logo-and-Icons/Illustrated/trophy",-128,0,104,104);
            Move("Level card",40,505,250,144); Skin(Find("Level card"),"10-Individual-Parts/hud-score/hud-score-01-plate");
            Move("Timer card",300,505,200,200); Skin(Find("Timer card"),"10-Individual-Parts/hud-timer/hud-timer-04-face",false);
            Move("Pause button",455,505,96,96); Skin(Find("Pause button"),"06-Controls/control-pause-small",false);
            Find("Pause button/Label").gameObject.SetActive(false);
            Text("objective",0,398,480,50,30); PlateBehind((TMP_Text)data.FindProperty("objective").objectReferenceValue);
            Move("Objective track",0,335,930,22); Skin(Find("Objective track"),"09-Panels-and-Effects/row-panel");
            var arena = Find("Target arena"); Place(arena,0,-167,970,925);
            Skin(arena,"02-Backgrounds/city",false);
            var oldCity = arena.Find("Neon City backdrop"); if (oldCity != null) oldCity.gameObject.SetActive(false);
            Picture(frame,"Arena rim","09-Panels-and-Effects/outer-rim",0,-167,1010,965,true).transform.SetSiblingIndex(arena.GetSiblingIndex()+1);
            Place(Find("Pooled feedback"),0,-167,970,925);
            Text("hint",0,-690,910,54,38); PlateBehind((TMP_Text)data.FindProperty("hint").objectReferenceValue,"09-Panels-and-Effects/message-panel",36);
            Text("subhint",0,-744,930,42,25);
            Move("Combo card",0,-848,960,132); Skin(Find("Combo card"),"10-Individual-Parts/hud-score/hud-score-01-plate");
            Move("Shield",345,280,260,64); ButtonSkin(Find("Shield"),"blue"); Move("World 2 progress",-130,280,660,52);

            var titleMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/LilitaOne-Title.mat");
            if(titleMaterial==null) { titleMaterial=new Material(displayFont.material); titleMaterial.name="Lilita One Title"; titleMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth,.2f); titleMaterial.SetColor(ShaderUtilities.ID_OutlineColor,Ink); AssetDatabase.CreateAsset(titleMaterial,"Assets/Fonts/LilitaOne-Title.mat"); }
            titleMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth,.075f);
            titleMaterial.SetColor(ShaderUtilities.ID_OutlineColor,new Color32(0,70,139,255));
            titleMaterial.SetFloat("_FaceDilate",.14f);
            titleMaterial.EnableKeyword("UNDERLAY_ON");
            titleMaterial.SetColor("_UnderlayColor",new Color32(0,28,82,255));
            titleMaterial.SetFloat("_UnderlayOffsetY",-.8f); titleMaterial.SetFloat("_UnderlayDilate",.15f);
            titleMaterial.EnableKeyword("OUTLINE_ON"); EditorUtility.SetDirty(titleMaterial);

            var progressStars=data.FindProperty("objectiveStars"); progressStars.arraySize=3;
            for(int i=0;i<3;i++) progressStars.GetArrayElementAtIndex(i).objectReferenceValue=Picture(frame,"Objective star "+(i+1),"09-Panels-and-Effects/star-gold",-155+i*310,335,54,54);
            Picture(Find("Combo card"),"Combo flame","03-Logo-and-Icons/Illustrated/flame",-420,0,80,90);
            Move("Combo card/Combo caption",-300,28.5f,160,40); Move("Combo card/Combo value",-300,-25.5f,160,66);
            // Briefing.
            var brief = Panel("Level briefing","Level briefing card",0,-40,960,1570);
            Picture(brief,"Briefing logo","03-Logo-and-Icons/Illustrated/logo",0,815,540,260);
            Move("Level briefing/Level briefing card/Eyebrow",0,600,680,52);
            Text("introName",0,530,850,120,100); Text("introGoal",0,100,820,82,39); PlateBehind((TMP_Text)data.FindProperty("introGoal").objectReferenceValue);
            Place((GameObject)data.FindProperty("introHero").objectReferenceValue == null ? brief : ((GameObject)data.FindProperty("introHero").objectReferenceValue).transform,0,300,280,280);
            Skin(((GameObject)data.FindProperty("introHero").objectReferenceValue).transform,"04-Targets/With-Stars/target-blue",false);
            foreach (Transform child in ((GameObject)data.FindProperty("introHero").objectReferenceValue).transform) child.gameObject.SetActive(false);
            Place(((GameObject)data.FindProperty("sizePreview").objectReferenceValue).transform,0,300,810,270);
            Text("description",0,-12,850,122,29);

            var oldRules=brief.Find("Star rules plate"); if(oldRules!=null) Object.DestroyImmediate(oldRules.gameObject);
            Picture(brief,"Mastery card","09-Panels-and-Effects/row-panel",0,-192,865,230,true).transform.SetAsFirstSibling();
            var one=Text("starRules",-280,-230,265,133,32);
            var two=Label(brief,"Two star requirement","",0,-230,265,133,32);
            var three=Label(brief,"Three star requirement","",280,-230,265,133,32);
            for(int c=0;c<3;c++) for(int i=0;i<=c;i++) Picture(brief,"Mastery star "+c+"-"+i,"09-Panels-and-Effects/star-gold",(c-1)*280+(i-c*.5f)*66,-120,70,70);
            var briefing=root.GetComponent<GuiBriefing>() ?? root.AddComponent<GuiBriefing>(); var briefData=new SerializedObject(briefing);
            briefData.FindProperty("oneStar").objectReferenceValue=one; briefData.FindProperty("twoStars").objectReferenceValue=two; briefData.FindProperty("threeStars").objectReferenceValue=three;
            
            var reward=Text("rewardPreview",-120,-370,350,85,32); briefData.FindProperty("coins").objectReferenceValue=reward;
            Picture(brief,"Reward coin","03-Logo-and-Icons/Illustrated/coin",-365,-370,100,100);
            Picture(brief,"Reward XP","03-Logo-and-Icons/Illustrated/xp",115,-370,100,100);
            briefData.FindProperty("xp").objectReferenceValue=Label(brief,"XP reward","",290,-370,230,85,42); briefData.ApplyModifiedPropertiesWithoutUndo();
            Move("Level briefing/Level briefing card/Start level",0,-505,800,125); ButtonSkin(Find("Level briefing/Level briefing card/Start level"),"green","play");
            Text("introBest",0,-666,820,120,29); PlateBehind((TMP_Text)data.FindProperty("introBest").objectReferenceValue,"09-Panels-and-Effects/row-panel",30);
            var back = Find("Level briefing/Level briefing card/Back to levels"); Place(back,-370,646,100,100); Skin(back,"06-Controls/control-back",false); back.GetComponentInChildren<TMP_Text>(true).gameObject.SetActive(false);

            ((TMP_Text)data.FindProperty("introName").objectReferenceValue).fontSharedMaterial=titleMaterial;
            ((TMP_Text)data.FindProperty("introName").objectReferenceValue).color=new Color32(255,226,56,255);
            // Pause.
            var paused = Panel("Paused","Paused card",0,-75,880,1240);
            Move("Paused/Paused card/Eyebrow",0,438,740,53); Move("Paused/Paused card/Title",0,548,780,100);
            Move("Paused/Paused card/Detail",0,345,760,90); PlateBehind(Find("Paused/Paused card/Detail").GetComponent<TMP_Text>());
            string[] names={"Resume","Restart","Sound","Motion","Vibration","Exit to levels"};
            string[] skins={"green","gold","blue","purple","orange","red"};
            string[] icons={"play","restart","sound","motion","vibrate","exit"};
            for (int i=0;i<6;i++) { var t=paused.Find(names[i]); Place(t,0,205-i*135,770,116); ButtonSkin(t,skins[i],icons[i]); }

            var pauseTitle=Find("Paused/Paused card/Title").GetComponent<TMP_Text>(); pauseTitle.fontSharedMaterial=titleMaterial; pauseTitle.color=new Color32(255,226,56,255); pauseTitle.fontSizeMax=90;
            // Countdown.
            var count = Panel("Countdown","Countdown card",0,-90,930,760);
            Move("Countdown/Countdown card/Caption",0,265,800,110); Find("Countdown/Countdown card/Caption").GetComponent<TMP_Text>().fontSizeMax=76;
            Picture(count,"Countdown bubble","04-Targets/Empty-Bases/target-blue",0,15,350,350).transform.SetAsFirstSibling();
            Text("countdownText",0,15,350,260,185).color=Color.white;
            Text("countdownGoal",0,-255,800,130,40); PlateBehind((TMP_Text)data.FindProperty("countdownGoal").objectReferenceValue);
            // Level list: one saved row per level, real progress, a single page.
            var select = Panel("Level select","Level select card",0,34,960,1740);
            Picture(select,"Selection logo","03-Logo-and-Icons/Illustrated/logo",0,760,390,190);
            Text("selectionWorld",0,650,450,45,36); Text("selectionTitle",0,568,800,78,61);
            Move("Level select/Level select card/Description",0,500,820,48);
            for(int i=0;i<data.FindProperty("levelButtons").arraySize;i++)
            {
                var b=(Button)data.FindProperty("levelButtons").GetArrayElementAtIndex(i).objectReferenceValue;
                Place(b.transform,0,405-i*111,880,102); Skin(b.transform,"09-Panels-and-Effects/row-panel");
                var title=b.GetComponentInChildren<TMP_Text>(true); Place(title.transform,58,20,460,43); title.alignment=TextAlignmentOptions.MidlineLeft; title.fontSize=title.fontSizeMax=32;
                var status=(TMP_Text)data.FindProperty("selectionTexts").GetArrayElementAtIndex(i).objectReferenceValue;
                status.transform.SetParent(b.transform,false); Place(status.transform,58,-24,460,32); status.alignment=TextAlignmentOptions.MidlineLeft; status.fontSize=status.fontSizeMax=20;
                Picture(b.transform,"Number badge","09-Panels-and-Effects/number-badge",-370,0,125,98);
                var number=Label(b.transform,"Level number",(i+1).ToString("00"),-370,0,100,75,48); number.color=Color.white;
                Picture(b.transform,"Level landscape","02-Backgrounds/valley-reference",-257,0,90,80);
                var row=b.GetComponent<GuiLevelRow>() ?? b.gameObject.AddComponent<GuiLevelRow>(); var rowData=new SerializedObject(row); rowData.FindProperty("number").objectReferenceValue=number;
                rowData.FindProperty("stars").arraySize=3;
                for(int s=0;s<3;s++) rowData.FindProperty("stars").GetArrayElementAtIndex(s).objectReferenceValue=Picture(b.transform,"Earned star "+(s+1),"09-Panels-and-Effects/star-gold",285+s*52,0,53,53);
                rowData.ApplyModifiedPropertiesWithoutUndo();
                var cs=b.colors; cs.normalColor=Color.white; cs.highlightedColor=Color.white; cs.pressedColor=new Color(.8f,.9f,1); cs.disabledColor=new Color(.75f,.8f,.87f); b.colors=cs;
            }
            Text("selectionWallet",0,-730,800,103,31); PlateBehind((TMP_Text)data.FindProperty("selectionWallet").objectReferenceValue,"09-Panels-and-Effects/row-panel",30);
            Move("Level select/Level select card/Footer",0,-827,820,38);
            // Results use the same visual language; all existing reward/replay bindings survive.
            Panel("Results","Results card",0,0,960,1460);
            foreach(var star in (Image[])new Image[]{(Image)data.FindProperty("resultStars").GetArrayElementAtIndex(0).objectReferenceValue,(Image)data.FindProperty("resultStars").GetArrayElementAtIndex(1).objectReferenceValue,(Image)data.FindProperty("resultStars").GetArrayElementAtIndex(2).objectReferenceValue}) star.sprite=Sprite("09-Panels-and-Effects/star-gold");
            ButtonSkin(Find("Results/Results card/Result actions/Replay"),"gold"); ButtonSkin(((Button)data.FindProperty("<Next>k__BackingField").objectReferenceValue).transform,"green"); ButtonSkin(Find("Results/Results card/Result level select"),"blue");
            Skin(Find("Results/Results card/Round rewards"),"09-Panels-and-Effects/row-panel");

            var hand=(RectTransform)data.FindProperty("tapHand").FindPropertyRelative("hand").objectReferenceValue;
            Skin(hand,"03-Logo-and-Icons/Illustrated/hand",false);
            foreach(Transform child in hand) child.gameObject.SetActive(false);
            var nav=root.GetComponent<GuiNavigation>() ?? root.AddComponent<GuiNavigation>(); var navData=new SerializedObject(nav);
            var hp=Picture(frame,"Header pause","06-Controls/control-pause",-435,865,115,115); hp.raycastTarget=true;
            navData.FindProperty("headerPause").objectReferenceValue=Ensure<Button>(hp.gameObject);
            var wallet=Picture(frame,"Wallet shortcut","10-Individual-Parts/hud-score/hud-score-01-plate",340,865,320,90,true); wallet.raycastTarget=true;
            navData.FindProperty("walletButton").objectReferenceValue=Ensure<Button>(wallet.gameObject);
            Picture(wallet.transform,"Coin","03-Logo-and-Icons/Illustrated/coin",-105,0,65,65);
            navData.FindProperty("headerBalance").objectReferenceValue=Label(wallet.transform,"Balance","0",8,0,150,65,36);
            Picture(wallet.transform,"Plus","06-Controls/control-plus",120,0,60,60);
            hp.transform.SetSiblingIndex(3); wallet.transform.SetSiblingIndex(4);
            var close=Picture(Find("Level select"),"Close level selection","06-Controls/control-close",0,-866,115,115); close.raycastTarget=true;
            navData.FindProperty("selectionClose").objectReferenceValue=Ensure<Button>(close.gameObject);
            var walletOverlay=frame.Find("Wallet information") != null ? frame.Find("Wallet information").gameObject : new GameObject("Wallet information",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)); walletOverlay.transform.SetParent(frame,false); Stretch((RectTransform)walletOverlay.transform);
            walletOverlay.GetComponent<Image>().color=new Color(0,.05f,.12f,.65f);
            var wp=Picture(walletOverlay.transform,"Wallet card","10-Individual-Parts/hud-score/hud-score-01-plate",0,0,900,850,true);
            Label(wp.transform,"Wallet title","YOUR WALLET",0,305,800,100,62);
            navData.FindProperty("walletText").objectReferenceValue=Label(wp.transform,"Wallet details","",0,-10,780,440,38);
            var wc=Picture(wp.transform,"Close wallet","06-Controls/control-close",0,-330,100,100); wc.raycastTarget=true;
            navData.FindProperty("walletClose").objectReferenceValue=Ensure<Button>(wc.gameObject);
            navData.FindProperty("walletPanel").objectReferenceValue=walletOverlay; walletOverlay.SetActive(false);

            navData.ApplyModifiedPropertiesWithoutUndo();
            var visibility=root.GetComponent<GuiScreenVisibility>() ?? root.AddComponent<GuiScreenVisibility>(); var vd=new SerializedObject(visibility);
            vd.FindProperty("briefing").objectReferenceValue=Find("Level briefing").gameObject; vd.FindProperty("levels").objectReferenceValue=Find("Level select").gameObject; vd.FindProperty("results").objectReferenceValue=Find("Results").gameObject;
            var groups=new List<CanvasGroup>();
            foreach(Transform child in frame)
            {
                if(child.name=="Level briefing" || child.name=="Level select" || child.name=="Results" || child.name=="Paused" || child.name=="Countdown" || child.name=="Wallet information") continue;
                groups.Add(Ensure<CanvasGroup>(child.gameObject));
            }
            foreach(var group in groups) group.transform.SetSiblingIndex(Mathf.Min(group.transform.GetSiblingIndex(), Find("Level briefing").GetSiblingIndex()));
            var gp=vd.FindProperty("gameplay"); gp.arraySize=groups.Count; for(int i=0;i<groups.Count;i++) gp.GetArrayElementAtIndex(i).objectReferenceValue=groups[i]; vd.ApplyModifiedPropertiesWithoutUndo();

            var theme=root.GetComponent<GuiArenaTheme>() ?? root.AddComponent<GuiArenaTheme>(); var td=new SerializedObject(theme);
            td.FindProperty("arena").objectReferenceValue=Find("Target arena").GetComponent<Image>();
            td.FindProperty("background").objectReferenceValue=root.transform.Find("Background").GetComponent<Image>();
            td.FindProperty("arenaRim").objectReferenceValue=Find("Arena rim").gameObject;
            td.FindProperty("city").objectReferenceValue=Sprite("02-Backgrounds/city"); td.FindProperty("valley").objectReferenceValue=Sprite("02-Backgrounds/valley-reference"); td.FindProperty("waterfall").objectReferenceValue=Sprite("02-Backgrounds/pool"); td.ApplyModifiedPropertiesWithoutUndo();
            foreach(var pathToPill in new[]{"Level briefing/Level briefing card/Eyebrow","Paused/Paused card/Eyebrow","Level select/Level select card/World","Level select/Level select card/Title"})
            {
                var label=Find(pathToPill).GetComponent<TMP_Text>(); PlateBehind(label,"09-Panels-and-Effects/objective-plate",20);
                label.transform.parent.Find(label.name+" plate").GetComponent<Image>().color=new Color32(0,155,238,255); label.color=Color.white;
            }
            var caption=Find("Countdown/Countdown card/Caption").GetComponent<TMP_Text>(); caption.fontSharedMaterial=titleMaterial; caption.color=new Color32(255,226,56,255);
            var leftoverReward=brief.Find("Reward preview plate"); if(leftoverReward!=null) Object.DestroyImmediate(leftoverReward.gameObject);
            RefineReferenceLayout(root, brief, titleMaterial);
            RefineSelectionReference(titleMaterial);
            RefineScreenSpacing(root);
            data.FindProperty("punchStrength").floatValue=.08f;
            data.ApplyModifiedPropertiesWithoutUndo(); TypographyAuthoring.Apply(root); PreserveArtwork(root); PrefabUtility.SaveAsPrefabAsset(root,path); PrefabUtility.UnloadPrefabContents(root);

            var targetRoot = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Targets/NormalTarget.prefab");
            var targetData = new SerializedObject(targetRoot.GetComponent<TargetView>());
            var bodies = targetData.FindProperty("illustratedBodies"); bodies.arraySize=5;
            string[] bodyNames={"target","target-blue","target-green","target-gold","target-pink"};
            for(int i=0;i<5;i++) bodies.GetArrayElementAtIndex(i).objectReferenceValue=Sprite("04-Targets/Empty-Bases/"+bodyNames[i]);
            var bodyImage=(Image)targetData.FindProperty("hitImage").objectReferenceValue;
            bodyImage.sprite=(Sprite)bodies.GetArrayElementAtIndex(0).objectReferenceValue; bodyImage.color=Color.white;
            var normal=(GameObject)targetData.FindProperty("normalSymbol").objectReferenceValue;
            Skin(normal.transform,"09-Panels-and-Effects/star-gold",false);
            foreach(string field in new[]{"normalOutline","normalHighlight"})
                foreach(var img in ((GameObject)targetData.FindProperty(field).objectReferenceValue).GetComponentsInChildren<Image>(true)) img.enabled=false;
            foreach(var text in targetRoot.GetComponentsInChildren<TMP_Text>(true)) { text.font=font; text.fontSharedMaterial=font.material; }
            targetData.ApplyModifiedPropertiesWithoutUndo();
            TypographyAuthoring.Apply(targetRoot); PreserveArtwork(targetRoot); PrefabUtility.SaveAsPrefabAsset(targetRoot,"Assets/Prefabs/Targets/NormalTarget.prefab"); PrefabUtility.UnloadPrefabContents(targetRoot);

            AssetDatabase.SaveAssets(); File.WriteAllText("gui-authoring-results.txt","PASS GUI prefab authored and saved.\n"); EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e); File.WriteAllText("gui-authoring-results.txt","FAIL "+e); EditorApplication.Exit(1); }
    }
}
