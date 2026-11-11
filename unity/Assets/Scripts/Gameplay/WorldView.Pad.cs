// Fabian 11.11.: Landeplatz und Lieferschiff. Lieferungen von anderen Planeten (Spezialgüter fürs Terraforming) kommen nicht mehr
// mit einer Rakete „aus dem Nichts“: Ein Lieferschiff fliegt an, schwenkt die Triebwerke nach unten, landet auf dem Landeplatz,
// öffnet die Heckrampe, schiebt die Kisten hinaus, schließt die Rampe und startet wieder. Nur Darstellung – die Güter bucht die
// Simulation wie bisher (Game.OnSupply). Ohne Landeplatz im Plan (kein Platz) landet weiter die alte Lieferrakete.
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        GameObject padGo; Transform padLights; string padFor; int padVer = -1; bool padDone;
        Vector3 padTop, padUp, padOut; // Mitte der Plattform-Oberfläche, Normale, Richtung nach außen (Anflugseite)
        const float PAD_TOP = .3f;     // Höhe der Plattform im Modell

        class Ship { public GameObject go; public Transform duesen, rampe, ladung, fahrwerk; public float t; public Vector3 from; }
        Ship ship;
        const float SHIP_T = 30f;      // ganzer Besuch in Sekunden

        bool PadOn => ps != null && ps.plan != null && ps.plan.hasPad && Game.IsDone(ps.pid);

        void UpdatePad()
        {
            if (ps == null) return;
            bool on = PadOn;
            if (padFor != ps.pid || padVer != ps.baseVersion || padDone != on) BuildPad(on);
            if (padLights) { bool busy = ship != null; float f = busy ? 2.5f : .6f; padLights.gameObject.SetActive(Mathf.Repeat(Time.time * f, 1f) < (busy ? .5f : .8f)); }
            UpdateShip();
        }

        void BuildPad(bool on)
        {
            padFor = ps.pid; padVer = ps.baseVersion; padDone = on;
            if (padGo) Destroy(padGo); padGo = null; padLights = null; if (ship != null) { if (ship.go) Destroy(ship.go); ship = null; }
            if (!on) return;
            var P = ps.plan; Vector2 c = P.pad;
            Vector2 hq = ps.lay.pos.ContainsKey("hq") ? ps.lay.pos["hq"] : Vector2.zero; var u = c - hq; if (u.sqrMagnitude < .01f) u = Vector2.up; u.Normalize();
            float yaw = Mathf.Atan2(u.x, -u.y); /* PlanRot: vorne (+Z des Modells) = e1·sin − e2·cos → zeigt nach außen, zur Anflugseite */
            var d = pd.PlanDir(c); float hmax = pd.Height(d);
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; for (float r = 2f; r <= FactoryPlan.PAD_H; r += 2f) hmax = Mathf.Max(hmax, pd.Height(pd.PlanDir(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r))); }
            Color acc = ps.P.col0; if (acc.grayscale > .8f) acc = GameData.Hex("#ffbf4d");
            padGo = LcKit.Has("Welt/landeplatz") ? LcKit.Spawn("Welt/landeplatz", transform, Vector3.zero, 0, acc) : null;
            if (!padGo)
            {
                padGo = new GameObject("Landeplatz"); padGo.transform.SetParent(transform, false);
                MeshKit.Box(padGo.transform, MeshKit.Mat(new Color(.29f, .31f, .38f), .2f, .7f), new Vector3(0, PAD_TOP / 2, 0), new Vector3(7.8f, PAD_TOP, 7.8f));
                MeshKit.Box(padGo.transform, MeshKit.Mat(acc, .3f, .45f), new Vector3(0, PAD_TOP + .01f, 0), new Vector3(5f, .02f, .25f));
            }
            padGo.name = "Landeplatz";
            foreach (var t in padGo.GetComponentsInChildren<Transform>(true)) if (t.name.EndsWith("_Lichter")) padLights = t;
            padGo.transform.position = d * (pd.R + hmax); padGo.transform.rotation = PlanRot(d, yaw);
            padUp = d; padTop = padGo.transform.position + d * PAD_TOP; padOut = padGo.transform.forward;
        }

        /// <summary>Lieferung kommt an: Schiff starten (wenn schon eins da ist, bleibt es bei dem einen Besuch). Gibt false zurück, wenn kein Landeplatz da ist.</summary>
        bool SupplyShip(Color col)
        {
            if (!padGo) return false; if (ship != null) return true;
            var go = new GameObject("Lieferschiff"); go.transform.SetParent(transform, false);
            GameObject m = LcKit.Has("Ereignisse/lieferschiff") ? LcKit.Spawn("Ereignisse/lieferschiff", go.transform, Vector3.zero, 0, col) : null;
            if (!m) { Destroy(go); return false; }
            var s = new Ship { go = go };
            /* OBJ hat keine Drehpunkte: bewegliche Teile unter einen Drehpunkt hängen (Werte aus lunar_models.py, in Unity-Achsen) */
            Transform Piv(string suf, Vector3 p)
            {
                Transform part = null; foreach (var t in m.GetComponentsInChildren<Transform>(true)) if (t.name.EndsWith(suf)) part = t; if (!part) return null;
                var pv = new GameObject("Drehpunkt" + suf).transform; pv.SetParent(part.parent, false); pv.localPosition = p; pv.localRotation = Quaternion.identity; part.SetParent(pv, true); return pv;
            }
            s.duesen = Piv("_Duesen", new Vector3(0, 1.65f, -.5f)); s.rampe = Piv("_Rampe", new Vector3(0, 1.15f, -3.92f)); s.ladung = Piv("_Ladung", new Vector3(0, 1f, -3.4f)); s.fahrwerk = Piv("_Fahrwerk", new Vector3(0, 1f, 0));
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fl = MeshKit.Part(go.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(1f, .7f, .35f, 1)), new Vector3(0, .9f, 0), Vector3.one * 3.2f); NoShadow(fl); fl.AddComponent<Billboard>(); fl.name = "Glut";
            s.from = padTop + padOut * pd.R * 1.1f + padUp * pd.R * .55f; ship = s; Sfx.Play("rocket", .7f, .45f); PoseShip(0); return true;
        }

        void UpdateShip()
        {
            if (ship == null) return;
            if (!ship.go) { ship = null; return; }
            ship.t += Time.deltaTime; if (ship.t >= SHIP_T) { Destroy(ship.go); ship = null; return; }
            PoseShip(ship.t);
        }

        static float S01(float a, float b, float t) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, t));
        /// <summary>Ablauf (s): 0–9 Anflug, 9–12 senkrecht landen, 12–13,5 Rampe auf, 13,5–16 Kisten raus, 18–19,5 Rampe zu, 19,5–22 abheben, 22–30 weiterfliegen.</summary>
        void PoseShip(float t)
        {
            var s = ship; var tr = s.go.transform; Vector3 hover = padTop + padUp * 6f, high = padTop + padUp * 14f, inward = -padOut; /* abheben bis 14 m – höher als jedes Gebäude */
            Vector3 pos; Vector3 fwd = inward; float tilt; /* 0 = Triebwerke nach hinten (Flug), 1 = nach unten (Landen) */
            if (t < 9f)
            {
                float k = S01(0, 9, t); var mid = Vector3.Lerp(s.from, hover, .55f) + padUp * pd.R * .08f;
                pos = Vector3.Lerp(Vector3.Lerp(s.from, mid, k), Vector3.Lerp(mid, hover, k), k);
                var ahead = (Vector3.Lerp(Vector3.Lerp(s.from, mid, k + .02f), Vector3.Lerp(mid, hover, k + .02f), k + .02f) - pos); if (ahead.sqrMagnitude > 1e-4f && k < .9f) fwd = ahead.normalized;
                tilt = S01(4, 9, t);
            }
            else if (t < 12f) { pos = Vector3.Lerp(hover, padTop, S01(9, 12, t)); tilt = 1; }
            else if (t < 19.5f) { pos = padTop; tilt = 1; }
            else if (t < 22f) { pos = Vector3.Lerp(padTop, high, S01(19.5f, 22, t)); tilt = 1 - S01(20.5f, 22, t); }
            else
            {
                float k = Mathf.InverseLerp(22, SHIP_T, t); float e = k * k; /* beschleunigt */
                pos = high + inward * pd.R * 1.3f * e + padUp * pd.R * .7f * e * e; tilt = 0;
                var ahead = inward * pd.R * 1.3f * (2 * k) + padUp * pd.R * .7f * (4 * k * k * k); if (ahead.sqrMagnitude > 1e-4f) fwd = Vector3.Slerp(inward, ahead.normalized, S01(0, .4f, k));
            }
            var up = pos.normalized; fwd = (fwd - up * Vector3.Dot(fwd, up)); if (fwd.sqrMagnitude < 1e-4f) fwd = inward; fwd.Normalize();
            /* im Flug leicht nach vorn geneigt */
            float pitch = t < 9f ? -8f * (1 - S01(6, 9, t)) : t > 22f ? -10f * S01(22, 24, t) : 0;
            tr.position = pos; tr.rotation = Quaternion.LookRotation(fwd, up) * Quaternion.Euler(-pitch, 0, 0);
            if (s.duesen) s.duesen.localRotation = Quaternion.Euler(-90f * tilt, 0, 0);
            float gear = t < 7f ? 0 : t < 9.5f ? S01(7, 9.5f, t) : t < 20.5f ? 1 : 1 - S01(20.5f, 22, t);
            if (s.fahrwerk) s.fahrwerk.localScale = new Vector3(1, Mathf.Lerp(.12f, 1, gear), 1);
            float ramp = S01(12, 13.5f, t) * (1 - S01(18, 19.5f, t));
            if (s.rampe) s.rampe.localRotation = Quaternion.Euler(-112f * ramp, 0, 0);
            if (s.ladung)
            {
                float a = S01(13.5f, 14.5f, t), b = S01(14.5f, 16f, t), gone = S01(17f, 18f, t);
                s.ladung.localPosition = new Vector3(0, 1f - 1.05f * b, -3.4f - .9f * a - 1.7f * b);
                s.ladung.localScale = Vector3.one * Mathf.Max(.001f, 1 - gone); s.ladung.gameObject.SetActive(gone < .999f);
            }
            var glow = tr.Find("Glut"); if (glow) { float on = t < 12f ? 1 : t < 19.5f ? 0 : 1; glow.gameObject.SetActive(on > 0 && tilt > .3f); }
        }
    }
}
