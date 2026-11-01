// Fabian 01.11.: Auf jedem Planeten gibt es etwas Unerwartetes zu entdecken – ein Wahrzeichen mit eigener Bewegung,
// nahe der Basis, beim ersten Besuch mit Meldung. Reine Optik, keine Spielregeln.
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        public Vector3 WonderDir => wonderAt;
        GameObject wonder; string wonderFor; float wonderT; Vector3 wonderAt; Transform wonderSpin; readonly List<Transform> wonderBits = new List<Transform>(); readonly List<Vector3> wonderBase = new List<Vector3>();
        static readonly Dictionary<string, (string name, string text)> WonderInfo = new Dictionary<string, (string, string)>
        {
            { "moon", ("Alte Landestelle", "Neben deiner Basis steht eine uralte Landefähre – die Fußspuren sind noch da.") },
            { "mars", ("Staubteufel", "Wirbelstürme aus rotem Staub ziehen über die Ebene.") },
            { "europa", ("Eisgeysir", "Unter dem Eis brodelt ein Ozean – ab und zu schießt eine Fontäne hoch.") },
            { "titan", ("Methanregen", "Über Titan nieselt es – flüssiges Methan.") },
            { "io", ("Feuervulkan", "Ein Vulkan spuckt Schwefelfontänen bis in den Himmel.") },
            { "venus", ("Wolkenblitze", "In den dichten Wolken zucken Blitze.") },
            { "ganymed", ("Eisbogen", "Ein riesiger Bogen aus blauem Eis spannt sich über das Land.") },
            { "enceladus", ("Tigerstreifen", "Blaue Eisfontänen schießen aus langen Rissen.") },
            { "triton", ("Stickstoff-Geysire", "Dunkle Fahnen steigen aus dem Boden und treiben im Wind.") },
            { "pluto", ("Das Herz", "Eine riesige, sanft leuchtende Herzform liegt auf der Ebene.") },
            { "eris", ("Eisnadeln", "Glitzernde Nadeln aus Eis ragen in den schwarzen Himmel.") },
            { "sedna", ("Roter Kristallgarten", "Rubinrote Kristalle wachsen aus dem Boden.") },
            { "proxima", ("Schwebende Felsen", "Ein paar Felsbrocken schweben langsam über dem Boden.") },
            { "trappist", ("Leuchtende Becken", "In flachen Becken leuchtet etwas Lebendiges.") },
            { "kepler", ("Leuchtpflanzen", "Hohe Pflanzen wiegen sich – ihre Spitzen leuchten.") },
            { "cancri", ("Diamantberg", "Ein riesiger Diamant funkelt im Licht der nahen Sonne.") },
            { "gliese", ("Dampfquellen", "Heißer Dampf zischt aus dem Boden.") },
            { "poltergeist", ("Geisterlichter", "Leuchtende Kugeln schweben wie Geister umher.") },
        };

        void BuildWonder()
        {
            wonder = null; wonderSpin = null; wonderBits.Clear(); wonderBase.Clear(); wonderFor = ps.pid;
            if (!WonderInfo.ContainsKey(ps.pid)) return;
            /* Platz: ein paar Parzellen von der Basis entfernt, schräg vor ihr, nicht auf einem Außenlager-Platz */
            var bc = pd.cells[pd.baseCell]; var dirW = (bc.e1 * .8f + bc.e2 * .6f).normalized; var sites = Game.OutpostSites(ps.pid); int best = -1; float bs = -1e9f;
            foreach (var c in pd.cells) { if (c.IsBase || c.Wet || sites.Contains(c.id) || c.ring < 2 || c.ring > 3) continue; float sc = Vector3.Dot(c.c - bc.c, dirW); if (sc > bs) { bs = sc; best = c.id; } }
            if (best < 0) return; var cl = pd.cells[best]; var d = cl.c.normalized; wonderAt = d;
            wonder = new GameObject("Entdeckung " + WonderInfo[ps.pid].name); wonder.transform.SetParent(transform, false);
            wonder.transform.position = pd.Surf(d, 0, best); wonder.transform.rotation = Quaternion.LookRotation(cl.e1, d); var t = wonder.transform;
            Material M(string hex, float metal = .1f, float sm = .4f) => MeshKit.Mat(GameData.Hex(hex), metal, sm);
            Material G(string hex, float k = 2.5f) => new Material(MeshKit.Glow(GameData.Hex(hex), k));
            var r = new System.Random(best);
            switch (ps.pid)
            {
                case "moon":
                    MeshKit.Box(t, M("#c8b26a", .7f, .7f), new Vector3(0, 1.1f, 0), new Vector3(1.6f, .9f, 1.6f), new Vector3(0, 45, 0));
                    for (int i = 0; i < 4; i++) { var a = Quaternion.Euler(0, 45 + i * 90, 0); MeshKit.Cyl(t, M("#9aa0a8", .8f), a * new Vector3(0, .55f, 1.2f), .05f, .05f, 1.3f, 6, new Vector3(25, 45 + i * 90, 0)); MeshKit.Cyl(t, M("#9aa0a8", .8f), a * new Vector3(0, .05f, 1.5f), .22f, .22f, .06f, 10); }
                    MeshKit.Cyl(t, M("#d8dce2", .6f), new Vector3(2.4f, 1.2f, .4f), .03f, .03f, 2.4f, 6); MeshKit.Box(t, M("#e8ecf0", .1f, .2f), new Vector3(2.75f, 2.1f, .4f), new Vector3(.7f, .45f, .02f));
                    for (int i = 0; i < 14; i++) MeshKit.Box(t, M("#3a3e46", 0, .05f), new Vector3(1.4f + i * .45f, .02f, -1.2f + (i % 2) * .3f), new Vector3(.16f, .02f, .3f)); // Fußspuren
                    break;
                case "mars":
                    for (int i = 0; i < 3; i++) { var g = new GameObject("Staubteufel"); g.transform.SetParent(t, false); g.transform.localPosition = new Vector3(-6 + i * 6, 0, (float)r.NextDouble() * 6 - 3); wonderBits.Add(g.transform); wonderBase.Add(g.transform.localPosition); }
                    var rover = new GameObject("Rover"); rover.transform.SetParent(t, false); rover.transform.localPosition = new Vector3(0, 0, 4);
                    MeshKit.Box(rover.transform, M("#d8c8a8", .5f), new Vector3(0, .7f, 0), new Vector3(1.4f, .35f, 1.9f)); for (int i = 0; i < 6; i++) MeshKit.Cyl(rover.transform, M("#3a3a3e"), new Vector3(i % 2 == 0 ? -.8f : .8f, .3f, -.7f + (i / 2) * .7f), .3f, .3f, .2f, 12, new Vector3(0, 0, 90));
                    MeshKit.Cyl(rover.transform, M("#bfc4ca", .7f), new Vector3(.3f, 1.4f, .6f), .05f, .05f, 1.1f, 6); MeshKit.Box(rover.transform, M("#bfc4ca", .7f), new Vector3(.3f, 2f, .6f), new Vector3(.35f, .25f, .25f));
                    break;
                case "europa": case "enceladus": case "triton": case "gliese":
                    {
                        int n = ps.pid == "europa" ? 1 : ps.pid == "gliese" ? 4 : 3; string ring = ps.pid == "triton" ? "#4a3c3a" : "#cfe6f8";
                        /* Fabian 01.11.: kein schwebender Ring mehr – flacher Eishügel, der im Boden steckt, mit dunklem Schlot, Rissen und Eisbrocken */
                        string top = ps.pid == "triton" ? "#5a4c48" : ps.pid == "gliese" ? "#8a9aa2" : "#e4f2fc";
                        for (int i = 0; i < n; i++)
                        {
                            var p = new Vector3(-4 + i * 4, 0, (i % 2) * 2 - 1); float sc = ps.pid == "gliese" ? .7f : 1f;
                            MeshKit.Cyl(t, M(ring, .05f, .5f), p + new Vector3(0, -.05f, 0), 1.5f * sc, 2.6f * sc, .5f, 18);   // breiter Fuß, unten im Boden
                            MeshKit.Cyl(t, M(top, .05f, .85f), p + new Vector3(0, .32f, 0), .7f * sc, 1.45f * sc, .35f, 16);  // Kuppe
                            MeshKit.Cyl(t, M("#0c1014", 0, .1f), p + new Vector3(0, .5f, 0), .42f * sc, .5f * sc, .04f, 12);   // Schlot
                            for (int q = 0; q < 5; q++) { float a = q * 72 + i * 23; var dd = Quaternion.Euler(0, a, 0) * Vector3.forward; MeshKit.Box(t, M("#2a3a48", 0, .2f), p + dd * 1.9f * sc + new Vector3(0, .03f, 0), new Vector3(.07f, .03f, 1.3f * sc), new Vector3(0, a, 0)); } // Risse
                            for (int q = 0; q < 4; q++) { float a = q * 90 + 40 + i * 31; var dd = Quaternion.Euler(0, a, 0) * Vector3.forward; MeshKit.Part(t, Octa(), M(top, .05f, .8f), p + dd * (2.4f + q * .2f) * sc + new Vector3(0, .1f, 0), new Vector3(.35f, .25f, .3f) * sc, new Vector3(0, a, 20)); } // Eisbrocken
                            var g = new GameObject("Quelle"); g.transform.SetParent(t, false); g.transform.localPosition = p + new Vector3(0, .5f, 0); wonderBits.Add(g.transform);
                        }
                        if (ps.pid == "enceladus") for (int i = 0; i < 3; i++) MeshKit.Box(t, G("#3d93ff", 1.4f), new Vector3(-4 + i * 4, .03f, 0), new Vector3(.35f, .02f, 6f), new Vector3(0, 15, 0)); // Tigerstreifen
                        break;
                    }
                case "titan": break; // nur Regen (in UpdateWonder)
                case "io":
                    MeshKit.Cyl(t, M("#6a4a24", 0, .2f), new Vector3(0, 1.6f, 0), 1.2f, 4.5f, 3.2f, 18); MeshKit.Cyl(t, G("#ff6a1a", 3f), new Vector3(0, 3.22f, 0), 1.1f, 1.1f, .05f, 14);
                    { var lg = new GameObject("Glut"); lg.transform.SetParent(t, false); lg.transform.localPosition = new Vector3(0, 4.5f, 0); var L = lg.AddComponent<Light>(); L.type = LightType.Point; L.color = new Color(1, .55f, .2f); L.range = 18; L.intensity = 4; L.shadows = LightShadows.None; wonderBits.Add(lg.transform); }
                    break;
                case "venus": break; // Blitze in UpdateWonder
                case "ganymed":
                    for (int i = 0; i <= 12; i++) { float a = i / 12f * Mathf.PI; MeshKit.Box(t, M("#9fd2ff", .1f, .9f), new Vector3(Mathf.Cos(a) * 5, Mathf.Sin(a) * 4.2f, 0), new Vector3(1.3f, 1.3f, 1.1f), new Vector3(0, 0, a * Mathf.Rad2Deg)); }
                    break;
                case "pluto":
                    { var heart = G("#ffd8c8", 1.2f); for (int i = 0; i < 2; i++) MeshKit.Cyl(t, heart, new Vector3(i == 0 ? -1.6f : 1.6f, .03f, 1.2f), 2.2f, 2.2f, .02f, 24); MeshKit.Box(t, heart, new Vector3(0, .03f, -.6f), new Vector3(3.6f, .02f, 3.6f), new Vector3(0, 45, 0)); wonderSpin = t; }
                    break;
                case "eris": case "sedna": case "cancri":
                    {
                        string col = ps.pid == "sedna" ? "#ff3a4a" : ps.pid == "cancri" ? "#c8f6ff" : "#dfefff"; var gm = G(col, ps.pid == "eris" ? 1.2f : 2f);
                        int n = ps.pid == "cancri" ? 1 : 9;
                        for (int i = 0; i < n; i++)
                        {
                            float h = ps.pid == "cancri" ? 6f : 1.5f + (float)r.NextDouble() * 3.5f; var p = ps.pid == "cancri" ? Vector3.zero : new Vector3((float)r.NextDouble() * 8 - 4, 0, (float)r.NextDouble() * 8 - 4);
                            var c = MeshKit.Part(t, Octa(), gm, p + new Vector3(0, h * .5f, 0), new Vector3(h * .35f, h, h * .35f), new Vector3((float)r.NextDouble() * 20 - 10, (float)r.NextDouble() * 90, (float)r.NextDouble() * 20 - 10)); if (ps.pid == "cancri") wonderSpin = c.transform;
                        }
                        break;
                    }
                case "proxima":
                    for (int i = 0; i < 5; i++) { var b = MeshKit.Ball(t, M("#5a3a30", 0, .15f), new Vector3((float)r.NextDouble() * 8 - 4, 3 + (float)r.NextDouble() * 3, (float)r.NextDouble() * 8 - 4), .8f + (float)r.NextDouble() * 1.2f); b.transform.localScale = new Vector3(1.3f, .8f, 1.1f) * b.transform.localScale.x; wonderBits.Add(b.transform); wonderBase.Add(b.transform.localPosition); }
                    break;
                case "trappist":
                    for (int i = 0; i < 4; i++) MeshKit.Cyl(t, G(i % 2 == 0 ? "#3affd0" : "#5ab0ff", 1.6f), new Vector3(-4.5f + i * 3, .03f, (i % 2) * 2.5f - 1.2f), 1.2f + .3f * i, 1.2f + .3f * i, .02f, 20);
                    break;
                case "kepler":
                    for (int i = 0; i < 10; i++) { var p = new Vector3((float)r.NextDouble() * 9 - 4.5f, 0, (float)r.NextDouble() * 9 - 4.5f); float h = 2 + (float)r.NextDouble() * 2.5f; var stem = new GameObject("Pflanze"); stem.transform.SetParent(t, false); stem.transform.localPosition = p; MeshKit.Cyl(stem.transform, M("#3a6a2a", 0, .3f), new Vector3(0, h / 2, 0), .06f, .12f, h, 6); MeshKit.Ball(stem.transform, G(i % 3 == 0 ? "#d0ff6a" : "#6affc8", 3f), new Vector3(0, h, 0), .45f); wonderBits.Add(stem.transform); }
                    break;
                case "poltergeist":
                    for (int i = 0; i < 7; i++) { var b = MeshKit.Ball(t, G(i % 2 == 0 ? "#5affb0" : "#d04aff", 3f), new Vector3((float)r.NextDouble() * 10 - 5, 1.5f + (float)r.NextDouble() * 2, (float)r.NextDouble() * 10 - 5), .7f); NoShadow(b); wonderBits.Add(b.transform); wonderBase.Add(b.transform.localPosition); }
                    break;
            }
            /* an den Boden anpassen: jedes Teil auf die Bodenhöhe an seiner Stelle (Hügel, Krater), schwebende Teile behalten ihren Abstand */
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i); var lp = c.localPosition; var wp = t.TransformPoint(new Vector3(lp.x, 0, lp.z));
                float g = t.InverseTransformPoint(pd.Surf(wp.normalized, 0)).y; c.localPosition = lp + new Vector3(0, g, 0);
                int bi = wonderBits.IndexOf(c); if (bi >= 0 && bi < wonderBase.Count) wonderBase[bi] = c.localPosition;
            }
            foreach (var mr in wonder.GetComponentsInChildren<MeshRenderer>()) if (mr.sharedMaterial && mr.sharedMaterial.IsKeywordEnabled("_EMISSION")) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            /* beim ersten Besuch: Meldung */
            string k = "LunarCleanup.Entdeckt." + ps.pid; if (!Game.Offline && PlayerPrefs.GetInt(k, 0) == 0) { PlayerPrefs.SetInt(k, 1); PlayerPrefs.Save(); var info = WonderInfo[ps.pid]; Game.Toast("Entdeckt: " + info.name + " – " + info.text, "good"); }
        }

        void UpdateWonder()
        {
            if (ps == null || pd == null) return; if (wonderFor != ps.pid) BuildWonder(); if (MapMode) return;
            wonderT += Time.deltaTime; float tt = wonderT; var up = wonderAt; var w = wonder ? wonder.transform : null;
            Vector3 W(Vector3 local) => w ? w.TransformPoint(local) : pd.Surf(up, local.y);
            void Puff(Vector3 pos, Vector3 vel, Color c, float size, float life) { if (dust) dust.Emit(new ParticleSystem.EmitParams { position = pos, velocity = vel, startColor = c, startSize = size, startLifetime = life, applyShapeToPosition = false }, 1); }
            switch (ps.pid)
            {
                case "mars":
                    for (int i = 0; i < wonderBits.Count; i++) { var b = wonderBits[i]; b.localPosition = wonderBase[i] + new Vector3(Mathf.Sin(tt * .12f + i * 2) * 6, 0, Mathf.Cos(tt * .09f + i) * 5); if (Random.value < .8f) { float h = Random.value * 7; float a = tt * 6 + h * 1.4f; var p = W(b.localPosition + new Vector3(Mathf.Cos(a) * (.4f + h * .18f), h, Mathf.Sin(a) * (.4f + h * .18f))); Puff(p, up * 1.6f, new Color(.78f, .42f, .26f, .45f), .7f + h * .12f, 1.4f); } }
                    break;
                case "europa": case "enceladus": case "triton": case "gliese":
                    {
                        float period = ps.pid == "europa" ? 9 : ps.pid == "gliese" ? 3 : 6; Color c = ps.pid == "triton" ? new Color(.18f, .16f, .16f, .6f) : ps.pid == "enceladus" ? new Color(.7f, .9f, 1f, .7f) : ps.pid == "gliese" ? new Color(.85f, .92f, .95f, .35f) : new Color(.9f, .97f, 1f, .7f);
                        float hMax = ps.pid == "europa" ? 22 : ps.pid == "gliese" ? 4 : 12;
                        for (int i = 0; i < wonderBits.Count; i++) { float ph = Mathf.Repeat(tt / period + i * .37f, 1); if (ph < .35f && Random.value < .9f) { var side = ps.pid == "triton" ? w.right * 2.5f : Vector3.zero; Puff(W(wonderBits[i].localPosition) + up * .3f, up * (hMax * (1.1f - ph)) + side + Random.insideUnitSphere * 1.2f, c, .6f + Random.value * .8f, 2.2f); } }
                        break;
                    }
                case "titan": if (Camera.main) for (int i = 0; i < 3; i++) { var cp = Camera.main.transform.position; var p = cp + Camera.main.transform.forward * Random.Range(8, 30) + Camera.main.transform.right * Random.Range(-14, 14) + Camera.main.transform.up * Random.Range(4, 12); Puff(p, -up * 9, new Color(1f, .75f, .4f, .35f), .12f, 1.6f); } break;
                case "io":
                    { float ph = Mathf.Repeat(tt / 7f, 1); if (ph < .4f) for (int i = 0; i < 2; i++) Puff(W(new Vector3(0, 3.4f, 0)), up * (14 * (1 - ph)) + Random.insideUnitSphere * 3, Random.value < .5f ? new Color(1, .85f, .3f, .7f) : new Color(1, .45f, .15f, .7f), .6f + Random.value, 2.4f); if (wonderBits.Count > 0) { var L = wonderBits[0].GetComponent<Light>(); if (L) L.intensity = ph < .4f ? 6 : 2.5f; } break; }
                case "venus": if (Random.value < Time.deltaTime * .3f) { var at = pd.Surf((up + Random.insideUnitSphere * .08f).normalized, 18 + Random.value * 14); Flash(at, new Color(.95f, .9f, 1f)); Flash(at + Random.insideUnitSphere * 3, new Color(1f, .85f, .6f)); Sfx.Play("pop", .3f, .15f); } break; // Wetterleuchten in den Wolken
                case "pluto": if (wonderSpin) { float k = .85f + .15f * Mathf.Sin(tt * 1.3f); foreach (var mr in wonder.GetComponentsInChildren<MeshRenderer>()) mr.sharedMaterial.SetColor("_EmissionColor", GameData.Hex("#ffd8c8") * 1.2f * k); } break;
                case "cancri": if (wonderSpin) wonderSpin.Rotate(Vector3.up, 12 * Time.deltaTime, Space.Self); break;
                case "proxima": for (int i = 0; i < wonderBits.Count; i++) { wonderBits[i].localPosition = wonderBase[i] + new Vector3(0, Mathf.Sin(tt * .5f + i) * .8f, 0); wonderBits[i].Rotate(new Vector3(.3f, 1, .1f), 8 * Time.deltaTime, Space.Self); } break;
                case "kepler": for (int i = 0; i < wonderBits.Count; i++) wonderBits[i].localRotation = Quaternion.Euler(Mathf.Sin(tt * .9f + i) * 6, 0, Mathf.Cos(tt * .7f + i * 1.3f) * 6); break;
                case "poltergeist": for (int i = 0; i < wonderBits.Count; i++) wonderBits[i].localPosition = wonderBase[i] + new Vector3(Mathf.Sin(tt * .4f + i * 1.7f) * 3, Mathf.Sin(tt * 1.1f + i) * .8f, Mathf.Cos(tt * .33f + i * 2.1f) * 3); break;
                case "moon": if (Random.value < Time.deltaTime * .01f && Camera.main) { CometNow = true; FreighterNow = false; } break; // ab und zu eine Sternschnuppe
            }
        }
    }
}
