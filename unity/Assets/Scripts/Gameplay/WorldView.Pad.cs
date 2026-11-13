// Fabian 11.11.: Landeplatz und Lieferschiff. Lieferungen von anderen Planeten (Spezialgüter fürs Terraforming) kommen nicht mehr
// mit einer Rakete „aus dem Nichts“: Ein Lieferschiff fliegt an, schwenkt die Triebwerke nach unten, landet auf dem Landeplatz,
// öffnet die Heckrampe, schiebt die Kisten hinaus, schließt die Rampe und startet wieder. Nur Darstellung – die Güter bucht die
// Simulation wie bisher (Game.OnSupply). Der Landeplatz ist immer im Plan (notfalls kleiner oder direkt vor der Basis).
// Fabian 13.11.: statt der Plattform eine U-förmige Landebahn im selben Feld (9 × 9 m). Schiffe landen wie Flugzeuge auf dem
// linken Schenkel (offenes Ende zeigt von der Basis weg), rollen durch die Kurve bis zum Scheitel (dort wird ent- oder beladen),
// rollen weiter auf den rechten Schenkel und starten dort mit Boost. Die Nase zeigt immer in Fahrtrichtung.
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
        class Visit { public string model, kind; public Color col; public float stay, scale = 1, foot, len = 8; public bool ramp; public int cargo; public System.Action onLand; } // cargo: 1 = Kisten raus, −1 = Kisten rein
        class Ship { public GameObject go; public Transform duesen, rampe, ladung, fahrwerk, flame; public Material flameM; public float t; public Vector3 from; public Visit v; public bool landed; }
        Ship ship; readonly Queue<Visit> visits = new Queue<Visit>();
        const float T_TD = 7f, T_LAND = 11f, T_DEP = 13f; /* Aufsetzen, Halt am Scheitel; Abflug: 4,5 s Rollen mit Boost + 8,5 s Steigflug */
        float ShipT(Visit v) => T_LAND + v.stay + T_DEP;
        const float RW_X = 2.6f, RW_L = 4.4f, RW_W = 2.2f; /* Mittellinie der Schenkel bei x = ±2,6, Länge 4,4, Breite 2,2; Kurve mit Radius 2,6 */
        static float RwLen => 2 * RW_L + Mathf.PI * RW_X;
        static float RwApex => RW_L + Mathf.PI * RW_X * .5f;
        /// <summary>Punkt (lokal, x/z) und Fahrtrichtung auf der Mittellinie der U-Bahn, s = Meter ab dem offenen Ende des Landeschenkels.</summary>
        static Vector2 RwAt(float s, out Vector2 dir)
        {
            if (s <= RW_L) { dir = new Vector2(0, -1); return new Vector2(-RW_X, RW_L - s); }
            float arc = Mathf.PI * RW_X;
            if (s <= RW_L + arc) { float a = Mathf.PI + (s - RW_L) / RW_X; dir = new Vector2(-Mathf.Sin(a), Mathf.Cos(a)); return new Vector2(RW_X * Mathf.Cos(a), RW_X * Mathf.Sin(a)); }
            dir = new Vector2(0, 1); return new Vector2(RW_X, s - RW_L - arc);
        }

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
        /// <summary>Editor/Test: Schiff auf einen Zeitpunkt seines Ablaufs setzen.</summary>
        public string DevShipT(float t) { if (ship == null) return "kein Schiff"; ship.t = t; ship.landed = t >= T_LAND; PoseShip(t); return "ok"; }
        /// <summary>Lieferung von einem anderen Planeten (Spezialgüter): Lieferschiff landet, Kisten raus.</summary>
        bool SupplyShip(Color col) => Enqueue(new Visit { kind = "supply", model = "Ereignisse/lieferschiff", col = col, stay = 7.5f, ramp = true, cargo = 1, scale = .5f });
        /// <summary>Zuschuss an einen anderen Planeten: Lieferschiff holt die Credits ab.</summary>
        public bool CashShip() => Enqueue(new Visit { kind = "cash", model = "Ereignisse/lieferschiff", col = GameData.Hex("#ffd24d"), stay = 7.5f, ramp = true, cargo = -1, scale = .5f });
        /// <summary>Besucher bestellt: Händlerschiff landet kurz und fliegt wieder.</summary>
        public bool VisitorArrive() => Enqueue(new Visit { kind = "visit", model = "Ereignisse/haendlerschiff", col = VisCol(), stay = 3.5f, scale = .42f, foot = 2.4f, len = 7 });
        /// <summary>Bestellung fertig: Händlerschiff landet, die Crew trägt die Kisten hinein, dann startet es.</summary>
        public bool VisitorPickup() => Enqueue(new Visit { kind = "pickup", model = "Ereignisse/haendlerschiff", col = VisCol(), stay = 14f, scale = .42f, foot = 2.4f, len = 7, onLand = () => CrewCarryTo(PadEdge(), 13f) });
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
            padGo = new GameObject("Landeplatz"); padGo.transform.SetParent(transform, false); BuildRunway(padGo.transform, acc); padGo.transform.localScale = Vector3.one * PadK;
            padGo.name = "Landeplatz";
            padLights = padGo.transform.Find("Lichter");
            padGo.transform.position = d * (pd.R + hmax); padGo.transform.rotation = PlanRot(d, yaw);
            padUp = d; padTop = padGo.transform.position + d * PAD_TOP * PadK; padOut = padGo.transform.forward;
        }

        /// <summary>13.11.: U-förmige Landebahn (lokal: +Z = offenes Ende, von der Basis weg). Landen links, Scheitel zum Be- und Entladen, Start rechts.</summary>
        void BuildRunway(Transform t, Color acc)
        {
            Material asph = MeshKit.Mat(new Color(.2f, .21f, .25f), .1f, .55f), rim = MeshKit.Mat(new Color(.36f, .38f, .44f), .2f, .5f), hell = MeshKit.Mat(new Color(.92f, .93f, .95f), 0, .4f), ak = MeshKit.Mat(acc, .3f, .45f);
            float h = PAD_TOP, y = h / 2;
            for (int sx = -1; sx <= 1; sx += 2)
            {
                MeshKit.Box(t, rim, new Vector3(sx * RW_X, y - .02f, RW_L / 2), new Vector3(RW_W + .3f, h - .04f, RW_L));
                MeshKit.Box(t, asph, new Vector3(sx * RW_X, y, RW_L / 2), new Vector3(RW_W, h, RW_L));
                for (float z = .5f; z < RW_L - .6f; z += 1f) MeshKit.Box(t, hell, new Vector3(sx * RW_X, h + .01f, z + .25f), new Vector3(.14f, .02f, .5f)); /* Mittellinie */
                for (int k = -2; k <= 2; k++) MeshKit.Box(t, hell, new Vector3(sx * RW_X + k * .38f, h + .01f, RW_L - .35f), new Vector3(.22f, .02f, .5f)); /* Schwelle am offenen Ende */
                foreach (float ex in new[] { -1f, 1f }) MeshKit.Box(t, ak, new Vector3(sx * RW_X + ex * (RW_W / 2 - .08f), h + .012f, RW_L / 2), new Vector3(.1f, .02f, RW_L)); /* Randlinien */
                /* Pfeil: links nach innen (Landen), rechts nach außen (Start) */
                float dz = sx < 0 ? -1 : 1, z0 = RW_L * .55f;
                MeshKit.Box(t, ak, new Vector3(sx * RW_X - .2f, h + .013f, z0), new Vector3(.12f, .02f, .7f), new Vector3(0, 35f * dz, 0));
                MeshKit.Box(t, ak, new Vector3(sx * RW_X + .2f, h + .013f, z0), new Vector3(.12f, .02f, .7f), new Vector3(0, -35f * dz, 0));
            }
            const int N = 16; float arc = Mathf.PI / N;
            for (int i = 0; i < N; i++)
            {
                float a = Mathf.PI + (i + .5f) * arc; var p = new Vector3(RW_X * Mathf.Cos(a), y, RW_X * Mathf.Sin(a)); var tg = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
                float yaw = Mathf.Atan2(tg.x, tg.z) * Mathf.Rad2Deg, chord = 2 * (RW_X + RW_W / 2) * Mathf.Sin(arc / 2) * 1.05f;
                MeshKit.Box(t, rim, p - Vector3.up * .02f, new Vector3(RW_W + .3f, h - .04f, chord), new Vector3(0, yaw, 0));
                MeshKit.Box(t, asph, p, new Vector3(RW_W, h, chord), new Vector3(0, yaw, 0));
                if (i % 2 == 0) MeshKit.Box(t, hell, new Vector3(p.x, h + .01f, p.z), new Vector3(.14f, .02f, .5f), new Vector3(0, yaw, 0));
            }
            /* Haltepunkt am Scheitel */
            MeshKit.Box(t, ak, new Vector3(-.5f, h + .013f, -RW_X), new Vector3(.12f, .02f, 1.3f)); MeshKit.Box(t, ak, new Vector3(.5f, h + .013f, -RW_X), new Vector3(.12f, .02f, 1.3f));
            /* kleiner Tower in der Mitte des U */
            MeshKit.Box(t, rim, new Vector3(0, .6f, 1.6f), new Vector3(.7f, 1.2f, .7f)); MeshKit.Box(t, MeshKit.Mat(new Color(.4f, .75f, .9f), .6f, .9f, new Color(.2f, .5f, .7f)), new Vector3(0, 1.4f, 1.6f), new Vector3(.9f, .4f, .9f)); MeshKit.Box(t, asph, new Vector3(0, 1.65f, 1.6f), new Vector3(1f, .1f, 1f));
            /* Randlichter (blinken) */
            var L = new GameObject("Lichter").transform; L.SetParent(t, false); Material lm = MeshKit.Glow(new Color(1f, .78f, .35f), 3f), lm2 = MeshKit.Glow(new Color(.45f, 1f, .8f), 3f);
            void Lamp(Vector3 p, Material m) { var g = MeshKit.Part(L, MeshKit.Sphere, m, p, Vector3.one * .14f); NoShadow(g); }
            for (int sx = -1; sx <= 1; sx += 2) for (float z = .3f; z <= RW_L; z += 1.1f) foreach (float ex in new[] { -1f, 1f }) Lamp(new Vector3(sx * RW_X + ex * (RW_W / 2 + .1f), h + .06f, z), z > RW_L - .5f ? lm2 : lm);
            for (int i = 0; i <= 8; i++) { float a = Mathf.PI + i * Mathf.PI / 8; foreach (float r in new[] { RW_X - RW_W / 2 - .1f, RW_X + RW_W / 2 + .1f }) Lamp(new Vector3(r * Mathf.Cos(a), h + .06f, r * Mathf.Sin(a)), lm); }
            foreach (var r in t.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        /// <summary>Lokaler Punkt der Bahn (Oberfläche) in Weltkoordinaten.</summary>
        Vector3 RwWorld(Vector2 p, float lift = 0) => padGo.transform.TransformPoint(new Vector3(p.x, PAD_TOP, p.y)) + padUp * lift;
        Vector3 RwDir(Vector2 d) => padGo.transform.TransformDirection(new Vector3(d.x, 0, d.y)).normalized;

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
            if (v.model.EndsWith("lieferschiff")) { s.duesen = Piv("_Duesen", new Vector3(0, 1.65f, .5f)); s.rampe = Piv("_Rampe", new Vector3(0, 1.15f, 3.92f)); s.ladung = Piv("_Ladung", new Vector3(0, 1f, 3.4f)); s.fahrwerk = Piv("_Fahrwerk", new Vector3(0, 1f, 0)); }
            else s.fahrwerk = Piv("_Fahrwerk", new Vector3(0, -1.5f, 0));
            m.transform.localPosition = Vector3.up * v.foot * sk; /* Füße auf der Plattform */
            m.transform.localRotation = Quaternion.Euler(0, 180, 0); /* 13.11.: Fabian sah die Schiffe rückwärts landen – im importierten Modell zeigt die Nase nach −Z, das Heck (Rampe) nach +Z */
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fm = GlowMat(MeshKit.GlowTex(), new Color(1f, .62f, .28f, 1)); var fl = MeshKit.Part(go.transform, MeshKit.Quad, fm, new Vector3(0, (v.foot + 1f) * sk, -v.len * .55f * sk), Vector3.one * 3f * sk); NoShadow(fl); fl.AddComponent<Billboard>(); fl.name = "Boost"; s.flame = fl.transform; s.flameM = fm; /* 13.11.: Triebwerksflamme hinten, beim Start groß (Boost) */
            if (v.cargo < 0 && s.ladung) s.ladung.gameObject.SetActive(false);
            s.from = RwWorld(RwAt(0, out _)) + padOut * pd.R * 1.1f + padUp * pd.R * .45f; ship = s; Sfx.Play("rocket", .7f, .45f); PoseShip(0);
        }

        void UpdateShip()
        {
            if (ship == null) return;
            if (!ship.go) { ship = null; return; }
            ship.t += Time.deltaTime; if (ship.t >= ShipT(ship.v)) { Destroy(ship.go); ship = null; return; }
            if (!ship.landed && ship.t >= T_LAND) { ship.landed = true; ship.v.onLand?.Invoke(); }
            if (ship.t >= T_LAND + ship.v.stay && ship.t - Time.deltaTime < T_LAND + ship.v.stay) Sfx.Play("rocket", 1.1f, .7f); /* Boost */
            PoseShip(ship.t);
        }

        static float S01(float a, float b, float t) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, t));
        static Vector3 Bez(Vector3 a, Vector3 b, Vector3 c, float k) => Vector3.Lerp(Vector3.Lerp(a, b, k), Vector3.Lerp(b, c, k), k);
        /// <summary>13.11.: Ablauf (s): 0–7 Anflug und Aufsetzen am offenen Ende des linken Schenkels, 7–11 Ausrollen bis zum Scheitel,
        /// dann stay s Be-/Entladen, danach 4,5 s Rollen mit Boost auf dem rechten Schenkel und 8,5 s Steigflug von der Basis weg.</summary>
        void PoseShip(float t)
        {
            var s = ship; var v = s.v; var tr = s.go.transform; float t1 = T_LAND + v.stay, tRoll = t1 + 4.5f, tEnd = t1 + T_DEP;
            Vector3 pos, fwd, up = padUp; float boost = 0, gearOut = 1;
            float sTd = .5f; var tdP = RwAt(sTd, out var tdD); Vector3 td = RwWorld(tdP), tdF = RwDir(tdD);
            if (t < T_TD)
            {
                float k = S01(0, T_TD, t); var ctl = td - tdF * pd.R * .35f + padUp * 3f; /* letzter Anflug genau in Bahnrichtung */
                pos = Bez(s.from, ctl, td, k); var ahead = Bez(s.from, ctl, td, Mathf.Min(1, k + .02f)) - pos; fwd = ahead.sqrMagnitude > 1e-6f ? ahead.normalized : tdF;
                if (k > .97f) fwd = tdF; gearOut = S01(3, 5.5f, t); boost = .35f * (1 - S01(4, 6.5f, t));
            }
            else if (t < T_LAND)
            {
                float k = Mathf.InverseLerp(T_TD, T_LAND, t), e = 1 - (1 - k) * (1 - k); /* ausrollen */
                var p = RwAt(Mathf.Lerp(sTd, RwApex, e), out var d); pos = RwWorld(p); fwd = RwDir(d);
            }
            else if (t < t1) { var p = RwAt(RwApex, out var d); pos = RwWorld(p); fwd = RwDir(d); }
            else if (t < tRoll)
            {
                float k = Mathf.InverseLerp(t1, tRoll, t), e = k * k; float sNow = Mathf.Lerp(RwApex, RwLen, e);
                var p = RwAt(sNow, out var d); float lift = Mathf.Max(0, sNow - (RwLen - 2.2f)) * .45f; /* hebt am Ende des Startschenkels ab */
                pos = RwWorld(p, lift * PadK); fwd = RwDir(d); if (lift > 0) fwd = (fwd + padUp * .25f).normalized;
                boost = S01(.15f, .45f, k); gearOut = 1 - S01(.85f, 1, k);
            }
            else
            {
                var endP = RwAt(RwLen, out var endD); Vector3 e0 = RwWorld(endP, 1f * PadK), eF = RwDir(endD);
                float tau = t - tRoll, k = Mathf.Clamp01(tau / (T_DEP - 4.5f)), v0 = RwLen / 4.5f * 2 * PadK; /* Geschwindigkeit am Ende des Rollens */
                pos = e0 + eF * (v0 * tau + pd.R * 1.1f * k * k) + padUp * (1.2f * tau + pd.R * .55f * k * k * k);
                var vel = eF * (v0 + pd.R * 2.2f * k / (T_DEP - 4.5f)) + padUp * (1.2f + pd.R * 1.65f * k * k / (T_DEP - 4.5f)); fwd = vel.normalized; boost = 1; gearOut = 0;
            }
            up = pos.normalized; var f2 = fwd - up * Vector3.Dot(fwd, up); if (f2.sqrMagnitude < 1e-6f) f2 = padOut;
            var rot = Quaternion.LookRotation(f2.normalized, up); float pitch = Vector3.SignedAngle(f2.normalized, fwd, Vector3.Cross(up, f2.normalized)); /* Nase hebt sich beim Steigen */
            tr.position = pos; tr.rotation = rot * Quaternion.Euler(Mathf.Clamp(pitch, -35, 35), 0, 0);
            if (s.duesen) s.duesen.localRotation = Quaternion.identity; /* Triebwerke zeigen nach hinten – Landung wie ein Flugzeug */
            if (s.fahrwerk) s.fahrwerk.localScale = new Vector3(1, Mathf.Lerp(.12f, 1, gearOut), 1);
            float tl = T_LAND; /* Rampe und Kisten wie vorher, nur ab dem Halt am Scheitel */
            if (v.ramp && s.rampe) s.rampe.localRotation = Quaternion.Euler(112f * S01(tl, tl + 1.5f, t) * (1 - S01(t1 - 1.5f, t1, t)), 0, 0);
            if (s.ladung && v.cargo != 0)
            {
                float a, b, vis;
                if (v.cargo > 0) { a = S01(tl + 1.5f, tl + 2.5f, t); b = S01(tl + 2.5f, tl + 4f, t); vis = 1 - S01(t1 - 2.5f, t1 - 1.6f, t); }
                else { float r0 = S01(tl + 2.5f, tl + 4f, t), r1 = S01(tl + 4f, tl + 5f, t); b = 1 - r0; a = 1 - r1; vis = t < tl + 1.5f ? 0 : S01(tl + 1.5f, tl + 2.2f, t); }
                s.ladung.localPosition = new Vector3(0, 1f - 1.05f * b, 3.4f + .9f * a + 1.7f * b);
                s.ladung.localScale = Vector3.one * Mathf.Max(.001f, vis); s.ladung.gameObject.SetActive(vis > .001f);
            }
            if (s.flame)
            {
                float fl = boost * (.85f + .15f * Mathf.Sin(Time.time * 40f)); s.flame.gameObject.SetActive(fl > .02f);
                float sk = v.scale * PadK; s.flame.localScale = Vector3.one * (1.2f + 3.4f * fl) * sk * 1.6f; s.flame.localPosition = new Vector3(0, (v.foot + 1f) * sk, -(v.len * .55f + 1.2f * fl) * sk);
                if (boost > .5f && t < tRoll && Time.frameCount % 3 == 0) EmitDust(pos.normalized, new Color(.8f, .75f, .7f), 6); /* Staub auf der Bahn beim Start */
            }
            if (t > tEnd - 1f) { float f = 1 - S01(tEnd - 1f, tEnd, t); tr.localScale = Vector3.one * Mathf.Max(.01f, f); }
        }
    }
}
