// Oberfläche wie im Browser: Kopfzeile, Zielkarte, Meldungen, Handbehälter, Karte, untere Leiste und Fenster.
// Wird zur Laufzeit gebaut (Referenzgröße 390 x 844 wie die Browser-CSS-Pixel), damit alle Maße 1:1 übernommen werden.
using System;
using System.Collections.Generic;
using System.Linq;
using LunarCleanup.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LunarCleanup.UI
{
    public class Hud : MonoBehaviour
    {
        public static Hud I;
        public Action<string> OnNav;              // Klick in der unteren Leiste
        public Action OnMap, OnFocusQuest;
        RectTransform root, sheetPanel, sheetContent, toastBox, floatLayer;
        TextMeshProUGUI credits, rate, pname, pprog, goalKick, goalText, goalReward, handTxt, sheetKick, sheetTitle, offlineTxt;
        RectTransform goalFill, handFill; Image handImg, pdot; GameObject sheetGo, offlineGo, goalGo;
        readonly List<Action> liveLabels = new List<Action>(); Action lowWarn;
        readonly List<(TextMeshProUGUI v, RectTransform fill, Func<string> val, Func<float> f)> meters = new List<(TextMeshProUGUI, RectTransform, Func<string>, Func<float>)>();
        readonly Dictionary<string, Image> navIcons = new Dictionary<string, Image>(); readonly Dictionary<string, TextMeshProUGUI> navTxt = new Dictionary<string, TextMeshProUGUI>();
        readonly List<Action> binders = new List<Action>();
        string sheetKind, sheetArg; float bindT, lastCred; double credShown;
        public bool SheetOpen => sheetGo && sheetGo.activeSelf;

        public void Build(Transform canvas)
        {
            I = this; UiKit.Init();
            root = UiKit.Rt(canvas, "SafeArea").Anchor(0, 0, 1, 1); root.gameObject.AddComponent<SafeAreaFitter>();
            floatLayer = UiKit.Rt(root, "Schwebetexte").Anchor(0, 0, 1, 1);
            BuildTop(); BuildGoal(); BuildBottom(); BuildSheet(); BuildOffline();
            toastBox = UiKit.Rt(root, "Meldungen").Anchor(0, 1, 1, 1, 24, -330, 24, 196);
            var vl = toastBox.gameObject.AddComponent<VerticalLayoutGroup>(); vl.spacing = 6; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.childAlignment = TextAnchor.UpperCenter;
            Game.OnToast += Toast;
        }
        void OnDestroy() { Game.OnToast -= Toast; }

        // ---------- Kopfzeile ----------
        void BuildTop()
        {
            var top = UiKit.Rt(root, "Kopf").Anchor(0, 1, 1, 1, 12, -62, 12, 10);
            var cr = UiKit.Glass(top, "Credits"); cr.rectTransform.Anchor(0, 0, .42f, 1);
            UiKit.Img(cr.transform, "Münze", UiKit.Circle(Pal.C("#fff2c4"), Pal.C("#c77d12")), Color.white).rectTransform.Anchor(0, .5f, 0, .5f, 12, -11, -34, -11);
            credits = UiKit.Text(cr.transform, "Wert", "0", 22, Pal.Credit, true, FontStyles.Bold); credits.rectTransform.Anchor(0, .3f, 1, 1, 42, 0, 8, 4); credits.textWrappingMode = TextWrappingModes.NoWrap;
            rate = UiKit.Text(cr.transform, "Rate", "", 11, Pal.Clean); rate.rectTransform.Anchor(0, 0, 1, .38f, 42, 4, 8, 0);
            var pc = UiKit.Btn(top, "Planet", () => OpenSheet("planets"), Pal.Glass, 16); ((RectTransform)pc.transform).Anchor(.43f, 0, .78f, 1, 4, 0, 4, 0);
            pdot = UiKit.Img(pc.transform, "Kugel", UiKit.Circle(Color.white, Color.gray), Color.white); pdot.rectTransform.Anchor(0, .5f, 0, .5f, 10, -12, -34, -12);
            pname = UiKit.Text(pc.transform, "Name", "MOND", 14, Pal.Ink, true, FontStyles.Bold | FontStyles.UpperCase); pname.rectTransform.Anchor(0, .45f, 1, 1, 40, 0, 6, 6); pname.textWrappingMode = TextWrappingModes.NoWrap; pname.characterSpacing = 6;
            pprog = UiKit.Text(pc.transform, "Fortschritt", "", 11, Pal.Ink2); pprog.rectTransform.Anchor(0, 0, 1, .48f, 40, 6, 6, 0); pprog.textWrappingMode = TextWrappingModes.NoWrap;
            var gb = UiKit.Btn(top, "Ziele", () => OpenSheet("goals"), Pal.Glass, 14); ((RectTransform)gb.transform).Anchor(.79f, 0, .885f, 1, 2, 0, 2, 0);
            UiKit.Img(gb.transform, "Icon", UiKit.Icon("goals"), Pal.Ink2).rectTransform.Anchor(.5f, .5f, .5f, .5f, -11, -11, -11, -11);
            var sb = UiKit.Btn(top, "Einstellungen", () => OpenSheet("settings"), Pal.Glass, 14); ((RectTransform)sb.transform).Anchor(.895f, 0, 1, 1, 2, 0, 0, 0);
            UiKit.Img(sb.transform, "Icon", UiKit.Icon("gear"), Pal.Ink2).rectTransform.Anchor(.5f, .5f, .5f, .5f, -11, -11, -11, -11);
            // Anzeigen
            var mr = UiKit.Rt(root, "Anzeigen").Anchor(0, 1, 1, 1, 12, -104, 12, 68);
            var hl = mr.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 8; hl.childControlWidth = true; hl.childForceExpandWidth = true; hl.childControlHeight = true;
            Meter(mr, "ENERGIE", new Color(1f, .85f, .3f), () => Game.Fmt(Game.Cur.sv.energy), () => Game.Cur.sv.energy / Game.EnergyCap(Game.Cur),
                () => Game.IsDay(Game.Cur) ? "TAG" : "NACHT", () => OpenSheet("energy"));
            Meter(mr, "ROH", Pal.Clean, () => Game.Fmt(Game.RohSum(Game.Cur)) + "/" + Game.Fmt(Game.RohCap(Game.Cur)), () => Game.RohSum(Game.Cur) / Game.RohCap(Game.Cur));
            Meter(mr, "PROD", Pal.Prod, () => Game.Cur.bld.Contains("prod") ? Game.Fmt(Game.ProdSum(Game.Cur)) + "/" + Game.Fmt(Game.ProdCap(Game.Cur)) : "–", () => Game.ProdSum(Game.Cur) / Game.ProdCap(Game.Cur));
        }
        void Meter(Transform p, string label, Color col, Func<string> val, Func<float> f, Func<string> labelLive = null, Action click = null)
        {
            Component m = click != null ? (Component)UiKit.Btn(p, label, click, Pal.Glass, 12) : UiKit.Glass(p, label, Pal.Glass, 12); var l = UiKit.Text(m.transform, "Name", label, 9, Pal.Ink2, true); l.rectTransform.Anchor(0, 0, .45f, 1, 10, 0, 0, 0); l.characterSpacing = 8; l.alignment = TextAlignmentOptions.MidlineLeft;
            var v = UiKit.Text(m.transform, "Wert", "", 11.5f, Pal.Ink, true); v.rectTransform.Anchor(.3f, 0, 1, 1, 0, 0, 10, 0); v.alignment = TextAlignmentOptions.MidlineRight; v.textWrappingMode = TextWrappingModes.NoWrap;
            var bar = UiKit.Bar(m.transform, "Balken", col, out var fill, 3); bar.Anchor(0, 0, 1, 0, 10, 4, 10, -7);
            meters.Add((v, fill, val, f));
            if (labelLive != null) { var lo = l; liveLabels.Add(() => { string t = labelLive(); lo.text = t; lo.color = t == "NACHT" ? new Color(.62f, .71f, 1f) : Pal.Ink2; }); }
            if (lowWarn == null && label == "ENERGIE") { var img = m.GetComponent<Image>(); lowWarn = () => img.color = Game.Cur.ef < .99f && (Game.Cur.sv.veh.Count > 0 || Game.Cur.bld.Contains("presse")) ? new Color(.35f, .1f, .1f, .9f) : Pal.Glass; }
        }

        // ---------- Zielkarte ----------
        void BuildGoal()
        {
            var b = UiKit.Btn(root, "Ziel", () => { if (!Game.ClaimQuest()) OnFocusQuest?.Invoke(); }, Pal.Glass, 16); goalGo = b.gameObject;
            ((RectTransform)b.transform).Anchor(0, 1, 1, 1, 12, -190, 12, 112);
            goalKick = UiKit.Text(b.transform, "Kopf", "", 10, Pal.Credit, true, FontStyles.Bold | FontStyles.UpperCase); goalKick.rectTransform.Anchor(0, 1, 1, 1, 14, -22, 14, 8); goalKick.characterSpacing = 8;
            goalText = UiKit.Text(b.transform, "Text", "", 14, Pal.Ink); goalText.rectTransform.Anchor(0, 0, 1, 1, 14, 14, 74, 24);
            goalReward = UiKit.Text(b.transform, "Belohnung", "", 13, Pal.Credit, true, FontStyles.Bold); goalReward.rectTransform.Anchor(1, 0, 1, 1, -70, 14, 12, 24); goalReward.alignment = TextAlignmentOptions.MidlineRight;
            UiKit.Bar(b.transform, "Fortschritt", Pal.Credit, out goalFill, 4).Anchor(0, 0, 1, 0, 14, 8, 14, -12);
            // Warnhinweis (Sturm, zu schwache Fahrzeuge) – antippen öffnet das passende Fenster
            var w = UiKit.Btn(root, "Warnung", () => OpenSheet(warnTarget), new Color(.16f, .1f, .03f, .9f), 12); warnGo = w.gameObject; warnImg = w.GetComponent<Image>();
            ((RectTransform)w.transform).Anchor(0, 1, 1, 1, 12, -228, 12, 196);
            warnTxt = UiKit.Text(w.transform, "Text", "", 12, Pal.Credit, true, FontStyles.Bold, TextAlignmentOptions.Center); warnTxt.rectTransform.Anchor(0, 0, 1, 1, 10, 0, 10, 0); warnTxt.enableAutoSizing = true; warnTxt.fontSizeMin = 9; warnTxt.fontSizeMax = 12;
            warnGo.SetActive(false);
        }
        GameObject warnGo; Image warnImg; TextMeshProUGUI warnTxt; string warnTarget = "hazard";
        void UpdateWarn(PState ps)
        {
            string t = null; var st = ps.P.storm;
            if (st != null && Game.StormOn(ps)) { t = st.name + (ps.shOn ? " · Schild hält 80 % ab" : " · " + Game.StormFx(st)) + " · noch " + Mathf.CeilToInt(ps.sv.hzOn) + " s"; warnTarget = "hazard"; }
            else if (st != null && Game.StormIn(ps) <= 30) { t = "Achtung: " + st.name + " in " + Mathf.CeilToInt(Game.StormIn(ps)) + " s" + (Game.ResDone("shield") ? (ps.sv.shield ? " · Schild bereit" : " · Tippen: Schild einschalten") : " · Tippen für Infos"); warnTarget = "hazard"; }
            else if (ps.sv.veh.Any(v => v.mode == "broken" || v.mode == "fieldRepair")) { int n = ps.sv.veh.Count(v => v.mode == "broken" || v.mode == "fieldRepair"); t = n + (n > 1 ? " Fahrzeuge" : " Fahrzeug") + " ausgefallen" + (ps.bld.Contains("werkstatt") ? " – fährt zur Werkstatt" : " – baue eine Roboterwerkstatt (Basisstufe 3)"); warnTarget = "vehicles"; }
            else if (ps.sv.veh.Count > 0 && !Game.PowOK(ps)) { t = "Fahrzeuge zu schwach für " + ps.P.name + " (nur 30 %): Doppelbürsten Stufe " + Game.PowReq(ps.pid) + " nötig"; warnTarget = "vehicles"; }
            else if (ps.ef < .99f && (ps.sv.veh.Count > 0 || ps.bld.Contains("presse"))) { t = "Energie reicht nicht – alles läuft langsamer"; warnTarget = "energy"; }
            warnGo.SetActive(t != null && !SheetOpen); if (t != null) warnTxt.text = t;
        }

        // ---------- unten ----------
        void BuildBottom()
        {
            var hb = UiKit.Btn(root, "Behälter", () => Game.UnloadHand(), Pal.Glass, 16); handImg = hb.GetComponent<Image>();
            ((RectTransform)hb.transform).Anchor(0, 0, 0, 0, 12, 78, -120, -130);
            UiKit.Img(hb.transform, "Icon", UiKit.Icon("bucket"), Pal.Clean).rectTransform.Anchor(0, .5f, 0, .5f, 10, -12, -34, -12);
            handTxt = UiKit.Text(hb.transform, "Wert", "0/60", 13, Pal.Ink, true, FontStyles.Bold); handTxt.rectTransform.Anchor(0, .45f, 1, 1, 42, 0, 8, 6); handTxt.alignment = TextAlignmentOptions.MidlineLeft;
            UiKit.Bar(hb.transform, "Balken", Pal.Clean, out handFill, 5).Anchor(0, 0, 1, 0, 42, 12, 12, -17);
            var mb = UiKit.Btn(root, "Karte", () => OnMap?.Invoke(), Pal.Glass, 16); ((RectTransform)mb.transform).Anchor(1, 0, 1, 0, -64, 78, 12, -130);
            UiKit.Img(mb.transform, "Icon", UiKit.Icon("globe"), Pal.Ink).rectTransform.Anchor(.5f, .5f, .5f, .5f, -13, -13, -13, -13);
            var nav = UiKit.Glass(root, "Leiste", Pal.GlassHi, 2); nav.rectTransform.Anchor(0, 0, 1, 0, 0, 0, 0, -66);
            var hl = nav.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.childControlWidth = true; hl.childForceExpandWidth = true; hl.childControlHeight = true; hl.padding = new RectOffset(4, 4, 6, 6); hl.spacing = 2;
            foreach (var (k, lbl, ic) in new[] { ("clean", "Reinigen", "clean"), ("vehicles", "Fahrzeuge", "car"), ("production", "Produktion", "factory"), ("base", "Basis", "home"), ("upgrades", "Upgrades", "up"), ("planets", "Planeten", "planet") })
            {
                string key = k; var b = UiKit.Btn(nav.transform, lbl, () => OnNav?.Invoke(key), new Color(1, 1, 1, 0), 12);
                var i = UiKit.Img(b.transform, "Icon", UiKit.Icon(ic), Pal.Ink2); i.rectTransform.Anchor(.5f, 1, .5f, 1, -12, -30, -12, 6); navIcons[key] = i;
                var t = UiKit.Text(b.transform, "Text", lbl, 11, Pal.Ink2, false, FontStyles.Normal, TextAlignmentOptions.Center); t.rectTransform.Anchor(0, 0, 1, 0, 0, 2, 0, -18); t.textWrappingMode = TextWrappingModes.NoWrap; navTxt[key] = t;
            }
        }

        // ---------- Fenster ----------
        void BuildSheet()
        {
            var sh = UiKit.Glass(root, "Fenster", Pal.GlassHi, 22); sheetGo = sh.gameObject; sh.raycastTarget = true; sheetPanel = sh.rectTransform; sheetPanel.Anchor(0, 0, 1, .74f, 0, 60, 0, 0);
            sheetKick = UiKit.Text(sh.transform, "Planet", "", 11, Pal.Ink2, true, FontStyles.UpperCase); sheetKick.rectTransform.Anchor(0, 1, 1, 1, 20, -34, 60, 16); sheetKick.characterSpacing = 10;
            sheetTitle = UiKit.Text(sh.transform, "Titel", "", 26, Pal.Ink, true, FontStyles.Bold); sheetTitle.rectTransform.Anchor(0, 1, 1, 1, 20, -70, 60, 34); sheetTitle.enableAutoSizing = true; sheetTitle.fontSizeMin = 13; sheetTitle.fontSizeMax = 26; sheetTitle.textWrappingMode = TextWrappingModes.NoWrap; sheetTitle.overflowMode = TextOverflowModes.Overflow; // lange Gebäudenamen werden kleiner statt abgeschnitten
            var x = UiKit.Btn(sh.transform, "Schließen", CloseSheet, new Color(1, 1, 1, .06f), 18); ((RectTransform)x.transform).Anchor(1, 1, 1, 1, -54, -54, 14, 14);
            UiKit.Img(x.transform, "Icon", UiKit.Icon("close"), Pal.Ink).rectTransform.Anchor(.5f, .5f, .5f, .5f, -10, -10, -10, -10);
            var sv = UiKit.Rt(sh.transform, "Inhalt").Anchor(0, 0, 1, 1, 0, 0, 0, 80); var scroll = sv.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            var vp = UiKit.Rt(sv, "Ausschnitt").Anchor(0, 0, 1, 1); vp.gameObject.AddComponent<RectMask2D>(); var vpi = vp.gameObject.AddComponent<Image>(); vpi.color = new Color(0, 0, 0, 0.001f); scroll.viewport = vp;
            sheetContent = UiKit.Rt(vp, "Liste"); sheetContent.anchorMin = new Vector2(0, 1); sheetContent.anchorMax = new Vector2(1, 1); sheetContent.pivot = new Vector2(.5f, 1); sheetContent.offsetMin = new Vector2(0, 0); sheetContent.offsetMax = new Vector2(0, 0);
            var vl = sheetContent.gameObject.AddComponent<VerticalLayoutGroup>(); vl.padding = new RectOffset(20, 20, 6, 30); vl.spacing = 6; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false;
            sheetContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; scroll.content = sheetContent;
            sheetGo.SetActive(false);
        }

        void BuildOffline()
        {
            var o = UiKit.Glass(root, "Offline", new Color(.03f, .05f, .1f, .92f), 2); o.rectTransform.Anchor(0, 0, 1, 1); o.raycastTarget = true; offlineGo = o.gameObject;
            offlineTxt = UiKit.Text(o.transform, "Text", "", 18, Pal.Ink, true, FontStyles.Normal, TextAlignmentOptions.Center); offlineTxt.rectTransform.Anchor(0, .4f, 1, .6f, 30, 0, 30, 0);
            offlineGo.SetActive(false);
        }
        public void ShowOffline(string t) { offlineGo.SetActive(t != null); if (t != null) offlineTxt.text = t; }

        public void OpenSheet(string kind, string arg = null)
        {
            if (kind == "clean") { CloseSheet(); return; }
            sheetKind = kind; sheetArg = arg; lastKind = null; sheetGo.SetActive(true); Rebuild();
        }
        public void CloseSheet() { sheetGo.SetActive(false); sheetKind = null; lastKind = null; binders.Clear(); }
        string lastKind, lastArg;
        public void Rebuild()
        {
            var keep = sheetContent.anchoredPosition; bool same = lastKind == sheetKind && lastArg == sheetArg; lastKind = sheetKind; lastArg = sheetArg;
            binders.Clear(); foreach (Transform c in sheetContent) Destroy(c.gameObject);
            var ps = Game.Cur; sheetKick.text = ps.P.name; Sheets.Fill(this, sheetKind, sheetArg, out string title); sheetTitle.text = title;
            Canvas.ForceUpdateCanvases(); sheetContent.anchoredPosition = same ? keep : Vector2.zero; Bind(); // nach einem Kauf bleibt das Fenster an derselben Stelle
        }

        // ---------- Bausteine für Fenster ----------
        public void Section(string title) { var t = UiKit.Text(sheetContent, "Abschnitt", title, 11, Pal.Ink2, true, FontStyles.Bold | FontStyles.UpperCase); t.characterSpacing = 10; UiKit.LE(t, 30, 30); t.alignment = TextAlignmentOptions.BottomLeft; }
        public void Note(string txt, Func<string> live = null)
        {
            var t = UiKit.Text(sheetContent, "Hinweis", txt, 14, Pal.Ink2); UiKit.Add<ContentSizeFitter>(t).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (live != null) binders.Add(() => t.text = live());
        }
        /// <summary>Zeile wie im Browser: Titel, Beschreibung, rechts ein Knopf (Gold = kaufen, Mint = Aktion).</summary>
        public void Row(string title, Func<string> desc, Func<string> btn = null, Func<bool> enabled = null, Action click = null, bool gold = true, Func<string> titleLive = null)
        {
            var row = UiKit.Rt(sheetContent, title); var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 10; hl.childControlHeight = true; hl.childControlWidth = true; hl.childForceExpandWidth = false; hl.padding = new RectOffset(0, 0, 8, 8); hl.childAlignment = TextAnchor.MiddleLeft;
            var col = UiKit.Rt(row, "Text"); var vl = col.gameObject.AddComponent<VerticalLayoutGroup>(); vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.spacing = 2; UiKit.LE(col, -1, -1, 1);
            var tt = UiKit.Text(col, "Titel", title, 16, Pal.Ink, true, FontStyles.Bold); UiKit.Add<ContentSizeFitter>(tt).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var dd = UiKit.Text(col, "Beschreibung", desc != null ? desc() : "", 13.5f, Pal.Ink2); UiKit.Add<ContentSizeFitter>(dd).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (titleLive != null) binders.Add(() => tt.text = titleLive());
            if (desc != null) binders.Add(() => { var s = desc(); dd.text = s; dd.gameObject.SetActive(!string.IsNullOrEmpty(s)); });
            if (btn != null)
            {
                var b = UiKit.Btn(row, "Knopf", () => { click?.Invoke(); Rebuild(); }, gold ? Pal.Credit : new Color(.43f, .95f, .82f, .16f), 14);
                UiKit.LE(b, 46, 46, -1, 118).minWidth = 104;
                var bt = UiKit.Text(b.transform, "Text", btn(), 14, gold ? Pal.CreditInk : Pal.Clean, true, FontStyles.Bold, TextAlignmentOptions.Center); bt.rectTransform.Anchor(0, 0, 1, 1, 8, 2, 8, 2); bt.enableAutoSizing = true; bt.fontSizeMin = 9; bt.fontSizeMax = 15; bt.textWrappingMode = TextWrappingModes.NoWrap;
                var cg = b.gameObject.AddComponent<CanvasGroup>();
                binders.Add(() => { bt.text = btn(); bool on = enabled == null || enabled(); b.interactable = on; cg.alpha = on ? 1 : .38f; });
            }
            var line = UiKit.Glass(sheetContent, "Linie", new Color(1, 1, 1, .07f), 1); UiKit.LE(line, 1, 1);
        }
        public void BigButton(string label, Func<bool> enabled, Action click)
        {
            var b = UiKit.Btn(sheetContent, label, () => { click(); Rebuild(); }, Pal.Credit, 14); UiKit.LE(b, 50, 50);
            var t = UiKit.Text(b.transform, "Text", label, 16, Pal.CreditInk, true, FontStyles.Bold, TextAlignmentOptions.Center); t.rectTransform.Anchor(0, 0, 1, 1);
            var cg = b.gameObject.AddComponent<CanvasGroup>();
            binders.Add(() => { bool on = enabled == null || enabled(); b.interactable = on; cg.alpha = on ? 1 : .38f; });
        }
        void Bind() { foreach (var b in binders) b(); }

        // ---------- Meldungen und Schwebetexte ----------
        public void Toast(string text, string kind)
        {
            var g = UiKit.Glass(toastBox, "Meldung", kind == "bad" ? new Color(.25f, .07f, .08f, .94f) : Pal.GlassHi, 14);
            var hl = g.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.padding = new RectOffset(14, 14, 10, 10); hl.childControlHeight = true; hl.childControlWidth = true;
            var t = UiKit.Text(g.transform, "Text", text, 13.5f, kind == "good" ? Pal.Clean : kind == "bad" ? Pal.Warn : Pal.Ink, false, FontStyles.Normal, TextAlignmentOptions.Center);
            UiKit.Add<ContentSizeFitter>(g).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            while (toastBox.childCount > 3) DestroyImmediate(toastBox.GetChild(0).gameObject);
            var cg = g.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false; if (isActiveAndEnabled) StartCoroutine(Fade(cg, 2.8f)); else Destroy(g.gameObject);
        }
        System.Collections.IEnumerator Fade(CanvasGroup cg, float life)
        {
            float t = 0; while (t < life && cg) { t += Time.unscaledDeltaTime; if (t > life - .5f) cg.alpha = (life - t) / .5f; yield return null; }
            if (cg) Destroy(cg.gameObject);
        }
        public void FloatText(Vector2 screen, string text, string kind)
        {
            var t = UiKit.Text(floatLayer, "Schwebetext", text, kind == "big" ? 20 : 14, kind == "big" ? Pal.Credit : Pal.Clean, true, FontStyles.Bold, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.rectTransform.sizeDelta = new Vector2(240, 30);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(floatLayer, screen, null, out var lp);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(.5f, .5f); t.rectTransform.anchoredPosition = lp; if (isActiveAndEnabled) StartCoroutine(Rise(t)); else Destroy(t.gameObject);
        }
        System.Collections.IEnumerator Rise(TextMeshProUGUI t) { float a = 0; var p0 = t.rectTransform.anchoredPosition; while (a < 1.4f && t) { a += Time.deltaTime; t.rectTransform.anchoredPosition = p0 + new Vector2(0, a * 34); t.alpha = Mathf.Clamp01(1.6f - a * 1.15f); yield return null; } if (t) Destroy(t.gameObject); }

        // ---------- pro Frame ----------
        void Update()
        {
            if (Game.S == null) return; var ps = Game.Cur; var P = ps.P;
            credShown = credShown < Game.S.credits ? Math.Min(Game.S.credits, credShown + Math.Max(1, (Game.S.credits - credShown) * Time.deltaTime * 6)) : Game.S.credits;
            credits.text = Game.Fmt(credShown);
            pname.text = P.name; pname.fontSize = P.name.Length > 9 ? 11 : P.name.Length > 7 ? 12.5f : 14;
            int c = ps.nClean, tot = ps.pd.cells.Count(x => !x.IsBase); pprog.text = c + "/" + tot + " sauber · Stufe " + ps.sv.stage;
            pdot.sprite = UiKit.Circle(P.col0, P.col1);
            foreach (var m in meters) { m.v.text = m.val(); UiKit.SetFill(m.fill, m.f()); }
            foreach (var a in liveLabels) a(); lowWarn?.Invoke(); UpdateWarn(ps);
            float hs = Game.HandSum(), hc = Game.HandCap(); handTxt.text = Game.Fmt(hs) + "/" + Game.Fmt(hc); UiKit.SetFill(handFill, hs / hc);
            handImg.color = hs >= hc * .99f ? new Color(.3f, .08f, .08f, .9f) : ps.sv.cart.owned && ps.sv.cart.mode == "wait" ? new Color(.1f, .3f, .27f, .95f) : Pal.Glass;
            int q = Game.S.quest; bool has = q < Game.QuestsSupported && q < GameData.Quests.Count; goalGo.SetActive(has && !SheetOpen);
            if (has)
            {
                float pr = Game.QuestProgress(q); goalKick.text = "Ziel " + (q + 1) + " von " + GameData.Quests.Count + (pr >= 1 ? " · antippen zum Abholen" : "");
                goalText.text = GameData.Quests[q].t; goalReward.text = "+" + Game.Fmt(GameData.Quests[q].r); UiKit.SetFill(goalFill, pr);
            }
            foreach (var kv in navIcons) { bool on = kv.Key == (sheetKind ?? "clean"); kv.Value.color = on ? Pal.Clean : Pal.Ink2; navTxt[kv.Key].color = on ? Pal.Clean : Pal.Ink2; }
            bindT += Time.unscaledDeltaTime; if (SheetOpen && bindT > .25f) { bindT = 0; Bind(); }
        }
        public static bool PointerOverUi(int id = -1) { var es = EventSystem.current; if (!es) return false; return id >= 0 ? es.IsPointerOverGameObject(id) : es.IsPointerOverGameObject(); }
    }
}
