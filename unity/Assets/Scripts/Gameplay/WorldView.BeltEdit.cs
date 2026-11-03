// Build 34 (Fabian 02.11.): Band-Editor. Band antippen → Punkte ziehen, Abschnitte anheben, Zurücksetzen.
// Anfang und Ende bleiben an den Gebäudeanschlüssen. Die Kurven werden mit großem Radius gerundet; zu enge Kurven,
// Gebäude oder andere Bänder auf gleicher Höhe machen die Vorschau rot – dann lässt sie sich nicht übernehmen.
// Eigene Wege sind nur Optik: die Simulation rechnet weiter mit der Bandlänge aus dem Layout (siehe DECISIONS).
using System.Collections.Generic;
using System.Linq;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        // ----- eigene Wege in die Bandverlegung einsetzen (nach RouteBelts, vor PlanBridges) -----
        readonly Dictionary<string, Vector2[]> autoRouted = new Dictionary<string, Vector2[]>(); string autoKey;
        const float CUSTOM_R = 1.5f, MIN_R = .9f, MAX_H = 4.5f; /* 03.11.: kleinerer Rundungsradius – der Weg folgt den Punkten genauer */
        void ApplyCustomRoutes()
        {
            if (autoKey != routedKey) { autoRouted.Clear(); foreach (var kv in routed) autoRouted[kv.Key] = kv.Value; autoKey = routedKey; }
            routed.Clear(); foreach (var kv in autoRouted) routed[kv.Key] = kv.Value;
            if (ps.sv.bRoutes == null) return;
            foreach (var cr in ps.sv.bRoutes) { var bd = ps.lay.belts.FirstOrDefault(x => x.id == cr.id); if (bd != null && autoRouted.TryGetValue(cr.id, out var a) && a.Length >= 3 && cr.p.Count > 0) routed[cr.id] = CustomShape(bd, a, cr.p, cr.a0, cr.a1, cr.d0, cr.d1); }
        }
        /// <summary>Anschlüsse aus dem automatischen Weg (erste/letzte 1,6 m im Gebäude), dazwischen die eigenen Punkte.</summary>
        /// <summary>a0/a1: eigener Aus-/Eingang als Winkel um das Gebäude (Fabian 02.11.: Ausgang ziehen), −999 = Anschluss des automatischen Wegs.</summary>
        /// <summary>Fabian 03.11.: d0/d1 = freier Endpunkt in diesem Abstand vor der Wand (statt fester 3,4-m-Stummel) – der Weg geht von der Wand gerade zu deinem Griff und dann zu deinen Punkten.</summary>
        Vector2[] CustomShape(BeltDef b, Vector2[] auto, List<Vector2> mid, float a0, float a1, float d0 = -1, float d1 = -1)
        {
            Vector2 p0 = auto[0], q0 = auto[auto.Length - 1]; var da = (auto[1] - p0).normalized; var db = (auto[auto.Length - 2] - q0).normalized; float s0 = 3.4f, s1 = 3.4f;
            if (a0 > -900 && ps.lay.pos.ContainsKey(b.from)) { var d = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)); float fp = GameData.Bld[b.from].fp; p0 = ps.lay.pos[b.from] + d * (fp * .95f - 1.6f); da = d; if (d0 > 0) s0 = 1.6f + d0; }
            if (a1 > -900 && ps.lay.pos.ContainsKey(b.to)) { var d = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)); float fp = GameData.Bld[b.to].fp; q0 = ps.lay.pos[b.to] + d * (fp * .95f - 1.6f); db = d; if (d1 > 0) s1 = 1.6f + d1; }
            var pts = new List<Vector2> { p0, p0 + da * 1.6f, p0 + da * s0 }; pts.AddRange(mid); pts.Add(q0 + db * s1); pts.Add(q0 + db * 1.6f); pts.Add(q0);
            return Round(Dedupe(pts), CUSTOM_R);
        }
        float eD0 = -1, eD1 = -1;
        /// <summary>Höhen der eigenen Punkte als Brücken-Abschnitte (Stützen kommen von selbst).</summary>
        void AddCustomLifts()
        {
            if (ps.sv.bRoutes == null) return;
            foreach (var cr in ps.sv.bRoutes)
            {
                if (!routed.ContainsKey(cr.id) || cr.h.All(h => h <= .01f)) continue; var b = ps.lay.belts.FirstOrDefault(x => x.id == cr.id); if (b == null) continue;
                var V = Vis(b); var L = AddLifts(V.rp, V.cum, cr.p, cr.h); if (L.Count == 0) continue;
                if (!bridges.TryGetValue(cr.id, out var lst)) { lst = new List<Vector3>(); bridges[cr.id] = lst; } lst.AddRange(L);
            }
        }
        static List<Vector3> AddLifts(Vector2[] rp, float[] cum, List<Vector2> pts, List<float> hs)
        {
            var res = new List<Vector3>(); float Lt = cum[cum.Length - 1], lo = EndGuard, hi = Lt - EndGuard; if (hi <= lo) return res;
            var sAt = pts.Select(p => SAt(rp, cum, p)).ToList();
            for (int i = 0; i < pts.Count; i++)
            {
                float h = i < hs.Count ? hs[i] : 0; if (h <= .01f) continue;
                res.Add(new Vector3(Mathf.Clamp(sAt[i] - 1.5f, lo, hi), Mathf.Clamp(sAt[i] + 1.5f, lo, hi), h));
                if (i + 1 < pts.Count && i + 1 < hs.Count && hs[i + 1] > .01f) res.Add(new Vector3(Mathf.Clamp(Mathf.Min(sAt[i], sAt[i + 1]), lo, hi), Mathf.Clamp(Mathf.Max(sAt[i], sAt[i + 1]), lo, hi), Mathf.Min(h, hs[i + 1])));
            }
            return res;
        }
        static float SAt(Vector2[] rp, float[] cum, Vector2 p)
        {
            float best = 1e9f, bs = 0;
            for (int i = 1; i < rp.Length; i++) { var a = rp[i - 1]; var d = rp[i] - a; float l2 = d.sqrMagnitude; float t = l2 > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, d) / l2) : 0; float dd = (a + d * t - p).sqrMagnitude; if (dd < best) { best = dd; bs = cum[i - 1] + (cum[i] - cum[i - 1]) * t; } }
            return bs;
        }
        static float SegDist(Vector2[] rp, Vector2 p) { float best = 1e9f; for (int i = 1; i < rp.Length; i++) { var a = rp[i - 1]; var d = rp[i] - a; float l2 = d.sqrMagnitude; float t = l2 > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, d) / l2) : 0; best = Mathf.Min(best, (a + d * t - p).magnitude); } return best; }

        // ----- Bearbeiten -----
        public string EditBelt { get; private set; }
        public bool Editing => EditBelt != null;
        int selPt = -1; public int SelPt { get => selPt; set { if (selPt != value) { selPt = value; eDirty = true; } } }
        readonly List<Vector2> ePts = new List<Vector2>(); readonly List<float> eH = new List<float>(); float eA0 = -999, eA1 = -999; readonly Transform[] eEnds = new Transform[2]; Material eEndMat;
        public const int END0 = 1000, END1 = 1001; /* Griff-Nummern für Anfang und Ende */
        Vector2[] eShape; float[] eCum; string eWhy = ""; bool eDirty; int eAuto;
        Transform eRoot; readonly List<Transform> eHandles = new List<Transform>(); readonly List<Transform> eKnobs = new List<Transform>(); GameObject ePrev; Mesh ePrevMesh; Material eOk, eBad, eHandle, eHandleSel, eKnob;
        /// <summary>Band unter diesem Planpunkt (für das Antippen).</summary>
        public bool BeltAt(Vector2 pl, out string id)
        {
            id = null; float best = 1.4f;
            foreach (var b in ps.lay.belts) { if (!Game.BeltLive(ps, b) || Game.HiddenBelt(b) || !routed.TryGetValue(b.id, out var r)) continue; float d = SegDist(r, pl); if (d < best) { best = d; id = b.id; } }
            return id != null;
        }
        public string BeltName(string id) { var b = ps.lay.belts.FirstOrDefault(x => x.id == id); if (b == null) return "Band"; string N(string bid) => GameData.Bld.ContainsKey(bid) ? (bid == "presse" || bid == "schmelze" || bid == "raff" ? GameData.MachName(ps.pid, bid) : GameData.Bld[bid].name) : bid; return N(b.from) + " → " + N(b.to); }
        public void BeginEdit(string id)
        {
            EndEdit(); if (!autoRouted.TryGetValue(id, out var auto)) return; EditBelt = id; SelPt = -1; eAuto = 0; ePts.Clear(); eH.Clear();
            var cr = ps.sv.bRoutes.FirstOrDefault(x => x.id == id);
            eA0 = eA1 = -999; eD0 = eD1 = -1; if (cr != null && cr.p.Count > 0) { ePts.AddRange(cr.p); eH.AddRange(cr.h); while (eH.Count < ePts.Count) eH.Add(0); eA0 = cr.a0; eA1 = cr.a1; eD0 = cr.d0; eD1 = cr.d1; }
            else
            {
                // Startpunkte: Ecken des automatischen Wegs (vereinfacht), ohne die Anschlussstücke
                var cur = routed.TryGetValue(id, out var r0) ? r0 : auto; var mid = new List<Vector2>(); float acc = 0;
                for (int i = 2; i < cur.Length - 2; i++) { acc += (cur[i] - cur[i - 1]).magnitude; var d1 = (cur[i] - cur[i - 1]).normalized; var d2 = (cur[i + 1] - cur[i]).normalized; if (Vector2.Dot(d1, d2) < .97f && acc > 2.5f) { mid.Add(cur[i]); acc = 0; } }
                if (mid.Count == 0 || mid.Count > 8) { mid.Clear(); int n = Mathf.Clamp(Mathf.RoundToInt(Cum(cur) / 8f), 1, 5); for (int k = 1; k <= n; k++) mid.Add(PointAtArr(cur, Cum(cur) * k / (n + 1))); }
                ePts.AddRange(mid); foreach (var _ in mid) eH.Add(0); eAuto = 5;
            }
            eOk = MeshKit.Mat(new Color(.3f, 1f, .6f), 0, .4f, new Color(.2f, .9f, .5f) * 1.2f); eBad = MeshKit.Mat(new Color(1f, .3f, .25f), 0, .4f, new Color(1f, .2f, .15f) * 1.4f);
            eHandle = MeshKit.Mat(new Color(1f, 1f, 1f), 0, .5f, new Color(.6f, .8f, 1f)); eHandleSel = MeshKit.Mat(new Color(1f, .85f, .3f), 0, .5f, new Color(1f, .75f, .2f) * 1.5f); eKnob = MeshKit.Mat(new Color(.5f, .8f, 1f), 0, .5f, new Color(.4f, .7f, 1f) * 1.4f);
            eRoot = New("Band-Editor"); eDirty = true; var go = beltRoot.Find(id); if (go) go.gameObject.SetActive(false);
            Sfx.Play("click", 1.2f, .5f);
        }
        static float Cum(Vector2[] r) { float s = 0; for (int i = 1; i < r.Length; i++) s += (r[i] - r[i - 1]).magnitude; return s; }
        static Vector2 PointAtArr(Vector2[] r, float s) { for (int i = 1; i < r.Length; i++) { float l = (r[i] - r[i - 1]).magnitude; if (s <= l) return Vector2.Lerp(r[i - 1], r[i], l > 0 ? s / l : 0); s -= l; } return r[r.Length - 1]; }
        public void EndEdit()
        {
            if (eRoot) Destroy(eRoot.gameObject); eRoot = null; eHandles.Clear(); eKnobs.Clear(); ePrev = null;
            if (EditBelt != null) { var go = beltRoot ? beltRoot.Find(EditBelt) : null; if (go) go.gameObject.SetActive(true); }
            EditBelt = null; SelPt = -1;
        }
        public string EditWhy => eWhy;
        public bool EditValid => Editing && eWhy == "";
        public int EditCount => ePts.Count; public IReadOnlyList<Vector2> EditPts => ePts;
        public float EditHeight => SelPt >= 0 && SelPt < eH.Count ? eH[SelPt] : -1;
        /// <summary>Übernehmen: eigener Weg wird gespeichert, die Basis neu aufgebaut.</summary>
        public bool CommitEdit()
        {
            if (!EditValid) return false; var id = EditBelt; ps.sv.bRoutes.RemoveAll(x => x.id == id);
            ps.sv.bRoutes.Add(new BeltRouteSave { id = id, p = new List<Vector2>(ePts), h = new List<float>(eH), a0 = eA0, a1 = eA1, d0 = eD0, d1 = eD1 });
            EndEdit(); ps.baseVersion++; Game.Save(); Game.Toast("Band verlegt", "good"); return true;
        }
        public void ResetEdit() { var id = EditBelt; if (id == null) return; ps.sv.bRoutes.RemoveAll(x => x.id == id); EndEdit(); ps.baseVersion++; Game.Save(); Game.Toast("Band wieder automatisch verlegt"); }
        public void AddPoint()
        {
            if (!Editing || ePts.Count >= 12) return; int i = SelPt >= 0 && SelPt < ePts.Count ? SelPt : ePts.Count / 2; // neuer Punkt hinter dem gewählten (Mitte bis zum nächsten)
            var a = i < ePts.Count ? ePts[i] : ePts.Last(); var b = i + 1 < ePts.Count ? ePts[i + 1] : (eShape != null ? eShape[eShape.Length - 3] : a + Vector2.one * 3);
            ePts.Insert(Mathf.Min(i + 1, ePts.Count), (a + b) / 2); eH.Insert(Mathf.Min(i + 1, eH.Count), 0); SelPt = Mathf.Min(i + 1, ePts.Count - 1); eDirty = true;
        }
        public void DeletePoint() { if (!Editing || SelPt < 0 || SelPt >= ePts.Count || ePts.Count <= 1) return; ePts.RemoveAt(SelPt); eH.RemoveAt(SelPt); SelPt = -1; eDirty = true; }
        public void MovePoint(int i, Vector2 pl)
        {
            if (i == END0 || i == END1) /* Anfang/Ende um das Gebäude ziehen */
            {
                var b0 = ps.lay.belts.FirstOrDefault(x => x.id == EditBelt); if (b0 == null) return; string bid = i == END0 ? b0.from : b0.to; if (!ps.lay.pos.ContainsKey(bid)) return;
                var d = pl - ps.lay.pos[bid]; if (d.sqrMagnitude < .01f) return; float a = Mathf.Atan2(d.y, d.x), dist = Mathf.Clamp(d.magnitude - GameData.Bld[bid].fp * .95f, .6f, 14f); /* 03.11.: Griff frei – Richtung UND Abstand */ if (i == END0) { eA0 = a; eD0 = dist; } else { eA1 = a; eD1 = dist; } eDirty = true; return;
            }
            if (i < 0 || i >= ePts.Count) return; pl.x = Mathf.Clamp(pl.x, -GOFF + 2, GOFF - 2); pl.y = Mathf.Clamp(pl.y, -GOFF + 2, GOFF - 2); ePts[i] = pl; eDirty = true; }
        /// <summary>Höher/Tiefer-Knöpfe: gewählter Punkt, ohne Auswahl das ganze Band.</summary>
        public void NudgeHeight(float dh) { if (!Editing) return; if (SelPt >= 0 && SelPt < eH.Count) SetHeight(SelPt, eH[SelPt] + dh); else for (int i = 0; i < eH.Count; i++) SetHeight(i, eH[i] + dh); }
        public void SetHeight(int i, float h) { if (i < 0 || i >= eH.Count) return; eH[i] = Mathf.Clamp(h, 0, MAX_H); eDirty = true; }
        /// <summary>Griff unter dem Finger: Punkt (knob = false) oder Höhenknopf über dem gewählten Punkt (knob = true).</summary>
        public int HandleAt(Camera cam, Vector2 pos, out bool knob)
        {
            knob = false; int best = -1; float bd = 60;
            for (int i = 0; i < eKnobs.Count; i++) { if (!eKnobs[i] || !eKnobs[i].gameObject.activeSelf) continue; var sp = cam.WorldToScreenPoint(eKnobs[i].position); if (sp.z <= 0) continue; float d = ((Vector2)sp - pos).magnitude; if (d < bd) { bd = d; best = i; knob = true; } }
            for (int i = 0; i < eHandles.Count; i++) { if (!eHandles[i]) continue; var sp = cam.WorldToScreenPoint(eHandles[i].position); if (sp.z <= 0) continue; float d = ((Vector2)sp - pos).magnitude; if (d < bd) { bd = d; best = i; knob = false; } }
            for (int i = 0; i < 2; i++) { if (!eEnds[i]) continue; var sp = cam.WorldToScreenPoint(eEnds[i].position); if (sp.z <= 0) continue; float d = ((Vector2)sp - pos).magnitude; if (d < bd) { bd = d; best = END0 + i; knob = false; } }
            return best;
        }
        /// <summary>Tipp auf die Vorschau: neuen Punkt an dieser Stelle einfügen.</summary>
        public bool TapEdit(Vector2 pl)
        {
            if (!Editing || eShape == null) return false; if (SegDist(eShape, pl) > 1.6f) { SelPt = -1; return false; }
            float s = SAt(eShape, eCum, pl); int at = 0; for (int i = 0; i < ePts.Count; i++) if (SAt(eShape, eCum, ePts[i]) < s) at = i + 1;
            if (ePts.Count >= 12) return true; ePts.Insert(at, pl); eH.Insert(at, at > 0 && at - 1 < eH.Count ? eH[at - 1] : 0); SelPt = at; eDirty = true; return true;
        }
        // ---------- Fabian 02.11.: Weg frei mit dem Finger nachzeichnen ----------
        public bool DrawMode; readonly List<Vector2> dTrace = new List<Vector2>();
        Vector2 ClampPl(Vector2 pl) { pl.x = Mathf.Clamp(pl.x, -GOFF + 2, GOFF - 2); pl.y = Mathf.Clamp(pl.y, -GOFF + 2, GOFF - 2); return pl; }
        public void DrawBegin(Vector2 pl) { if (!Editing) return; dTrace.Clear(); dTrace.Add(ClampPl(pl)); SelPt = -1; }
        public void DrawAdd(Vector2 pl) { if (!Editing || dTrace.Count == 0) return; pl = ClampPl(pl); if ((pl - dTrace[dTrace.Count - 1]).magnitude < .7f) return; dTrace.Add(pl); ApplyTrace(false); }
        public void DrawEnd() { if (!Editing) return; ApplyTrace(true); DrawMode = false; dTrace.Clear(); }
        /// <summary>Strich → Wegpunkte: Stücke an den beiden Gebäuden weglassen, glätten (höchstens 12 Punkte, mindestens 2,4 m Abstand), Ein- und Ausgang zeigen in Strichrichtung.</summary>
        void ApplyTrace(bool final)
        {
            var b = ps.lay.belts.FirstOrDefault(x => x.id == EditBelt); if (b == null || !ps.lay.pos.ContainsKey(b.from) || !ps.lay.pos.ContainsKey(b.to)) return;
            Vector2 c0 = ps.lay.pos[b.from], c1 = ps.lay.pos[b.to]; float r0 = GameData.Bld[b.from].fp + 1.8f, r1 = GameData.Bld[b.to].fp + 1.8f;
            var pts = dTrace.Where(p => (p - c0).magnitude > r0 && (p - c1).magnitude > r1).ToList(); if (pts.Count == 0) return;
            if ((pts[0] - c0).magnitude + (pts[pts.Count - 1] - c1).magnitude > (pts[0] - c1).magnitude + (pts[pts.Count - 1] - c0).magnitude) pts.Reverse(); /* egal, in welche Richtung gezeichnet wurde */
            List<Vector2> simp = null; for (float eps = .6f; eps < 8; eps *= 1.5f) { simp = Rdp(pts, eps); var sp = new List<Vector2>(); foreach (var p in simp) if (sp.Count == 0 || (p - sp[sp.Count - 1]).magnitude >= 2.4f) sp.Add(p); simp = sp; if (simp.Count <= 12) break; }
            ePts.Clear(); ePts.AddRange(simp); eH.Clear(); foreach (var _ in simp) eH.Add(0);
            var f = simp[0] - c0; var l = simp[simp.Count - 1] - c1; if (f.sqrMagnitude > .01f) { eA0 = Mathf.Atan2(f.y, f.x); eD0 = .6f; } if (l.sqrMagnitude > .01f) { eA1 = Mathf.Atan2(l.y, l.x); eD1 = .6f; } /* gezeichnet: Weg beginnt direkt an der Wand */
            eAuto = final ? 5 : 0; eDirty = true;
        }
        static List<Vector2> Rdp(List<Vector2> p, float eps)
        {
            if (p.Count < 3) return new List<Vector2>(p); int idx = -1; float dm = 0;
            for (int i = 1; i < p.Count - 1; i++) { float d = SegDist(p[i], p[0], p[p.Count - 1]); if (d > dm) { dm = d; idx = i; } }
            if (dm <= eps) return new List<Vector2> { p[0], p[p.Count - 1] };
            var a = Rdp(p.GetRange(0, idx + 1), eps); var c = Rdp(p.GetRange(idx, p.Count - idx), eps); a.RemoveAt(a.Count - 1); a.AddRange(c); return a;
        }
        float PlanH(Vector2 p) => pd.Height(pd.PlanDir(p));
        void UpdateEdit()
        {
            if (!Editing) return; if (!eRoot) { EditBelt = null; return; }
            var b = ps.lay.belts.FirstOrDefault(x => x.id == EditBelt); if (b == null || !autoRouted.TryGetValue(EditBelt, out var auto)) { EndEdit(); return; }
            if (eDirty)
            {
                eDirty = false; eShape = CustomShape(b, auto, ePts, eA0, eA1, eD0, eD1); eCum = new float[eShape.Length]; for (int i = 1; i < eShape.Length; i++) eCum[i] = eCum[i - 1] + (eShape[i] - eShape[i - 1]).magnitude;
                var lifts = AddLifts(eShape, eCum, ePts, eH); float Lf(float s) { float best = 0; foreach (var w in lifts) { float k = s < w.x ? Mathf.SmoothStep(0, 1, 1 - (w.x - s) / 3f) : s > w.y ? Mathf.SmoothStep(0, 1, 1 - (s - w.y) / 3f) : 1; best = Mathf.Max(best, w.z * Mathf.Clamp01(k)); } return best; }
                eWhy = Validate(b, Lf);
                /* Fabian 02.11.: der vorgeschlagene Weg soll gleich gültig sein – kreuzt er ein Band, die Punkte selbst etwas anheben (wie die Brücken des automatischen Wegs) */
                if (eAuto > 0 && eWhy.Contains("gleicher Höhe")) { eAuto--; for (int i = 0; i < eH.Count; i++) eH[i] = Mathf.Clamp(eH[i] + .5f, 0, MAX_H); eDirty = true; return; }
                eAuto = 0;
                // Vorschau: flaches Band über dem Boden, grün oder rot
                if (!ePrev) { ePrev = new GameObject("Vorschau"); ePrev.transform.SetParent(eRoot, false); ePrev.AddComponent<MeshFilter>(); ePrev.AddComponent<MeshRenderer>(); NoShadow(ePrev); ePrevMesh = new Mesh { name = "Bandvorschau" }; ePrev.GetComponent<MeshFilter>().sharedMesh = ePrevMesh; }
                var v = new List<Vector3>(); var t = new List<int>(); float Lt = eCum[eCum.Length - 1];
                for (float s = 0; s <= Lt + .01f; s += .4f)
                {
                    var p = PointAt(eShape, eCum, Mathf.Min(s, Lt), out var tan); var nrm = new Vector2(-tan.y, tan.x); float h = BeltDeck(b) + Lf(s) + .12f;
                    foreach (float sd in new[] { -1f, 1f }) { var q = p + nrm * BW * sd; var d = pd.PlanDir(q); v.Add(d * (pd.R + Mathf.Max(PlanH(q), 0) + h)); }
                    int n = v.Count; if (n >= 4) { t.AddRange(new[] { n - 4, n - 2, n - 3, n - 3, n - 2, n - 1, n - 4, n - 3, n - 2, n - 3, n - 1, n - 2 }); }
                }
                ePrevMesh.Clear(); ePrevMesh.SetVertices(v); ePrevMesh.SetTriangles(t, 0); ePrevMesh.RecalculateNormals(); ePrevMesh.RecalculateBounds();
                ePrev.GetComponent<MeshRenderer>().sharedMaterial = eWhy == "" ? eOk : eBad;
                // Griffe: Kugel am Punkt (in Bandhöhe), Höhenknopf darüber für den gewählten Punkt
                while (eHandles.Count < ePts.Count) { var g = MeshKit.Ball(eRoot, eHandle, Vector3.zero, 1.1f); NoShadow(g); eHandles.Add(g.transform); var k = MeshKit.Ball(eRoot, eKnob, Vector3.zero, .8f); NoShadow(k); var stem = MeshKit.Cyl(k.transform, eKnob, new Vector3(0, -1.6f, 0), .06f, .06f, 3.2f, 6); NoShadow(stem); eKnobs.Add(k.transform); }
                while (eHandles.Count > ePts.Count) { Destroy(eHandles[eHandles.Count - 1].gameObject); eHandles.RemoveAt(eHandles.Count - 1); Destroy(eKnobs[eKnobs.Count - 1].gameObject); eKnobs.RemoveAt(eKnobs.Count - 1); }
                /* orange Griffe an Anfang und Ende: um das Gebäude ziehen = Ausgang/Eingang verlegen */
                if (!eEndMat) eEndMat = MeshKit.Mat(new Color(1f, .6f, .2f), 0, .5f, new Color(1f, .5f, .1f) * 1.4f);
                for (int e = 0; e < 2; e++) { if (!eEnds[e]) { var g = MeshKit.Ball(eRoot, eEndMat, Vector3.zero, 1.3f); NoShadow(g); eEnds[e] = g.transform; } var ep = e == 0 ? eShape[Mathf.Min(2, eShape.Length - 1)] : eShape[Mathf.Max(0, eShape.Length - 3)]; var dd = pd.PlanDir(ep); eEnds[e].position = dd * (pd.R + Mathf.Max(PlanH(ep), 0) + BeltDeck(b) + .7f); eEnds[e].localScale = Vector3.one * (SelPt == END0 + e ? 1.6f : 1.3f); }
                for (int i = 0; i < ePts.Count; i++)
                {
                    var d = pd.PlanDir(ePts[i]); float top = pd.R + Mathf.Max(PlanH(ePts[i]), 0) + BeltDeck(b) + (i < eH.Count ? eH[i] : 0) + .5f;
                    eHandles[i].position = d * top; eHandles[i].GetComponent<MeshRenderer>().sharedMaterial = i == SelPt ? eHandleSel : eHandle;
                    eKnobs[i].gameObject.SetActive(i == SelPt); eKnobs[i].position = d * (top + 3.2f); eKnobs[i].rotation = Quaternion.FromToRotation(Vector3.up, d);
                }
            }
        }
        /// <summary>Leer = in Ordnung, sonst der Grund (Anzeige im Editor).</summary>
        string Validate(BeltDef b, System.Func<float, float> Lf)
        {
            float Lt = eCum[eCum.Length - 1]; if (MinRadius(eShape) < MIN_R) return "Kurve zu eng";
            if (autoRouted.TryGetValue(b.id, out var a0) && Lt > Cum(a0) * 3 + 20) return "Band zu lang";
            for (float s = 3.6f; s < Lt - 3.6f; s += .5f)
            {
                var p = PointAt(eShape, eCum, s, out _); float lh = Lf(s);
                foreach (var id in ps.bld) { if (!ps.lay.pos.ContainsKey(id) || (Game.IsUnit(id) && id != "u1")) continue; float r = GameData.Bld[id].fp * .72f + .7f; if ((ps.lay.pos[id] - p).magnitude < r) return "Gebäude im Weg: " + GameData.Bld[id].name; }
                foreach (var o in ps.lay.belts)
                {
                    if (o.id == b.id || !Game.BeltLive(ps, o) || Game.HiddenBelt(o) || !routed.TryGetValue(o.id, out var orp)) continue;
                    if (SegDist(orp, p) > 1.15f) continue; var V = Vis(o); float os = SAt(V.rp, V.cum, p);
                    if (os < 4 || os > V.cum[V.cum.Length - 1] - 4) continue; /* Bandenden an gemeinsamen Gebäuden */
                    if (Mathf.Abs(Lift(o.id, os) - lh) < 1.3f) return "Kreuzt ein anderes Band auf gleicher Höhe – Punkt anheben";
                }
            }
            return "";
        }
    }
}
