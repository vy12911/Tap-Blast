// Editor-only authoring utility. Run in the disposable validation project;
// copy the reviewed saved prefab back afterwards. Never runs during gameplay.
// Restructures GameCanvas into HUD / Screens with one parent per visual section,
// fixes overlapping placements, styles Results and Wallet like the other cards
// and removes unreferenced legacy objects. Every binding is preserved by reference.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using TapBlast;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class CanvasLayoutAuthoring
{
    const string CanvasPath = "Assets/Prefabs/UI/GameCanvas.prefab";
    static readonly Vector2 Top = new Vector2(.5f, 1), Center = new Vector2(.5f, .5f), Bottom = new Vector2(.5f, 0);
    static readonly Color32 Ink = new Color32(13, 53, 100, 255), Caption = new Color32(0, 115, 182, 255), Muted = new Color32(78, 139, 169, 255);
    static readonly StringBuilder log = new StringBuilder();
    static GameObject root;

    static Transform Child(Transform parent, string path)
    {
        var t = parent.Find(path);
        if (t == null) throw new Exception("Missing " + AnimationUtility.CalculateTransformPath(parent, root.transform) + "/" + path);
        return t;
    }
    static float Tidy(float v) => Mathf.Round(v * 1000) / 1000;
    static void Place(Transform t, float x, float y, float w, float h)
    {
        var r = (RectTransform)t; r.anchorMin = r.anchorMax = r.pivot = Center;
        r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h); r.localScale = Vector3.one;
    }
    static RectTransform Stretch(Transform parent, string name)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        return r;
    }
    // Re-anchors a centred element without moving it.
    static void Anchor(RectTransform r, Vector2 anchor)
    {
        var parent = (RectTransform)r.parent;
        Vector2 center = r.anchoredPosition + parent.rect.min + Vector2.Scale(r.anchorMin, parent.rect.size);
        r.anchorMin = r.anchorMax = anchor;
        r.anchoredPosition = center - parent.rect.min - Vector2.Scale(anchor, parent.rect.size);
    }
    // New parent sized to its members' bounds; members keep their screen position and given render order.
    // Local-space math only: prefab contents have no reliable world scale in batch mode.
    static readonly HashSet<string> SectionNames = new HashSet<string> { "Header", "Level title", "Stats", "Objective", "Objective label", "Mechanics", "Arena",
        "Tutorial hint", "Frame", "Hero", "Goal", "Star rules", "Reward", "Actions", "Wallet", "Level list", "Count", "Star rating", "Score", "Rewards", "Content" };
    static RectTransform Section(Transform parent, string name, Vector2 anchor, params string[] members) =>
        Section(parent, name, anchor, members.Select(m => (RectTransform)Child(parent, m)).ToArray());
    static RectTransform Section(Transform parent, string name, Vector2 anchor, params RectTransform[] members)
    {
        var group = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        group.SetParent(parent, false); group.anchorMin = group.anchorMax = group.pivot = Center;
        group.anchoredPosition = Vector2.zero; group.sizeDelta = Vector2.zero;
        group.SetAsLastSibling(); // Sections are created back-to-front, top-to-bottom.
        foreach (var m in members) Reparent(m, group);
        Fit(group); Anchor(group, anchor);
        return group;
    }
    static void Reparent(RectTransform member, RectTransform group)
    {
        if (member.anchorMin != member.anchorMax) throw new Exception("Stretched member cannot move into a section: " + member.name);
        Vector3 local = member.localPosition - group.localPosition;
        member.SetParent(group, false); member.SetAsLastSibling(); member.localPosition = local;
        member.anchoredPosition = new Vector2(Tidy(member.anchoredPosition.x), Tidy(member.anchoredPosition.y));
    }
    // Resizes a section to its children's bounds without moving anything on screen.
    static int restoredTransforms;
    static void Fit(RectTransform group)
    {
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        foreach (RectTransform child in group)
        {
            // Authored Canvas elements are unscaled and unrotated at rest; repair any baked world transform.
            if (child.localScale != Vector3.one || child.localRotation != Quaternion.identity || child.localPosition.z != 0)
            {
                child.localScale = Vector3.one; child.localRotation = Quaternion.identity;
                child.localPosition = new Vector3(child.localPosition.x, child.localPosition.y, 0); restoredTransforms++;
            }
            Vector2 at = child.localPosition, scale = child.localScale;
            min = Vector2.Min(min, at + Vector2.Scale(child.rect.min, scale)); max = Vector2.Max(max, at + Vector2.Scale(child.rect.max, scale));
        }
        if (min.x > max.x) return;
        Vector3 center = (min + max) * .5f;
        var children = group.Cast<RectTransform>().ToList();
        var targets = children.Select(c => c.localPosition - center).ToList();
        // Resize before restoring children: resizing moves any child anchored to an edge.
        group.localPosition += center;
        group.sizeDelta = new Vector2(Tidy(max.x - min.x), Tidy(max.y - min.y));
        for (int i = 0; i < children.Count; i++) children[i].localPosition = targets[i];
        group.anchoredPosition = new Vector2(Tidy(group.anchoredPosition.x), Tidy(group.anchoredPosition.y));
        foreach (RectTransform child in group) child.anchoredPosition = new Vector2(Tidy(child.anchoredPosition.x), Tidy(child.anchoredPosition.y));
    }
    // Fits every section container, innermost first (sections are plain RectTransforms with children).
    static void FitSections(Transform frame)
    {
        var sections = frame.GetComponentsInChildren<RectTransform>(true)
            .Where(r => SectionNames.Contains(r.name) && r.childCount > 0 && r.anchorMin == r.anchorMax &&
                        r.GetComponent<Graphic>() == null && r.GetComponent<LayoutGroup>() == null)
            .OrderByDescending(r => r.GetComponentsInParent<Transform>(true).Length).ToList();
        restoredTransforms = 0;
        foreach (var section in sections) Fit(section);
        log.AppendLine("Fitted " + sections.Count + " section containers to their contents; restored " + restoredTransforms + " unit transforms.");
    }
    static T Copy<T>(Transform source, Transform parent, string name) where T : Component
    {
        var copy = Object.Instantiate(source.gameObject, parent, false); copy.name = name;
        return copy.GetComponent<T>();
    }
    static void Style(TMP_Text from, TMP_Text to, float size)
    {
        to.font = from.font; to.fontSharedMaterial = from.fontSharedMaterial; to.color = from.color;
        to.enableVertexGradient = from.enableVertexGradient; to.colorGradient = from.colorGradient;
        to.extraPadding = from.extraPadding; to.fontStyle = from.fontStyle;
        to.enableAutoSizing = true; to.fontSize = to.fontSizeMax = size; to.fontSizeMin = Mathf.Min(20, size);
        to.alignment = TextAlignmentOptions.Center;
    }
    static void Nudge(Transform t, float up) { var r = (RectTransform)t; r.anchoredPosition += Vector2.up * up; }
    static void Tone(TMP_Text label, Color color, float size) { label.color = color; label.fontSize = label.fontSizeMax = size; label.fontSizeMin = Mathf.Min(20, size); }

    // True when a component outside `owners` serializes a reference to one of `targets`
    // (hierarchy links such as m_Father / m_Children are not references).
    static bool Referenced(HashSet<Object> targets, HashSet<Object> owners)
    {
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null || owners.Contains(component)) continue;
            var property = new SerializedObject(component).GetIterator();
            while (property.Next(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null &&
                    property.propertyPath != "m_GameObject" && property.propertyPath != "m_Father" && !property.propertyPath.StartsWith("m_Children") &&
                    targets.Contains(property.objectReferenceValue))
                    return true;
        }
        return false;
    }
    static bool SubtreeReferenced(GameObject candidate)
    {
        var components = new HashSet<Object>(candidate.GetComponentsInChildren<Component>(true));
        var targets = new HashSet<Object>(components.Concat(candidate.GetComponentsInChildren<Transform>(true).Select(t => (Object)t.gameObject)));
        return Referenced(targets, components);
    }
    static void RemoveUnused(Transform frame, params string[] paths)
    {
        foreach (var path in paths)
        {
            var t = frame.Find(path);
            if (t == null) continue;
            if (SubtreeReferenced(t.gameObject)) { log.AppendLine("KEPT referenced " + path); continue; }
            Object.DestroyImmediate(t.gameObject); log.AppendLine("Removed unused " + path);
        }
    }

    static void PlaceHud(Transform frame, SerializedObject hud)
    {
        // Single pause control: the header button becomes GameHUD.Pause.
        var small = frame.Find("Pause button"); var header = frame.Find("Header pause");
        if (small != null && header != null)
        {
            hud.FindProperty("<Pause>k__BackingField").objectReferenceValue = header.GetComponent<Button>();
            Object.DestroyImmediate(small.gameObject); header.name = "Pause button";
        }
        Child(frame, "World plate").name = "Level title plate"; Child(frame, "World").name = "Level title";
        Child(frame, "World 2 progress").name = "Mechanics status";
        // Reference-frame coordinates (1080 x 1920): no overlaps between logo, title, stats and objective.
        Place(Child(frame, "Illustrated logo"), 0, 812, 440, 224);
        Place(Child(frame, "Level title plate"), 0, 668, 542, 60); Place(Child(frame, "Level title"), 0, 668, 530, 48);
        Place(Child(frame, "Score card"), -290, 525, 410, 144);
        Place(Child(frame, "Level card"), 105, 525, 280, 144);
        Place(Child(frame, "Timer card"), 395, 525, 200, 200);
        Place(Child(frame, "Objective plate"), 0, 415, 360, 66); Place(Child(frame, "Objective"), 0, 415, 340, 58);
        var track = Child(frame, "Objective track"); Place(track, 0, 352, 940, 58);
        for (int i = 1; i <= 3; i++) Child(frame, "Objective star " + i).SetParent(track, false);
        CenterObjectiveStars(track);
        Place(Child(frame, "Mechanics status"), -110, 308, 690, 36); Place(Child(frame, "Shield"), 375, 308, 200, 36);
    }

    // Star markers sit on the objective track's centre line, at their authored x thresholds.
    static void CenterObjectiveStars(Transform track)
    {
        for (int i = 1; i <= 3; i++)
        {
            var star = (RectTransform)Child(track, "Objective star " + i);
            star.anchorMin = star.anchorMax = star.pivot = Center; star.localScale = Vector3.one; star.localRotation = Quaternion.identity;
            star.anchoredPosition = new Vector2(star.anchoredPosition.x, 0);
        }
    }

    static void StyleResults(Transform briefing, Transform card)
    {
        var eyebrowPlate = Copy<Image>(Child(briefing, "Eyebrow plate"), card, "Eyebrow plate"); Place(eyebrowPlate.transform, 0, 612, 460, 64);
        var eyebrow = Child(card, "Eyebrow").GetComponent<TMP_Text>(); Style(Child(briefing, "Eyebrow").GetComponent<TMP_Text>(), eyebrow, 34); Place(eyebrow.transform, 0, 612, 420, 48);
        var title = Child(card, "Title").GetComponent<TMP_Text>(); Style(Child(briefing, "Name").GetComponent<TMP_Text>(), title, 120); Place(title.transform, 0, 500, 680, 130);
        Copy<Image>(Child(briefing, "Title star left"), card, "Title star left").rectTransform.anchoredPosition = new Vector2(-390, 490);
        Copy<Image>(Child(briefing, "Title star right"), card, "Title star right").rectTransform.anchoredPosition = new Vector2(390, 490);
        for (int i = 1; i <= 3; i++) Place(Child(card, "Star " + i), -195 + (i - 1) * 195, 330, 150, 150);
        var scorePlate = Copy<Image>(Child(briefing, "Goal plate"), card, "Score plate"); Place(scorePlate.transform, 0, 100, 760, 250);
        Tone(Child(card, "Score caption").GetComponent<TMP_Text>(), Caption, 34); Place(Child(card, "Score caption"), 0, 190, 600, 44);
        var score = Child(card, "Score").GetComponent<TMP_Text>(); Style(Child(briefing, "Name").GetComponent<TMP_Text>(), score, 120); Place(score.transform, 0, 105, 600, 120);
        Tone(Child(card, "Personal best").GetComponent<TMP_Text>(), Muted, 32); Place(Child(card, "Personal best"), 0, 22, 600, 44);
        var statsPlate = Copy<Image>(Child(briefing, "Goal plate"), card, "Stats plate"); Place(statsPlate.transform, 0, -132, 760, 170);
        var stats = Child(card, "Stats").GetComponent<TMP_Text>(); Tone(stats, Ink, 34); Place(stats.transform, 0, -132, 620, 140);
        stats.alignment = TextAlignmentOptions.MidlineLeft;
        Tone(Child(card, "Mastery hint").GetComponent<TMP_Text>(), Caption, 34);
    }

    static void StyleWallet(Transform overlay, Transform paused)
    {
        overlay.GetComponent<Image>().color = new Color(.015f, .08f, .16f, .6f);
        // Same footprint as the Paused card, which it covers when opened from pause.
        var card = Child(overlay, "Wallet card"); Place(card, 0, -135, 880, 1100);
        var skin = card.GetComponent<Image>(); var source = paused.GetComponent<Image>();
        skin.sprite = source.sprite; skin.type = source.type; skin.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier; skin.color = source.color;
        if (card.GetComponent<CanvasGroup>() == null) card.gameObject.AddComponent<CanvasGroup>();
        if (card.GetComponent<GuiPanelFade>() == null) card.gameObject.AddComponent<GuiPanelFade>();
        var mask = Copy<RectTransform>(Child(paused, "Scenery mask"), card, "Scenery mask"); Place(mask, 0, 0, 880, 1100); Place(Child(mask, "Scenery"), 0, 0, 880, 1100);
        mask.SetAsFirstSibling();
        var rim = Copy<RectTransform>(Child(paused, "Illustrated outer rim"), card, "Illustrated outer rim"); Place(rim, 0, 0, 900, 1120); rim.SetSiblingIndex(1);
        var title = Child(card, "Wallet title").GetComponent<TMP_Text>(); Style(Child(paused, "Title").GetComponent<TMP_Text>(), title, 104); Place(title.transform, 0, 400, 620, 130);
        Copy<Image>(Child(paused, "Pause star left"), card, "Title star left").rectTransform.anchoredPosition = new Vector2(-372, 385);
        Copy<Image>(Child(paused, "Pause star right"), card, "Title star right").rectTransform.anchoredPosition = new Vector2(372, 385);
        Place(Copy<Image>(Child(paused, "Eyebrow plate"), card, "Eyebrow plate").transform, 0, 295, 380, 65);
        var eyebrow = Copy<TMP_Text>(Child(paused, "Eyebrow"), card, "Eyebrow"); eyebrow.text = "PLAYER PROFILE"; Place(eyebrow.transform, 0, 295, 350, 48);
        Place(Copy<Image>(Child(paused, "Detail plate"), card, "Details plate").transform, 0, -25, 760, 480);
        var details = Child(card, "Wallet details").GetComponent<TMP_Text>(); Tone(details, Ink, 44); Place(details.transform, 0, -25, 700, 440);
        Place(Child(card, "Close wallet"), 0, -445, 120, 120);
    }

    static void GroupScreens(Transform screens)
    {
        var brief = Child(screens, "Level briefing/Level briefing card");
        Nudge(Child(brief, "Best"), 10); // Clear of its plate's lower border.
        Section(brief, "Frame", Center, "Illustrated outer rim");
        Section(brief, "Header", Top, "Briefing logo", "Back to levels", "Eyebrow plate", "Eyebrow", "Name", "Title star left", "Title star right");
        Section(brief, "Hero", Center, "Hero target", "Target size preview", "Hero star left", "Hero star low", "Hero star right");
        Section(brief, "Goal", Center, "Goal plate", "Goal", "Description");
        Section(brief, "Star rules", Center, "Mastery card", "Mastery divider 0", "Mastery divider 1", "Mastery star 0-0", "Mastery star 1-0", "Mastery star 1-1",
            "Mastery star 2-0", "Mastery star 2-1", "Mastery star 2-2", "Star rules", "Two star requirement", "Three star requirement");
        Section(brief, "Reward", Center, "Reward background", "Reward coin", "Reward coin two", "Reward coin three", "Reward caption", "Reward preview", "Reward divider", "Reward XP", "XP reward");
        Section(brief, "Actions", Bottom, "Start level");
        Section(brief, "Wallet", Bottom, "Best plate", "Best", "Wallet coin", "Wallet XP", "Wallet coins", "Wallet experience");

        var select = Child(screens, "Level select/Level select card");
        Child(select, "World plate").name = "Eyebrow plate"; Child(select, "World").name = "Eyebrow";
        Nudge(Child(select, "Footer"), 12); // Clear of the wallet plate's lower border.
        Section(select, "Frame", Center, "Illustrated outer rim");
        Section(select, "Header", Top, "Selection logo", "Title plate", "Selection star left", "Selection star right", "Eyebrow plate", "Eyebrow", "Title", "Description");
        Section(select, "Level list", Center, "Level list viewport", "Level list scrollbar");
        Section(select, "Wallet", Bottom, "Wallet plate", "Selection coins left", "Selection coins right", "Selection coins top", "Selection XP icon",
            "Selection wallet divider", "Wallet", "Selection experience", "Footer");

        // Centred between the stats row and combo card; the objective is hidden beneath it.
        var count = Child(screens, "Countdown/Countdown card"); Place(count, 0, -145, 930, 790);
        Section(count, "Frame", Center, "Scenery mask", "Illustrated outer rim");
        Section(count, "Header", Top, "Caption", "Countdown star left", "Countdown star right");
        Section(count, "Count", Center, "Countdown bubble", "Count", "Countdown star middle left", "Countdown star middle right", "Countdown star low");
        Section(count, "Goal", Bottom, "Goal plate", "Goal");

        // Rim clears the timer above and the combo card below.
        var paused = Child(screens, "Paused/Paused card"); Place(paused, 0, -135, 880, 1100);
        Section(paused, "Frame", Center, "Scenery mask", "Illustrated outer rim");
        Section(paused, "Header", Top, "Pause leaves left", "Pause leaves right", "Title", "Pause star left", "Pause star right", "Eyebrow plate", "Eyebrow", "Detail plate", "Detail");
        Section(paused, "Actions", Bottom, "Resume", "Restart", "Sound", "Motion", "Vibration", "Exit to levels");

        var results = Child(screens, "Results/Results card");
        Section(results, "Frame", Center, "Illustrated outer rim");
        Section(results, "Header", Top, "Eyebrow plate", "Eyebrow", "Title star left", "Title star right", "Title");
        Section(results, "Star rating", Center, "Star 1", "Star 2", "Star 3");
        Section(results, "Score", Center, "Score plate", "Score caption", "Score", "Personal best");
        Section(results, "Stats", Center, "Stats plate", "Stats");
        Section(results, "Rewards", Center, "Round rewards", "Mastery hint");
        Section(results, "Actions", Bottom, "Result actions", "Result level select");

        var wallet = Child(screens, "Wallet information/Wallet card");
        Section(wallet, "Frame", Center, "Scenery mask", "Illustrated outer rim");
        Section(wallet, "Header", Top, "Title star left", "Title star right", "Wallet title", "Eyebrow plate", "Eyebrow");
        Section(wallet, "Content", Center, "Details plate", "Wallet details");
        Section(wallet, "Actions", Bottom, "Close wallet");
    }

    static void Dump(Transform t, int depth, StringBuilder text)
    {
        var r = t as RectTransform;
        text.Append(new string(' ', depth * 2)).Append(t.name).Append(t.gameObject.activeSelf ? "" : " [off]");
        if (depth > 0 && r != null) text.Append("  pos ").Append(r.anchoredPosition).Append(" size ").Append(r.sizeDelta).Append(" anchor ").Append(r.anchorMin);
        text.AppendLine();
        if (t.name.StartsWith("Select Level") || t.name.StartsWith("Tap feedback") || t.name == "Completion confetti" || t.name == "Scenery mask") return;
        foreach (Transform child in t) Dump(child, depth + 1, text);
    }

    public static void Run()
    {
        try
        {
            root = PrefabUtility.LoadPrefabContents(CanvasPath);
            var frame = root.transform.Find("Device safe area/Portrait frame");
            if (frame.Find("HUD") == null) Restructure(frame);
            else log.AppendLine("Canvas already uses HUD / Screens; normalizing sections only.");
            CenterObjectiveStars(Child(frame, "HUD/Objective/Objective track"));
            FitSections(frame);

            PrefabUtility.SaveAsPrefabAsset(root, CanvasPath);
            var tree = new StringBuilder(); Dump(root.transform, 0, tree);
            PrefabUtility.UnloadPrefabContents(root); AssetDatabase.SaveAssets();
            File.WriteAllText("canvas-layout-tree.txt", tree.ToString());
            File.WriteAllText("canvas-layout-authoring.txt", "PASS Canvas uses HUD / Screens sections.\n" + log);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("canvas-layout-authoring.txt", "FAIL " + e + "\n" + log); EditorApplication.Exit(1); }
    }

    // ---------------------------------------------------------------------------------
    // UX layout pass: every element placed in Portrait-frame coordinates (1080 x 1920,
    // origin at the centre) on a 16 px rhythm with 24 px outer margins. Touch targets are
    // at least ~116 reference px (44pt / 48dp on a 1080-wide phone); text meets 4.5:1.
    // ---------------------------------------------------------------------------------
    static Transform uxFrame;
    // Frame-space offset of a transform's local origin, accumulated with local math only.
    static Vector2 FrameOffset(Transform t)
    {
        Vector2 offset = Vector2.zero;
        for (; t != null && t != uxFrame; t = t.parent) offset += (Vector2)t.localPosition;
        return offset;
    }
    static RectTransform At(string path, float x, float y, float w, float h)
    {
        var r = (RectTransform)Child(uxFrame, path);
        r.localScale = Vector3.one; r.localRotation = Quaternion.identity; r.sizeDelta = new Vector2(w, h);
        Vector2 pivot = new Vector2(x, y) + Vector2.Scale(r.pivot - Center, r.sizeDelta) - FrameOffset(r.parent);
        r.localPosition = new Vector3(pivot.x, pivot.y, 0);
        r.anchoredPosition = new Vector2(Tidy(r.anchoredPosition.x), Tidy(r.anchoredPosition.y));
        return r;
    }
    static Vector2 Where(string path) { var r = (RectTransform)Child(uxFrame, path); return FrameOffset(r.parent) + (Vector2)r.localPosition - Vector2.Scale(r.pivot - Center, r.sizeDelta); }
    // Simple-type button art keeps its aspect; size the rect to the art so nothing letterboxes.
    static float ArtWidth(string path, float height, float maxWidth)
    {
        var image = Child(uxFrame, path).GetComponent<Image>();
        float aspect = image != null && image.sprite != null ? image.sprite.rect.width / image.sprite.rect.height : maxWidth / height;
        return Mathf.Min(maxWidth, Mathf.Round(height * aspect));
    }
    // Scales a pause-style action button (icon left, label right) from its 700 x 130 design.
    static void ActionButton(string path, float y, float height)
    {
        float width = ArtWidth(path, height, 700), s = height / 130f;
        var button = At(path, Where(path).x, y, width, height);
        var label = button.GetComponentInChildren<TMP_Text>(true);
        var icon = button.Find("Button icon");
        Place(label.transform, 45 * s, 3 * s, width - 145 * s, 90 * s);
        label.fontSize = label.fontSizeMax = Mathf.Max(36, Mathf.Round(48 * s)); label.fontSizeMin = 28;
        if (icon != null) Place(icon, -width * .33f, 3 * s, 58 * s, 58 * s);
    }
    static void Recolor(string path, Color32 color) => Child(uxFrame, path).GetComponent<TMP_Text>().color = color;

    static void UxHud()
    {
        const string H = "HUD/";
        // Header row: one pause control, compact logo, wallet. Logo is decorative during play.
        At(H + "Header/Pause button", -436, 872, 120, 120);
        At(H + "Header/Illustrated logo", 0, 872, 300, 152);
        var wallet = (RectTransform)Child(uxFrame, H + "Header/Wallet shortcut");
        var walletImage = wallet.GetComponent<Image>();
        if (walletImage != null && walletImage.type == Image.Type.Sliced)
        {
            At(H + "Header/Wallet shortcut", 356, 872, 300, 104);
            Place(Child(wallet, "Coin"), -108, 0, 76, 76); Place(Child(wallet, "Balance"), 8, 0, 160, 76); Place(Child(wallet, "Plus"), 118, 0, 70, 70);
        }
        else { At(H + "Header/Wallet shortcut", 356, 872, wallet.sizeDelta.x, wallet.sizeDelta.y); log.AppendLine("Wallet shortcut art is not sliced; kept its size."); }
        At(H + "Header/Level title/Level title plate", 0, 764, 542, 56); At(H + "Header/Level title/Level title", 0, 764, 530, 46);
        // Stats row, evenly inset from both edges.
        At(H + "Stats/Score card", -290, 620, 410, 144); At(H + "Stats/Level card", 105, 620, 280, 144); At(H + "Stats/Timer card", 395, 620, 200, 200);
        // Objective: label above its track.
        At(H + "Objective/Objective label/Objective plate", 0, 474, 360, 60); At(H + "Objective/Objective label/Objective", 0, 474, 340, 52);
        At(H + "Objective/Objective track", 0, 407, 940, 58);
        // Mechanics band: Shield becomes a real touch target (was 36 px tall).
        var status = At(H + "Mechanics/Mechanics status", -170, 314, 600, 48).GetComponent<TMP_Text>();
        status.fontSize = status.fontSizeMax = 32; status.fontSizeMin = 24;
        var shield = At(H + "Mechanics/Shield", 330, 314, 320, 96);
        var shieldImage = shield.GetComponent<Image>(); shieldImage.type = Image.Type.Sliced; shieldImage.preserveAspect = true; shieldImage.pixelsPerUnitMultiplier = 3;
        var shieldLabel = shield.GetComponentInChildren<TMP_Text>(true); Place(shieldLabel.transform, 0, 2, 290, 64);
        shieldLabel.fontSize = shieldLabel.fontSizeMax = 36; shieldLabel.fontSizeMin = 24;
        // Arena fills the space between the mechanics band and the hint, 16 px either side.
        At(H + "Arena/Target arena", 0, -187, 970, 854); At(H + "Arena/Target arena/Arena scenery", 0, -187, 970, 854);
        At(H + "Arena/Arena rim", 0, -187, 1000, 874); At(H + "Arena/Pooled feedback", 0, -187, 970, 854);
        // Bottom: hint above the combo card, 24 px bottom margin.
        At(H + "Tutorial hint/Tutorial hint plate", 0, -700, 800, 120);
        At(H + "Tutorial hint/Tutorial hint", 0, -676, 760, 50); At(H + "Tutorial hint/Tutorial detail", 0, -724, 760, 42);
        At(H + "Combo card", 0, -856, 990, 160);
    }

    static void UxScreens()
    {
        const string S = "Screens/";
        // Modal cards share the clear area between the stats row (bottom 520) and combo card (top -776).
        const float ModalY = -128;
        string paused = S + "Paused/Paused card", wallet = S + "Wallet information/Wallet card", count = S + "Countdown/Countdown card";
        At(paused, 0, ModalY, 880, 1100); At(wallet, 0, ModalY, 880, 1100); At(count, 0, ModalY, 930, 790);
        // Pause: one primary action, a secondary restart, a compact settings group, exit set apart.
        ActionButton(paused + "/Actions/Resume", ModalY + 170, 130);
        ActionButton(paused + "/Actions/Restart", ModalY + 32, 112);
        ActionButton(paused + "/Actions/Sound", ModalY - 94, 96);
        ActionButton(paused + "/Actions/Motion", ModalY - 202, 96);
        ActionButton(paused + "/Actions/Vibration", ModalY - 310, 96);
        ActionButton(paused + "/Actions/Exit to levels", ModalY - 440, 112);

        // Briefing: 48dp back control and a 120 px primary CTA.
        string brief = S + "Level briefing/Level briefing card";
        var back = Where(brief + "/Header/Back to levels"); At(brief + "/Header/Back to levels", back.x, back.y, 120, 120);
        var start = Where(brief + "/Actions/Start level");
        float startWidth = ArtWidth(brief + "/Actions/Start level", 120, 640);
        var startButton = At(brief + "/Actions/Start level", start.x, start.y, startWidth, 120);
        Place(startButton.Find("Button icon"), startWidth * .35f, 3, 56, 56);
        var startLabel = startButton.GetComponentInChildren<TMP_Text>(true); Place(startLabel.transform, -35, 4, startWidth - 150, 96);
        foreach (string path in new[] { "/Goal/Description", "/Wallet/Best" }) Recolor(brief + path, new Color32(36, 92, 124, 255));
        Recolor(brief + "/Reward/Reward caption", new Color32(0, 92, 150, 255));

        // Results: taller card so the actions get full-size targets.
        string results = S + "Results/Results card";
        At(results, 0, -20, 960, 1580); At(results + "/Frame/Illustrated outer rim", 0, -20, 980, 1600);
        At(results + "/Header/Eyebrow plate", 0, 690, 460, 64); At(results + "/Header/Eyebrow", 0, 690, 420, 48);
        At(results + "/Header/Title", 0, 580, 680, 130);
        At(results + "/Header/Title star left", -390, 570, 64, 64); At(results + "/Header/Title star right", 390, 570, 64, 64);
        for (int i = 1; i <= 3; i++) At(results + "/Star rating/Star " + i, -195 + (i - 1) * 195, 410, 150, 150);
        At(results + "/Score/Score plate", 0, 190, 760, 250); At(results + "/Score/Score caption", 0, 280, 600, 44);
        At(results + "/Score/Score", 0, 195, 600, 120); At(results + "/Score/Personal best", 0, 112, 600, 44);
        At(results + "/Stats/Stats plate", 0, -45, 760, 170); At(results + "/Stats/Stats", 0, -45, 620, 140);
        At(results + "/Rewards/Round rewards", 0, -235, 816, 178.5f); At(results + "/Rewards/Mastery hint", 0, -370, 802, 60);
        var actions = At(results + "/Actions/Result actions", 0, -480, 780, 120);
        foreach (Transform button in actions)
        {
            var r = (RectTransform)button; r.sizeDelta = new Vector2(r.sizeDelta.x, 120);
            var label = button.GetComponentInChildren<TMP_Text>(true); Place(label.transform, 0, 4, r.sizeDelta.x - 32, 96);
        }
        float levelsWidth = ArtWidth(results + "/Actions/Result level select", 104, 600);
        var levels = At(results + "/Actions/Result level select", 0, -610, levelsWidth, 104);
        var levelsLabel = levels.GetComponentInChildren<TMP_Text>(true); Place(levelsLabel.transform, 0, 4, levelsWidth - 48, 76);
        levelsLabel.fontSize = levelsLabel.fontSizeMax = 46; levelsLabel.fontSizeMin = 30;
        Recolor(results + "/Score/Score caption", new Color32(0, 92, 150, 255)); Recolor(results + "/Rewards/Mastery hint", new Color32(0, 92, 150, 255));
        Recolor(results + "/Score/Personal best", new Color32(36, 92, 124, 255));

        // Level select: footer readable on its white plate.
        Recolor(S + "Level select/Level select card/Wallet/Footer", new Color32(0, 104, 150, 255));
    }

    public static void RunUxPass()
    {
        try
        {
            root = PrefabUtility.LoadPrefabContents(CanvasPath);
            uxFrame = root.transform.Find("Device safe area/Portrait frame");
            if (uxFrame.Find("HUD") == null) throw new Exception("Run LayoutAuthor first: the UX pass expects the HUD / Screens hierarchy.");
            UxHud(); UxScreens();
            FitSections(uxFrame);
            PrefabUtility.SaveAsPrefabAsset(root, CanvasPath);
            var tree = new StringBuilder(); Dump(root.transform, 0, tree);
            PrefabUtility.UnloadPrefabContents(root); AssetDatabase.SaveAssets();
            File.WriteAllText("canvas-layout-tree.txt", tree.ToString());
            File.WriteAllText("canvas-ux-authoring.txt", "PASS UX layout pass applied.\n" + log);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("canvas-ux-authoring.txt", "FAIL " + e + "\n" + log); EditorApplication.Exit(1); }
    }

    static void Restructure(Transform frame)
    {
        {
            var hud = new SerializedObject(root.GetComponent<GameHUD>());
            var visibility = new SerializedObject(root.GetComponent<GuiScreenVisibility>());
            var oldGroups = new List<CanvasGroup>();
            foreach (string array in new[] { "gameplay", "modalObscured" })
            {
                var list = visibility.FindProperty(array);
                for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue is CanvasGroup g) oldGroups.Add(g);
                list.arraySize = 0; // Rebound to section groups below.
            }
            visibility.ApplyModifiedPropertiesWithoutUndo();

            RemoveUnused(frame, "Teal atmosphere", "Blue atmosphere", "Wordmark", "Target arena/Neon City backdrop", "Combo card/Tip");
            PlaceHud(frame, hud);
            StyleResults(Child(frame, "Level briefing/Level briefing card"), Child(frame, "Results/Results card"));
            StyleWallet(Child(frame, "Wallet information"), Child(frame, "Paused/Paused card"));

            // HUD sections, back to front.
            var header = Section(frame, "Header", Top, (RectTransform)Child(frame, "Illustrated logo"),
                Section(frame, "Level title", Top, "Level title plate", "Level title"),
                (RectTransform)Child(frame, "Pause button"), (RectTransform)Child(frame, "Wallet shortcut"));
            var stats = Section(frame, "Stats", Top, "Score card", "Level card", "Timer card");
            var objective = Section(frame, "Objective", Top, (RectTransform)Child(frame, "Objective track"), Section(frame, "Objective label", Top, "Objective plate", "Objective"));
            var mechanics = Section(frame, "Mechanics", Top, "Mechanics status", "Shield");
            var arena = Section(frame, "Arena", Center, "Target arena", "Arena rim", "Pooled feedback");
            var hint = Section(frame, "Tutorial hint", Bottom, "Tutorial hint plate", "Tutorial hint", "Tutorial detail");
            var combo = (RectTransform)Child(frame, "Combo card"); Anchor(combo, Bottom);

            var hudRoot = Stretch(frame, "HUD"); hudRoot.SetSiblingIndex(0);
            foreach (var section in new[] { header, stats, objective, mechanics, arena, hint, combo }) { section.SetParent(hudRoot, false); section.SetAsLastSibling(); }
            var screens = Stretch(frame, "Screens"); screens.SetSiblingIndex(1);
            foreach (string name in new[] { "Level select", "Level briefing", "Countdown", "Paused", "Results", "Wallet information" })
            { var overlay = Child(frame, name); overlay.SetParent(screens, false); overlay.SetAsLastSibling(); }
            var leftovers = string.Join(", ", frame.Cast<Transform>().Where(t => t != hudRoot && t != screens).Select(t => t.name));
            if (leftovers.Length > 0) throw new Exception("Unsorted Portrait frame children: " + leftovers);
            GroupScreens(screens);

            // Visibility: one HUD group, plus sections hidden beneath Pause / Countdown cards.
            var gameplay = visibility.FindProperty("gameplay"); gameplay.arraySize = 1;
            gameplay.GetArrayElementAtIndex(0).objectReferenceValue = hudRoot.gameObject.AddComponent<CanvasGroup>();
            var obscured = new[] { objective, mechanics, arena, hint };
            var modal = visibility.FindProperty("modalObscured"); modal.arraySize = obscured.Length;
            for (int i = 0; i < obscured.Length; i++) modal.GetArrayElementAtIndex(i).objectReferenceValue = obscured[i].gameObject.AddComponent<CanvasGroup>();
            visibility.ApplyModifiedPropertiesWithoutUndo(); hud.ApplyModifiedPropertiesWithoutUndo();
            int removedGroups = 0;
            foreach (var group in oldGroups)
                if (group != null && group.GetComponent<GuiPanelFade>() == null && !Referenced(new HashSet<Object> { group }, new HashSet<Object>()))
                { Object.DestroyImmediate(group); removedGroups++; }
            log.AppendLine("Removed " + removedGroups + " per-element visibility CanvasGroups now covered by section groups.");
        }
    }
}
