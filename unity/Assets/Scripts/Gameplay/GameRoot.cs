// Startpunkt der Unity-Version: baut Welt, Kamera und Oberfläche, verarbeitet Eingaben und treibt die Simulation an.
using System.Collections;
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
        float TILT = 38f * Mathf.Deg2Rad; float lastAng;
        bool offlineRunning; float saveT;
        // Eingabe
        enum Mode { None, Wipe, Pan, Ui }
        Mode mode; Vector2 downPos, lastPos; float downT, lastPinch; bool moved;
        GameObject brush; Light brushLight; Vector3 lastWipe; bool hasLastWipe;

        void Awake()
        {
            Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0;
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.maxAdditionalLightsCount = 8;
                try { var f = typeof(UniversalRenderPipelineAsset).GetField("m_AdditionalLightsRenderingMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); if (f != null && (LightRenderingMode)f.GetValue(urp) == LightRenderingMode.Disabled) f.SetValue(urp, LightRenderingMode.PerPixel); } catch { }
            } // Laternen, Scheinwerfer, Saugkopf bei Nacht
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            Game.TestMode = UnityEditor.EditorPrefs.GetBool("LunarCleanup.TestMode", false);
#endif
            Game.Init(); MeshKit.Init();
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
            var cv = new GameObject("Oberfläche"); var canvas = cv.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var sc = cv.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(390, 844); sc.matchWidthOrHeight = .5f;
            cv.AddComponent<GraphicRaycaster>();
            if (!FindFirstObjectByType<EventSystem>()) { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>(); }
            hud = cv.AddComponent<Hud>(); hud.Build(cv.transform);
            hud.OnNav = k => { if (k == "clean") { hud.CloseSheet(); FocusFront(); } else hud.OpenSheet(k); };
            hud.OnMap = () => { mapMode = !mapMode; altGoal = mapMode ? Game.Cur.pd.R * 2.3f : 64; };
            hud.OnFocusQuest = FocusFront;
            planet = new GameObject("Planet").AddComponent<PlanetView>(); world = new GameObject("Welt").AddComponent<WorldView>();
            Game.OnPlanetChanged += BuildPlanet; Game.OnFloat += OnFloat; Game.OnBoom += d => { world.EmitDust(d, new Color(1, .54f, .23f), 40); world.EmitDust(d, new Color(.33f, .33f, .33f), 30); }; Game.OnRocket += (p, v) => world.LaunchRocket();
            brush = MeshKit.Cyl(null, MeshKit.Unlit(new Color(.43f, .95f, .82f, .35f), true), Vector3.zero, 1, 1, .04f, 40); brush.name = "Saugkopf"; brush.SetActive(false);
            { var bl = new GameObject("Licht"); bl.transform.SetParent(brush.transform, false); bl.transform.localPosition = new Vector3(0, 3.2f, 0); brushLight = bl.AddComponent<Light>(); brushLight.type = LightType.Point; brushLight.range = 9; brushLight.color = new Color(.75f, 1f, .92f); brushLight.intensity = 0; }
            BuildPlanet();
        }
        void OnDestroy() { Game.OnPlanetChanged -= BuildPlanet; Game.OnFloat -= OnFloat; }

        void BuildPlanet()
        {
            var ps = Game.Cur; planet.Build(ps); world.Build(ps); hud.CloseSheet();
            var c = ps.pd.cells[ps.pd.baseCell]; cd = ps.clean[ps.pd.start] ? c.c : (ps.pd.cells[ps.pd.start].c * .7f + c.c * .3f).normalized; north = c.e2; mapMode = false; alt = altGoal = 64;
            ApplyCam();
        }

        void Start() { StartCoroutine(Offline((float)(Game.Now() - Game.S.lastActive))); }

        IEnumerator Offline(float seconds)
        {
            float t = Mathf.Min(seconds, Game.OfflineCap); if (t < 5) yield break;
            offlineRunning = true; Game.Offline = true; double c0 = Game.S.credits; float el = 0;
            while (el < t)
            {
                float t0 = Time.realtimeSinceStartup;
                while (el < t && Time.realtimeSinceStartup - t0 < .03f) { float d = Mathf.Min(1, t - el); Game.Step(d); el += d; }
                if (t > 600) hud.ShowOffline("Deine Basis hat weitergearbeitet … " + Mathf.FloorToInt(el / t * 100) + " %");
                yield return null;
            }
            Game.Offline = false; offlineRunning = false; hud.ShowOffline(null);
            double gain = Game.S.credits - c0; int h = Mathf.FloorToInt(t / 3600), m = Mathf.FloorToInt(t % 3600 / 60);
            Game.Toast("Während du weg warst (" + (h > 0 ? h + " h " : "") + m + " min): +" + Game.Fmt(gain) + " Cr", "good");
            if (Game.S.wrecks.Count > 0) { Game.Toast("Totalschaden: " + string.Join(", ", Game.S.wrecks) + " – neue Fahrzeuge bauen", "bad"); Game.S.wrecks.Clear(); }
            Game.Save();
        }

        void OnApplicationPause(bool p) { if (p) Game.Save(); else if (!offlineRunning && Game.S != null) StartCoroutine(Offline((float)(Game.Now() - Game.S.lastActive))); }
        void OnApplicationQuit() { Game.Save(); }

        void Update()
        {
            if (Game.S == null) return; float dt = Mathf.Min(Time.deltaTime, .1f);
            HandleInput(dt);
            var kbd = Keyboard.current; if (kbd != null) { if (kbd.qKey.isPressed) Twist(90 * dt); if (kbd.eKey.isPressed) Twist(-90 * dt); }
#if UNITY_EDITOR
            DevKeys();
#endif
            if (!offlineRunning) Game.Step(dt);
            planet.Tick();
            alt = Mathf.Lerp(alt, altGoal, 1 - Mathf.Exp(-6 * dt)); ApplyCam();
            Game.CartGoal = cd; WorldView.MapMode = mapMode;
            saveT += dt; if (saveT > 10) { saveT = 0; if (!offlineRunning) Game.Save(); }
        }

#if UNITY_EDITOR
        // Test-Tasten nur im Editor (nicht im Spiel-Build): F5 Parzelle vorne säubern, F6 +10.000 Cr, F7 8 h offline simulieren, F8 Planet zu 81 % (nochmal: 100 %) säubern, F9 Vierteltag vorspulen, F10 Sturm in 25 s.
        void DevKeys()
        {
            var kb = Keyboard.current; if (kb == null || !Game.TestMode) return; // Test-Tasten nur im Testmodus (eigener Spielstand)
            if (kb.f5Key.wasPressedThisFrame) { var ps = Game.Cur; int best = -1; float bd = -2; foreach (int id in ps.front) { float d = Vector3.Dot(ps.pd.cells[id].c, cd); if (d > bd) { bd = d; best = id; } } if (best >= 0) Game.DevClean(best); }
            if (kb.f6Key.wasPressedThisFrame) Game.Earn(10000);
            if (kb.f9Key.wasPressedThisFrame) { var cs = Game.Cur.sv; cs.dayT = (cs.dayT < 0 ? 0 : cs.dayT) + Game.DayLen(Game.Cur.pid) / 4; }
            if (kb.f10Key.wasPressedThisFrame && Game.Cur.P.storm != null) { var cs = Game.Cur.sv; cs.hzOn = 0; cs.hzWarned = false; cs.hzT = Game.Cur.P.storm.every - 25; }
            if (kb.f8Key.wasPressedThisFrame) Game.DevCleanTo(Game.CleanPct(Game.Cur) >= .8f ? 1.01f : .81f);
            if (kb.f7Key.wasPressedThisFrame && !offlineRunning) StartCoroutine(Offline(8 * 3600));
        }
#endif
        // ---------- Kamera ----------
        void ApplyCam()
        {
            float R = Game.Cur.pd.R; float tilt = Mathf.Lerp(TILT, 0, Mathf.InverseLerp(60, R * 2f, alt));
            north = (north - cd * Vector3.Dot(north, cd)).normalized;
            var focus = cd * R; var pos = focus + (cd * Mathf.Cos(tilt) - north * Mathf.Sin(tilt)) * alt;
            cam.transform.position = pos; cam.transform.rotation = Quaternion.LookRotation(focus - pos, north * Mathf.Cos(tilt) + cd * Mathf.Sin(tilt));
        }
        void Pan(Vector2 delta)
        {
            float R = Game.Cur.pd.R, mpp = 2 * alt * Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad) / Screen.height;
            var right = Vector3.Cross(cd, north).normalized * -1; // Bildschirm-rechts auf der Oberfläche
            right = cam.transform.right; right = (right - cd * Vector3.Dot(right, cd)).normalized;
            var move = (-right * delta.x - north * delta.y) * mpp; float ang = move.magnitude / R; if (ang < 1e-7f) return;
            var axis = Vector3.Cross(cd, move.normalized); var q = Quaternion.AngleAxis(ang * Mathf.Rad2Deg, axis); cd = (q * cd).normalized; north = q * north;
        }
        void Twist(float deg) { if (Mathf.Abs(deg) < 1e-4f) return; north = Quaternion.AngleAxis(deg, cd) * north; }
        void Zoom(float f) { float R = Game.Cur.pd.R; altGoal = Mathf.Clamp(altGoal * f, 9, R * 2.6f); mapMode = altGoal > R * 1.5f; }
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
            if (touches.Count == 1) { pressed = true; pos = touches[0].screenPosition; pid = touches[0].touchId; }
            else if (Mouse.current != null && (Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed))
            {
                // PC: rechte/mittlere Maustaste ziehen = Blickrichtung drehen (links/rechts) und neigen (hoch/runter)
                var mp = Mouse.current.position.ReadValue(); if (Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame) lastPos = mp;
                var dm = mp - lastPos; Twist(-dm.x * .25f); TILT = Mathf.Clamp(TILT + dm.y * .004f, 0, 70 * Mathf.Deg2Rad); lastPos = mp; mode = Mode.None; return;
            }
            else if (Mouse.current != null) { pressed = Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasPressedThisFrame; pos = Mouse.current.position.ReadValue(); float sc = Mouse.current.scroll.ReadValue().y; if (Mathf.Abs(sc) > .01f && !Hud.PointerOverUi()) Zoom(sc > 0 ? .88f : 1.14f); }
            else { pressed = false; pos = Vector2.zero; }

            if (pressed && mode == Mode.None)
            {
                downPos = lastPos = pos; downT = Time.time; moved = false;
                if (Hud.PointerOverUi(pid) || offlineRunning) { mode = Mode.Ui; return; }
                if (!mapMode && Hit(pos, out var dir) && Game.CanClean(dir) && !Hud.I.SheetOpen) { mode = Mode.Wipe; Game.BeginStroke(); hasLastWipe = false; }
                else mode = Mode.Pan;
            }
            if (pressed)
            {
                if ((pos - downPos).magnitude > 10) moved = true;
                if (mode == Mode.Wipe)
                {
                    if (Hit(pos, out var dir))
                    {
                        // Zwischenpunkte, damit auch schnelle Striche lückenlos reinigen
                        float R0 = Game.Cur.pd.R, rr = Game.Uv("radius"); int steps = hasLastWipe ? Mathf.Clamp(Mathf.CeilToInt(Game.Cur.pd.Arc(lastWipe, dir) / (rr * .5f)), 1, 12) : 1; float amt = 0; int mat = -1;
                        for (int s = 1; s <= steps; s++) { var dd = hasLastWipe ? Vector3.Slerp(lastWipe, dir, s / (float)steps).normalized : dir; amt += Game.Stroke(dd, dt / steps, out int m0); if (m0 >= 0) mat = m0; }
                        lastWipe = dir; hasLastWipe = true;
                        brush.SetActive(true); float r = Game.Uv("radius"); brush.transform.position = Game.Cur.pd.Surf(dir, .08f); brush.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir); brush.transform.localScale = new Vector3(r, 1, r); brushLight.intensity = 2.4f * Mathf.Clamp01((WorldView.NightF - .2f) / .6f);
                        if (amt > 0 && Random.value < .6f) { int c = Game.Cur.pd.CellOf(dir); world.EmitDust(dir, GameData.Dirt[Game.Cur.pd.cells[c].type].lightC, 3); }
                    }
                }
                else if (mode == Mode.Pan) Pan(pos - lastPos);
                lastPos = pos; return;
            }
            if (mode == Mode.Wipe) EndWipe();
            if (mode == Mode.Pan && !moved && Time.time - downT < .4f) Tap(pos);
            mode = Mode.None;
        }
        void EndWipe() { brush.SetActive(false); mode = Mode.None; }

        bool Hit(Vector2 screen, out Vector3 dir) => PlanetView.RayHit(cam.ScreenPointToRay(screen), Game.Cur.pd, out dir);

        void Tap(Vector2 pos)
        {
            if (!Hit(pos, out var dir)) return; var ps = Game.Cur; var pl = ps.pd.PlanLocal(dir);
            string best = null; float bd = 1e9f;
            foreach (var id in ps.bld) { if (!ps.lay.pos.ContainsKey(id)) continue; float d = (ps.lay.pos[id] - pl).magnitude; if (d < GameData.Bld[id].fp + .6f && d < bd) { bd = d; best = id; } }
            if (best != null) { hud.OpenSheet("building", best); return; }
            int c = ps.pd.CellOf(dir);
            if (Game.OutpostSites(ps.pid).Contains(c) && (Game.ResDone("drones") || ps.sv.outposts.Any(o => o.cell == c))) { hud.OpenSheet("outpost", c.ToString()); return; }
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
