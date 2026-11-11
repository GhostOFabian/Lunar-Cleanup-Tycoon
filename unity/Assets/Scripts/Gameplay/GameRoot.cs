// Startpunkt der Unity-Version: baut Welt, Kamera und Oberfläche, verarbeitet Eingaben und treibt die Simulation an.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LunarCleanup.Core;
using LunarCleanup.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace LunarCleanup.Gameplay
{
    public class GameRoot : MonoBehaviour
    {
        PlanetView planet; WorldView world; Hud hud; Camera cam;
        Vector3 cd, north; float alt = 64, altGoal = 64; bool mapMode;
        /// <summary>Editor-Fernsteuerung: gewünschte Kamerahöhe (wird einmal übernommen).</summary>
        public static string MoveReq; /* 03.11.: Gebäude verschieben (aus dem Gebäudefenster) */
        public static float ZoomReq = -1; public static Vector3? FocusReq; public static bool CheckReq, CheckNoPhotos;
        float TILT = 38f * Mathf.Deg2Rad; float lastAng;
        bool offlineRunning; float saveT;
        // Eingabe
        enum Mode { None, Wipe, Pan, Ui } int editDrag = -1; bool moving; /* 10.11.: Band-Editor entfernt */
        Mode mode; Vector2 downPos, lastPos; float downT, lastPinch; bool moved;
        GameObject brush; Light brushLight; Vector3 lastWipe; bool hasLastWipe; Vector2 rotPos, wipeS;
        bool lastTouch; int followCell = -1; Vector2 followRef; // Touch-Ende merken; Kamera folgt der Parzelle, die gerade gereinigt wird
        /// <summary>Bildschirm-Pixel je Layout-Punkt (iPhone ≈ 3, Browser ≈ 1): Tipp-Grenzen in echter Fingergröße statt fester Pixel.</summary>
        static float U => Mathf.Max(1f, Mathf.Min(Screen.width / 390f, Screen.height / 844f));

        UniversalRenderPipelineAsset urpA; bool dailyDone, photo;
#if UNITY_IOS
        const int GfxDefaultSparsam = 1; /* Fabian 01.11.: mit „Hoch“ blieb das iPhone schwarz – Start immer in der sicheren Handy-Stufe, „Hoch“ nur auf Probe */
#else
        const int GfxDefaultSparsam = 0;
#endif
        static bool gfxTrial; static float gfxRevert = -1;
        public static bool GfxHigh => gfxTrial || PlayerPrefs.GetInt("LunarCleanup.GrafikSparsam", GfxDefaultSparsam) == 0;
        public static float GfxTrialLeft => gfxRevert < 0 ? 0 : Mathf.Max(0, gfxRevert - Time.realtimeSinceStartup);
        /// <summary>„Hoch“ ausprobieren: 15 s lang, dann automatisch zurück – außer du tippst „Behalten“ (wie beim Wechsel der Bildschirmauflösung). Bleibt das Bild schwarz, ist nach 15 s alles wieder normal.</summary>
        public static void TryHigh() { gfxTrial = true; gfxRevert = Time.realtimeSinceStartup + 15; ApplyGraphics(); }
        public static void KeepHigh() { gfxTrial = false; gfxRevert = -1; PlayerPrefs.SetInt("LunarCleanup.GrafikSparsam", 0); PlayerPrefs.Save(); ApplyGraphics(); }
        public static void SetLow() { gfxTrial = false; gfxRevert = -1; PlayerPrefs.SetInt("LunarCleanup.GrafikSparsam", 1); PlayerPrefs.Save(); ApplyGraphics(); }
        static void GfxTick() { if (gfxRevert > 0 && Time.realtimeSinceStartup > gfxRevert) { SetLow(); Game.Toast("Grafik zurück auf „Sparsam“", ""); } }
        /// <summary>Grafikstufe setzen: „Hoch“ = PC-Stufe (SSAO, weiche Schatten, Tiefenbild), „Sparsam“ = Handy-Stufe. Danach die URP-Feinheiten wieder anwenden.</summary>
        public static void ApplyGraphics()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            QualitySettings.SetQualityLevel(1, true); /* Browser: PC-Qualität statt Handy-Stufe (volle Auflösung, weiche Schatten, 4 Kaskaden) */
#endif
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.renderScale = 1f; urp.msaaSampleCount = 4; /* Kanten glätten – ohne sahen Bänder und Gebäude im Browser zackig und unscharf aus */
                urp.shadowDistance = Mathf.Max(urp.shadowDistance, 60f); urp.shadowCascadeCount = 4;
                /* feinere, weiche Schatten (sonst nur eckige Klötze): Auflösung 2048, weiche Kanten – Felder sind nicht öffentlich setzbar */
                void SetF(string n, object v) { try { var f = typeof(UniversalRenderPipelineAsset).GetField(n, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); if (f != null) f.SetValue(urp, f.FieldType.IsEnum ? System.Enum.ToObject(f.FieldType, v) : System.Convert.ChangeType(v, f.FieldType)); } catch { } }
                I0urpA = urp;
                urp.maxAdditionalLightsCount = 8;
                try { var f = typeof(UniversalRenderPipelineAsset).GetField("m_AdditionalLightsRenderingMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); if (f != null && (LightRenderingMode)f.GetValue(urp) == LightRenderingMode.Disabled) f.SetValue(urp, LightRenderingMode.PerPixel); } catch { }
            } // Laternen, Scheinwerfer, Saugkopf bei Nacht
        }
        static UniversalRenderPipelineAsset I0urpA;
        void Awake()
        {
            Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0;
            ApplyGraphics(); /* Fabian 01.11.: auch auf dem iPhone die PC-Grafik (Umgebungsverdeckung SSAO, weiche Schatten) – in den Einstellungen abschaltbar */
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            Game.TestMode = UnityEditor.EditorPrefs.GetBool("LunarCleanup.TestMode", false);
#elif DEVELOPMENT_BUILD
            Game.TestMode = PlayerPrefs.GetInt("LunarCleanup.TestMode", 1) == 1; // Browser-Testversion: standardmäßig Testmodus (eigener Spielstand)
#elif UNITY_WEBGL
            Game.TestMode = Game.AdminWeb = AdminCheck(); /* Fabian 02.11.: Admin-Zugang nur für Fabian – Seite einmal mit ?admin=CODE öffnen */
#endif
            Game.Init(); MeshKit.Init(); gameObject.AddComponent<Sfx>();
            cam = Camera.main; if (!cam) { var cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; cam = cg.AddComponent<Camera>(); }
            cam.nearClipPlane = .3f; cam.farClipPlane = 6000; cam.fieldOfView = 50;
            // Nachbearbeitung: Leuchten (Bloom), Filmlook (ACES), leichte Vignette
            var cdata = cam.GetComponent<UniversalAdditionalCameraData>() ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>(); cdata.renderPostProcessing = true; cdata.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var vol = new GameObject("Nachbearbeitung").AddComponent<Volume>(); vol.isGlobal = true; var prof = ScriptableObject.CreateInstance<VolumeProfile>(); vol.profile = prof;
            var bloom = prof.Add<Bloom>(true); bloom.intensity.Override(.9f); bloom.threshold.Override(1.05f); bloom.scatter.Override(.6f);
            var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var vg = prof.Add<Vignette>(true); vg.intensity.Override(.22f); vg.smoothness.Override(.5f);
            var ca = prof.Add<ColorAdjustments>(true); ca.postExposure.Override(.7f); ca.saturation.Override(8f);
            // Oberfläche
            var cv = new GameObject("Oberfläche"); uiRoot = cv; var canvas = cv.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var sc = cv.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(390, 844); sc.matchWidthOrHeight = .5f;
            cv.AddComponent<GraphicRaycaster>();
            if (!FindFirstObjectByType<EventSystem>()) { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>(); }
            hud = cv.AddComponent<Hud>(); hud.Build(cv.transform);
            hud.OnNav = k => { if (k == "clean") { hud.CloseSheet(); FocusFront(); } else hud.OpenSheet(k); };
            hud.OnMap = () => { mapMode = !mapMode; altGoal = mapMode ? Game.Cur.pd.R * 2.3f : 64; };
            hud.OnHome = () => { var ps = Game.Cur; hud.CloseSheet(); StartCoroutine(Fly(ps.pd.cells[ps.pd.baseCell].c, 64)); }; /* Fabian 01.11.: direkt zurück zur Basis-Ansicht */
            hud.OnFocusQuest = FocusQuest; hud.OnPhoto = () => { photo = true; hud.SetPhoto(true); }; hud.OnPhotoExit = () => { photo = false; hud.SetPhoto(false); }; hud.OnFocusEvent = () => { var p = Game.EventPlace(Game.Cur); if (p.HasValue) StartCoroutine(Fly(p.Value, 40)); };
            planet = new GameObject("Planet").AddComponent<PlanetView>(); world = new GameObject("Welt").AddComponent<WorldView>();
            Game.OnDoomImpact += pid => StartCoroutine(DoomImpact(pid)); /* Fabian 01.11.: Riesenmeteor */
            /* Fabian 02.11. (Build 34): Schild, Abfangen, Lieferraketen, Terraforming */
            Game.OnDoomBlocked += pid => { if (pid == Game.S.planet && world) world.ShieldBlock(); };
            Game.OnIcpStart += pid => { if (!world) return; hud.CloseSheet(); world.StartIntercept(); };
            Game.OnSupply += (pid, g, n) => { if (pid != Game.S.planet) return; if (Game.Offline) { offSup = g; return; } if (world) world.SupplyRocket(g, n); };
            Game.OnTerra += (pid, a, b) =>
            {
                if (world && pid == Game.S.planet) world.TerraChanged(a, b);
                /* Fabian 02.11.: der große Moment am Ende jedes Planeten */
                if (b >= Game.TERRA_MAX && a < Game.TERRA_MAX && !Game.Offline && Game.P.ContainsKey(pid))
                {
                    var q = Game.P[pid]; Game.S.stardust += 25; Sfx.Play("medal", 1, 1);
                    hud.Explain("Planet gerettet: " + q.P.name + "!", "Aus einer verdreckten Welt ist ein lebendiger Wald geworden. Danke, Putzkolonne!\n\n+25 Sternenstaub.\n\nDu kannst " + q.P.name + " jetzt ruhen lassen (Basis → Schutz → „Gerettete Welt“): Der Planet liefert dann dauerhaft sein Einkommen, hebt alle Preise um 5 % und braucht keine Pflege mehr – kein Verfall, kein Riesenmeteor. Besuchen kannst du ihn jederzeit.");
                }
            };
            /* Fabian 02.11.: der erste Riesenmeteor wird erklärt – was passiert und was hilft */
            Game.OnDoomStart += pid => { if (!hud || Game.S == null || Game.S.doomN > 0 || PlayerPrefs.GetInt("LunarCleanup.DoomHelp", 0) == 1) return; PlayerPrefs.SetInt("LunarCleanup.DoomHelp", 1);
                bool here = pid == Game.S.planet; hud.Explain("Riesenmeteor im Anflug!", "Ein Riesenmeteor fliegt auf " + GameData.Planets[pid].name + " zu. In 60 Sekunden schlägt er ein." + (here ? "" : "\n\nDu bist gerade nicht dort. Tippe auf das Warnband oben, um hinzufliegen – nur vor Ort kannst du Brocken abfangen.") +
                    "\n\nSo schützt du dich (Basis → Schutz):\n1. Meteorschild: lenkt den Meteor mit etwas Glück ab.\n2. Abwehrraketen: Der Meteor zerbricht in Brocken. Tippe jeden Brocken an, solange sein Ring GRÜN leuchtet.\n\nKommen zu viele Brocken durch, wird die Basis schwer verwüstet: ein großer Krater voller Schmutz, kaputte Fahrzeuge, 10 Minuten gedrosselte Produktion. Gebaut bleibt alles – räum auf, dann läuft es wieder."); };
            WorldView.OnFeedback += (wp, txt, kind) => { if (!cam || !hud) return; var sp = cam.WorldToScreenPoint(wp); if (sp.z > 0) hud.FloatText(sp, txt, kind); };
            hud.MoveOn = () => world && world.Moving; hud.MoveOk = () => world && world.MoveValid;
            hud.MoveTitle = () => world && world.Moving ? GameData.Bld[world.MoveId].name + " verschieben" : "";
            hud.MoveInfo = () => !world || !world.Moving ? "" : (world.MoveWhyText != "" ? "Rot: " + world.MoveWhyText + ". " : "Grün: passt. ") + "Zieh das Gebäude mit dem Finger an einen freien Platz im Basisgelände. Die Bänder werden danach neu verlegt.";
            hud.MoveBtn = k => { if (!world || !world.Moving) return; if (k == "ok") world.CommitMove(); else if (k == "reset") world.ResetMove(); else world.EndMove(); };
            Game.OnPlanetChanged += BuildPlanet; Game.OnFloat += OnFloat; Game.OnBoom += d => { if (!world) return; world.EmitDust(d, new Color(1, .54f, .23f), 40); world.EmitDust(d, new Color(.33f, .33f, .33f), 30); }; Game.OnRocket += (p, v) => { if (world) world.LaunchRocket(); }; Game.OnMeteors += cells => { if (world) world.MeteorShower(cells); }; Game.OnCellFlash += id => { if (world) world.FlashCell(id); }; Game.OnPerk += (id, d, cells) => { if (world) world.PerkFx(id, d, cells); }; /* 05.11.: im Editor bleiben alte Abos nach Stop/Play hängen */ Game.OnDrop += (d, t) => { if (world) world.DropFx(d, t); };
            Game.OnOrderDone += o => { if (o.pid != Game.S.planet) return; if (Game.Offline) { offDone++; return; } if (world && !world.VisitorPickup()) world.CrewCarry(); }; Game.OnOrderNew += o => { if (o.pid != Game.S.planet) return; if (Game.Offline) { offNew++; return; } if (world) world.VisitorArrive(); }; Game.OnCashSent += pid => { if (world) world.CashShip(); }; /* 11.11.: Schiffe auf dem Landeplatz */ /* 10.11.: Crew verlädt die Bestellung */ Game.OnPuzzle += (p, cells) => { if (world) world.PuzzleFx(p); else Game.PuzzleReveal(p); }; /* 09.11.: Puzzle fügt sich zusammen */ Game.OnLevelUp += (l, t) => { if (Game.PuzzleQuiet) { Game.Toast("Putz-Level " + l + (string.IsNullOrEmpty(t) ? "" : " · " + t), "good"); return; } if (hud) hud.BuffPop("Putz-Level " + l, new Color(.55f, 1f, .7f), 0, t); };
            brush = MeshKit.Cyl(null, MeshKit.Unlit(new Color(.43f, .95f, .82f, .35f), true), Vector3.zero, 1, 1, .04f, 40); brush.name = "Saugkopf"; brush.SetActive(false);
            { var bl = new GameObject("Licht"); bl.transform.SetParent(brush.transform, false); bl.transform.localPosition = new Vector3(0, 4.5f, 0); brushLight = bl.AddComponent<Light>(); brushLight.type = LightType.Point; brushLight.range = 16; brushLight.color = new Color(1f, .96f, .88f); brushLight.intensity = 0; brushLight.shadows = LightShadows.None; }
            BuildPlanet();
        }
        void OnDestroy() { Game.OnPlanetChanged -= BuildPlanet; Game.OnFloat -= OnFloat; }

        void BuildPlanet()
        {
            var ps = Game.Cur; var sw = System.Diagnostics.Stopwatch.StartNew(); planet.Build(ps); long tP = sw.ElapsedMilliseconds; world.Build(ps); Debug.Log("Lunar Cleanup: Planet " + ps.pid + " aufgebaut – Boden " + tP + " ms, Basis " + (sw.ElapsedMilliseconds - tP) + " ms"); hud.CloseSheet();
            var c = ps.pd.cells[ps.pd.baseCell]; cd = ps.clean[ps.pd.start] ? c.c : (ps.pd.cells[ps.pd.start].c * .7f + c.c * .3f).normalized; north = c.e2; mapMode = false; alt = altGoal = 64;
            ApplyCam(); Resources.UnloadUnusedAssets(); lastUnload = Time.realtimeSinceStartup; /* Fabian 02.11.: alte Meshes/Materialien des vorigen Planeten freigeben (Browser lief beim Meteor in „OOM“) */
        }
        float lastUnload;

        void Start() { if (!offlineRunning) StartCoroutine(Offline((float)(Game.Now() - Game.S.lastActive))); } /* OnApplicationPause(false) kommt beim Start schon vor Start() – sonst lief die Offline-Zeit doppelt */

        IEnumerator Offline(float seconds)
        {
            float t = Mathf.Min(seconds, Game.OfflineCap); if (t < 5) yield break;
            offlineRunning = true; Game.Offline = true; double c0 = Game.S.credits, e0 = Game.S.stats.earned; float el = 0; int r0 = 0, n0 = 0; foreach (var q in Game.P.Values) { r0 += q.sv.nRockets; n0 += q.nClean; }
            Game.DeferOthers(t); /* Fabian 02.11.: sofort nur dieser Planet – die anderen holen im Hintergrund auf */
            while (el < t)
            {
                float t0 = Time.realtimeSinceStartup;
                float stepS = t > 7200 ? 4 : t > 1800 ? 3 : t > 600 ? 2 : 1; /* Fabian 01.11.: lange Abwesenheit in größeren Schritten – dieselbe Simulation, nur gröber */
                while (el < t && Time.realtimeSinceStartup - t0 < .06f) { float d = Mathf.Min(stepS, t - el); Game.StepOffline(d); el += d; }
                if (t > 600) hud.ShowOffline("Deine Basis hat weitergearbeitet … " + Mathf.FloorToInt(el / t * 100) + " %");
                yield return null;
            }
            Game.Offline = false; offlineRunning = false; hud.ShowOffline(null);
            /* Fabian 11.11.: was offline passiert ist, fliegt beim Start sichtbar an – je Art ein Schiff, nacheinander auf dem Landeplatz */
            if (world) { if (offDone > 0) world.VisitorPickup(); if (offSup != null) world.SupplyRocket(offSup, 1); if (offNew > 0) world.VisitorArrive(); } offDone = offNew = 0; offSup = null;
            double gain = System.Math.Max(Game.S.credits - c0, Game.S.stats.earned - e0); int h = Mathf.FloorToInt(t / 3600), m = Mathf.FloorToInt(t % 3600 / 60); int r1 = 0, n1 = 0; foreach (var q in Game.P.Values) { r1 += q.sv.nRockets; n1 += q.nClean; }
            /* Fabian 01.11.: zeigen, was offline passiert ist – nicht nur Credits */
            Game.Toast("Während du weg warst (" + (h > 0 ? h + " h " : "") + m + " min): +" + Game.Fmt(gain) + " Cr" + (r1 > r0 ? " · " + (r1 - r0) + (r1 - r0 == 1 ? " Rakete" : " Raketen") : "") + (n1 > n0 ? " · " + (n1 - n0) + " Parzellen gereinigt" : ""), "good");
            if (Game.S.wrecks.Count > 0) { Game.Toast("Totalschaden: " + string.Join(", ", Game.S.wrecks) + " – neue Fahrzeuge bauen", "bad"); Game.S.wrecks.Clear(); }
            if (Game.PendingPlanets > 0) { catchC0 = Game.S.credits; catchE0 = Game.S.stats.earned; catchOn = true; Game.Toast("Die anderen " + Game.PendingPlanets + " Planeten rechnen im Hintergrund nach – ihre Einnahmen kommen gleich dazu", ""); }
            Game.Save();
        }
        double catchC0, catchE0; bool catchOn, curCatch;
        /// <summary>Fabian 02.11.: andere Planeten holen ihre Offline-Zeit nach, ein paar Millisekunden pro Bild.</summary>
        void CatchUpOthers()
        {
            if (offlineRunning || Game.S == null) return;
            if (Game.PendingPlanets > 0)
            {
                Game.CatchUpTick(6f, 25f);
                if (Game.Cur.sv.offPend > .5f) { int m = Mathf.CeilToInt(Game.Cur.sv.offPend / 60f); hud.ShowCatchUp("Offline-Zeit wird nachgeholt · noch " + (m >= 60 ? (m / 60) + " h " + (m % 60) + " min" : m + " min")); curCatch = true; }
                else if (curCatch) { curCatch = false; hud.ShowCatchUp(null); }
                return;
            }
            if (curCatch) { curCatch = false; hud.ShowCatchUp(null); }
            if (catchOn) { catchOn = false; double g = System.Math.Max(Game.S.credits - catchC0, Game.S.stats.earned - catchE0); if (g > 0) Game.Toast("Alle Planeten sind auf dem neuesten Stand · zusammen +" + Game.Fmt(g) + " Cr", "good"); Game.Save(); }
        }

        void OnApplicationPause(bool p) { if (p) Game.Save(); else if (!offlineRunning && Game.S != null) StartCoroutine(Offline((float)(Game.Now() - Game.S.lastActive))); }
        void OnApplicationQuit() { Game.Save(); }

        static int offDone, offNew; static string offSup; // 11.11.: Schiffe, die nach der Offline-Zeit beim Start anfliegen
        void Update()
        {
            GfxTick();
            if (Game.S == null) return; float dt = Mathf.Min(Time.deltaTime, .1f);
            { var kb0 = Keyboard.current; bool tl = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Mouse.current.position.ReadValue().x < 80 && Mouse.current.position.ReadValue().y > Screen.height - 80) || (ETouch.activeTouches.Count > 0 && ETouch.activeTouches[0].screenPosition.x < 80 && ETouch.activeTouches[0].screenPosition.y > Screen.height - 80);
              if ((kb0 != null && kb0.pKey.wasPressedThisFrame && preview == null) || (photo && tl)) { photo = !photo; hud.SetPhoto(photo); } }
            if (!dailyDone && !offlineRunning && Time.realtimeSinceStartup > 3) { dailyDone = true; var dtx = Game.DailyCheck(); if (dtx != null) Game.Toast(dtx, "good"); }
            bool menu = Hud.I.MenuOpen; if (menu) { Twist(4 * dt); } /* Startmenü: Kamera dreht sich langsam um die Basis, kein Wischen */
            if (!menu && !Game.PuzzleQuiet) HandleInput(dt);
            var kbd = Keyboard.current; if (kbd != null && !Hud.I.SheetOpen && !menu) { float ps = Screen.height * .7f * dt; /* Pfeiltasten/WASD verschieben, auch während des Wischens */ if (kbd.leftArrowKey.isPressed || kbd.aKey.isPressed) Pan(new Vector2(ps, 0)); if (kbd.rightArrowKey.isPressed || kbd.dKey.isPressed) Pan(new Vector2(-ps, 0)); if (kbd.upArrowKey.isPressed || kbd.wKey.isPressed) Pan(new Vector2(0, -ps)); if (kbd.downArrowKey.isPressed || kbd.sKey.isPressed) Pan(new Vector2(0, ps)); }
            if (kbd != null) { if (kbd.qKey.isPressed) Twist(90 * dt); if (kbd.eKey.isPressed) Twist(-90 * dt); if (kbd.rKey.isPressed || kbd.numpadPlusKey.isPressed) Zoom(1 - 1.2f * dt); if (kbd.fKey.isPressed || kbd.numpadMinusKey.isPressed) Zoom(1 + 1.2f * dt); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevKeys();
#endif
            if (Hud.PerkReq != null) { var pid0 = Hud.PerkReq; Hud.PerkReq = null; var aim = Game.CartGoal.sqrMagnitude > .5f ? Game.CartGoal : Game.Cur.pd.PlanDir(Game.Cur.lay.start); if ((pid0 == "ult" ? Game.UseUlt(aim) : Game.UseTool(pid0, aim)) > 0 && Vector3.Angle(Game.PerkAt, aim) > 4f) FocusReq = Game.PerkAt; } /* 03.11.: Perk sofort, Kamera fliegt hin */
            if (!offlineRunning) Game.Step(dt); CatchUpOthers(); if (Time.realtimeSinceStartup - lastUnload > 180 && !exploding && Game.Icp == null) { lastUnload = Time.realtimeSinceStartup; Resources.UnloadUnusedAssets(); } /* Umbauten erzeugen neue Meshes – alle 3 min aufräumen */
            planet.Tick();
            if (ZoomReq > 0) { altGoal = ZoomReq; ZoomReq = -1; }
            if (MoveReq != null) { var mr = MoveReq; MoveReq = null; if (world) { world.BeginMove(mr); if (Game.Cur.lay.pos.ContainsKey(mr)) { cd = Game.Cur.pd.PlanDir(Game.Cur.lay.pos[mr]); altGoal = Mathf.Clamp(altGoal, 30, 60); } } }
            if (((Game.DoomActive && Game.S.doomPid == Game.S.planet && Game.S.doomT < 12) || Game.Icp != null) && !exploding) { altGoal = Mathf.Max(altGoal, Game.Cur.pd.R * 3.4f); var bc0 = Game.Cur.pd.cells[Game.Cur.pd.baseCell].c; cd = Vector3.Slerp(cd, bc0, dt * .8f).normalized; } /* rauszoomen: man soll den Planeten explodieren sehen */
            if (FocusReq.HasValue) { cd = FocusReq.Value.normalized; FocusReq = null; }
            if (Game.PuzzleQuiet && Game.Cur.pd.BigGlyph != null) { cd = Game.Cur.pd.BigGlyph.d; altGoal = Game.Cur.pd.R * 3f; } /* 10.11.: Kamera steht senkrecht mittig über dem Zeichen, weit genug draußen */
            Game.ViewDir = cd.normalized; Game.ViewCos = Mathf.Cos(Mathf.Clamp(alt * .3f / VP.pd.R, .02f, .5f)); /* sichtbarer Bereich um die Bildmitte */
            alt = Mathf.Lerp(alt, altGoal, 1 - Mathf.Exp(-6 * dt)); ApplyCam(true); Sfx.CamAlt = alt; if (cam) Sfx.CamPos = cam.transform.position; PlanetView.GlowK = Mathf.Clamp01((alt - 35) / (VP.pd.R * 1.2f));
            Game.CartGoal = cd; WorldView.MapMode = mapMode;
            saveT += dt; if (saveT > 10 && !Game.PuzzleQuiet) { saveT = 0; if (!offlineRunning) Game.Save(); }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Test-Tasten nur im Editor/Testbuild (nicht im Spiel-Build): F5 Parzelle vorne säubern, F6 +10.000 Cr, F7 8 h offline simulieren, F8 Planet zu 81 % (nochmal: 100 %) säubern, F9 Vierteltag vorspulen, F10 Sturm in 25 s.
        void DevKeys()
        {
            var kb = Keyboard.current; if (kb == null || !Game.TestMode) return; // Test-Tasten nur im Testmodus (eigener Spielstand)
            if (kb.f5Key.wasPressedThisFrame) { var ps = Game.Cur; int best = -1; float bd = -2; foreach (int id in ps.front) { float d = Vector3.Dot(ps.pd.cells[id].c, cd); if (d > bd) { bd = d; best = id; } } if (best >= 0) Game.DevClean(best); }
            if (kb.f6Key.wasPressedThisFrame) Game.Earn(10000);
            if (kb.f11Key.wasPressedThisFrame && preview == null && Application.isEditor) StartCoroutine(CheckAllBases());
            if (CheckReq && preview == null) { CheckReq = false; StartCoroutine(CheckAllBases()); }
            if (kb.f12Key.wasPressedThisFrame) Game.DevMaxOut();
            if (kb.f4Key.wasPressedThisFrame && Application.isEditor) { if (kb.shiftKey.isPressed) StartCoroutine(AmpelShots("moon")); else StartCoroutine(BeltShots("moon")); }
            if (kb.f1Key.wasPressedThisFrame && Application.isEditor) StartCoroutine(SkyShots());
            if (kb.f3Key.wasPressedThisFrame) { var cs = Game.Cur; if (cs.sv.ev != null) { cs.sv.ev.key = ""; cs.sv.ev.t = 0; } Game.StartEvent(cs, kb.shiftKey.isPressed ? "wreck" : "impact"); } /* Test: F3 Kometeneinschlag, Umschalt+F3 Wrack-Absturz */
            if (kb.f2Key.wasPressedThisFrame && Application.isEditor) { string wd = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../BasisCheck/Web")); System.IO.Directory.CreateDirectory(wd); string fn = System.IO.Path.Combine(wd, "web_" + System.DateTime.Now.ToString("HHmmss") + ".png"); ScreenCapture.CaptureScreenshot(fn, 2); Debug.Log("Lunar Cleanup: Webseiten-Foto " + fn); } /* F2: Bildschirmfoto mit HUD für die Webseite */
            if (kb.f9Key.wasPressedThisFrame) { var cs = Game.Cur.sv; cs.dayT = (cs.dayT < 0 ? 0 : cs.dayT) + Game.DayLen(Game.Cur.pid) / 4; }
            if (kb.f10Key.wasPressedThisFrame && Game.Cur.P.storm != null) { var cs = Game.Cur.sv; cs.hzOn = 0; cs.hzWarned = false; cs.hzT = Game.Cur.P.storm.every - 25; }
            if (kb.f8Key.wasPressedThisFrame) Game.DevCleanTo(Game.CleanPct(Game.Cur) >= .8f ? 1.01f : .81f);
            if (kb.f7Key.wasPressedThisFrame && !offlineRunning) StartCoroutine(Offline(8 * 3600));
        }
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>F4 (Testmodus, Editor): Nahaufnahmen der Bänder mit Waren und Stützen nach BasisCheck/band_*.png.</summary>
        /// <summary>F1 (Testmodus, Editor): Nachbarkörper am Himmel auf allen Planeten fotografieren (BasisCheck/Himmel).</summary>
        IEnumerator SkyShots()
        {
            string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../BasisCheck/Himmel")); System.IO.Directory.CreateDirectory(dir);
            if (uiRoot) uiRoot.SetActive(false);
            foreach (var pid in GameData.POrder)
            {
                PState pv; try { pv = Game.Preview(pid, 0, 1); } catch (System.Exception e) { Debug.LogException(e); continue; }
                preview = pv; world.Build(pv); yield return null; yield return null;
                var body = FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.name.StartsWith("Nachbar")); if (body == null) continue;
                float R = pv.pd.R; var bd = body.position.normalized;
                for (int k = 0; k < 3; k++) { yield return null; cam.transform.position = bd * (R + 6) - Vector3.Cross(bd, Vector3.up).normalized * 2; cam.transform.LookAt(body.position); }
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, pid + ".png"), 1); yield return null; yield return null;
            }
            preview = null; if (uiRoot) uiRoot.SetActive(true); BuildPlanet(); Debug.Log("Lunar Cleanup: Himmelsfotos fertig");
        }
        /// <summary>Umschalt+F4 (Testmodus, Editor): jede Ampel der fertigen Basis schräg von der Seite fotografieren (BasisCheck/Ampeln).</summary>
        IEnumerator AmpelShots(string pid)
        {
            string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../BasisCheck/Ampeln")); System.IO.Directory.CreateDirectory(dir);
            foreach (var f in System.IO.Directory.GetFiles(dir, "*.png")) System.IO.File.Delete(f);
            if (uiRoot) uiRoot.SetActive(false);
            PState pv = null; try { pv = Game.Preview(pid, 0); } catch (System.Exception e) { Debug.LogException(e); }
            if (pv != null)
            {
                pv.sv.stage = Mathf.Max(pv.sv.stage, 3); mapMode = false; WorldView.MapMode = false; alt = altGoal = 12; preview = pv; planet.Build(pv); world.Build(pv); for (int q = 0; q < 6; q++) yield return null; yield return new WaitForSeconds(1.6f);
                float R = pv.pd.R;
                var amps = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name.StartsWith("Ampel ")).ToList(); foreach (var a in amps) a.gameObject.SetActive(true);
                var log = new System.Text.StringBuilder();
                foreach (var a in amps)
                {
                    var up = a.position.normalized; var side = Vector3.Cross(up, Vector3.forward); if (side.sqrMagnitude < .01f) side = Vector3.Cross(up, Vector3.right); side.Normalize(); var fw = Vector3.Cross(side, up);
                    log.AppendLine(a.name + " (an " + (a.parent ? a.parent.name : "-") + ") Höhe über Boden " + (a.position.magnitude - R).ToString("0.00") + " m");
                    foreach (var (nm, s, h, b) in new[] { ("a", 4.5f, 1.2f, -4.5f), ("b", -4.5f, 2.5f, 4.5f) })
                    {
                        yield return null; WorldView.NightF = 0;
                        cam.transform.position = a.position + side * s + up * h + fw * b; cam.transform.LookAt(a.position - up * .8f, up);
                        yield return new WaitForEndOfFrame();
                        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, a.name.Replace(' ', '_') + "_" + nm + ".png"), 1); yield return null;
                    }
                }
                log.AppendLine(amps.Count + " Ampeln, Stufe " + Game.Gs(pv)); System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "ampeln.txt"), log.ToString());
            }
            preview = null; if (uiRoot) uiRoot.SetActive(true); BuildPlanet(); Debug.Log("Lunar Cleanup: Ampelfotos fertig (" + dir + ")");
        }
        IEnumerator BeltShots(string pid)
        {
            string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../BasisCheck/Baender")); System.IO.Directory.CreateDirectory(dir);
            foreach (var f in System.IO.Directory.GetFiles(dir, "*.png")) System.IO.File.Delete(f);
            if (uiRoot) uiRoot.SetActive(false); var ord = Game.BuildOrder(); bool first = true; string lastSig = null;
            for (int k = 1; k <= ord.Count; k++)
            {
                PState pv; try { pv = Game.Preview(pid, 0, k); } catch (System.Exception e) { Debug.LogException(e); continue; }
                string sig = string.Join(",", pv.lay.belts.Where(b => Game.BeltLive(pv, b) && !Game.HiddenBelt(b)).Select(b => b.id)); if (sig == "" || sig == lastSig) continue; lastSig = sig; /* nur Stufen mit neuen Bändern */
                preview = pv; if (first) { planet.Build(pv); first = false; }
                // Tag: Sonne möglichst hoch über der Basis
                var bc = pv.pd.cells[pv.pd.baseCell].c; float bestT = 0, bestD = -2, DL = Game.DayLen(pid); for (int q = 0; q < 48; q++) { pv.sv.dayT = DL * q / 48f; float dd = Vector3.Dot(Game.SunDir(pv), bc); if (dd > bestD) { bestD = dd; bestT = pv.sv.dayT; } } pv.sv.dayT = bestT;
                world.Build(pv); yield return null;
                foreach (var (id, d, tan) in world.BeltViews())
                {
                    var bd = pv.lay.belts.Find(x => x.id == id); if (bd == null) continue;
                    if (!pv.belts.TryGetValue(id, out var its)) { its = new List<BeltItem>(); pv.belts[id] = its; }
                    if (its.Count == 0) for (float q = bd.L * .9f; q > bd.L * .1f; q -= 1.1f) its.Add(new BeltItem { s = q, m = (int)(q * 7) % GameData.Mats.Count });
                    cd = d; north = tan; alt = altGoal = 12; ApplyCam(); float R = pv.pd.R; var side = Vector3.Cross(d, tan).normalized;
                    foreach (var (nm, sd, h, back) in new[] { ("a", 3.2f, 1.6f, -2.5f), ("b", -3.2f, 1.6f, 2.5f), ("c", .01f, 6f, -7f) })
                    {
                        yield return null; WorldView.NightF = 0;
                        cam.transform.position = d * (R + h) + side * sd + tan * back; cam.transform.LookAt(d * (R + .7f), d);
                        yield return new WaitForEndOfFrame();
                        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, pid + "_k" + k.ToString("00") + "_" + id + "_" + nm + ".png"), 1); yield return null;
                    }
                }
            }
            preview = null; if (uiRoot) uiRoot.SetActive(true); BuildPlanet(); Debug.Log("Lunar Cleanup: Bandfotos fertig (" + dir + ")");
        }
#endif
        // ---------- Kamera ----------
        // ---------- Basis-Prüfung (Testmodus, F11): komplette Basis auf jedem Planeten bauen, prüfen, fotografieren ----------
        GameObject uiRoot; PState preview; PState VP => preview ?? Game.Cur;
        IEnumerator CheckAllBases()
        {
            string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../BasisCheck")); System.IO.Directory.CreateDirectory(dir);
            var rep = new System.Text.StringBuilder(); rep.AppendLine("Basis-Prüfung " + System.DateTime.Now.ToString("dd.MM.yyyy HH:mm")); int total = 0;
            var list = new List<(string pid, int f)>(); foreach (var pid in GameData.POrder) list.Add((pid, 0));
            foreach (var k in GameData.Layouts.Keys) { int at = k.IndexOf('@'); if (at > 0) list.Add((k.Substring(0, at), int.Parse(k.Substring(at + 1)))); }
            if (uiRoot) uiRoot.SetActive(false);
            foreach (var (pid, f) in list)
            {
                string key = f > 0 ? pid + "@" + f : pid;
                List<string> iss; string err = null;
                try { preview = Game.Preview(pid, f); planet.Build(preview); world.Build(preview); var c = preview.pd.cells[preview.pd.baseCell]; cd = c.c; north = c.e2; mapMode = false; WorldView.MapMode = false; }
                catch (System.Exception e) { err = e.GetType().Name + ": " + e.Message; Debug.LogException(e); }
                yield return null; yield return null;
                try { iss = err != null ? new List<string> { "FEHLER beim Aufbau: " + err } : world.CheckBase(); } catch (System.Exception e) { iss = new List<string> { "FEHLER bei der Prüfung: " + e.Message }; Debug.LogException(e); }
                total += iss.Count;
                rep.AppendLine(); rep.AppendLine("== " + GameData.Planets[pid].name + (f > 0 ? " (alte Größe " + f + ")" : "") + ": " + (iss.Count == 0 ? "alles in Ordnung" : iss.Count + " Befunde"));
                foreach (var s in iss) rep.AppendLine("  - " + s);
                foreach (var s in world.Notes) rep.AppendLine("  · " + s);
                var n0 = north;
                foreach (var (a, nm, rot) in new[] { (95f, "oben", 0f), (46f, "nord", 0f), (46f, "ost", 90f), (46f, "sued", 180f), (46f, "west", 270f) }) // schräg aus allen vier Richtungen
                {
                    if (f > 0 && nm != "oben" || CheckNoPhotos) continue; // alte Planetengrößen: nur Draufsicht; Fernprüfung ohne Fotos
                    north = Quaternion.AngleAxis(rot, cd) * n0;
                    alt = altGoal = a; WorldView.NightF = 0; ApplyCam(); yield return null; WorldView.NightF = 0; yield return null; yield return new WaitForEndOfFrame(); // Fotos immer bei Tag
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, key + "_" + nm + ".png"), 2); yield return null; yield return null;
                }
                // Bohrturm und Erzbagger aus der Nähe
                if (f == 0 && preview != null && !CheckNoPhotos) foreach (var r in preview.sv.rigs)
                    {
                        cd = preview.pd.cells[r.cell].c; north = preview.pd.cells[r.cell].e2; alt = altGoal = 20; WorldView.NightF = 0; ApplyCam(); yield return null; yield return null; yield return new WaitForEndOfFrame();
                        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, key + (r.bag ? "_bagger" : "_bohrturm") + ".png"), 2); yield return null; yield return null;
                    }
                // Teil-Ausbauten: nach jedem gekauften Gebäude (so wie beim echten Spielen) – jede Zwischenstufe hat andere Bänder und Wege
                if (f == 0)
                {
                    var ord = Game.BuildOrder(); int partBad = 0;
                    for (int k = 1; k < ord.Count; k++)
                    {
                        List<string> iss2 = null; bool any = false;
                        try
                        {
                            preview = Game.Preview(pid, 0, k); any = preview.lay.belts.Any(b => Game.BeltLive(preview, b) && !Game.HiddenBelt(b));
                            if (any) { world.Build(preview); }
                        }
                        catch (System.Exception e) { iss2 = new List<string> { "FEHLER beim Aufbau: " + e.Message }; Debug.LogException(e); }
                        if (!any && iss2 == null) continue;
                        yield return null;
                        if (iss2 == null) { try { iss2 = world.CheckBase(); } catch (System.Exception e) { iss2 = new List<string> { "FEHLER bei der Prüfung: " + e.Message }; } }
                        if (iss2.Count == 0) continue;
                        total += iss2.Count; partBad++;
                        rep.AppendLine("  == Teilausbau " + k + " (bis " + GameData.Bld[ord[k - 1]].name + "): " + iss2.Count + " Befunde");
                        foreach (var s in iss2) rep.AppendLine("    - " + s);
                        if (partBad <= 3 && !CheckNoPhotos)
                        {
                            var c = preview.pd.cells[preview.pd.baseCell]; cd = c.c; north = c.e2; alt = altGoal = 70; WorldView.NightF = 0; ApplyCam(); yield return null; yield return new WaitForEndOfFrame();
                            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, key + "_teil" + k + ".png"), 2); yield return null; yield return null;
                        }
                    }
                }
            }
            rep.AppendLine(); rep.AppendLine("Gesamt: " + total + " Befunde");
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "bericht.txt"), rep.ToString());
            Debug.Log("Lunar Cleanup: Basis-Prüfung fertig – " + total + " Befunde, Bericht in " + dir);
            preview = null; if (uiRoot) uiRoot.SetActive(true); BuildPlanet();
        }

        Vector3 cdS, northS; bool camS; Vector2 fling, panVel; float lastPanT;
        /// <summary>Kamera setzen. smooth: Fabian 04.11. – die sichtbare Kamera gleitet weich hinterher (kurze Wege schnell, weite Sprünge wie ein Flug).</summary>
        void ApplyCam(bool smooth = false)
        {
            float R = VP.pd.R; float tilt = Game.PuzzleQuiet ? 0 : Mathf.Lerp(TILT, 0, Mathf.InverseLerp(60, R * 2f, alt));
            north = (north - cd * Vector3.Dot(north, cd)).normalized;
            if (!smooth || !camS) { cdS = cd; northS = north; camS = true; }
            else { float ang = Vector3.Angle(cdS, cd), dt0 = Mathf.Min(Time.unscaledDeltaTime, .05f); float k = 1 - Mathf.Exp(-(ang > 6 ? 4.5f : 20f) * dt0); cdS = Vector3.Slerp(cdS, cd, k).normalized; northS = Vector3.Slerp(northS, north, k); northS = (northS - cdS * Vector3.Dot(northS, cdS)).normalized; if (northS.sqrMagnitude < .5f) northS = north; }
            var c0 = cdS; var n0 = northS;
            var focus = c0 * R; var pos = focus + (c0 * Mathf.Cos(tilt) - n0 * Mathf.Sin(tilt)) * alt;
            cam.transform.position = pos; cam.transform.rotation = Quaternion.LookRotation(focus - pos, n0 * Mathf.Cos(tilt) + c0 * Mathf.Sin(tilt));
            if (WorldView.Shake > 0) cam.transform.position += (cam.transform.right * (Mathf.PerlinNoise(Time.time * 25, 0) - .5f) + cam.transform.up * (Mathf.PerlinNoise(0, Time.time * 25) - .5f)) * WorldView.Shake * Mathf.Clamp(alt * .03f, .3f, 4f); // Meteoriteneinschlag
        }
        void Pan(Vector2 delta)
        {
            float R = VP.pd.R, mpp = 2 * alt * Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad) / Screen.height;
            var right = Vector3.Cross(cd, north).normalized * -1; // Bildschirm-rechts auf der Oberfläche
            right = cam.transform.right; right = (right - cd * Vector3.Dot(right, cd)).normalized;
            var move = (-right * delta.x - north * delta.y) * mpp; float ang = move.magnitude / R; if (ang < 1e-7f) return;
            var axis = Vector3.Cross(cd, move.normalized); var q = Quaternion.AngleAxis(ang * Mathf.Rad2Deg, axis); cd = (q * cd).normalized; north = q * north;
        }
        void Twist(float deg) { if (Mathf.Abs(deg) < 1e-4f) return; north = Quaternion.AngleAxis(deg, cd) * north; }
        void Zoom(float f) { float R = VP.pd.R; altGoal = Mathf.Clamp(altGoal * f, 9, R * 4.5f); mapMode = altGoal > R * 1.5f; } /* weit raus: ganzer Planet mit Atmosphäre, Lichtern und Kometen */
        /// <summary>Tipp auf das Ziel: an die passende Stelle fliegen bzw. das passende Fenster öffnen.</summary>
        public void FocusQuest()
        {
            var ps = Game.Cur; string t = Game.QuestTarget(Game.S.quest);
            if (t == "front") { FocusFront(); return; }
            if (t == "hand") { if (ps.lay.pos.TryGetValue("roh", out var rp)) StartCoroutine(Fly(ps.pd.PlanDir(rp), 40)); Game.Toast("Tippe unten links auf den Behälter, um ihn zu leeren"); return; }
            if (t.StartsWith("sheet:")) { hud.OpenSheet(t.Substring(6)); return; }
            if (t.StartsWith("build:"))
            {
                string id = t.Substring(6);
                if (ps.lay.pos.TryGetValue(Game.IsUnit(id) ? "u1" : id, out var bp)) StartCoroutine(Fly(ps.pd.PlanDir(bp), 40));
                hud.OpenSheet(ps.bld.Contains(id) ? "building" : "base", ps.bld.Contains(id) ? id : null); return;
            }
            if (t == "outpost")
            {
                var sites = Game.OutpostSites(ps.pid); int best = -1; float bd = -2; foreach (int c in sites) { float d = Vector3.Dot(ps.pd.cells[c].c, cd); if (d > bd) { bd = d; best = c; } }
                if (best >= 0) { StartCoroutine(Fly(ps.pd.cells[best].c, 40)); hud.OpenSheet("outpost", best.ToString()); } return;
            }
            FocusFront();
        }
        public void FocusFront()
        {
            var ps = Game.Cur; if (ps.front.Count == 0) return; int best = -1; float bd = -2;
            if (!ps.clean[ps.pd.start] && Game.Workable(ps, ps.pd.start)) { StartCoroutine(Fly(ps.pd.cells[ps.pd.start].c, 34)); return; }
            foreach (int id in ps.front.Concat(ps.act.Keys)) { float d = Vector3.Dot(ps.pd.cells[id].c, cd); if (d > bd) { bd = d; best = id; } }
            if (best >= 0) StartCoroutine(Fly(ps.pd.cells[best].c, 34));
        }
        IEnumerator Fly(Vector3 to, float a)
        {
            var from = cd; float t = 0; altGoal = a; mapMode = false;
            while (t < 1) { t += Time.deltaTime * 1.6f; var n0 = cd; cd = Vector3.Slerp(from, to, Mathf.SmoothStep(0, 1, t)).normalized; var q = Quaternion.FromToRotation(n0, cd); north = q * north; yield return null; }
        }

        // ---------- Eingabe ----------
        void HandleInput(float dt)
        {
            var touches = ETouch.activeTouches;
            if (touches.Count >= 2)
            {
                var a = touches[0].screenPosition; var b = touches[1].screenPosition; float d = Vector2.Distance(a, b); var mid = (a + b) / 2;
                float ang = Mathf.Atan2(b.y - a.y, b.x - a.x);
                if (mode != Mode.Pan || lastPinch <= 0) { if (mode == Mode.Wipe) EndWipe(); mode = Mode.Pan; lastPinch = d; lastPos = mid; lastAng = ang; moved = true; return; }
                // zwei Finger: zusammenziehen = Zoom, verschieben = Drehen über den Planeten, verdrehen = Blickrichtung drehen
                if (d > 1) Zoom(lastPinch / d); Pan(mid - lastPos); Twist(Mathf.DeltaAngle(lastAng * Mathf.Rad2Deg, ang * Mathf.Rad2Deg)); lastPinch = d; lastPos = mid; lastAng = ang; return;
            }
            lastPinch = 0;
            bool pressed; Vector2 pos; int pid = -1;
            if (touches.Count == 1) { pressed = true; pos = touches[0].screenPosition; pid = touches[0].touchId; lastTouch = true; }
            else if (lastTouch) { pressed = false; pos = lastPos; lastTouch = false; } /* Finger gerade gehoben: letzte Fingerposition (auf dem iPhone gibt es keine Maus – vorher wurde bei (0,0) getippt) */
            else if (Mouse.current != null && (Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed) && mode == Mode.Wipe && Mouse.current.leftButton.isPressed)
            {
                /* beim Wischen (linke Taste) zusätzlich rechts ziehen = Kamera drehen, der Wisch läuft weiter */
                var mp = Mouse.current.position.ReadValue(); if (Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame) rotPos = mp;
                var dm = mp - rotPos; Twist(-dm.x * .25f); TILT = Mathf.Clamp(TILT + dm.y * .004f, 0, 70 * Mathf.Deg2Rad); rotPos = mp; pressed = true; pos = mp;
            }
            else if (Mouse.current != null && (Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed))
            {
                // PC: rechte/mittlere Maustaste ziehen = Blickrichtung drehen (links/rechts) und neigen (hoch/runter)
                var mp = Mouse.current.position.ReadValue(); if (Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame) lastPos = mp;
                var dm = mp - lastPos; Twist(-dm.x * .25f); TILT = Mathf.Clamp(TILT + dm.y * .004f, 0, 70 * Mathf.Deg2Rad); lastPos = mp; mode = Mode.None; return;
            }
            else if (Mouse.current != null) { pressed = Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasPressedThisFrame; pos = Mouse.current.position.ReadValue(); float sc = Mouse.current.scroll.ReadValue().y; if (Mathf.Abs(sc) > .01f && !Hud.PointerOverUi()) Zoom(sc > 0 ? .88f : 1.14f); }
            else { pressed = false; pos = Vector2.zero; }

            /* Fabian 04.11.: weichere Kamera – nach dem Loslassen gleitet der Planet mit Schwung aus */
            if (!pressed && fling.sqrMagnitude > 400) { Pan(fling * dt); fling *= Mathf.Exp(-4.5f * dt); } else if (!pressed) fling = Vector2.zero;
            if (pressed && mode == Mode.None)
            {
                fling = Vector2.zero; panVel = Vector2.zero;
                downPos = lastPos = pos; downT = Time.time; moved = false;
                if (Hud.PointerOverUi(pid) || offlineRunning) { mode = Mode.Ui; return; }
                editDrag = -1; moving = false; if (world.Moving) { moving = Hit(pos, out var dm0); if (moving) world.MoveTo(Game.Cur.pd.PlanLocal(dm0)); mode = Mode.Pan; } /* 03.11.: Gebäude ziehen */
                else if (!mapMode && Hit(pos, out var dir) && (Game.CanClean(dir) || Game.FindAt(dir)) && !Hud.I.SheetOpen) { mode = Mode.Wipe; Game.BeginStroke(); hasLastWipe = false; followCell = Game.Cur.pd.CellOf(dir); followRef = pos; wipeS = pos; }
                else mode = Mode.Pan;
            }
            if (pressed)
            {
                if ((pos - downPos).magnitude > 12 * U) moved = true;
                if (mode == Mode.Wipe)
                {
                    /* Am Bildschirmrand weiterwischen: der Planet dreht sich langsam mit, so erreichst du auch Stellen außerhalb des Bildes */
                    float ex = pos.x / Screen.width, ey = pos.y / Screen.height, edge = .1f; var ep = Vector2.zero;
                    if (ex < edge) ep.x = (edge - ex) / edge; else if (ex > 1 - edge) ep.x = -(ex - (1 - edge)) / edge;
                    if (ey < edge * 1.6f) ep.y = (edge * 1.6f - ey) / (edge * 1.6f); else if (ey > 1 - edge * 2.2f) ep.y = -(ey - (1 - edge * 2.2f)) / (edge * 2.2f);
                    if (ep != Vector2.zero) Pan(ep * Screen.height * .45f * dt);
                    /* Fabian 01.11.: Glättung (Build 22) fühlte sich schlechter an – der Pinsel sitzt wieder direkt unter dem Finger */
                    if (Hit(pos, out var dir))
                    {
                        // Zwischenpunkte, damit auch schnelle Striche lückenlos reinigen
                        float R0 = Game.Cur.pd.R, rr = Game.Uv("radius"); int steps = hasLastWipe ? Mathf.Clamp(Mathf.CeilToInt(Game.Cur.pd.Arc(lastWipe, dir) / (rr * .5f)), 1, 12) : 1; float amt = 0; int mat = -1;
                        /* Fabian 01.11.: Wischen fühlte sich klebrig an – zügige Striche reinigen bis zu 3× mehr je Bild, damit man nicht hängen bleibt */
                        float spd = hasLastWipe ? Game.Cur.pd.Arc(lastWipe, dir) / Mathf.Max(.001f, dt) : 0, boost = 1 + 2 * Mathf.Clamp01((spd / Mathf.Max(.5f, rr) - 2f) / 10f);
                        for (int s = 1; s <= steps; s++) { var dd = hasLastWipe ? Vector3.Slerp(lastWipe, dir, s / (float)steps).normalized : dir; amt += Game.Stroke(dd, dt * boost / steps, out int m0); if (m0 >= 0) mat = m0; Game.FindWipe(dd, dt / steps); }
                        lastWipe = dir; hasLastWipe = true; WorldView.WipeDir = dir;
                        brush.SetActive(true); float r = Game.Uv("radius") * Game.RadiusF; brush.transform.position = Game.Cur.pd.Surf(dir, .08f); brush.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir); brush.transform.localScale = new Vector3(r, 1, r); brushLight.intensity = 9f * Mathf.Clamp01((WorldView.NightF - .1f) / .5f); /* hell und warmweiß – nachts sieht man, was man putzt */
                        Sfx.Wipe(true, spd / Mathf.Max(.5f, rr), amt > 0);
                        FollowWipe(dir, pos, dt); /* Kamera folgt wieder der Putzstelle (wie bis Build 21) */
                        if (amt > 0) { int c = Game.Cur.pd.CellOf(dir); int nd = Mathf.Clamp(Mathf.RoundToInt(1 + spd / Mathf.Max(.5f, rr) * .4f), 1, 6); world.EmitDust(dir, GameData.Dirt[Game.Cur.pd.cells[c].type].lightC, nd); }
                    }
                }
                else if (mode == Mode.Pan)
                {
                    if (moving && world.Moving) { if (Hit(pos, out var d4)) world.MoveTo(Game.Cur.pd.PlanLocal(d4)); }
                    else { var dl = pos - lastPos; if (dl.sqrMagnitude > .01f) lastPanT = Time.time; Pan(dl); panVel = Vector2.Lerp(panVel, dl / Mathf.Max(.001f, dt), .5f); } /* 04.11.: Schwung merken */
                }
                lastPos = pos; return;
            }
            if (mode == Mode.Wipe) EndWipe();
            if (moving) { moving = false; mode = Mode.None; return; }
            if (mode == Mode.Pan && !moved && Time.time - downT < .7f && editDrag < 0) Tap(pos);
            if (mode == Mode.Pan && moved && editDrag < 0 && !world.Moving && Time.time - lastPanT < .08f) fling = Vector2.ClampMagnitude(panVel, Screen.height * 2.5f); panVel = Vector2.zero;
            mode = Mode.None; editDrag = -1;
        }
        void EndWipe() { brush.SetActive(false); mode = Mode.None; Sfx.Wipe(false); WorldView.WipeDir = Vector3.zero; }
        /// <summary>Beim Putzen gleitet die Kamera sanft zur Parzelle, die gerade gereinigt wird; ist sie sauber, zur dreckigen Nachbarparzelle.
        /// Die Zielparzelle wechselt nur, wenn der Finger wirklich weiterwischt – hält man still, wandert die Kamera nicht von selbst weiter.</summary>
        void FollowWipe(Vector3 dir, Vector2 pos, float dt)
        {
            var ps = Game.Cur; var pd = ps.pd; int fc = pd.CellOf(dir);
            if (followCell < 0) { followCell = fc; followRef = pos; }
            if (fc != followCell && !ps.clean[fc] && (pos - followRef).magnitude > 40 * U) { followCell = fc; followRef = pos; }
            if (ps.clean[followCell] && ps.act.ContainsKey(followCell) == false)
            {
                int nb = -1; float bd = -2; foreach (int n in pd.cells[followCell].nb) { if (ps.clean[n] || pd.cells[n].IsBase || !Game.Workable(ps, n)) continue; float d = Vector3.Dot(pd.cells[n].c, dir); if (d > bd) { bd = d; nb = n; } }
                if (nb >= 0) { followCell = nb; followRef = pos; }
            }
            var to = pd.cells[followCell].c; float k = 1 - Mathf.Exp(-.55f * dt); /* sanfter: die Stelle unter dem Finger rutscht nicht mehr weg */ var n0 = cd; cd = Vector3.Slerp(cd, to, k).normalized; north = Quaternion.FromToRotation(n0, cd) * north;
        }

        bool Hit(Vector2 screen, out Vector3 dir) => PlanetView.RayHit(cam.ScreenPointToRay(screen), Game.Cur.pd, out dir);

        bool exploding;
        /// <summary>Browser: Admin-Modus (Test-Knöpfe + eigener Test-Spielstand). Einmal die Seite mit ?admin=CODE öffnen, danach merkt sich der Browser das.</summary>
        static bool AdminCheck()
        {
            const string H = "7f6681ab5da2901c3eaa9d35786a0e9db36fb19a79bc9e58202ae41562eea7fc";
            string Hash(string t) { using (var sha = System.Security.Cryptography.SHA256.Create()) { var b = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(t)); var sb = new System.Text.StringBuilder(); foreach (var x in b) sb.Append(x.ToString("x2")); return sb.ToString(); } }
            try
            {
                string url = Application.absoluteURL ?? ""; int i = url.IndexOf("admin=", System.StringComparison.Ordinal);
                if (i >= 0) { string code = url.Substring(i + 6); int e = code.IndexOfAny(new[] { '&', '#' }); if (e >= 0) code = code.Substring(0, e); code = System.Uri.UnescapeDataString(code).Trim();
                    if (code == "aus") { PlayerPrefs.DeleteKey("LunarCleanup.Admin"); PlayerPrefs.Save(); return false; }
                    if (Hash(code) == H) { PlayerPrefs.SetString("LunarCleanup.Admin", H); PlayerPrefs.Save(); return true; } }
                return PlayerPrefs.GetString("LunarCleanup.Admin", "") == H;
            }
            catch { return false; }
        }
        IEnumerator DoomImpact(string pid)
        {
            if (pid != Game.S.planet || exploding) { Game.Devastate(pid, Game.DoomSev); yield break; }
            exploding = true; hud.CloseSheet(); altGoal = Mathf.Max(altGoal, Game.Cur.pd.R * 3.6f); yield return world.DoomDive(); yield return world.Implode(planet.transform); /* Fabian 02.11.: Implosion mit Explosion, ganzer Planet im Bild */
            Game.Devastate(pid, Game.DoomSev); yield return new WaitForSeconds(.5f); yield return world.Reform(); exploding = false; /* … dann setzt er sich verwüstet wieder zusammen – nichts Gebautes geht verloren */
        }
        /// <summary>10.11.: liegt der Weltpunkt auf der zugewandten Seite des Planeten? (Rückseite wird sonst mitten in den Bildausschnitt projiziert)</summary>
        bool Front(Vector3 wp) => Vector3.Dot(wp.normalized, (cam.transform.position - wp).normalized) > .05f;
        /// <summary>10.11.: Trefferfläche aus der echten Größe im Bild (meters um wp), begrenzt auf minPx…maxPx.</summary>
        float PxRad(Vector3 wp, float meters, float minPx, float maxPx)
        {
            var a = cam.WorldToScreenPoint(wp); var b = cam.WorldToScreenPoint(wp + cam.transform.right * meters); return Mathf.Clamp(((Vector2)b - (Vector2)a).magnitude, minPx, maxPx);
        }
        /// <summary>Editor/Test: wie viele Flaggen/Ampeln liegen im Bild, aber auf der Rückseite (früher antippbar)?</summary>
        public string DevTapCheck()
        {
            int fIn = 0, fBack = 0, aIn = 0, aBack = 0;
            foreach (var (f, wp) in world.FlagSpots()) { var sp = cam.WorldToScreenPoint(wp); if (sp.z <= 0 || sp.x < 0 || sp.y < 0 || sp.x > Screen.width || sp.y > Screen.height) continue; fIn++; if (!Front(wp)) fBack++; }
            foreach (var (id, wp) in world.AmpelSpots()) { var sp = cam.WorldToScreenPoint(wp); if (sp.z <= 0 || sp.x < 0 || sp.y < 0 || sp.x > Screen.width || sp.y > Screen.height) continue; aIn++; if (!Front(wp)) aBack++; }
            return "Flaggen im Bild " + fIn + ", davon Rückseite " + fBack + " · Ampeln im Bild " + aIn + ", davon Rückseite " + aBack;
        }
        void Tap(Vector2 pos)
        {
            var ps = Game.Cur; Game.Attend(); /* 03.11.: Betreuung */
            if (Game.Icp != null && world.IcpTap(cam, pos, 110 * U)) return; /* Fabian 02.11.: Abfangen */
            if (world.Moving) { if (Hit(pos, out var dmv)) world.MoveTo(ps.pd.PlanLocal(dmv)); return; } /* Gebäude verschieben: Tipp = dorthin */
            if (Hud.ArmedTool != null) { var tool = Hud.ArmedTool; if (Hit(pos, out var dtool)) { int n = Game.UseTool(tool, dtool); if (n > 0) { Hud.ArmedTool = null; /* 03.11.: bleibt scharf, bis es wirklich gewirkt hat */ WorldView.Shake = Mathf.Max(WorldView.Shake, tool == "nuke" ? 2f : .8f); world.EmitDust(dtool, new Color(1f, .9f, .6f), tool == "nuke" ? 160 : 50); Sfx.Play("boom", tool == "nuke" ? .6f : 1.1f, .7f); } } return; } /* 03.11.: Perk-Werkzeug */
            if (world.PutziScreen(cam, out var psp) && ((Vector2)psp - pos).magnitude < 55 * U && Front(cam.ScreenToWorldPoint(psp))) { world.PutziTalk(); return; } /* Putzi-7 antippen */
            /* Leuchtkristall antippen = ernten. Auswahl im Bild (nicht am Boden): der Kristall ragt hoch, schräg von oben trifft der Strahl sonst weit dahinter den Boden */
            {
                CrystalSave bc = null; float bpx = Mathf.Max(44f, Screen.height * .045f);
                foreach (var cx in ps.sv.crystals)
                {
                    var cdir = Game.CrystalDir(ps, cx);
                    foreach (float h in new[] { .2f, .7f, 1.3f })
                    {
                        var wp = ps.pd.Surf(cdir, h * Mathf.Max(.4f, cx.g)); var sp = cam.WorldToScreenPoint(wp); if (sp.z <= 0) continue;
                        if (Vector3.Dot(cdir, (cam.transform.position - wp).normalized) < -.05f) continue; /* Rückseite des Planeten */
                        float px = ((Vector2)sp - pos).magnitude; if (px < bpx) { bpx = px; bc = cx; }
                    }
                }
                if (bc != null) { Game.HarvestCrystal(bc.id); return; }
            }
            /* Wrack oder Einschlag antippen: Stand des Suchtrupps zeigen */
            { var ep = Game.EventPlace(ps); if (ep.HasValue) { var ew = ps.pd.Surf(ep.Value, 1); var sp = cam.WorldToScreenPoint(ew); if (sp.z > 0 && Front(ew) && ((Vector2)sp - pos).magnitude < PxRad(ew, 4f, 24 * U, 50 * U)) { var tt = Game.TeamText(ps); if (tt != null) { Game.Toast(tt.Replace(" · Tippen: hinsehen", "")); return; } } } }
            /* Fabian 03.11.: Ampel antippen = Zustand dieses Gebäudes (großzügige Trefferfläche) */
            {
                string ba = null; float bpx = 1e9f;
                foreach (var (aid, wp) in world.AmpelSpots()) { var sp = cam.WorldToScreenPoint(wp); if (sp.z <= 0 || !Front(wp)) continue; float px = ((Vector2)sp - pos).magnitude; if (px < PxRad(wp, 1.4f, 22 * U, 52 * U) && px < bpx) { bpx = px; ba = aid; } }
                if (ba != null) { hud.OpenSheet("building", ba); return; }
            }
            /* Flagge antippen: einsammeln, gefundene beschriften (Fabian 03.11.: größere Trefferfläche) */
            {
                FlagSave bf = null; float bpx = 1e9f; int bi = -1, k = 0;
                foreach (var (f, wp) in world.FlagSpots()) { var sp = cam.WorldToScreenPoint(wp); if (sp.z > 0 && Front(wp)) { float px = ((Vector2)sp - pos).magnitude; if (px < PxRad(wp, 1.8f, 24 * U, 60 * U) && px < bpx) { bpx = px; bf = f; bi = ps.sv.flags.IndexOf(f); } } k++; }
                if (bf != null) { bool was = bf.got; Game.CollectFlag(ps, bf); if (was || bf.got) hud.OpenSheet("flag", bi.ToString()); return; } /* Fabian 01.11.: Truhe öffnen, dann gleich die Flagge beschriften */
            }
            /* Fabian 01.11.: Fahrzeug (oder Sammelwagen) antippen öffnet sein Fenster */
            {
                VehSave bv = null; float bpx = 40f * U; bool cartHit = false;
                foreach (var v in ps.sv.veh)
                {
                    if (v.mode == "boom") continue; var wp = ps.pd.Surf(v.d, .8f); var sp = cam.WorldToScreenPoint(wp); if (sp.z <= 0 || Vector3.Dot(v.d.normalized, (cam.transform.position - wp).normalized) < -.05f) continue;
                    float px = ((Vector2)sp - pos).magnitude; if (px < bpx) { bpx = px; bv = v; }
                }
                if (ps.sv.cart.owned) { var wp = ps.pd.Surf(ps.sv.cart.d, .8f); var sp = cam.WorldToScreenPoint(wp); if (sp.z > 0 && Front(wp) && ((Vector2)sp - pos).magnitude < bpx) { cartHit = true; } }
                if (cartHit) { hud.OpenSheet("vehicles", "1"); return; }
                if (bv != null) { hud.OpenSheet("vehicle", bv.id.ToString()); return; }
            }
            /* Gebäude im Bild antippen: Mittelpunkt und Rand der Grundfläche in Bildschirmpunkte umrechnen – trifft auch das Dach hoher Gebäude */
            {
                string bb = null; float bs = 1e9f;
                foreach (var id in ps.bld)
                {
                    if (!ps.lay.pos.ContainsKey(id) || (Game.IsUnit(id) && id != "u1")) continue; float fp = GameData.Bld[id].fp;
                    var d0 = ps.pd.PlanDir(ps.lay.pos[id]); var d1 = ps.pd.PlanDir(ps.lay.pos[id] + new Vector2(fp, 0));
                    foreach (float h in new[] { .5f, 2f })
                    {
                        var w0 = ps.pd.Surf(d0, h); var s0 = cam.WorldToScreenPoint(w0); var s1 = cam.WorldToScreenPoint(ps.pd.Surf(d1, h)); if (s0.z <= 0 || !Front(w0)) continue; /* 10.11.: nicht durch den Planeten hindurch */
                        float rad = Mathf.Max(18f * U, ((Vector2)s1 - (Vector2)s0).magnitude * 1.1f), dd = ((Vector2)s0 - pos).magnitude;
                        if (dd < rad && dd / rad < bs) { bs = dd / rad; bb = id; }
                    }
                }
                if (bb != null) { hud.OpenSheet("building", bb); return; }
            }
            if (!Hit(pos, out var dir)) return; var pl = ps.pd.PlanLocal(dir);
            string best = null; float bd = 1e9f;
            foreach (var id in ps.bld) { if (!ps.lay.pos.ContainsKey(id)) continue; float d = (ps.lay.pos[id] - pl).magnitude; if (d < GameData.Bld[id].fp + .6f && d < bd) { bd = d; best = id; } }
            if (best != null) { hud.OpenSheet("building", best); return; }
            if (!mapMode && world.BeltAt(pl, out var bid)) { Game.Toast(world.BeltName(bid) + " – vom Fabrik-Planer verlegt", ""); return; } /* 10.11.: kein Band-Editor mehr, der Planer verlegt alles */
            int c = ps.pd.CellOf(dir);
            if (Game.OutpostSites(ps.pid).Contains(c) && (Game.ResDone("drones") || ps.sv.outposts.Any(o => o.cell == c))) { hud.OpenSheet("outpost", c.ToString()); return; }
            if (Game.Hard && c >= 0 && ps.clean[c] && !ps.pd.cells[c].IsBase) { hud.OpenSheet("pylon", c.ToString()); return; } /* 04.11.: Ladesäule */
            if (ps.sv.rigs.Any(r => r.cell == c) || (Game.IsDone(ps.pid) && !ps.pd.cells[c].IsBase && ps.clean[c])) { hud.OpenSheet("rig", c.ToString()); return; }
            string why = Game.FrontWhy(ps, c);
            if (why != "" && why != "Basisgelände" && !Game.Workable(ps, c)) Game.Toast(why);
        }

        void OnFloat(Vector3 dir, string text, string kind)
        {
            var w = Game.Cur.pd.Surf(dir, 3); var sp = cam.WorldToScreenPoint(w); if (sp.z <= 0) return;
            if (Vector3.Dot(dir, (cam.transform.position - w).normalized) < 0) return; hud.FloatText(sp, text, kind);
        }
    }
}
