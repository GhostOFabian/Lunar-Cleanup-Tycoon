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
        public Action OnMap, OnFocusQuest, OnFocusEvent, OnPhoto, OnHome, OnPhotoExit; GameObject photoGo; CanvasGroup photoCg; float photoT;
        /// <summary>Foto-Modus: Oberfläche weg, nur ein dezenter Knopf zum Zurückholen (wird nach 3 s fast unsichtbar, bleibt aber antippbar).</summary>
        public void SetPhoto(bool on) { if (root) root.gameObject.SetActive(!on); if (photoGo) { photoGo.SetActive(on); photoT = 0; } }
        RectTransform root, sheetPanel, sheetContent, toastBox, floatLayer;
        TextMeshProUGUI credits, rate, pname, pprog, goalKick, goalText, goalReward, handTxt, sheetKick, sheetTitle, offlineTxt;
        RectTransform goalFill, handFill, rocketFill; Image rocketImg; TextMeshProUGUI rocketTxt; Image handImg, pdot; GameObject sheetGo, offlineGo, goalGo, goalBadge; TextMeshProUGUI goalBadgeTxt;
        readonly List<Action> liveLabels = new List<Action>(); Action lowWarn;
        readonly List<(TextMeshProUGUI v, RectTransform fill, Func<string> val, Func<float> f)> meters = new List<(TextMeshProUGUI, RectTransform, Func<string>, Func<float>)>();
        readonly Dictionary<string, Image> navIcons = new Dictionary<string, Image>(); readonly Dictionary<string, TextMeshProUGUI> navTxt = new Dictionary<string, TextMeshProUGUI>();
        readonly List<Action> binders = new List<Action>();
        string sheetKind, sheetArg; float bindT, lastCred, badgeT; double credShown;
        public bool SheetOpen => sheetGo && sheetGo.activeSelf;
        public bool MenuOpen => menuGo && menuGo.activeSelf;
        public void HideMenu() { if (menuGo) menuGo.SetActive(false); }
        GameObject menuGo; float quitT = -99;
        TextMeshProUGUI hardMenuTxt, hardMenuSub; Image hardMenuImg;
        void UpdateHardMenu()
        {
            if (!MenuOpen || hardMenuTxt == null) return; bool open = Game.Hard || Game.HardOpen;
            hardMenuTxt.text = Game.Hard ? "Normales Spiel" : open ? "Hard Mode" : "Hard Mode (gesperrt)";
            hardMenuSub.text = Game.Hard ? "Zurück zu deinem normalen Spielstand" : open ? "Eigener Spielstand · du putzt selbst · Expedition" : "Gesperrt · " + Game.HardDoneN + "/" + GameData.POrder.Length + " Planeten sauber – oder im Shop · antippen für Infos";
            hardMenuImg.color = open ? new Color(1, .75f, .3f, .3f) : new Color(1, 1, 1, .05f); hardMenuTxt.color = open ? Pal.Credit : Pal.Ink3;
        }
        /// <summary>Startmenü (Fabian 01.11.): Spiel starten, Einstellungen, Beenden – liegt über der Basis, unter den Einstellungen.</summary>
        void BuildMenu()
        {
            var bg = UiKit.Glass(root, "Startmenü", new Color(.02f, .04f, .1f, .72f), 0); bg.raycastTarget = true; bg.rectTransform.Anchor(0, 0, 1, 1, -300, -300, -300, -300); menuGo = bg.gameObject;
            var box = UiKit.Rt(bg.transform, "Mitte").Anchor(.5f, .5f, .5f, .5f, -150, -250, -150, -230); /* 03.11.: Platz für Hard Mode */
            var t1 = UiKit.Text(box, "Titel", "LUNAR CLEANUP", 34, Pal.Clean, true, FontStyles.Bold, TextAlignmentOptions.Center); t1.rectTransform.Anchor(0, 1, 1, 1, 0, -50, 0, 0); t1.textWrappingMode = TextWrappingModes.NoWrap; t1.enableAutoSizing = true; t1.fontSizeMin = 18; t1.fontSizeMax = 34;
            var t2 = UiKit.Text(box, "Untertitel", "TYCOON", 18, Pal.Credit, true, FontStyles.Bold, TextAlignmentOptions.Center); t2.rectTransform.Anchor(0, 1, 1, 1, 0, -82, 0, 50); t2.characterSpacing = 18;
            float y = -150;
            void Big(string label, string sub, Color bgc, Color fg, System.Action act)
            {
                var b = UiKit.Btn(box, label, act, bgc, 16); ((RectTransform)b.transform).Anchor(0, 1, 1, 1, 0, y - 58, 0, -y); y -= 72;
                var bt = UiKit.Text(b.transform, "Text", label, 19, fg, true, FontStyles.Bold, TextAlignmentOptions.Center); bt.rectTransform.Anchor(0, .38f, 1, 1, 8, 0, 8, 2);
                var st = UiKit.Text(b.transform, "Hinweis", sub, 11, new Color(fg.r, fg.g, fg.b, .7f), false, FontStyles.Normal, TextAlignmentOptions.Center); st.rectTransform.Anchor(0, 0, 1, .42f, 8, 6, 8, 0);
            }
            Big("Spiel starten", "Weiter mit deiner Basis", new Color(.43f, .95f, .82f, .95f), new Color(.03f, .08f, .12f), () => { menuGo.SetActive(false); LunarCleanup.Gameplay.Sfx.Play("pop", 1.2f, .4f); });
            /* Fabian 03.11.: Hard Mode von Anfang an sichtbar – gesperrt mit Hinweis, wie man ihn freischaltet */
            Big("Hard Mode", "", new Color(1, .75f, .3f, .14f), Pal.Credit, () => { if (Game.Hard || Game.HardOpen) { menuGo.SetActive(false); Game.SwitchHard(!Game.Hard); } else OpenSheet("hard"); });
            { var hb = box.Find("Hard Mode"); if (hb) { hardMenuTxt = hb.Find("Text")?.GetComponent<TextMeshProUGUI>(); hardMenuSub = hb.Find("Hinweis")?.GetComponent<TextMeshProUGUI>(); hardMenuImg = hb.GetComponent<Image>(); } }
            Big("Einstellungen", "Sprache, Ton, Spielstand", new Color(1, 1, 1, .1f), Pal.Ink, () => OpenSheet("settings"));
            Big("Beenden", "Speichert und schließt das Spiel", new Color(1, 1, 1, .06f), Pal.Ink2, () =>
            {
                Game.Save();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
                Game.Toast("Gespeichert – du kannst den Browser-Tab jetzt schließen.", "good");
#else
                Application.Quit();
#endif
            });
            var ver = UiKit.Text(bg.transform, "Version", Game.VersionText, 10, Pal.Ink2, false, FontStyles.Normal, TextAlignmentOptions.Center); ver.rectTransform.Anchor(0, 0, 1, 0, 0, 330, 0, -360);
        }
        void LateUpdate() { if (photoGo && photoGo.activeSelf) { photoT += Time.unscaledDeltaTime; photoCg.alpha = photoT < 3 ? 1 : Mathf.Lerp(photoCg.alpha, .28f, Time.unscaledDeltaTime * 2); } }
        public void ShowMenu() { if (menuGo) { CloseSheet(); menuGo.SetActive(true); } }

        public void Build(Transform canvas)
        {
            I = this; UiKit.Init();
            root = UiKit.Rt(canvas, "SafeArea").Anchor(0, 0, 1, 1); root.gameObject.AddComponent<SafeAreaFitter>();
            floatLayer = UiKit.Rt(root, "Schwebetexte").Anchor(0, 0, 1, 1);
            BuildTop(); BuildGoal(); BuildBottom(); BuildMenu(); BuildSheet(); BuildOffline();
            /* Fabian 01.11.: Foto-Modus mit sichtbarem Knopf zum Zurückholen der Oberfläche (liegt außerhalb der ausgeblendeten Oberfläche) */
            { var pb = UiKit.Btn(canvas, "Foto-Modus beenden", () => OnPhotoExit?.Invoke(), new Color(.05f, .08f, .15f, .55f), 22); photoGo = pb.gameObject; photoCg = photoGo.AddComponent<CanvasGroup>();
              ((RectTransform)pb.transform).Anchor(0, 1, 0, 1, 24, -140, -330, 70);
              var pt = UiKit.Text(pb.transform, "Text", "Oberfläche einblenden", 15, Pal.Ink, true, FontStyles.Bold, TextAlignmentOptions.Center); pt.rectTransform.Anchor(0, 0, 1, 1, 10, 0, 10, 0); photoGo.SetActive(false); }
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
            rate = UiKit.Text(cr.transform, "Rate", "", 11, Pal.Clean); rate.rectTransform.Anchor(0, 0, 1, .38f, 42, 4, 8, 0); rate.color = new Color(.75f, .6f, 1f); rate.raycastTarget = false;
            cr.raycastTarget = true; cr.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenSheet("crystals")); /* 04.11.: Geld-Kachel antippen = Kristalle ausgeben */
            var pc = UiKit.Btn(top, "Planet", () => OpenSheet("planets"), Pal.Glass, 16); ((RectTransform)pc.transform).Anchor(.43f, 0, .78f, 1, 4, 0, 4, 0);
            pdot = UiKit.Img(pc.transform, "Kugel", UiKit.Circle(Color.white, Color.gray), Color.white); pdot.rectTransform.Anchor(0, .5f, 0, .5f, 10, -12, -34, -12);
            pname = UiKit.Text(pc.transform, "Name", "MOND", 14, Pal.Ink, true, FontStyles.Bold | FontStyles.UpperCase); pname.rectTransform.Anchor(0, .45f, 1, 1, 40, 0, 6, 6); pname.textWrappingMode = TextWrappingModes.NoWrap; pname.characterSpacing = 6;
            pprog = UiKit.Text(pc.transform, "Fortschritt", "", 11, Pal.Ink2); pprog.rectTransform.Anchor(0, 0, 1, .48f, 40, 6, 6, 0); pprog.textWrappingMode = TextWrappingModes.NoWrap; pprog.enableAutoSizing = true; pprog.fontSizeMin = 7; pprog.fontSizeMax = 11; /* „983/983 sauber · Stufe 5“ passt sonst nicht aufs Handy */
            var gb = UiKit.Btn(top, "Ziele", () => OpenSheet("goals"), Pal.Glass, 14); ((RectTransform)gb.transform).Anchor(.79f, 0, .885f, 1, 2, 0, 2, 0);
            UiKit.Img(gb.transform, "Icon", UiKit.Icon("goals"), Pal.Ink2).rectTransform.Anchor(.5f, .5f, .5f, .5f, -11, -11, -11, -11);
            /* Fabian 01.11.: Ziele nicht mehr dauerhaft im Bild – ein Punkt am Ziele-Knopf zeigt, dass etwas abholbar ist */
            var bd = UiKit.Img(gb.transform, "Abholbar", UiKit.Circle(Pal.C("#ffd35a"), Pal.C("#e0a020")), Color.white); bd.rectTransform.Anchor(1, 1, 1, 1, -20, -20, 2, 2); goalBadge = bd.gameObject;
            goalBadgeTxt = UiKit.Text(bd.transform, "Zahl", "", 11, Pal.CreditInk, true, FontStyles.Bold, TextAlignmentOptions.Center); goalBadgeTxt.rectTransform.Anchor(0, 0, 1, 1); goalBadge.SetActive(false);
            var sb = UiKit.Btn(top, "Einstellungen", () => OpenSheet("settings"), Pal.Glass, 14); ((RectTransform)sb.transform).Anchor(.895f, 0, 1, 1, 2, 0, 0, 0);
            UiKit.Img(sb.transform, "Icon", UiKit.Icon("gear"), Pal.Ink2).rectTransform.Anchor(.5f, .5f, .5f, .5f, -11, -11, -11, -11);
            // Anzeigen
            var mr = UiKit.Rt(root, "Anzeigen").Anchor(0, 1, 1, 1, 12, -104, 12, 68);
            var hl = mr.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 8; hl.childControlWidth = true; hl.childForceExpandWidth = true; hl.childControlHeight = true;
            Meter(mr, "ENERGIE", new Color(1f, .85f, .3f), () => Game.Fmt(Game.Cur.sv.energy), () => Game.Cur.sv.energy / Game.EnergyCap(Game.Cur),
                () => Game.IsDay(Game.Cur) ? "TAG" : "NACHT", () => OpenSheet("energy"));
            Meter(mr, "ROH", Pal.Clean, () => Game.Fmt(Game.RohSum(Game.Cur)) + "/" + Game.Fmt(Game.RohCap(Game.Cur)), () => Game.RohSum(Game.Cur) / Game.RohCap(Game.Cur), null, () => OpenSheet("production", "0"));
            Meter(mr, "PROD", Pal.Prod, () => Game.Cur.bld.Contains("prod") ? Game.Fmt(Game.ProdSum(Game.Cur)) + "/" + Game.Fmt(Game.ProdCap(Game.Cur)) : "–", () => Game.ProdSum(Game.Cur) / Game.ProdCap(Game.Cur), () => Game.Gs(Game.Cur) < 2 ? "PROD" : Game.Cur.sv.flowLv == 2 ? "STAU" : Game.Cur.sv.flowLv == 1 ? "ENGPASS" : "PROD", () => OpenSheet("production", "0")); /* Ampel im HUD */
        }
        void Meter(Transform p, string label, Color col, Func<string> val, Func<float> f, Func<string> labelLive = null, Action click = null)
        {
            Component m = click != null ? (Component)UiKit.Btn(p, label, click, Pal.Glass, 12) : UiKit.Glass(p, label, Pal.Glass, 12); var l = UiKit.Text(m.transform, "Name", label, 9, Pal.Ink2, true); l.rectTransform.Anchor(0, 0, .45f, 1, 10, 0, 0, 0); l.characterSpacing = 8; l.alignment = TextAlignmentOptions.MidlineLeft;
            var v = UiKit.Text(m.transform, "Wert", "", 11.5f, Pal.Ink, true); v.rectTransform.Anchor(.3f, 0, 1, 1, 0, 0, 10, 0); v.alignment = TextAlignmentOptions.MidlineRight; v.textWrappingMode = TextWrappingModes.NoWrap;
            var bar = UiKit.Bar(m.transform, "Balken", col, out var fill, 3); bar.Anchor(0, 0, 1, 0, 10, 4, 10, -7);
            meters.Add((v, fill, val, f));
            if (labelLive != null) { var lo = l; liveLabels.Add(() => { string t = labelLive(); lo.text = t; lo.color = t == "NACHT" ? new Color(.62f, .71f, 1f) : t == "STAU" ? new Color(1f, .42f, .36f) : t == "ENGPASS" ? new Color(1f, .82f, .3f) : Pal.Ink2; }); }
            if (lowWarn == null && label == "ENERGIE") { var img = m.GetComponent<Image>(); lowWarn = () => img.color = Game.Cur.ef < .99f && (Game.Cur.sv.veh.Count > 0 || Game.Cur.bld.Contains("presse")) ? new Color(.35f, .1f, .1f, .9f) : Pal.Glass; }
        }

        // ---------- Zielkarte ----------
        const int TutorialSteps = 10;
        void BuildGoal()
        {
            var b = UiKit.Btn(root, "Ziel", () => { if (!Game.ClaimQuest()) OnFocusQuest?.Invoke(); }, Pal.Glass, 16); goalGo = b.gameObject;
            ((RectTransform)b.transform).Anchor(0, 1, 1, 1, 12, -232, 12, 154); /* unter dem Warnhinweis */
            goalKick = UiKit.Text(b.transform, "Kopf", "", 10, Pal.Credit, true, FontStyles.Bold | FontStyles.UpperCase); goalKick.rectTransform.Anchor(0, 1, 1, 1, 14, -22, 14, 8); goalKick.characterSpacing = 8;
            goalText = UiKit.Text(b.transform, "Text", "", 14, Pal.Ink); goalText.rectTransform.Anchor(0, 0, 1, 1, 14, 14, 74, 24);
            goalReward = UiKit.Text(b.transform, "Belohnung", "", 13, Pal.Credit, true, FontStyles.Bold); goalReward.rectTransform.Anchor(1, 0, 1, 1, -70, 14, 12, 24); goalReward.alignment = TextAlignmentOptions.MidlineRight;
            UiKit.Bar(b.transform, "Fortschritt", Pal.Credit, out goalFill, 4).Anchor(0, 0, 1, 0, 14, 8, 14, -12);
            // Warnhinweis (Sturm, zu schwache Fahrzeuge) – antippen öffnet das passende Fenster
            var w = UiKit.Btn(root, "Warnung", () => { if (warnTarget == "wave") { var wc = Game.WaveCell(Game.Cur); if (wc >= 0) LunarCleanup.Gameplay.GameRoot.FocusReq = Game.Cur.pd.cells[wc].c.normalized; } else if (warnTarget == "care") Game.Attend(); else if (warnTarget == "focus") OnFocusEvent?.Invoke(); else if (warnTarget == "trader") { Game.SellToTrader(); } else if (warnTarget == "doom") { OpenSheet("base", "2"); } else if (warnTarget == "doomgo") { if (Game.DoomActive) Game.Travel(Game.S.doomPid); } else if (warnTarget == "icp") { } else OpenSheet(warnTarget); }, new Color(.16f, .1f, .03f, .9f), 12); warnGo = w.gameObject; warnImg = w.GetComponent<Image>();
            ((RectTransform)w.transform).Anchor(0, 1, 1, 1, 12, -144, 12, 112); /* sitzt jetzt dort, wo früher die Zielkarte war */
            warnTxt = UiKit.Text(w.transform, "Text", "", 12, Pal.Credit, true, FontStyles.Bold, TextAlignmentOptions.Center); warnTxt.rectTransform.Anchor(0, 0, 1, 1, 10, 0, 10, 0); warnTxt.enableAutoSizing = true; warnTxt.fontSizeMin = 9; warnTxt.fontSizeMax = 12;
            warnGo.SetActive(false);
        }
        GameObject warnGo; Image warnImg; TextMeshProUGUI warnTxt; string warnTarget = "hazard";
        // ---------- Band-Editor (Fabian 02.11.) ----------
        public Func<bool> BeltOn, BeltOk, BeltDrawOn; Image beDraw; public Func<string> BeltTitle, BeltInfo; public Action<string> BeltBtn;
        GameObject beGo; TextMeshProUGUI beTitle, beInfo; Button beOk; CanvasGroup beOkCg;
        void BuildBeltEdit()
        {
            var bg = UiKit.Glass(root, "Band-Editor", new Color(.04f, .07f, .11f, 1f), 16); beGo = bg.gameObject; bg.raycastTarget = true; bg.rectTransform.Anchor(0, 0, 1, 0, 8, 72, 8, -318);
            beTitle = UiKit.Text(bg.transform, "Titel", "", 14, Pal.Ink, true, FontStyles.Bold); beTitle.rectTransform.Anchor(0, 1, 1, 1, 14, -30, 14, 10);
            beInfo = UiKit.Text(bg.transform, "Info", "", 12, Pal.Ink2); beInfo.rectTransform.Anchor(0, 1, 1, 1, 14, -110, 14, 34); beInfo.enableAutoSizing = true; beInfo.fontSizeMin = 9; beInfo.fontSizeMax = 12;
            /* Fabian 02.11.: zwei Reihen – oben Punkte und Höhe, unten Zurücksetzen/Abbrechen/Fertig */
            void BtnRow(float y0, float y1, (string k, string lbl, bool gold)[] items)
            {
                var row = UiKit.Rt(bg.transform, "Knöpfe").Anchor(0, 0, 1, 0, 10, y0, 10, -y1); var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 6; hl.childControlWidth = true; hl.childForceExpandWidth = true; hl.childControlHeight = true;
                foreach (var (k, lbl, gold) in items)
                {
                    string key = k; var b = UiKit.Btn(row, lbl, () => BeltBtn?.Invoke(key), gold ? Pal.Credit : new Color(.43f, .95f, .82f, .16f), 12);
                    var t = UiKit.Text(b.transform, "Text", lbl, 12, gold ? Pal.CreditInk : Pal.Clean, true, FontStyles.Bold, TextAlignmentOptions.Center); t.rectTransform.Anchor(0, 0, 1, 1, 4, 2, 4, 2); t.enableAutoSizing = true; t.fontSizeMin = 8; t.fontSizeMax = 12;
                    if (key == "ok") { beOk = b; beOkCg = b.gameObject.AddComponent<CanvasGroup>(); }
                    if (key == "draw") beDraw = b.GetComponent<Image>();
                }
            }
            BtnRow(66, 114, new[] { ("draw", "Zeichnen", false), ("add", "Punkt +", false), ("del", "Punkt –", false), ("up", "Höher", false), ("down", "Tiefer", false) });
            BtnRow(10, 58, new[] { ("reset", "Zurücksetzen", false), ("cancel", "Abbrechen", false), ("ok", "Fertig", true) });
        }
        // ---------- Fabian 03.11.: Gebäude verschieben ----------
        public Func<bool> MoveOn, MoveOk; public Func<string> MoveTitle, MoveInfo; public Action<string> MoveBtn;
        GameObject mvGo; TextMeshProUGUI mvTitle, mvInfo; Button mvOk; CanvasGroup mvOkCg;
        void BuildMovePanel()
        {
            var bg = UiKit.Glass(root, "Gebäude verschieben", new Color(.04f, .07f, .11f, 1f), 16); mvGo = bg.gameObject; bg.raycastTarget = true; bg.rectTransform.Anchor(0, 0, 1, 0, 8, 72, 8, -232);
            mvTitle = UiKit.Text(bg.transform, "Titel", "", 14, Pal.Ink, true, FontStyles.Bold); mvTitle.rectTransform.Anchor(0, 1, 1, 1, 14, -30, 14, 10);
            mvInfo = UiKit.Text(bg.transform, "Info", "", 12, Pal.Ink2); mvInfo.rectTransform.Anchor(0, 1, 1, 1, 14, -96, 14, 34); mvInfo.enableAutoSizing = true; mvInfo.fontSizeMin = 9; mvInfo.fontSizeMax = 12;
            var row = UiKit.Rt(bg.transform, "Knöpfe").Anchor(0, 0, 1, 0, 10, 10, 10, -58); var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 6; hl.childControlWidth = true; hl.childForceExpandWidth = true; hl.childControlHeight = true;
            foreach (var (k, lbl, gold) in new[] { ("reset", "Standardplatz", false), ("cancel", "Abbrechen", false), ("ok", "Fertig", true) })
            {
                string key = k; var b = UiKit.Btn(row, lbl, () => MoveBtn?.Invoke(key), gold ? Pal.Credit : new Color(.43f, .95f, .82f, .16f), 12);
                var t = UiKit.Text(b.transform, "Text", lbl, 12, gold ? Pal.CreditInk : Pal.Clean, true, FontStyles.Bold, TextAlignmentOptions.Center); t.rectTransform.Anchor(0, 0, 1, 1, 4, 2, 4, 2); t.enableAutoSizing = true; t.fontSizeMin = 8; t.fontSizeMax = 12;
                if (key == "ok") { mvOk = b; mvOkCg = b.gameObject.AddComponent<CanvasGroup>(); }
            }
        }
        void UpdateMovePanel()
        {
            bool on = MoveOn != null && MoveOn(); if (on && !mvGo) BuildMovePanel(); if (!mvGo) return;
            if (mvGo.activeSelf != on) mvGo.SetActive(on); if (!on) return;
            mvTitle.text = MoveTitle?.Invoke() ?? ""; mvInfo.text = MoveInfo?.Invoke() ?? ""; bool ok = MoveOk == null || MoveOk(); mvOk.interactable = ok; mvOkCg.alpha = ok ? 1 : .38f;
        }
        void UpdateBeltEdit()
        {
            bool on = BeltOn != null && BeltOn(); if (on && !beGo) BuildBeltEdit(); if (!beGo) return;
            if (beGo.activeSelf != on) beGo.SetActive(on); if (!on) return;
            beTitle.text = BeltTitle?.Invoke() ?? ""; beInfo.text = BeltInfo?.Invoke() ?? ""; bool ok = BeltOk == null || BeltOk(); beOk.interactable = ok; beOkCg.alpha = ok ? 1 : .38f; if (beDraw) beDraw.color = BeltDrawOn != null && BeltDrawOn() ? new Color(1f, .78f, .3f, .55f) : new Color(.43f, .95f, .82f, .16f);
        }
        void UpdateWarn(PState ps)
        {
            string t = null; var st = ps.P.storm; string tt = Game.TeamText(ps);
            if (Game.PuzzleAnim) { } /* 10.11.: während der Puzzle-Animation keine Warnleiste */
            else if (Game.Icp != null) { var I = Game.Icp; t = "ABFANGEN · Brocken antippen, wenn ihr Ring GRÜN ist · Raketen " + I.rockets + " · abgefangen " + I.hits + " · durch " + I.through + " / " + I.total; warnTarget = "icp"; } /* Fabian 02.11.: Minispiel */
            else if (Game.DoomActive && Game.S.doomPid != Game.S.planet) { var dp = Game.P[Game.S.doomPid]; t = "RIESENMETEOR auf " + dp.P.name + " in " + Mathf.CeilToInt(Game.S.doomT) + " s · Schild " + Mathf.RoundToInt(Game.ShieldChance(dp) * 100) + " %" + (dp.sv.flakLv > 0 ? " · Tippen: hinfliegen und abfangen" : " · Tippen: hinfliegen"); warnTarget = "doomgo"; }
            else if (Game.DoomActive) { var dp = Game.Cur; t = "RIESENMETEOR auf " + dp.P.name + " · Einschlag in " + Mathf.CeilToInt(Game.S.doomT) + " s" + (dp.sv.msLv > 0 ? " · Schild " + Mathf.RoundToInt(Game.ShieldChance(dp) * 100) + " %" : "") + (dp.sv.flakLv > 0 ? " · " + Game.FlakRockets(dp) + " Raketen bereit – Brocken antippen, wenn der Ring GRÜN ist" : "") + (dp.sv.msLv == 0 && dp.sv.flakLv == 0 ? " · keine Abwehr! Tippen: Schild und Raketen kaufen" : ""); warnTarget = "doom"; } /* Fabian 01.11.: Countdown */
            else if (Game.EvOn(ps, "trader")) { t = "Händler kauft " + GameData.Mats[ps.sv.ev.mat].name + " ×3 · " + Game.Fmt(Game.TraderStock(ps)) + " E = " + Game.Fmt(Game.TraderValue(ps)) + " Cr · noch " + Mathf.CeilToInt(ps.sv.ev.t) + " s · antippen"; warnTarget = "trader"; }
            else if (Game.EvOn(ps, "pile")) { int crew = Game.PileCrew(ps); t = "Müllberg · " + Mathf.RoundToInt(Game.PileLeft(ps) * 100) + " % übrig · " + crew + " Fahrzeuge dran" + (crew < Game.PILE_TEAM ? " (mind. " + Game.PILE_TEAM + " nötig)" : "") + " · noch " + Mathf.CeilToInt(ps.sv.ev.t / 60f) + " min"; warnTarget = "focus"; }
            else if (tt != null) { t = tt; warnTarget = "focus"; }
            else if (st != null && Game.StormOn(ps)) { t = st.name + (ps.shOn ? " · Schild hält 80 % ab" : " · " + Game.StormFx(st)) + " · noch " + Mathf.CeilToInt(ps.sv.hzOn) + " s"; warnTarget = "hazard"; }
            else if (st != null && Game.StormIn(ps) <= 30) { t = "Achtung: " + st.name + " in " + Mathf.CeilToInt(Game.StormIn(ps)) + " s" + (Game.ResDone("shield") ? (ps.sv.shield ? " · Schild bereit" : " · Tippen: Schild einschalten") : " · Tippen für Infos"); warnTarget = "hazard"; }
            else if (ps.sv.veh.Any(v => v.mode == "broken" || v.mode == "fieldRepair")) { int n = ps.sv.veh.Count(v => v.mode == "broken" || v.mode == "fieldRepair"); t = n + (n > 1 ? " Fahrzeuge" : " Fahrzeug") + " ausgefallen" + (ps.bld.Contains("werkstatt") ? " – fährt zur Werkstatt" : " – baue eine Roboterwerkstatt (Basisstufe 3)"); warnTarget = "vehicles"; }
            else if (ps.sv.veh.Count > 0 && !Game.PowOK(ps)) { t = "Fahrzeuge zu schwach für " + ps.P.name + " (nur 30 %): Doppelbürsten Stufe " + Game.PowReq(ps.pid) + " nötig"; warnTarget = "vehicles"; }
            else if (ps.ef < .99f && (ps.sv.veh.Count > 0 || ps.bld.Contains("presse"))) { t = "Energie reicht nicht – alles läuft langsamer"; warnTarget = "energy"; }
            else if (Game.Hard && Game.WaveCell(ps) >= 0) { t = "Schmutzwelle in " + Mathf.CeilToInt(Mathf.Max(0, Game.WaveIn(ps))) + " s · putz die dreckigen Nachbarn der Stelle, dann stoppst du sie · antippen = hin"; warnTarget = "wave"; }
            else if (Game.CareF(ps) < .97f) { t = "Deine Crew braucht dich – alles läuft nur mit " + Mathf.RoundToInt(Game.CareF(ps) * 100) + " % · tippe hier oder auf den Planeten"; warnTarget = "care"; } /* 03.11.: Betreuung */
            warnGo.SetActive(t != null && !SheetOpen && !MenuOpen); if (t != null) warnTxt.text = t;
        }

        // ---------- Fabian 03.11.: Perk-Leiste (Hard Mode) – Werkzeuge antippen und dann die Stelle, Buffs sofort; „+“ öffnet den Perk-Shop ----------
        GameObject cargoTip; Image cargoTipImg; bool tipWas; /* 04.11.: die Sprechblase nur die ersten 3 Male, danach pulsiert nur die Rakete */
        bool TipLeft() { int n = PlayerPrefs.GetInt("LunarCleanup.CargoTipN", 0); return n < 3 || tipWas; } public static string ArmedTool, PerkReq; GameObject pbGo; readonly List<(string id, Image img, TextMeshProUGUI txt)> pbBtns = new List<(string, Image, TextMeshProUGUI)>(); readonly string[] pbIds = new string[7];
        void BuildPerkBar()
        {
            var row = UiKit.Rt(root, "Perk-Leiste").Anchor(0, 0, 1, 0, 128, 80, 78, -128); pbGo = row.gameObject; var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 5; hl.childControlWidth = true; hl.childForceExpandWidth = false; hl.childControlHeight = true; /* 03.11.: feste Breite – ein einzelner Shop-Knopf wurde sonst riesig */
            for (int i = 0; i < 6; i++)
            {
                string id = i == 5 ? "shop" : "";
                int slot = i; var b = UiKit.Btn(row, "Perk " + i, () => PerkTap(slot), Pal.Glass, 12); var img = b.GetComponent<Image>(); UiKit.LE(b, -1, -1, 0, 48).minWidth = 30; /* 04.11.: wird bei Platzmangel schmaler statt über den Karten-Knopf zu ragen */
                var t = UiKit.Text(b.transform, "Text", "", 11, Pal.Ink, true, FontStyles.Bold, TextAlignmentOptions.Center); t.rectTransform.Anchor(0, 0, 1, 1, 2, 2, 2, 2); t.enableAutoSizing = true; t.fontSizeMin = 6; t.fontSizeMax = 11; t.textWrappingMode = TextWrappingModes.NoWrap;
                pbBtns.Add((id, img, t)); /* nicht in bottomBtns: die blenden alles wieder ein, auch leere Plätze */
            }
            /* Fabian 04.11.: „Mehr“ klappt nach oben auf – dort stehen alle weiteren Perks */
            var pop = UiKit.Glass(root, "Perk-Auswahl", new Color(.05f, .08f, .15f, .94f), 14); pbPop = pop.gameObject; pop.raycastTarget = true; var prt = pop.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(0, 0); prt.pivot = new Vector2(0, 0); prt.anchoredPosition = new Vector2(128, 136);
            var gl = pop.gameObject.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(58, 50); gl.spacing = new Vector2(6, 6); gl.padding = new RectOffset(8, 8, 8, 8); gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 4;
            var fit = pop.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            for (int i = 0; i < 12; i++)
            {
                int k = i; var b = UiKit.Btn(pop.transform, "Perk mehr " + i, () => { string id = k < popIds.Count ? popIds[k] : null; pbPop.SetActive(false); if (id != null) UsePerkId(id); }, Pal.Glass, 12);
                var t = UiKit.Text(b.transform, "Text", "", 11, Pal.Ink, true, FontStyles.Bold, TextAlignmentOptions.Center); t.rectTransform.Anchor(0, 0, 1, 1, 2, 2, 2, 2); t.enableAutoSizing = true; t.fontSizeMin = 7; t.fontSizeMax = 11; popBtns.Add((b.GetComponent<Image>(), t));
            }
            pbPop.SetActive(false);
        }
        GameObject pbPop; readonly List<(Image img, TextMeshProUGUI txt)> popBtns = new List<(Image, TextMeshProUGUI)>(); readonly List<string> popIds = new List<string>();
        /// <summary>Einen Perk auslösen (Leiste oder Aufklapp-Feld).</summary>
        void UsePerkId(string id)
        {
            if (id == "ult") { if (Game.Ult >= 1) PerkReq = "ult"; else Game.Toast("Ultimate lädt: " + Mathf.FloorToInt(Game.Ult * 100) + " % – wische selbst, Kombos laden schneller. Voll = " + (Game.Klasse != null ? Game.Klasse.ult : "Klassen-Ulti"), ""); return; }
            var P = Game.Perk(id); if (P == null) return; if (Game.PerkN(id) <= 0) { if (P.kind == 1 && Game.BuffLeft(id) > 0) Game.Toast(P.name + " läuft noch " + Mathf.CeilToInt(Game.BuffLeft(id)) + " s", ""); else OpenSheet("perks"); return; }
            if (P.kind == 0) { ArmedTool = null; PerkReq = id; } else Game.UseBuff(id);
        }
        void PerkTap(int slot)
        {
            string id = slot >= 0 && slot < pbIds.Length ? pbIds[slot] : null; if (string.IsNullOrEmpty(id) || id == "shop") { OpenSheet("perks"); return; }
            if (id == "more") { pbPop.SetActive(!pbPop.activeSelf); if (pbPop.activeSelf) pbPop.transform.SetAsLastSibling(); return; }
            if (pbPop) pbPop.SetActive(false);
            UsePerkId(id);
        }
        // ---------- Fabian 03.11.: Perks sichtbar machen – Banner beim Start eines Buffs, Leuchtrand solange er läuft, Blitz bei der Nuke ----------
        TextMeshProUGUI buffBan, buffBanSub; CanvasGroup buffBanCg; float buffBanT = -99; Image flashImg; float flashT, flashDur = 1; readonly Image[] edges = new Image[4];
        public void BuffPop(string name, Color c, float dur, string sub = null)
        {
            if (!buffBan)
            {
                var g = UiKit.Rt(root, "Buff-Banner").Anchor(0, .58f, 1, .58f, 0, -60, 0, -60); buffBanCg = g.gameObject.AddComponent<CanvasGroup>(); buffBanCg.blocksRaycasts = false; buffBanCg.interactable = false;
                buffBan = UiKit.Text(g, "Name", "", 30, Pal.Ink, true, FontStyles.Bold, TextAlignmentOptions.Center); buffBan.rectTransform.Anchor(0, .4f, 1, 1, 10, 0, 10, 0); buffBan.enableAutoSizing = true; buffBan.fontSizeMin = 16; buffBan.fontSizeMax = 30;
                buffBanSub = UiKit.Text(g, "Dauer", "", 15, Pal.Ink, false, FontStyles.Bold, TextAlignmentOptions.Center); buffBanSub.rectTransform.Anchor(0, 0, 1, .4f, 10, 0, 10, 0); buffBan.raycastTarget = buffBanSub.raycastTarget = false;
            }
            buffBan.text = name.ToUpper() + "!"; buffBan.color = c; buffBanSub.text = sub ?? (Mathf.RoundToInt(dur) + " s aktiv"); buffBanSub.color = new Color(c.r, c.g, c.b, .85f); buffBanT = Time.unscaledTime;
        }
        public void Flash(Color c, float dur)
        {
            if (!flashImg) { flashImg = UiKit.Img(root, "Blitz", null, c); flashImg.rectTransform.Anchor(0, 0, 1, 1, -400, -400, -400, -400); flashImg.raycastTarget = false; }
            flashImg.color = c; flashT = Time.unscaledTime; flashDur = dur; flashImg.gameObject.SetActive(true);
        }
        void UpdatePerkFx()
        {
            if (buffBan) { float e = (Time.unscaledTime - buffBanT) / 1.8f; bool on = e < 1; if (buffBanCg.gameObject.activeSelf != on) buffBanCg.gameObject.SetActive(on); if (on) { buffBanCg.alpha = e < .15f ? e / .15f : e > .7f ? (1 - e) / .3f : 1; buffBanCg.transform.localScale = Vector3.one * (e < .15f ? Mathf.Lerp(.6f, 1.08f, e / .15f) : Mathf.Lerp(1.08f, 1f, Mathf.Min(1, (e - .15f) * 4))); } }
            if (flashImg && flashImg.gameObject.activeSelf) { float e = (Time.unscaledTime - flashT) / flashDur; if (e >= 1) flashImg.gameObject.SetActive(false); else { var c = flashImg.color; c.a = .9f * (1 - e) * (1 - e); flashImg.color = c; } }
            /* Leuchtrand: solange ein Buff läuft, in seiner Farbe */
            string act = null; if (Game.S != null && Game.Hard) foreach (var P in Game.Perks) if ((P.kind == 1 || P.kind == 3) && Game.BuffLeft(P.id) > 0) { act = P.id; break; }
            if (act == null) { foreach (var e0 in edges) if (e0 && e0.gameObject.activeSelf) e0.gameObject.SetActive(false); return; }
            if (!edges[0])
            {
                var cnv = root; edges[0] = UiKit.Img(cnv, "Rand oben", null, Color.white); edges[0].rectTransform.Anchor(0, 1, 1, 1, 0, -10, 0, 0);
                edges[1] = UiKit.Img(cnv, "Rand unten", null, Color.white); edges[1].rectTransform.Anchor(0, 0, 1, 0, 0, 0, 0, -10);
                edges[2] = UiKit.Img(cnv, "Rand links", null, Color.white); edges[2].rectTransform.Anchor(0, 0, 0, 1, 0, 0, -10, 0);
                edges[3] = UiKit.Img(cnv, "Rand rechts", null, Color.white); edges[3].rectTransform.Anchor(1, 0, 1, 1, -10, 0, 0, 0);
                foreach (var e0 in edges) { e0.raycastTarget = false; e0.transform.SetAsFirstSibling(); }
            }
            var col = LunarCleanup.Gameplay.WorldView.PerkColor(act); float k = .16f + .1f * Mathf.Sin(Time.unscaledTime * 3.2f);
            foreach (var e0 in edges) { if (!e0.gameObject.activeSelf) e0.gameObject.SetActive(true); e0.color = new Color(col.r, col.g, col.b, k); }
        }
        /* Fabian 04.11.: Anzug-Energie (Hard Mode) – schmale Leiste über der Perk-Leiste */
        GameObject suitGo; RectTransform suitFill; Image suitFillImg; TextMeshProUGUI suitTxt;
        void UpdateSuit()
        {
            bool on = Game.SuitActive && pbGo && pbGo.activeSelf; if (!on) { if (suitGo && suitGo.activeSelf) suitGo.SetActive(false); return; }
            if (!suitGo)
            {
                var g = UiKit.Glass(root, "Anzug-Energie", new Color(.05f, .08f, .15f, .85f), 6); suitGo = g.gameObject; g.raycastTarget = false; g.rectTransform.Anchor(0, 0, 1, 0, 128, 130, 78, -143); /* 04.11.: unter die Fracht-Sprechblase */
                UiKit.Bar(g.transform, "Balken", Pal.Clean, out suitFill, 4).Anchor(0, 0, 1, 1, 70, 5, 6, 5); suitFillImg = suitFill.GetComponent<Image>();
                suitTxt = UiKit.Text(g.transform, "Text", "Anzug", 9, Pal.Ink, true, FontStyles.Bold); suitTxt.rectTransform.Anchor(0, 0, 0, 1, 6, 0, -68, 0); suitTxt.alignment = TextAlignmentOptions.MidlineLeft; suitTxt.raycastTarget = false;
            }
            if (!suitGo.activeSelf) suitGo.SetActive(true); float f = Game.Suit / Game.SUIT_MAX; UiKit.SetFill(suitFill, f);
            if (suitFillImg) suitFillImg.color = f < .25f ? Color.Lerp(new Color(1f, .4f, .35f), new Color(1f, .6f, .3f), .5f + .5f * Mathf.Sin(Time.time * 6)) : f < .5f ? new Color(1f, .8f, .3f) : Pal.Clean;
            suitTxt.text = "Anzug " + Mathf.RoundToInt(f * 100) + " %";
        }
        static string PerkShort(string id) { switch (id) { case "bomb": return "Bombe"; case "nuke": return "Nuke"; case "kaercher": return "Kärcher"; case "foam": return "Schaum"; case "tornado": return "Tornado"; case "turbo": return "Turbo"; case "wide": return "Breit"; case "magnet": return "Magnet"; case "espresso": return "Espresso"; case "goldmop": return "Gold"; case "rohrpost": return "Rohr"; } return id; }
        void UpdatePerkBar()
        {
            bool on = Game.PerksOpen && !SheetOpen && !MenuOpen && Game.Icp == null; if (on && !pbGo) BuildPerkBar(); if (!pbGo) return; if (pbGo.activeSelf != on) pbGo.SetActive(on); if (!on) { ArmedTool = null; if (pbPop && pbPop.activeSelf) pbPop.SetActive(false); return; }
            /* Fabian 03.11.: die Leiste zeigt nur, was du hast (oder was gerade läuft) – höchstens 6, dahinter der Shop */
            var own = Game.Perks.Where(x => x.kind < 2 && (Game.PerkN(x.id) > 0 || Game.BuffLeft(x.id) > 0)).Select(x => x.id).ToList();
            /* 04.11.: Ulti + bis zu 4 Perks; sind es mehr, zeigt Platz 4 „Mehr“ und der Rest klappt nach oben auf */
            var have = new List<string> { "ult" }; bool more = own.Count > 4; have.AddRange(own.Take(more ? 3 : 4)); if (more) have.Add("more");
            popIds.Clear(); if (more) popIds.AddRange(own.Skip(3)); if (!more && pbPop && pbPop.activeSelf) pbPop.SetActive(false);
            if (pbPop && pbPop.activeSelf) for (int i = 0; i < popBtns.Count; i++) { var (pimg, ptxt) = popBtns[i]; bool show = i < popIds.Count; pimg.gameObject.SetActive(show); if (!show) continue; string pid = popIds[i]; var PP = Game.Perk(pid); float pbl = PP.kind == 1 ? Game.BuffLeft(pid) : 0; ptxt.text = PerkShort(pid) + "\n" + (pbl > 0 ? Mathf.CeilToInt(pbl) + " s" : "×" + Game.PerkN(pid)); pimg.color = pbl > 0 ? new Color(.43f, .95f, .82f, .35f) : Pal.Glass; }
            for (int i = 0; i < pbBtns.Count; i++)
            {
                var (_, img, txt) = pbBtns[i]; string id = i == 5 ? "shop" : i < have.Count ? have[i] : ""; pbIds[i] = id; img.gameObject.SetActive(id != "");
                if (id == "") continue;
                if (id == "shop") { txt.text = "Alle\nPerks"; continue; }
                if (id == "more") { bool op = pbPop && pbPop.activeSelf; txt.text = op ? "Zu" : "Mehr\n+" + popIds.Count; img.color = op ? new Color(.43f, .95f, .82f, .3f) : Pal.Glass; continue; }
                if (id == "ult") { bool full = Game.Ult >= 1; txt.text = full ? "ULTI\nBEREIT" : "Ulti\n" + Mathf.FloorToInt(Game.Ult * 100) + " %"; img.color = full ? Color.Lerp(new Color(.5f, .35f, .05f, .95f), new Color(.85f, .6f, .1f, .95f), .5f + .5f * Mathf.Sin(Time.time * 5)) : Color.Lerp(Pal.Glass, new Color(.45f, .25f, .7f, .9f), Game.Ult); continue; } var P = Game.Perk(id); int n = Game.PerkN(id); float bl = P.kind == 1 ? Game.BuffLeft(id) : 0;
                txt.text = PerkShort(id) + "\n" + (bl > 0 ? Mathf.CeilToInt(bl) + " s" : "×" + n);
                img.color = ArmedTool == id ? new Color(1f, .75f, .3f, .55f) : bl > 0 ? new Color(.43f, .95f, .82f, .35f) : n > 0 ? Pal.Glass : new Color(Pal.Glass.r, Pal.Glass.g, Pal.Glass.b, Pal.Glass.a * .5f);
            }
        }
        // ---------- unten ----------
        readonly List<GameObject> bottomBtns = new List<GameObject>(); /* Fabian 02.11.: beim Abfangen ausgeblendet, damit kein Brocken hinter einem Knopf liegt */
        void BuildBottom()
        {
            var hb = UiKit.Btn(root, "Behälter", () => Game.UnloadHand(), Pal.Glass, 16); handImg = hb.GetComponent<Image>(); bottomBtns.Add(hb.gameObject);
            ((RectTransform)hb.transform).Anchor(0, 0, 0, 0, 12, 78, -120, -130);
            UiKit.Img(hb.transform, "Icon", UiKit.Icon("bucket"), Pal.Clean).rectTransform.Anchor(0, .5f, 0, .5f, 10, -12, -34, -12);
            handTxt = UiKit.Text(hb.transform, "Wert", "0/60", 13, Pal.Ink, true, FontStyles.Bold); handTxt.rectTransform.Anchor(0, .45f, 1, 1, 42, 0, 8, 6); handTxt.alignment = TextAlignmentOptions.MidlineLeft;
            UiKit.Bar(hb.transform, "Balken", Pal.Clean, out handFill, 5).Anchor(0, 0, 1, 0, 42, 12, 12, -17);
            /* Fabian 01.11.: Rakete nicht mehr versteckt – Frachtraum-Anzeige über dem Behälter, Tippen startet die Rakete (leer: öffnet den Verkauf) */
            var rb = UiKit.Btn(root, "Rakete", () => { var ps = Game.Cur; if (Game.CargoHint(ps)) Game.LoadAllCargo(); else if (ps.sv.rk <= 0 && Game.Sum(ps.sv.sellBuf) > 0) Game.LaunchRocket(); else OpenSheet("production", "1"); } /* 03.11.: Anfangsbereich – erst laden, dann starten */, Pal.Glass, 16); rocketImg = rb.GetComponent<Image>(); bottomBtns.Add(rb.gameObject);
            ((RectTransform)rb.transform).Anchor(0, 0, 0, 0, 12, 142, -120, -194);
            UiKit.Img(rb.transform, "Icon", UiKit.Icon("rocket"), Pal.Credit).rectTransform.Anchor(0, .5f, 0, .5f, 10, -12, -34, -12);
            rocketTxt = UiKit.Text(rb.transform, "Wert", "", 12, Pal.Ink, true, FontStyles.Bold); rocketTxt.rectTransform.Anchor(0, .45f, 1, 1, 42, 0, 8, 6); rocketTxt.alignment = TextAlignmentOptions.MidlineLeft; rocketTxt.enableAutoSizing = true; rocketTxt.fontSizeMin = 8; rocketTxt.fontSizeMax = 12; rocketTxt.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Bar(rb.transform, "Balken", Pal.Credit, out rocketFill, 5).Anchor(0, 0, 1, 0, 42, 12, 12, -17);
            /* Fabian 03.11.: Sprechblase neben der Rakete, solange im Anfangsbereich Ware von Hand geladen werden muss */
            var cb = UiKit.Glass(root, "Fracht-Hinweis", new Color(.35f, .27f, .06f, .96f), 12); cargoTip = cb.gameObject; cb.raycastTarget = true; ((RectTransform)cb.transform).Anchor(0, 0, 1, 0, 136, 147, 76, -196);
            var cbb = cb.gameObject.AddComponent<Button>(); cbb.onClick.AddListener(() => Game.LoadAllCargo()); cargoTipImg = cb;
            var ct = UiKit.Text(cb.transform, "Text", "Ware im Lager! Tippe hier oder auf die Rakete, um sie einzuladen", 13, Pal.Ink, true, FontStyles.Bold); ct.rectTransform.Anchor(0, 0, 1, 1, 10, 4, 10, 4); ct.alignment = TextAlignmentOptions.MidlineLeft; ct.enableAutoSizing = true; ct.fontSizeMin = 9; ct.fontSizeMax = 13;
            cargoTip.SetActive(false);
            var mb = UiKit.Btn(root, "Karte", () => OnMap?.Invoke(), Pal.Glass, 16); bottomBtns.Add(mb.gameObject); ((RectTransform)mb.transform).Anchor(1, 0, 1, 0, -64, 78, 12, -130);
            UiKit.Img(mb.transform, "Icon", UiKit.Icon("globe"), Pal.Ink).rectTransform.Anchor(.5f, .5f, .5f, .5f, -13, -13, -13, -13);
            /* Fabian 01.11.: ein Tipp zurück zur Basis-Ansicht */
            var hm = UiKit.Btn(root, "Zur Basis", () => OnHome?.Invoke(), Pal.Glass, 16); bottomBtns.Add(hm.gameObject); ((RectTransform)hm.transform).Anchor(1, 0, 1, 0, -64, 142, 12, -194);
            UiKit.Img(hm.transform, "Icon", UiKit.Icon("home"), Pal.Ink).rectTransform.Anchor(.5f, .5f, .5f, .5f, -13, -13, -13, -13);
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
            /* Fabian 02.11.: kleiner Hinweis (blockiert nichts), solange ein Planet seine Offline-Zeit nachholt */
            var k = UiKit.Glass(root, "CatchUp", new Color(.03f, .05f, .1f, .85f), 2); k.rectTransform.Anchor(.08f, .775f, .92f, .805f); k.raycastTarget = false; catchGo = k.gameObject;
            catchTxt = UiKit.Text(k.transform, "Text", "", 13, Pal.Ink, true, FontStyles.Normal, TextAlignmentOptions.Center); catchTxt.rectTransform.Anchor(0, 0, 1, 1, 10, 0, 10, 0); catchTxt.raycastTarget = false;
            catchGo.SetActive(false);
        }
        GameObject catchGo; TextMeshProUGUI catchTxt;
        public void ShowCatchUp(string t) { if (catchGo == null) return; catchGo.SetActive(t != null); if (t != null) catchTxt.text = t; }
        public void ShowOffline(string t) { offlineGo.SetActive(t != null); if (t != null) offlineTxt.text = t; }

        public void OpenSheet(string kind, string arg = null)
        {
            if (kind == "clean") { CloseSheet(); return; }
            sheetKind = kind; sheetArg = arg; lastKind = null; sheetGo.SetActive(true); Rebuild();
        }
        public void CloseSheet() { if (sheetKind == "class" && Game.NeedClass) { Game.Toast("Wähle erst deine Klasse", ""); return; } sheetGo.SetActive(false); sheetKind = null; lastKind = null; binders.Clear(); }
        string lastKind, lastArg;
        public void Rebuild()
        {
            var keep = sheetContent.anchoredPosition; bool same = lastKind == sheetKind && lastArg == sheetArg; lastKind = sheetKind; lastArg = sheetArg;
            binders.Clear(); foreach (Transform c in sheetContent) Destroy(c.gameObject);
            var ps = Game.Cur; sheetKick.text = ps.P.name; Sheets.Fill(this, sheetKind, sheetArg, out string title); sheetTitle.text = title;
            Canvas.ForceUpdateCanvases(); UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(sheetContent); /* Fabian 01.11.: erst Höhe berechnen, sonst springt die Liste nach dem Kauf nach oben */ sheetContent.anchoredPosition = same ? keep : Vector2.zero; Bind(); // nach einem Kauf bleibt das Fenster an derselben Stelle
        }

        // ---------- Bausteine für Fenster ----------
        public void Section(string title) { var t = UiKit.Text(sheetContent, "Abschnitt", title, 11, Pal.Ink2, true, FontStyles.Bold | FontStyles.UpperCase); t.characterSpacing = 10; UiKit.LE(t, 30, 30); t.alignment = TextAlignmentOptions.BottomLeft; }
        /// <summary>Hervorgehobener Kasten (Gold) für wichtige Hinweise.</summary>
        public void Callout(string title, string txt)
        {
            var box = UiKit.Glass(sheetContent, "Wichtig", new Color(.35f, .27f, .06f, .95f), 14); var vl = box.gameObject.AddComponent<VerticalLayoutGroup>(); vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.padding = new RectOffset(14, 14, 12, 12); vl.spacing = 4;
            UiKit.Add<ContentSizeFitter>(box).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var t = UiKit.Text(box.transform, "Titel", title, 17, Pal.Credit, true, FontStyles.Bold); UiKit.Add<ContentSizeFitter>(t).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var d = UiKit.Text(box.transform, "Text", txt, 14, Pal.Ink); UiKit.Add<ContentSizeFitter>(d).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var bi = box; binders.Add(() => bi.color = Color.Lerp(new Color(.35f, .27f, .06f, .95f), new Color(.5f, .38f, .08f, .95f), .5f + .5f * Mathf.Sin(Time.time * 3)));
        }
        public void Note(string txt, Func<string> live = null)
        {
            var t = UiKit.Text(sheetContent, "Hinweis", txt, 14, Pal.Ink2); UiKit.Add<ContentSizeFitter>(t).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (live != null) binders.Add(() => t.text = live());
        }
        /// <summary>Zeile wie im Browser: Titel, Beschreibung, rechts ein Knopf (Gold = kaufen, Mint = Aktion).</summary>
        public void Row(string title, Func<string> desc, Func<string> btn = null, Func<bool> enabled = null, Action click = null, bool gold = true, Func<string> titleLive = null, bool rebuild = true, Func<string> why = null)
        {
            var row = UiKit.Rt(sheetContent, title); var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 10; hl.childControlHeight = true; hl.childControlWidth = true; hl.childForceExpandWidth = false; hl.padding = new RectOffset(0, 0, 8, 8); hl.childAlignment = TextAnchor.MiddleLeft;
            var col = UiKit.Rt(row, "Text"); var vl = col.gameObject.AddComponent<VerticalLayoutGroup>(); vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.spacing = 2; UiKit.LE(col, -1, -1, 1);
            var tt = UiKit.Text(col, "Titel", title, 16, Pal.Ink, true, FontStyles.Bold); UiKit.Add<ContentSizeFitter>(tt).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var dd = UiKit.Text(col, "Beschreibung", desc != null ? desc() : "", 13.5f, Pal.Ink2); UiKit.Add<ContentSizeFitter>(dd).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (titleLive != null) binders.Add(() => tt.text = titleLive());
            if (desc != null) binders.Add(() => { var s = desc(); dd.text = s; dd.gameObject.SetActive(!string.IsNullOrEmpty(s)); });
            if (btn != null)
            {
                var b = UiKit.Btn(row, "Knopf", () => { if (enabled != null && !enabled()) { Locked(titleLive != null ? titleLive() : title, btn, why, desc); return; } click?.Invoke(); if (rebuild) Rebuild(); else Bind(); }, gold ? Pal.Credit : new Color(.43f, .95f, .82f, .16f), 14);
                UiKit.LE(b, 46, 46, -1, 118).minWidth = 104;
                var bt = UiKit.Text(b.transform, "Text", btn(), 14, gold ? Pal.CreditInk : Pal.Clean, true, FontStyles.Bold, TextAlignmentOptions.Center); bt.rectTransform.Anchor(0, 0, 1, 1, 8, 2, 8, 2); bt.enableAutoSizing = true; bt.fontSizeMin = 9; bt.fontSizeMax = 15; bt.textWrappingMode = TextWrappingModes.NoWrap;
                var cg = b.gameObject.AddComponent<CanvasGroup>();
                binders.Add(() => { bt.text = btn(); bool on = enabled == null || enabled(); cg.alpha = on ? 1 : .38f; }); /* bleibt antippbar: gesperrt = Erklärung */
            }
            var line = UiKit.Glass(sheetContent, "Linie", new Color(1, 1, 1, .07f), 1); UiKit.LE(line, 1, 1);
        }
        /// <summary>Fabian 01.11.: kleines Säulendiagramm (Statistik). Werte älteste zuerst; Beschriftung links/rechts unten.</summary>
        public void BarChart(string title, double[] vals, Color col, string left, string right, Func<double, string> fmt)
        {
            var box = UiKit.Glass(sheetContent, "Diagramm " + title, new Color(1, 1, 1, .05f), 12); UiKit.LE(box, 150, 150);
            double mx = 0, sum = 0; foreach (var v in vals) { mx = Math.Max(mx, v); sum += v; }
            var tt = UiKit.Text(box.transform, "Titel", title, 13, Pal.Ink, true, FontStyles.Bold); tt.rectTransform.Anchor(0, 1, 1, 1, 12, -26, 12, 6);
            var mt = UiKit.Text(box.transform, "Max", mx > 0 ? "max " + fmt(mx) : "noch keine Daten", 11, Pal.Ink2, false, FontStyles.Normal, TextAlignmentOptions.TopRight); mt.rectTransform.Anchor(0, 1, 1, 1, 12, -26, 12, 8);
            var area = UiKit.Rt(box.transform, "Säulen").Anchor(0, 0, 1, 1, 12, 26, 12, 32); int n = vals.Length;
            for (int i = 0; i < n; i++)
            {
                float h = mx > 0 ? (float)(vals[i] / mx) : 0; var b = UiKit.Glass(area, "S" + i, new Color(col.r, col.g, col.b, i == n - 1 ? 1 : .75f), 3);
                b.rectTransform.anchorMin = new Vector2((float)i / n, 0); b.rectTransform.anchorMax = new Vector2((i + .78f) / n, Mathf.Max(.015f, h)); b.rectTransform.offsetMin = b.rectTransform.offsetMax = Vector2.zero;
            }
            var l = UiKit.Text(box.transform, "Links", left, 10, Pal.Ink2); l.rectTransform.Anchor(0, 0, .5f, 0, 12, 6, 0, -22);
            var r = UiKit.Text(box.transform, "Rechts", right, 10, Pal.Ink2, false, FontStyles.Normal, TextAlignmentOptions.Right); r.rectTransform.Anchor(.5f, 0, 1, 0, 0, 6, 12, -22);
        }
        public void BigButton(string label, Func<bool> enabled, Action click)
        {
            var b = UiKit.Btn(sheetContent, label, () => { click(); Rebuild(); }, Pal.Credit, 14); UiKit.LE(b, 50, 50);
            var t = UiKit.Text(b.transform, "Text", label, 16, Pal.CreditInk, true, FontStyles.Bold, TextAlignmentOptions.Center); t.rectTransform.Anchor(0, 0, 1, 1);
            var cg = b.gameObject.AddComponent<CanvasGroup>();
            binders.Add(() => { bool on = enabled == null || enabled(); b.interactable = on; cg.alpha = on ? 1 : .38f; });
        }
        /// <summary>Reiter oben im Fenster (Kaufen / Flotte / Upgrades …). Die Auswahl steckt im Fenster-Argument.</summary>
        public int Tab => int.TryParse(sheetArg, out int t) ? t : 0;
        public void Tabs(params string[] names)
        {
            int sel = Mathf.Clamp(Tab, 0, names.Length - 1);
            var row = UiKit.Rt(sheetContent, "Reiter"); var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 6; hl.childControlHeight = true; hl.childControlWidth = true; hl.childForceExpandWidth = true; hl.padding = new RectOffset(0, 0, 4, 8); UiKit.LE(row, 46, 46);
            for (int i = 0; i < names.Length; i++)
            {
                int k = i; bool on = i == sel; string kind = sheetKind;
                var b = UiKit.Btn(row, names[i], () => OpenSheet(kind, k.ToString()), on ? new Color(.43f, .95f, .82f, .22f) : new Color(1, 1, 1, .05f), 12); UiKit.LE(b, 38, 38, 1);
                var t = UiKit.Text(b.transform, "Text", names[i], 13, on ? Pal.Clean : Pal.Ink2, true, FontStyles.Bold, TextAlignmentOptions.Center); t.rectTransform.Anchor(0, 0, 1, 1, 4, 0, 4, 0); t.enableAutoSizing = true; t.fontSizeMin = 9; t.fontSizeMax = 13; t.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }
        /// <summary>Karte für Kaufbares (Gebäude, Fahrzeuge, Drohnen): eigener Kasten mit Farbstreifen – deutlich anders als Upgrade-Zeilen.</summary>
        public void Card(string title, Func<string> desc, Color accent, Func<string> btn, Func<bool> enabled, Action click, string badge = null, Func<string> why = null)
        {
            var card = UiKit.Glass(sheetContent, "Karte " + title, new Color(.07f, .11f, .2f, .95f), 14);
            var hl = card.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 12; hl.childControlHeight = true; hl.childControlWidth = true; hl.childForceExpandWidth = false; hl.padding = new RectOffset(0, 12, 0, 0); hl.childAlignment = TextAnchor.MiddleLeft;
            var stripe = UiKit.Glass(card.transform, "Streifen", accent, 6); UiKit.LE(stripe, 70, -1, -1, 8).minWidth = 8;
            var col = UiKit.Rt(card.transform, "Text"); var vl = col.gameObject.AddComponent<VerticalLayoutGroup>(); vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.spacing = 3; vl.padding = new RectOffset(0, 0, 12, 12); UiKit.LE(col, -1, -1, 1);
            var tt = UiKit.Text(col, "Titel", title + (badge != null ? "  <color=#6ff2d2><size=11>" + badge + "</size></color>" : ""), 17, Pal.Ink, true, FontStyles.Bold); UiKit.Add<ContentSizeFitter>(tt).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var dd = UiKit.Text(col, "Beschreibung", desc != null ? desc() : "", 13, Pal.Ink2); UiKit.Add<ContentSizeFitter>(dd).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (desc != null) binders.Add(() => dd.text = desc());
            if (btn != null)
            {
                var b = UiKit.Btn(card.transform, "Knopf", () => { if (enabled != null && !enabled()) { Locked(title, btn, why, desc); return; } click?.Invoke(); Rebuild(); }, Pal.Credit, 14); UiKit.LE(b, 50, 50, -1, 122).minWidth = 108;
                var bt = UiKit.Text(b.transform, "Text", btn(), 14, Pal.CreditInk, true, FontStyles.Bold, TextAlignmentOptions.Center); bt.rectTransform.Anchor(0, 0, 1, 1, 8, 2, 8, 2); bt.enableAutoSizing = true; bt.fontSizeMin = 9; bt.fontSizeMax = 15; bt.textWrappingMode = TextWrappingModes.NoWrap;
                var cg = b.gameObject.AddComponent<CanvasGroup>();
                binders.Add(() => { bt.text = btn(); bool on = enabled == null || enabled(); cg.alpha = on ? 1 : .38f; }); /* bleibt antippbar: gesperrt = Erklärung */
            }
            var gap = UiKit.Rt(sheetContent, "Abstand"); UiKit.LE(gap, 8, 8);
        }
        /// <summary>Texteingabe (Flagge beschriften): auf dem Handy öffnet sich die Tastatur, „Fertig“ speichert.</summary>
        public void TextInput(string value, string placeholder, Action<string> done)
        {
            var bg = UiKit.Glass(sheetContent, "Eingabe", new Color(1, 1, 1, .08f), 10); UiKit.LE(bg, 48, 48); bg.raycastTarget = true;
            var area = UiKit.Rt(bg.transform, "Textbereich").Anchor(0, 0, 1, 1, 12, 6, 12, 6); area.gameObject.AddComponent<RectMask2D>();
            var ph = UiKit.Text(area, "Platzhalter", placeholder, 16, new Color(1, 1, 1, .35f)); ph.rectTransform.Anchor(0, 0, 1, 1); ph.alignment = TextAlignmentOptions.MidlineLeft;
            var txr = UiKit.Rt(area, "Text"); var tx = txr.gameObject.AddComponent<TextMeshProUGUI>(); tx.font = UiKit.Body; tx.fontSize = 16; tx.color = Pal.Ink; tx.rectTransform.Anchor(0, 0, 1, 1); /* eigener Text wird nicht übersetzt */ tx.alignment = TextAlignmentOptions.MidlineLeft;
            var inp = bg.gameObject.AddComponent<TMP_InputField>(); inp.textViewport = area; inp.textComponent = tx; inp.placeholder = ph; inp.characterLimit = 24; inp.lineType = TMP_InputField.LineType.SingleLine; inp.fontAsset = UiKit.Body;
            inp.text = value ?? ""; inp.onEndEdit.AddListener(v => { done?.Invoke(v); Game.Toast("Flagge beschriftet: „" + v + "“", "good"); });
        }
        /// <summary>Schieberegler (0–1), z. B. Lautstärke.</summary>
        public void Slider(string title, Func<float> get, Action<float> set)
        {
            var row = UiKit.Rt(sheetContent, title); var vl = row.gameObject.AddComponent<VerticalLayoutGroup>(); vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.spacing = 6; vl.padding = new RectOffset(0, 0, 8, 10);
            var tt = UiKit.Text(row, "Titel", title + " · " + Mathf.RoundToInt(get() * 100) + " %", 15, Pal.Ink, true, FontStyles.Bold); UiKit.LE(tt, 22, 22);
            var bar = UiKit.Glass(row, "Spur", new Color(1, 1, 1, .1f), 6); UiKit.LE(bar, 26, 26);
            var fillArea = UiKit.Rt(bar.transform, "Füllbereich"); fillArea.Anchor(0, 0, 1, 1, 6, 8, 6, 8);
            var fill = UiKit.Glass(fillArea, "Füllung", Pal.Clean, 4); fill.rectTransform.Anchor(0, 0, 0, 1);
            var hArea = UiKit.Rt(bar.transform, "Griffbereich"); hArea.Anchor(0, 0, 1, 1, 10, 0, 10, 0);
            var handle = UiKit.Glass(hArea, "Griff", Pal.Ink, 12); handle.rectTransform.sizeDelta = new Vector2(22, 0); handle.rectTransform.anchorMin = new Vector2(0, 0); handle.rectTransform.anchorMax = new Vector2(0, 1);
            var sl = bar.gameObject.AddComponent<UnityEngine.UI.Slider>(); sl.fillRect = fill.rectTransform; sl.handleRect = handle.rectTransform; sl.targetGraphic = handle; sl.minValue = 0; sl.maxValue = 1; sl.value = get(); bar.raycastTarget = true;
            sl.onValueChanged.AddListener(v => { set(v); tt.text = title + " · " + Mathf.RoundToInt(v * 100) + " %"; });
        }
        public void Bind() { foreach (var b in binders) b(); }

        // ---------- Erklärfenster (Fabian 02.11.): warum etwas gesperrt ist und wie man es freischaltet ----------
        GameObject exGo; TextMeshProUGUI exTitle, exBody; RectTransform exBox;
        public void Explain(string title, string body)
        {
            if (string.IsNullOrEmpty(body)) return;
            if (!exGo)
            {
                var dim = UiKit.Glass(root, "Erklärung", new Color(0, 0, 0, .55f), 0); exGo = dim.gameObject; dim.raycastTarget = true; dim.rectTransform.Anchor(0, 0, 1, 1);
                var box = UiKit.Glass(dim.transform, "Kasten", new Color(.06f, .1f, .18f, 1), 18); box.raycastTarget = true; box.rectTransform.anchorMin = new Vector2(0, .5f); box.rectTransform.anchorMax = new Vector2(1, .5f); box.rectTransform.offsetMin = new Vector2(20, -230); box.rectTransform.offsetMax = new Vector2(-20, 230); exBox = box.rectTransform;
                exTitle = UiKit.Text(box.transform, "Titel", "", 17, Pal.Ink, true, FontStyles.Bold); exTitle.rectTransform.Anchor(0, 1, 1, 1, 20, -54, 20, 18);
                exBody = UiKit.Text(box.transform, "Text", "", 14.5f, Pal.Ink2); exBody.rectTransform.Anchor(0, 0, 1, 1, 20, 76, 20, 60); exBody.overflowMode = TextOverflowModes.Ellipsis; exBody.enableAutoSizing = true; exBody.fontSizeMin = 11; exBody.fontSizeMax = 14.5f; exBody.alignment = TextAlignmentOptions.TopLeft;
                var ok = UiKit.Btn(box.transform, "Verstanden", () => exGo.SetActive(false), Pal.Credit, 12); ((RectTransform)ok.transform).Anchor(.5f, 0, .5f, 0, -90, 16, -90, -64);
                var ot = UiKit.Text(ok.transform, "Text", "Verstanden", 15, Pal.CreditInk, true, FontStyles.Bold, TextAlignmentOptions.Center); ot.rectTransform.Anchor(0, 0, 1, 1);
            }
            exGo.transform.SetAsLastSibling(); exTitle.text = title; exBody.text = body; exGo.SetActive(true);
            /* Kasten so hoch wie der Text (zwischen 220 und 460) */
            float w = Mathf.Max(200, ((RectTransform)root).rect.width - 80); float h = exBody.GetPreferredValues(body, w, 0).y; float half = Mathf.Clamp((h + 76 + 60 + 16) / 2f, 110, 230);
            exBox.offsetMin = new Vector2(20, -half); exBox.offsetMax = new Vector2(-20, half);
        }
        /// <summary>Tipp auf einen grauen Knopf: Grund und Schritte erklären statt nichts zu tun.</summary>
        void Locked(string title, Func<string> btn, Func<string> why, Func<string> desc)
        {
            string label = btn != null ? btn() : "";
            if (label == "Max" || label == "Aktiv" || label == "Gekauft" || label == "Erforscht" || label == "Steht" || label == "Voll" || label == "Verbunden") { Game.Toast(label == "Max" ? "Schon auf der höchsten Stufe" : label == "Voll" ? "Alle Plätze sind belegt" : "Schon erledigt"); return; }
            string w = why?.Invoke(); if (string.IsNullOrEmpty(w)) w = "Das geht gerade noch nicht." + (desc != null && !string.IsNullOrEmpty(desc()) ? "\n\n" + desc() : "");
            Explain(title, w);
        }

        // ---------- Meldungen und Schwebetexte ----------
        string lastToast; float lastToastT = -99;
        public void Toast(string text, string kind)
        {
            if (text == lastToast && Time.unscaledTime - lastToastT < 4f) return; lastToast = text; lastToastT = Time.unscaledTime; /* 04.11.: gleiche Meldung nicht doppelt */
            var g = UiKit.Glass(toastBox, "Meldung", kind == "bad" ? new Color(.25f, .07f, .08f, .94f) : Pal.GlassHi, 14);
            var hl = g.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.padding = new RectOffset(14, 14, 10, 10); hl.childControlHeight = true; hl.childControlWidth = true;
            var t = UiKit.Text(g.transform, "Text", text, 13.5f, kind == "good" ? Pal.Clean : kind == "bad" ? Pal.Warn : Pal.Ink, false, FontStyles.Normal, TextAlignmentOptions.Center);
            UiKit.Add<ContentSizeFitter>(g).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            while (toastBox.childCount > 2) DestroyImmediate(toastBox.GetChild(0).gameObject); /* 04.11.: höchstens 2 Meldungen */
            var cg = g.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false; if (isActiveAndEnabled) StartCoroutine(Fade(cg, 2.4f)); else Destroy(g.gameObject);
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
        bool toastLow; PlanetData totFor; int totCells;
        void Update()
        {
            if (Game.S == null) return; var ps = Game.Cur; var P = ps.P;
            if (Game.NeedClass && !MenuOpen && sheetKind != "class") OpenSheet("class");
            if (Game.StoryNew && !MenuOpen && !SheetOpen && !Game.PuzzleAnim && Time.realtimeSinceStartup >= Game.StoryAfter) { Game.StoryNew = false; if (Game.SecretKnown(Game.Cur)) OpenSheet("secret"); } /* 08.11.: neuer Logbuch-Eintrag */ /* 05.11.: Klasse wählen ist Pflicht – auch in alten Hard-Spielständen */
            /* Fabian 02.11.: beim Abfangen Meldungen nach unten, damit sie keine Brocken verdecken */
            bool icpLow = Game.Icp != null; if (icpLow != toastLow) { toastLow = icpLow; if (icpLow) toastBox.Anchor(0, 0, 1, 0, 24, 40, 24, -170); else toastBox.Anchor(0, 1, 1, 1, 24, -330, 24, 196); }
            credShown = credShown < Game.S.credits ? Math.Min(Game.S.credits, credShown + Math.Max(1, (Game.S.credits - credShown) * Time.deltaTime * 6)) : Game.S.credits;
            credits.text = Game.Fmt(credShown); { int kk = Game.Kristalle; float bl = Game.BoostLeft(Game.Cur); rate.text = (kk > 0 ? kk + " Kristall" + (kk == 1 ? "" : "e") : "") + (bl > 0 ? (kk > 0 ? " · " : "") + "Schub " + Mathf.CeilToInt(bl / 60f) + " min" : ""); }
            pname.text = P.name; pname.fontSize = P.name.Length > 9 ? 11 : P.name.Length > 7 ? 12.5f : 14;
            int c = ps.nClean; if (totFor != ps.pd) { totFor = ps.pd; totCells = ps.pd.cells.Count(x => !x.IsBase); } int tot = totCells; /* Fabian 03.11.: nicht jedes Bild alle Parzellen zählen */ pprog.text = Game.SecretKnown(ps) ? (tot >= 1000 ? Mathf.FloorToInt(100f * c / Mathf.Max(1, tot)) + " %" : c + "/" + tot) + " · Sterne " + Game.StarCount(ps) + "/3" : c + "/" + tot + " sauber · Stufe " + ps.sv.stage;
            pdot.sprite = UiKit.Circle(P.col0, P.col1);
            foreach (var m in meters) { m.v.text = m.val(); UiKit.SetFill(m.fill, m.f()); }
            foreach (var a in liveLabels) a(); lowWarn?.Invoke(); UpdateWarn(ps); UpdateBeltEdit(); UpdateMovePanel(); UpdatePerkBar(); UpdatePerkFx(); UpdateSuit(); UpdateHardMenu(); { bool show = Game.Icp == null && !SheetOpen; /* 04.11.: unter offenen Fenstern nichts durchscheinen lassen */ foreach (var g in bottomBtns) if (g && g.activeSelf != show) g.SetActive(show); }
            float hs = Game.HandSum(), hc = Game.HandCap(); handTxt.text = Game.Fmt(hs) + "/" + Game.Fmt(hc); UiKit.SetFill(handFill, hs / hc);
            handImg.color = hs >= hc * .99f ? new Color(.3f, .08f, .08f, .9f) : Pal.Glass;
            /* Fabian 01.11.: Einführung – die ersten 10 Ziele stehen als „Erste Schritte“ im Bild, danach nur noch im Ziele-Fenster */
            { int q = Game.S.quest; bool tut = !Game.Hard && q < TutorialSteps && q < Game.QuestsSupported && q < GameData.Quests.Count && Game.S.planets.Count <= 1 && ps.sv.stage < 3; /* 04.11.: im Hard Mode gibt es die Hard-Ziele */ /* nur für neue Spieler */ goalGo.SetActive(tut && !SheetOpen && !MenuOpen);
              if (tut) { float pr = Game.QuestProgress(q); goalKick.text = "Erste Schritte · " + (q + 1) + "/" + TutorialSteps + (pr >= 1 ? " · antippen zum Abholen" : ""); goalText.text = GameData.Quests[q].t; goalReward.text = "+" + Game.Fmt(System.Math.Round(GameData.Quests[q].r * Game.GoalF)); UiKit.SetFill(goalFill, pr);
                var gi = goalGo.GetComponent<UnityEngine.UI.Image>(); if (gi) gi.color = pr >= 1 ? Color.Lerp(Pal.Glass, new Color(.35f, .27f, .06f, .95f), .5f + .5f * Mathf.Sin(Time.time * 5)) : Pal.Glass; } }
            { float cg = Game.Sum(ps.sv.sellBuf), cc = Game.SellTarget(ps); bool away = ps.sv.rk > 0; bool hint = Game.CargoHint(ps); rocketTxt.text = away ? "Unterwegs · " + Mathf.CeilToInt(ps.sv.rk) + " s" : hint ? "Laden!" : cg > 0 ? "Starten" : "Leer"; if (cargoTip) { bool show = hint && TipLeft() && !SheetOpen && !MenuOpen && !(pbPop && pbPop.activeSelf) && Game.Icp == null && rocketImg.gameObject.activeInHierarchy; if (cargoTip.activeSelf != show) { cargoTip.SetActive(show); if (show && !tipWas) { tipWas = true; PlayerPrefs.SetInt("LunarCleanup.CargoTipN", PlayerPrefs.GetInt("LunarCleanup.CargoTipN", 0) + 1); } if (!show && !hint) tipWas = false; } if (show) { float k = .5f + .5f * Mathf.Sin(Time.time * 4); cargoTipImg.color = Color.Lerp(new Color(.35f, .27f, .06f, .96f), new Color(.55f, .42f, .1f, .96f), k); cargoTip.transform.localScale = Vector3.one * (1 + .03f * k); } } UiKit.SetFill(rocketFill, away ? 0 : cg / Mathf.Max(1, cc)); rocketImg.color = hint ? Color.Lerp(Pal.Glass, new Color(.45f, .34f, .07f, .95f), .5f + .5f * Mathf.Sin(Time.time * 5)) : !away && cg >= cc * .99f ? new Color(.3f, .22f, .05f, .92f) : Pal.Glass; }
            badgeT -= Time.unscaledDeltaTime; if (badgeT <= 0) { badgeT = .5f; int nc = Game.ClaimableCount(); goalBadge.SetActive(nc > 0); if (nc > 0) goalBadgeTxt.text = nc > 9 ? "9+" : nc.ToString(); }
            foreach (var kv in navIcons) { bool on = kv.Key == (sheetKind ?? "clean"); kv.Value.color = on ? Pal.Clean : Pal.Ink2; navTxt[kv.Key].color = on ? Pal.Clean : Pal.Ink2; }
            bindT += Time.unscaledDeltaTime; if (SheetOpen && bindT > .25f) { bindT = 0; Bind(); }
        }
        public static bool PointerOverUi(int id = -1) { var es = EventSystem.current; if (!es) return false; return id >= 0 ? es.IsPointerOverGameObject(id) : es.IsPointerOverGameObject(); }
    }
}
