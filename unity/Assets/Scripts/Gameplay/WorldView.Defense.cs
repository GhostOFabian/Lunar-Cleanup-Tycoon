// Build 36 (Fabian 02.11.): Meteorschild-Generator und Abwehrraketen-Batterie stehen sichtbar an der Basis.
// Modelle aus Fabians Blender-Skript (Modelle_Quelle/lunar_models.py: schild, flak), Platz wird frei gesucht (keine Gebäude, keine Bänder).
using System.Collections.Generic;
using System.Linq;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        Transform defRoot; string defFor; int defVer = -1, defMs = -1, defFlak = -1;
        GameObject shieldGo, flakGo; Transform shieldSpin, flakTurret; Material shieldField; Vector2 shieldAt, flakAt; object flakAim;
        const float DEF_FP = 2.4f;
        Transform dome; Material domeMat; public float domeR = 30; public Vector3 domeAt; public float DomePulse;
        public Vector3? DefPos(int i) { var g = i == 0 ? shieldGo : flakGo; return g ? g.transform.position : (Vector3?)null; }
        /// <summary>Freier Platz an der Basis: nicht auf Gebäudeflächen (auch noch nicht gebauten), nicht auf Bändern, Laternen oder dem anderen Abwehrbau.</summary>
        Vector2? FreeSpot(Vector2 prefer, params Vector2[] avoid)
        {
            Vector2? best = null; float bs = 1e9f;
            for (float r = 8; r <= 34; r += 2)
                for (int k = 0; k < 36; k++)
                {
                    float a = k * Mathf.PI / 18; var p = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                    bool ok = true;
                    foreach (var kv in ps.lay.pos) { if (!GameData.Bld.ContainsKey(kv.Key)) continue; if ((kv.Value - p).magnitude < GameData.Bld[kv.Key].fp + DEF_FP + 1.2f) { ok = false; break; } }
                    if (!ok) continue;
                    if (ps.plan != null && ps.plan.hasPad && Mathf.Abs(p.x - ps.plan.pad.x) < ps.plan.padH + DEF_FP + 1f && Mathf.Abs(p.y - ps.plan.pad.y) < ps.plan.padH + DEF_FP + 1f) continue; /* 11.11.: Landeplatz frei lassen */
                    foreach (var kv in routed) { if (SegDist(kv.Value, p) < DEF_FP + 1.4f) { ok = false; break; } }
                    if (!ok) continue;
                    foreach (var l in lampSpots) if ((l - p).magnitude < DEF_FP + 1) { ok = false; break; }
                    if (!ok) continue; if (avoid != null) foreach (var av in avoid) if ((av - p).magnitude < DEF_FP * 2 + 2) { ok = false; break; } if (!ok) continue;
                    var dir = pd.PlanDir(p); int cell = pd.CellOf(dir); if (cell < 0 || pd.cells[cell].Wet) continue;
                    float sc = (p - prefer).magnitude; if (sc < bs) { bs = sc; best = p; }
                }
            return best;
        }
        void BuildDefense()
        {
            defFor = ps.pid; defVer = ps.baseVersion; defMs = ps.sv.msLv; defFlak = ps.sv.flakLv;
            if (defRoot) Destroy(defRoot.gameObject); if (domeMat) Destroy(domeMat); dome = null; domeMat = null; defRoot = New("Abwehr"); shieldGo = flakGo = null; shieldSpin = flakTurret = null; shieldField = null;
            Color acc = ps.P.col0; if (acc.grayscale > .8f) acc = GameData.Hex("#ffbf4d");
            Vector2 hq = ps.lay.pos.ContainsKey("hq") ? ps.lay.pos["hq"] : Vector2.zero, sell = ps.lay.pos.ContainsKey("sell") ? ps.lay.pos["sell"] : Vector2.one * 10;
            /* Fabian 03.11.: Schildkuppel über der ganzen Basis – Mitte und Radius aus allen Basisparzellen, der Generator steht möglichst mittig */
            Vector2 bc = Vector2.zero; int nb = 0; foreach (var cl in pd.cells) if (cl.IsBase) { bc += pd.PlanLocal(cl.c); nb++; } if (nb > 0) bc /= nb;
            float br = 0; foreach (var cl in pd.cells) if (cl.IsBase) br = Mathf.Max(br, (pd.PlanLocal(cl.c) - bc).magnitude + cl.H * 1.25f); domeR = Mathf.Max(20, br + 3); domeAt = pd.Surf(pd.PlanDir(bc), 0);
            if (ps.sv.msLv > 0) { var sp = FreeSpot(bc); if (sp.HasValue) { shieldAt = sp.Value; shieldGo = PlaceDef("Abwehr/schild", shieldAt, acc, .85f + .05f * ps.sv.msLv); } }
            if (ps.sv.msLv > 0)
            {
                domeMat = GlowMat(Texture2D.whiteTexture, new Color(.35f, .75f, 1f, .05f)); var dg = MeshKit.Part(defRoot, MeshKit.Sphere, domeMat, Vector3.zero, Vector3.one); NoShadow(dg); dome = dg.transform;
                dome.position = domeAt; dome.rotation = Quaternion.FromToRotation(Vector3.up, domeAt.normalized); dome.localScale = Vector3.one * domeR * 2f / Mathf.Max(.01f, defRoot.lossyScale.x);
            }
            if (ps.sv.flakLv > 0) { var sp = shieldGo ? FreeSpot(sell + (sell - hq).normalized * 8, shieldAt) : FreeSpot(sell + (sell - hq).normalized * 8); if (sp.HasValue) { flakAt = sp.Value; flakGo = PlaceDef("Abwehr/flak", flakAt, acc, .9f + .03f * ps.sv.flakLv); } }
            if (shieldGo)
            {
                shieldSpin = shieldGo.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.EndsWith("_Dreh"));
                foreach (var r in shieldGo.GetComponentsInChildren<MeshRenderer>()) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) if (ms[i] && ms[i].name == "LC_schildfeld") { if (!shieldField) shieldField = new Material(ms[i]); ms[i] = shieldField; } r.sharedMaterials = ms; }
            }
            if (flakGo) flakTurret = flakGo.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.EndsWith("_Dreh"));
        }
        GameObject PlaceDef(string path, Vector2 at, Color acc, float scale)
        {
            GameObject g = LcKit.Has(path) ? LcKit.Spawn(path, defRoot, Vector3.zero, 0, acc, null, scale) : null;
            if (!g) { g = new GameObject(path); g.transform.SetParent(defRoot, false); var body = MeshKit.Cyl(g.transform, MeshKit.Mat(new Color(.8f, .82f, .86f), .2f, .5f), new Vector3(0, .9f, 0), .6f, .9f, 1.8f, 16); var top = MeshKit.Ball(g.transform, MeshKit.Mat(new Color(.5f, .9f, 1f), 0, .5f, new Color(.4f, .85f, 1f) * 2), new Vector3(0, 2.4f, 0), 1.1f); }
            var d = pd.PlanDir(at); float hmax = pd.Height(d); for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; hmax = Mathf.Max(hmax, pd.Height(pd.PlanDir(at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * DEF_FP))); }
            g.transform.position = d * (pd.R + hmax); g.transform.rotation = PlanRot(d, 0) * g.transform.localRotation; return g;
        }
        /// <summary>Mündung der Raketenbatterie (Abschusspunkt der Abwehrraketen).</summary>
        Vector3? FlakMuzzle(out Vector3 up)
        {
            up = Vector3.up; if (!flakGo) return null; up = flakGo.transform.up;
            return flakGo.transform.position + up * 2.6f * flakGo.transform.lossyScale.y;
        }
        void UpdateDefense()
        {
            if (ps == null) return;
            if (defFor != ps.pid || defVer != ps.baseVersion || defMs != ps.sv.msLv || defFlak != ps.sv.flakLv || !defRoot) { if (builtVersion == ps.baseVersion) BuildDefense(); else return; }
            float t = Time.time;
            if (dome && domeMat)
            {
                /* Kuppel: bereit = zartes Blau (stärker, solange ein Riesenmeteor anfliegt), lädt nach = schwach orange, Batterie leer = aus; bei Abwehr blitzt sie auf */
                DomePulse = Mathf.Max(0, DomePulse - Time.deltaTime * .6f); bool threat = Game.DoomActive && Game.S.doomPid == ps.pid;
                Color c = ps.sv.msCool > 0 ? new Color(1f, .55f, .2f, .03f) : ps.sv.energy < 2 ? new Color(.35f, .75f, 1f, 0) : new Color(.35f, .75f, 1f, (threat ? .1f : .035f) + .015f * Mathf.Sin(t * 1.6f));
                c.a += .35f * DomePulse; domeMat.SetColor("_BaseColor", c); dome.gameObject.SetActive(c.a > .004f && !MapMode);
            }
            if (shieldSpin) shieldSpin.Rotate(Vector3.forward, (ps.sv.msCool > 0 ? 15 : 60) * Time.deltaTime, Space.Self);
            if (shieldField)
            {
                // Zustand am Leuchten: bereit = hellblau pulsierend, lädt nach = schwach orange, Batterie leer = aus
                Color e = ps.sv.msCool > 0 ? new Color(1f, .5f, .15f) * .6f : ps.sv.energy < 2 ? Color.black : new Color(.2f, .7f, 1f) * (1.4f + .5f * Mathf.Sin(t * 2.2f));
                shieldField.SetColor("_EmissionColor", e);
            }
            if (flakTurret)
            {
                var ch = flakAim as Chunk; Vector3 local;
                if (ch != null && ch.go && !ch.dead) { local = flakGo.transform.InverseTransformPoint(ch.go.transform.position); float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg; flakTurret.localRotation = Quaternion.Slerp(flakTurret.localRotation, Quaternion.Euler(-90, 0, 0) * Quaternion.Euler(0, 0, -yaw + 180), Time.deltaTime * 6); }
                else flakTurret.Rotate(Vector3.forward, (icpOn ? 40 : 8) * Time.deltaTime, Space.Self);
            }
        }
    }
}
