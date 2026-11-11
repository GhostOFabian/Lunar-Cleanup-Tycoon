// Fabian 11.11.: Landeplatz und Lieferschiff. Lieferungen von anderen Planeten (Spezialgüter fürs Terraforming) kommen nicht mehr
// mit einer Rakete „aus dem Nichts“: Ein Lieferschiff fliegt an, schwenkt die Triebwerke nach unten, landet auf dem Landeplatz,
// öffnet die Heckrampe, schiebt die Kisten hinaus, schließt die Rampe und startet wieder. Nur Darstellung – die Güter bucht die
// Simulation wie bisher (Game.OnSupply). Der Landeplatz ist immer im Plan (notfalls kleiner oder direkt vor der Basis).
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

        /// <summary>Ein Besuch auf dem Landeplatz. Es landet immer nur ein Schiff; weitere warten in der Reihe (höchstens 4).</summary>
        class Visit { public string model, kind; public Color col; public float stay, scale = 1, foot; public bool ramp; public int cargo; public System.Action onLand; } // cargo: 1 = Kisten raus, −1 = Kisten rein
        class Ship { public GameObject go; public Transform duesen, rampe, ladung, fahrwerk; public float t; public Vector3 from; public Visit v; public bool landed; }
        Ship ship; readonly Queue<Visit> visits = new Queue<Visit>();
        float ShipT(Visit v) => 12f + v.stay + 2.5f + 8f;

        float PadK => ps?.plan == null ? 1 : (2 * ps.plan.padH + 1) / 9f; /* kleinerer Landeplatz → Platz und Schiffe etwas kleiner */
        bool PadOn => ps != null && ps.plan != null && ps.plan.hasPad; /* 11.11.: von Anfang an da – Schiffe kommen auch vor dem Terraforming */

        void UpdatePad()
        {
            if (ps == null) return;
            bool on = PadOn;
            if (padFor != ps.pid || padVer != ps.baseVersion || padDone != on) BuildPad(on);
            if (padLights) { bool busy = ship != null; float f = busy ? 2.5f : .6f; padLights.gameObject.SetActive(Mathf.Repeat(Time.time * f, 1f) < (busy ? .5f : .8f)); }
            if (ship == null && visits.Count > 0 && padGo) StartShip(visits.Dequeue());
            UpdateShip();
        }
        public bool HasPad => padGo != null;
        /// <summary>Lieferung von einem anderen Planeten (Spezialgüter): Lieferschiff landet, Kisten raus.</summary>
        bool SupplyShip(Color col) => Enqueue(new Visit { kind = "supply", model = "Ereignisse/lieferschiff", col = col, stay = 7.5f, ramp = true, cargo = 1 });
        /// <summary>Zuschuss an einen anderen Planeten: Lieferschiff holt die Credits ab.</summary>
        public bool CashShip() => Enqueue(new Visit { kind = "cash", model = "Ereignisse/lieferschiff", col = GameData.Hex("#ffd24d"), stay = 7.5f, ramp = true, cargo = -1 });
        /// <summary>Besucher bestellt: Händlerschiff landet kurz und fliegt wieder.</summary>
        public bool VisitorArrive() => Enqueue(new Visit { kind = "visit", model = "Ereignisse/haendlerschiff", col = VisCol(), stay = 3.5f, scale = .55f, foot = 2.4f });
        /// <summary>Bestellung fertig: Händlerschiff landet, die Crew trägt die Kisten hinein, dann startet es.</summary>
        public bool VisitorPickup() => Enqueue(new Visit { kind = "pickup", model = "Ereignisse/haendlerschiff", col = VisCol(), stay = 14f, scale = .55f, foot = 2.4f, onLand = () => CrewCarryTo(PadEdge(), 13f) });
        Color VisCol() { Color c = ps.P.col0; return c.grayscale > .8f ? GameData.Hex("#6fe0ff") : c; }
        bool Enqueue(Visit v) { if (!PadOn || !LcKit.Has(v.model)) return false; /* Landeplatz kommt evtl. erst im nächsten Bild – Schiff wartet in der Reihe */ if (visits.Count < 4) visits.Enqueue(v); return true; }
        /// <summary>Rand des Landeplatzes zur Seite des Produktlagers (Ziel der Crew).</summary>
        Vector2 PadEdge() { var c = (Vector2)ps.plan.pad; var from = ps.lay.pos.ContainsKey("prod") ? ps.lay.pos["prod"] : ps.lay.pos.ContainsKey("hq") ? ps.lay.pos["hq"] : Vector2.zero; var d = (from - c); if (d.sqrMagnitude < .01f) d = Vector2.down; return c + d.normalized * (ps.plan.padH + 1.2f); }

        void BuildPad(bool on)
        {
            bool other = padFor != ps.pid || !on; padFor = ps.pid; padVer = ps.baseVersion; padDone = on;
            if (padGo) Destroy(padGo); padGo = null; padLights = null; if (other) { if (ship != null && ship.go) Destroy(ship.go); ship = null; visits.Clear(); } /* nur beim Planetenwechsel: Schiff und Reihe weg */
            if (!on) return;
            var P = ps.plan; Vector2 c = P.pad;
            Vector2 hq = ps.lay.pos.ContainsKey("hq") ? ps.lay.pos["hq"] : Vector2.zero; var u = c - hq; if (u.sqrMagnitude < .01f) u = Vector2.up; u.Normalize();
            float yaw = Mathf.Atan2(u.x, -u.y); /* PlanRot: vorne (+Z des Modells) = e1·sin − e2·cos → zeigt nach außen, zur Anflugseite */
            var d = pd.PlanDir(c); float hmax = pd.Height(d);
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; for (float r = 2f; r <= P.padH; r += 2f) hmax = Mathf.Max(hmax, pd.Height(pd.PlanDir(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r))); }
            Color acc = ps.P.col0; if (acc.grayscale > .8f) acc = GameData.Hex("#ffbf4d");
            padGo = LcKit.Has("Welt/landeplatz") ? LcKit.Spawn("Welt/landeplatz", transform, Vector3.zero, 0, acc, null, PadK) : null;
            if (!padGo)
            {
                padGo = new GameObject("Landeplatz"); padGo.transform.SetParent(transform, false);
                MeshKit.Box(padGo.transform, MeshKit.Mat(new Color(.29f, .31f, .38f), .2f, .7f), new Vector3(0, PAD_TOP / 2, 0), new Vector3(7.8f, PAD_TOP, 7.8f));
                MeshKit.Box(padGo.transform, MeshKit.Mat(acc, .3f, .45f), new Vector3(0, PAD_TOP + .01f, 0), new Vector3(5f, .02f, .25f));
            }
            padGo.name = "Landeplatz";
            foreach (var t in padGo.GetComponentsInChildren<Transform>(true)) if (t.name.EndsWith("_Lichter")) padLights = t;
            padGo.transform.position = d * (pd.R + hmax); padGo.transform.rotation = PlanRot(d, yaw);
            padUp = d; padTop = padGo.transform.position + d * PAD_TOP * PadK; padOut = padGo.transform.forward;
        }

        void StartShip(Visit v)
        {
            var go = new GameObject("Schiff " + v.kind); go.transform.SetParent(transform, false);
            float sk = v.scale * PadK; GameObject m = LcKit.Spawn(v.model, go.transform, Vector3.zero, 0, v.col, null, sk);
            if (!m) { Destroy(go); return; }
            var s = new Ship { go = go, v = v };
            /* OBJ hat keine Drehpunkte: bewegliche Teile unter einen Drehpunkt hängen (Werte aus lunar_models.py, in Unity-Achsen, Modellmaß) */
            Transform Piv(string suf, Vector3 p)
            {
                Transform part = null; foreach (var t in m.GetComponentsInChildren<Transform>(true)) if (t.name.EndsWith(suf)) part = t; if (!part) return null;
                var pv = new GameObject("Drehpunkt" + suf).transform; pv.SetParent(part.parent, false); pv.localPosition = p; pv.localRotation = Quaternion.identity; part.SetParent(pv, true); return pv;
            }
            if (v.model.EndsWith("lieferschiff")) { s.duesen = Piv("_Duesen", new Vector3(0, 1.65f, -.5f)); s.rampe = Piv("_Rampe", new Vector3(0, 1.15f, -3.92f)); s.ladung = Piv("_Ladung", new Vector3(0, 1f, -3.4f)); s.fahrwerk = Piv("_Fahrwerk", new Vector3(0, 1f, 0)); }
            else s.fahrwerk = Piv("_Fahrwerk", new Vector3(0, -1.5f, 0));
            m.transform.localPosition = Vector3.up * v.foot * sk; /* Füße auf der Plattform */
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fl = MeshKit.Part(go.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(1f, .7f, .35f, 1)), new Vector3(0, .9f, 0), Vector3.one * 3.2f); NoShadow(fl); fl.AddComponent<Billboard>(); fl.name = "Glut";
            if (v.cargo < 0 && s.ladung) s.ladung.gameObject.SetActive(false);
            s.from = padTop + padOut * pd.R * 1.1f + padUp * pd.R * .55f; ship = s; Sfx.Play("rocket", .7f, .45f); PoseShip(0);
        }

        void UpdateShip()
        {
            if (ship == null) return;
            if (!ship.go) { ship = null; return; }
            ship.t += Time.deltaTime; if (ship.t >= ShipT(ship.v)) { Destroy(ship.go); ship = null; return; }
            if (!ship.landed && ship.t >= 12f) { ship.landed = true; ship.v.onLand?.Invoke(); }
            PoseShip(ship.t);
        }

        static float S01(float a, float b, float t) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, t));
        /// <summary>Ablauf (s): 0–9 Anflug, 9–12 senkrecht landen, 12 … 12+stay am Boden (Rampe/Kisten), dann 2,5 s abheben auf 14 m, 8 s Weiterflug.</summary>
        void PoseShip(float t)
        {
            var s = ship; var v = s.v; var tr = s.go.transform; Vector3 hover = padTop + padUp * 6f, high = padTop + padUp * 14f, inward = -padOut; /* abheben bis 14 m – höher als jedes Gebäude */
            float t1 = 12f + v.stay, t2 = t1 + 2.5f, t3 = t2 + 8f;
            Vector3 pos; Vector3 fwd = inward; float tilt; /* 0 = Triebwerke nach hinten (Flug), 1 = nach unten (Landen) */
            if (t < 9f)
            {
                float k = S01(0, 9, t); var mid = Vector3.Lerp(s.from, hover, .55f) + padUp * pd.R * .08f;
                pos = Vector3.Lerp(Vector3.Lerp(s.from, mid, k), Vector3.Lerp(mid, hover, k), k);
                float k2 = Mathf.Min(1, k + .02f); var ahead = Vector3.Lerp(Vector3.Lerp(s.from, mid, k2), Vector3.Lerp(mid, hover, k2), k2) - pos; if (ahead.sqrMagnitude > 1e-4f && k < .9f) fwd = ahead.normalized;
                tilt = S01(4, 9, t);
            }
            else if (t < 12f) { pos = Vector3.Lerp(hover, padTop, S01(9, 12, t)); tilt = 1; }
            else if (t < t1) { pos = padTop; tilt = 1; }
            else if (t < t2) { pos = Vector3.Lerp(padTop, high, S01(t1, t2, t)); tilt = 1 - S01(t1 + 1, t2, t); }
            else
            {
                float k = Mathf.InverseLerp(t2, t3, t); float e = k * k; /* beschleunigt */
                pos = high + inward * pd.R * 1.3f * e + padUp * pd.R * .7f * e * e; tilt = 0;
                var ahead = inward * pd.R * 1.3f * (2 * k) + padUp * pd.R * .7f * (4 * k * k * k); if (ahead.sqrMagnitude > 1e-4f) fwd = Vector3.Slerp(inward, ahead.normalized, S01(0, .4f, k));
            }
            var up = pos.normalized; fwd = (fwd - up * Vector3.Dot(fwd, up)); if (fwd.sqrMagnitude < 1e-4f) fwd = inward; fwd.Normalize();
            float pitch = t < 9f ? -8f * (1 - S01(6, 9, t)) : t > t2 ? -10f * S01(t2, t2 + 2, t) : 0; /* im Flug leicht nach vorn geneigt */
            tr.position = pos; tr.rotation = Quaternion.LookRotation(fwd, up) * Quaternion.Euler(-pitch, 0, 0);
            if (s.duesen) s.duesen.localRotation = Quaternion.Euler(-90f * tilt, 0, 0);
            float gear = t < 7f ? 0 : t < 9.5f ? S01(7, 9.5f, t) : t < t1 + 1 ? 1 : 1 - S01(t1 + 1, t2, t);
            if (s.fahrwerk) s.fahrwerk.localScale = new Vector3(1, Mathf.Lerp(.12f, 1, gear), 1);
            if (v.ramp && s.rampe) s.rampe.localRotation = Quaternion.Euler(-112f * S01(12, 13.5f, t) * (1 - S01(t1 - 1.5f, t1, t)), 0, 0);
            if (s.ladung && v.cargo != 0)
            {
                /* raus: 13,5–16 über die Rampe, kurz stehen, dann weg. Rein: erscheinen auf dem Boden, 14,5–17 hinein. */
                float a, b, vis;
                if (v.cargo > 0) { a = S01(13.5f, 14.5f, t); b = S01(14.5f, 16f, t); vis = 1 - S01(t1 - 2.5f, t1 - 1.6f, t); }
                else { float r0 = S01(14.5f, 16f, t), r1 = S01(16f, 17f, t); b = 1 - r0; a = 1 - r1; vis = t < 13.5f ? 0 : S01(13.5f, 14.2f, t); }
                s.ladung.localPosition = new Vector3(0, 1f - 1.05f * b, -3.4f - .9f * a - 1.7f * b);
                s.ladung.localScale = Vector3.one * Mathf.Max(.001f, vis); s.ladung.gameObject.SetActive(vis > .001f);
            }
            var glow = tr.Find("Glut"); if (glow) glow.gameObject.SetActive((t < 12f || t > t1) && tilt > .3f);
        }
    }
}
