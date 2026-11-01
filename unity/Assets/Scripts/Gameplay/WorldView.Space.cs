// Blick aus dem All: Atmosphärenring am Planetenrand, Lichter der Basis und der Fahrzeuge aus der Ferne,
// ab und zu ein Komet am Himmel und der Anflug eines abstürzenden Wracks (Feuerball mit Rauchspur, dann Einschlag).
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        /// <summary>Test: nächster Himmelsflug ist ein Frachter (true) oder Komet (false).</summary>
        public static bool? FreighterNow;
        void SpaceFx()
        {
            var cam = Camera.main; if (!cam || pd == null) return;
            UpdateAtmo(cam); FarLights(cam); AmbientComet(cam); UpdateCrash(); UpdateMeteors(); UpdateFlashes();
        }
        static Material GlowMat(Texture tex, Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
            m.SetTexture("_BaseMap", tex); MeshKit.MakeTransparent(m, true); m.SetFloat("_Cull", 0); m.SetColor("_BaseColor", c); return m;
        }

        // ---------- Atmosphäre: heller Saum genau am Planetenrand ----------
        const float LIMB = .8f; // Planetenrand im Ring-Bild (Anteil des Halbmessers)
        static Texture2D atmoTex;
        static Texture2D AtmoTex()
        {
            if (atmoTex) return atmoTex; const int S = 256; atmoTex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "Atmosphäre" };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float r = new Vector2(x - S / 2f + .5f, y - S / 2f + .5f).magnitude / (S / 2f), a;
                    if (r < LIMB) a = Mathf.Pow(Mathf.Clamp01((r - (LIMB - .1f)) / .1f), 2f); // innen verdeckt der Planet ohnehin
                    else a = Mathf.Pow(Mathf.Clamp01(1 - (r - LIMB) / (1 - LIMB)), 2.4f);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            atmoTex.SetPixels32(px); atmoTex.Apply(true); return atmoTex;
        }
        Transform atmo; Material atmoMat; Color atmoCol; float atmoA;
        void BuildAtmo(Color c, float a)
        {
            atmoCol = c; atmoA = a; atmoMat = GlowMat(AtmoTex(), new Color(c.r, c.g, c.b, a));
            var g = MeshKit.Part(skyRoot, MeshKit.Quad, atmoMat, Vector3.zero, Vector3.one); g.name = "Atmosphäre"; NoShadow(g); atmo = g.transform;
        }
        void UpdateAtmo(Camera cam)
        {
            if (!atmo) return; var cp = cam.transform.position; float D = cp.magnitude, R = pd.R;
            if (D < R * 1.02f) { atmo.gameObject.SetActive(false); return; }
            atmo.gameObject.SetActive(true);
            float app = R * D / Mathf.Sqrt(D * D - R * R); // sichtbarer Planetenradius in der Ebene durch den Mittelpunkt
            atmo.position = Vector3.zero; atmo.rotation = Quaternion.LookRotation(-cp, cam.transform.up); atmo.localScale = Vector3.one * (2 * app * 1.02f / LIMB);
            float k = Mathf.Lerp(.55f, 1f, Mathf.InverseLerp(R * 1.3f, R * 3f, D)); // aus der Ferne kräftiger
            atmoMat.SetColor("_BaseColor", new Color(atmoCol.r, atmoCol.g, atmoCol.b, atmoA * k));
        }

        // ---------- Lichter aus der Ferne: Gebäude, Fahrzeuge, Ereignisort ----------
        Transform farRoot; readonly List<Transform> farBld = new List<Transform>(), farVeh = new List<Transform>(); Transform farEv; Material farMat, farVehMat, farEvMat; int farVer = -1; int farCount = -1;
        void FarLights(Camera cam)
        {
            var cp = cam.transform.position; float D = cp.magnitude, R = pd.R;
            float vis = Mathf.InverseLerp(R + 75, R + 170, D); // erst beim Rauszoomen, dann immer deutlicher
            if (!farRoot || farVer != builtVersion || farCount != blds.Count)
            {
                if (farRoot) Destroy(farRoot.gameObject); farBld.Clear(); farVeh.Clear(); farRoot = New("Fernlichter"); farVer = builtVersion; farCount = blds.Count;
                farMat = GlowMat(MeshKit.GlowTex(), new Color(1f, .8f, .5f, 1)); farVehMat = GlowMat(MeshKit.GlowTex(), new Color(.75f, .95f, 1f, 1)); farEvMat = GlowMat(MeshKit.GlowTex(), new Color(1f, .3f, .2f, 1));
                foreach (var bp in blds.Values) { if (!bp.root) continue; var q = MeshKit.Part(farRoot, MeshKit.Quad, farMat, bp.root.transform.position + bp.root.transform.position.normalized * 2.5f, Vector3.one); NoShadow(q); farBld.Add(q.transform); }
                var e = MeshKit.Part(farRoot, MeshKit.Quad, farEvMat, Vector3.zero, Vector3.one); NoShadow(e); farEv = e.transform;
            }
            if (vis <= 0) { farRoot.gameObject.SetActive(false); return; }
            farRoot.gameObject.SetActive(true);
            float a = vis * (.4f + .6f * NightF), s = D * .03f; var up = cam.transform.up;
            farMat.SetColor("_BaseColor", new Color(1f, .8f, .5f, a)); farVehMat.SetColor("_BaseColor", new Color(.75f, .95f, 1f, a * .8f));
            void Place(Transform t, Vector3 p, float size) { t.position = p; t.rotation = Quaternion.LookRotation(p - cp, up); t.localScale = Vector3.one * size; t.gameObject.SetActive(Vector3.Dot(p.normalized, (cp - p).normalized) > .02f); } // Rückseite ausblenden
            foreach (var t in farBld) if (t) Place(t, t.position, s);
            var vl = ps.sv.veh; while (farVeh.Count < Mathf.Min(vl.Count, 40)) { var q = MeshKit.Part(farRoot, MeshKit.Quad, farVehMat, Vector3.zero, Vector3.one); NoShadow(q); farVeh.Add(q.transform); }
            for (int i = 0; i < farVeh.Count; i++) { if (i < vl.Count && vl[i].d != Vector3.zero) Place(farVeh[i], pd.Surf(vl[i].d, 1.5f), s * .5f); else farVeh[i].gameObject.SetActive(false); }
            var ep = Game.EventPlace(ps);
            if (ep.HasValue) { float pulse = .55f + .45f * Mathf.Sin(Time.time * 4); farEvMat.SetColor("_BaseColor", new Color(1f, .3f, .2f, vis * pulse)); Place(farEv, pd.Surf(ep.Value, 2), s * 1.4f); }
            else farEv.gameObject.SetActive(false);
        }

        // ---------- Kometen am Himmel (nur Optik, unabhängig vom Ereignis „Kometenschweif") ----------
        GameObject skyComet; float cometNext = 20, cometT, cometDur; Vector3 cometA, cometB;
        public static bool CometNow; // Test-Knopf: nächster Komet sofort
        void AmbientComet(Camera cam)
        {
            if (!skyComet)
            {
                if (CometNow) { CometNow = false; cometNext = 1.5f; }
                cometNext -= Time.deltaTime; if (cometNext > 0) return; cometNext = Random.Range(45f, 120f);
                var cp = cam.transform.position; float R = pd.R, D = cp.magnitude, dist = Mathf.Max(D + R * 1.3f, 420f);
                var fw = cam.transform.forward; var up = cam.transform.up; var rt = cam.transform.right;
                var mid = cp + (fw + up * Random.Range(.3f, .55f) + rt * Random.Range(-.35f, .35f)).normalized * dist; // oben im Bild, weit draußen (hinter dem Planeten)
                if (mid.magnitude < R * 1.8f) mid = mid.normalized * R * 1.8f;
                var along = (rt * (Random.value < .5f ? 1 : -1) + up * Random.Range(-.45f, .1f)).normalized; float len = dist * Random.Range(.55f, .9f);
                cometA = mid - along * len / 2; cometB = mid + along * len / 2; cometDur = Random.Range(8f, 12f); cometT = 0;
                skyComet = new GameObject("Komet"); skyComet.transform.SetParent(transform, false); skyComet.transform.position = cometA;
                bool freighter = !FreighterNow.HasValue ? Random.value < .35f : FreighterNow.Value; FreighterNow = null;
                if (freighter && LcKit.Has("Ereignisse/frachter")) /* Fabian 01.11.: Frachter nah vor der Kamera, hell beleuchtet, ohne Schatten – vorher nur ein dunkler Schatten über der Welt */
                {
                    float fd = Mathf.Clamp((cp - pd.Surf(cam.transform.position.normalized)).magnitude * .75f, 35f, 260f);
                    var fmid = cp + (fw + up * Random.Range(-.06f, .06f)).normalized * fd; /* Bildmitte – oben verdeckt die Anzeige den Himmel */ float side = Random.value < .5f ? 1 : -1;
                    var fal = (rt * side + up * Random.Range(-.08f, .08f)).normalized; float flen = fd * 1.9f;
                    cometA = fmid - fal * flen / 2; cometB = fmid + fal * flen / 2; cometDur = Random.Range(9f, 13f); skyComet.transform.position = cometA;
                    float sc = fd * .03f; var fm = LcKit.Spawn("Ereignisse/frachter", skyComet.transform, Vector3.zero, 0, GameData.Hex("#ffbf4d"), null, sc);
                    skyComet.transform.rotation = Quaternion.LookRotation(fal, up);
                    if (fm) foreach (var r in fm.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
                    var eng = MeshKit.Part(skyComet.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(.5f, .8f, 1f, 1)), new Vector3(0, .9f, -3.3f) * sc, Vector3.one * sc * 3.2f); NoShadow(eng); eng.AddComponent<Billboard>();
                    foreach (var (pos, col) in new[] { (new Vector3(-1.9f, 1f, 0), new Color(1f, .2f, .2f, 1)), (new Vector3(1.9f, 1f, 0), new Color(.2f, 1f, .35f, 1)), (new Vector3(0, 2.1f, 1.5f), new Color(1, 1, 1, 1)) })
                    { var nl = MeshKit.Part(skyComet.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), col), pos * sc, Vector3.one * sc * .9f); NoShadow(nl); nl.AddComponent<Billboard>(); nl.AddComponent<Blink>(); }
                    var fl = new GameObject("Licht"); fl.transform.SetParent(skyComet.transform, false); fl.transform.localPosition = new Vector3(0, 6f, 3f) * sc; var L = fl.AddComponent<Light>(); L.type = LightType.Point; L.color = new Color(1f, .97f, .9f); L.range = sc * 16; L.intensity = 4; L.shadows = LightShadows.None;
                    var trf = skyComet.AddComponent<TrailRenderer>(); trf.time = 1.6f; trf.minVertexDistance = sc * .3f; trf.alignment = LineAlignment.View; trf.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; trf.widthCurve = new AnimationCurve(new Keyframe(0, sc * 1.2f), new Keyframe(1, 0)); trf.material = GlowMat(MeshKit.GlowTex(), Color.white);
                    var gf = new Gradient(); gf.SetKeys(new[] { new GradientColorKey(new Color(.55f, .85f, 1f), 0), new GradientColorKey(new Color(.3f, .5f, 1f), 1) }, new[] { new GradientAlphaKey(.7f, 0), new GradientAlphaKey(0, 1) }); trf.colorGradient = gf;
                    return;
                }
                var head = MeshKit.Part(skyComet.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(.85f, .95f, 1f, 1)), Vector3.zero, Vector3.one * dist * .03f); NoShadow(head); head.AddComponent<Billboard>();
                var km = LcKit.Spawn("Ereignisse/komet", skyComet.transform, Vector3.zero, Random.value * 360, GameData.Hex("#9fd8ff"), null, dist * .004f); if (km) km.AddComponent<SlowSpin>(); /* Kometenkern dreht sich im Glühen */
                var tr = skyComet.AddComponent<TrailRenderer>(); tr.time = 3.2f; tr.minVertexDistance = dist * .004f; tr.alignment = LineAlignment.View; tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; tr.receiveShadows = false;
                tr.widthCurve = new AnimationCurve(new Keyframe(0, dist * .012f), new Keyframe(1, 0)); tr.material = GlowMat(MeshKit.GlowTex(), Color.white);
                var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(new Color(.85f, .95f, 1f), 0), new GradientColorKey(new Color(.45f, .7f, 1f), 1) }, new[] { new GradientAlphaKey(.9f, 0), new GradientAlphaKey(0, 1) }); tr.colorGradient = gr;
                return;
            }
            cometT += Time.deltaTime; float k = cometT / cometDur; if (k >= 1) { Destroy(skyComet); skyComet = null; return; }
            skyComet.transform.position = Vector3.Lerp(cometA, cometB, k);
        }

        // ---------- Absturz: Feuerball kommt vom Himmel, schlägt ein, danach liegt das Wrack da ----------
        GameObject crashGo; Transform crashBall; float crashT = -1, smokeT = -1; Vector3 crashFrom, crashTo, smokeAt; bool crashImpact;
        void StartCrash(int cell, bool impact)
        {
            var cl = pd.cells[cell]; crashTo = pd.Surf(cl.c, .5f); crashT = 0; crashImpact = impact;
            /* Anflug von der Seite: Richtung „Bild-oben“ und „Bild-seitlich“ auf den Boden der Absturzstelle umgerechnet – so zieht der Feuerball sichtbar über den Bildschirm */
            var cm = Camera.main; var n = cl.c; Vector3 tUp = cl.e2, tRt = cl.e1;
            if (cm) { tUp = cm.transform.up - n * Vector3.Dot(cm.transform.up, n); tRt = cm.transform.right - n * Vector3.Dot(cm.transform.right, n); if (tUp.sqrMagnitude < .01f) tUp = cl.e2; if (tRt.sqrMagnitude < .01f) tRt = cl.e1; tUp.Normalize(); tRt.Normalize(); }
            float far = cm ? Mathf.Clamp((cm.transform.position - crashTo).magnitude, 40, 400) : 80; // weiter weg = längere Bahn
            crashFrom = crashTo + n * far * .35f + tUp * far * .12f + tRt * far * (Random.value < .5f ? -.75f : .75f); // kommt seitlich durch die Bildmitte – oben verdecken Ziel und Meldungen den Himmel
            if (crashGo) Destroy(crashGo); crashGo = new GameObject("Absturz"); crashGo.transform.SetParent(transform, false); crashGo.transform.position = crashFrom;
            Color hot = impact ? new Color(.6f, .85f, 1f) : new Color(1f, .6f, .25f);
            var ball = MeshKit.Part(crashGo.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(hot.r, hot.g, hot.b, 1)), Vector3.zero, Vector3.one * 12); NoShadow(ball); ball.AddComponent<Billboard>(); crashBall = ball.transform;
            var core = MeshKit.Part(ball.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(1f, .97f, .85f, 1)), new Vector3(0, 0, -.01f), Vector3.one * .4f); NoShadow(core); // heller Kern, wächst mit dem Feuerball
            var tr = crashGo.AddComponent<TrailRenderer>(); tr.time = 3.5f; tr.minVertexDistance = 1f; tr.alignment = LineAlignment.View; tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.widthCurve = new AnimationCurve(new Keyframe(0, 6f), new Keyframe(1, 2f)); tr.material = GlowMat(MeshKit.GlowTex(), Color.white);
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(hot, 0), new GradientColorKey(new Color(.35f, .33f, .32f), .35f), new GradientColorKey(new Color(.25f, .25f, .25f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.5f, .4f), new GradientAlphaKey(0, 1) }); tr.colorGradient = gr;
            var lg = new GameObject("Glut"); lg.transform.SetParent(crashGo.transform, false); var L = lg.AddComponent<Light>(); L.type = LightType.Point; L.color = hot; L.range = 60; L.intensity = 10; L.shadows = LightShadows.None;
            Sfx.Play("rocket", .55f, .35f);
        }
        void UpdateCrash()
        {
            Smoke();
            if (crashT < 0) return; crashT += Time.deltaTime; const float DUR = 6f; float k = Mathf.Clamp01(crashT / DUR);
            if (crashGo)
            {
                crashGo.transform.position = Vector3.Lerp(crashFrom, crashTo, k * k * (1.4f - .4f * k)); // wird schneller, bremst am Ende kaum
                var cam = Camera.main; if (cam && crashBall) { float dist = (cam.transform.position - crashGo.transform.position).magnitude; crashBall.localScale = Vector3.one * Mathf.Max(16f, dist * .16f); var tr = crashGo.GetComponent<TrailRenderer>(); if (tr) tr.widthMultiplier = Mathf.Max(1f, dist * .03f); } // aus der Ferne mitwachsen
                if (dust && Random.value < .5f) dust.Emit(new ParticleSystem.EmitParams { position = crashGo.transform.position, velocity = Random.insideUnitSphere * 2, startColor = new Color(.4f, .38f, .36f, .6f), startSize = Random.Range(1.5f, 3f), startLifetime = 2.5f, applyShapeToPosition = false }, 1); // Rauchspur
            }
            if (k < 1) return;
            var d = crashTo.normalized; crashT = -1; if (crashGo) Destroy(crashGo); crashGo = null;
            EmitDust(d, crashImpact ? new Color(.6f, .85f, 1f) : new Color(1, .54f, .23f), 90); EmitDust(d, new Color(.33f, .33f, .33f), 70);
            Sfx.Play("pop", .45f, .7f); Sfx.Buzz(); smokeT = 0; smokeAt = d;
        }
        /// <summary>Nach dem Einschlag: 12 Sekunden lang eine dunkle Rauchsäule über der Absturzstelle.</summary>
        void Smoke()
        {
            if (smokeT < 0 || !dust) return; smokeT += Time.deltaTime; if (smokeT > 12) { smokeT = -1; return; }
            if (Random.value < .7f) dust.Emit(new ParticleSystem.EmitParams { position = pd.Surf(smokeAt, 1) + Random.insideUnitSphere * .8f, velocity = smokeAt * Random.Range(2.5f, 4f) + Random.insideUnitSphere * .5f, startColor = new Color(.22f, .21f, .2f, .55f * (1 - smokeT / 12)), startSize = Random.Range(1.5f, 3.2f), startLifetime = 4f, applyShapeToPosition = false }, 1);
        }
        bool Crashing => crashT >= 0;
    }
    /// <summary>Langsames Drehen um eine schräge Achse (Kometenkern).</summary>
    public class SlowSpin : MonoBehaviour { public float speed = 25; readonly Vector3 ax = new Vector3(.3f, 1, .2f).normalized; void Update() => transform.Rotate(ax, speed * Time.deltaTime, Space.Self); }
    /// <summary>Positionslicht: blinkt im Sekundentakt.</summary>
    public class Blink : MonoBehaviour { Vector3 s0; float ph; void Start() { s0 = transform.localScale; ph = Random.value; } void Update() { float k = Mathf.Repeat(Time.time + ph, 1f) < .5f ? 1 : .25f; transform.localScale = s0 * k; } }
}
