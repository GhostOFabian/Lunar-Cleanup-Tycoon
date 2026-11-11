// Fabian 09.11.: Zeichen als Puzzle. Die Teile liegen verstreut auf dem Planeten (je Teil eine Parzelle, nur sauber sichtbar).
// Das letzte Teil liegt erst bei etwa 80 % Reinigung (nach Ringen sortiert) – wer woanders putzt, findet es später.
// Sind alle Teile frei, fliegen sie zusammen und das Zeichen erscheint riesig auf der Rückseite des Planeten (gegenüber der Basis).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public partial class PlanetData
    {
        List<int> pieceCells; Feature bigGlyph;
        /// <summary>Parzellen mit Puzzleteilen, in Fundreihenfolge (nach Ring sortiert).</summary>
        public List<int> GlyphCells { get { if (hk == null) InitTerrain(); return pieceCells ?? (pieceCells = new List<int>()); } }
        public bool GlyphAt(int cell) => GlyphCells.Contains(cell);
        public Feature BigGlyph { get { if (hk == null) InitTerrain(); return bigGlyph; } }
        public int PieceOf(int cell) => GlyphCells.IndexOf(cell);
        void PlaceGlyph()
        {
            if (!Game.Glyphs.TryGetValue(pid, out var gs)) return; pieceCells = new List<int>();
            var bc = cells[baseCell]; var back = -bc.c.normalized;
            // großes Zeichen auf der Rückseite
            var ac = cells.OrderByDescending(c => Vector3.Dot(c.c.normalized, back)).First();
            bigGlyph = new Feature { kind = 6, d = back, e1 = ac.e1, e2 = ac.e2, rad = R * .8f, col = Hx(gs.col), col2 = Hx(gs.col2), glow = Hx(gs.glow), crat = gs.style, crD = gs.n, seed = 7, piece = -1 };
            AddFeature(bigGlyph);
            // Teile: nach Ring sortiert verteilt, das letzte bei ~80 %
            var pool = cells.Where(c => !c.IsBase && !c.Wet).OrderBy(c => c.ring).ThenBy(c => c.id).ToList(); if (pool.Count < 10) return;
            int N = Mathf.Clamp(pool.Count / 25, 6, 24); /* 10.11.: je größer der Planet, desto mehr Teile */ var rnd = new System.Random(pid.GetHashCode() ^ 0x5eed);
            for (int k = 0; k < N; k++)
            {
                float pct = .08f + (.80f - .08f) * k / (N - 1); int ring = pool[Mathf.Clamp(Mathf.RoundToInt(pct * (pool.Count - 1)), 0, pool.Count - 1)].ring;
                var cand = pool.Where(c => c.ring == ring && !pieceCells.Contains(c.id) && !pieceCells.Any(o => cells[o].nb.Contains(c.id))).ToList();
                if (cand.Count == 0) cand = pool.Where(c => !pieceCells.Contains(c.id) && Mathf.Abs(c.ring - ring) <= 1).ToList(); if (cand.Count == 0) continue;
                var cl = cand[rnd.Next(cand.Count)]; pieceCells.Add(cl.id);
                AddFeature(new Feature { kind = 8, d = cl.c.normalized, e1 = cl.e1, e2 = cl.e2, rad = cl.H * .95f, col = Hx(gs.col), col2 = Hx(gs.col2), glow = Hx(gs.glow), crat = gs.style, crD = gs.n, seed = k, piece = k, pieces = N });
            }
        }
        /// <summary>Zeichen und Puzzleteile malen. done = Puzzle fertig (großes Zeichen sichtbar). piece = Pixel gehört zu einem Teil (nur sauber sichtbar).</summary>
        /// <summary>10.11.: Linien wachsen nach dem Einfliegen von der Mitte aus (0 … 1,6), danach (2) leuchtet das Zeichen voll.</summary>
        public static float GlyphGrow = 2f, GlyphGrowT = -99f;
        public static bool GlyphOnSurface = false; /* 11.11.: true = fertiges Zeichen bleibt aufgemalt (alter Stand) */
        static float GrowNow => GlyphGrow; /* wird in Threads gelesen – Sicherung gegen Hängenbleiben in Game.SecretStep */
        public static readonly Color TipC = new Color(1f, .96f, .8f); static Color TipCol => TipC;
        /// <summary>Wie weit die Linie in Richtung ang schon gewachsen ist (je Richtung verschieden schnell).</summary>
        public static float GrowFront(float G, float ang, float seed) => G * (1.05f + .28f * Mathf.Sin(ang * 3 + seed) + .14f * Mathf.Sin(ang * 7 + seed * 2.3f));
        /// <summary>Farbe des fertigen Zeichens an Stelle (hx, hy) – für die Leuchtschicht der Aufbau-Animation.</summary>
        public static void GlyphPixel(Feature f, float hx, float hy, out Color c, out float k, out Color g)
        {
            GlyphLine(Mathf.RoundToInt(f.crat), Mathf.RoundToInt(f.crD), hx, hy, out float line, out float bright); Shade(f, line, bright, Mathf.Sqrt(hx * hx + hy * hy), .5f, out c, out k, out g);
        }
        public bool GlyphPaintCell(int cell, Vector3 dir, bool done, out Color col, out float a, out Color glow, out bool piece)
        {
            col = Color.black; a = 0; glow = Color.black; piece = false; if (hk == null) InitTerrain(); if (!cellFeats.TryGetValue(cell, out var l)) return false; bool any = false;
            foreach (var f in l)
            {
                if (f.kind != 6 && f.kind != 8) continue; if (f.kind == 6 && (!done || !GlyphOnSurface)) continue; /* Fabian 11.11.: fertiges Zeichen nicht mehr dauerhaft auf dem Boden – nur die Leuchtschicht zeigt es kurz */ if (f.kind == 8 && done) continue;
                float cd = Vector3.Dot(dir, f.d); if (cd < .3f) continue; float dist = Mathf.Acos(Mathf.Min(1, cd)) * R; if (dist / f.rad >= 1.1f) continue;
                float lx = Vector3.Dot(dir - f.d * cd, f.e1) * R, ly = Vector3.Dot(dir - f.d * cd, f.e2) * R, n1 = Noise2(lx * .7f + f.seed, ly * .7f);
                float hx = lx / f.rad, hy = ly / f.rad, k; Color c, g;
                if (f.kind == 6) { GlyphLine(Mathf.RoundToInt(f.crat), Mathf.RoundToInt(f.crD), hx, hy, out float line, out float bright); Shade(f, line, bright, Mathf.Sqrt(hx * hx + hy * hy), n1, out c, out k, out g);
                    float G = GrowNow; if (G < 1.9f) { float rr = Mathf.Sqrt(hx * hx + hy * hy), fr = GrowFront(G, Mathf.Atan2(hy, hx), f.seed); if (rr > fr) continue; float tip = Mathf.Clamp01(1 - (fr - rr) / .08f); k *= .55f + .45f * tip; c = Color.Lerp(c, TipCol, tip * .7f); g = g * .3f + TipCol * (tip * k * 1.6f); } /* 10.11.: Linien bauen sich auf */ }
                else
                {
                    // Teil k zeigt den Ausschnitt k des Zeichens (Tortenstück mit Puzzle-Rand)
                    float th = (f.piece + .5f) * 2 * Mathf.PI / f.pieces, half = Mathf.PI / f.pieces;
                    float qx = Mathf.Cos(th) * .55f + hx * .5f, qy = Mathf.Sin(th) * .55f + hy * .5f, qr = Mathf.Sqrt(qx * qx + qy * qy), qa = Mathf.Atan2(qy, qx);
                    float da = Mathf.Abs(Mathf.DeltaAngle(qa * Mathf.Rad2Deg, th * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                    float wob = .06f * Mathf.Sin(qr * 18f + f.piece) , edge = half + wob - da; float inside = Mathf.Clamp01(edge * 30f) * Mathf.Clamp01((1.02f - qr) * 25f) * Mathf.Clamp01((qr - .02f) * 25f);
                    if (inside <= 0 || hx * hx + hy * hy > 1) continue;
                    GlyphLine(Mathf.RoundToInt(f.crat), Mathf.RoundToInt(f.crD), qx, qy, out float line, out float bright);
                    float rim = Mathf.Clamp01(1 - Mathf.Abs(edge) * 40f) * .5f; /* Puzzle-Kante leuchtet leicht */
                    Shade(f, Mathf.Max(line, rim) * inside, bright * inside, qr, n1, out c, out k, out g); k = Mathf.Max(k, .28f * inside); /* Teil hebt sich als Fläche ab */
                    c = Color.Lerp(Color.Lerp(f.col2, Color.black, .55f), c, Mathf.Clamp01(line * 1.5f + rim)); piece = true;
                }
                if (k <= 0) continue; if (!any || k > a) { col = c; a = k; } glow += g; any = true;
            }
            return any;
        }
        static void Shade(Feature f, float line, float bright, float r, float n1, out Color c, out float k, out Color g)
        {
            line *= Mathf.Clamp01((1.02f - r) / .05f); bright *= line > 0 ? 1 : 0;
            k = line * .95f; c = Color.Lerp(f.col2, f.col, .5f + .5f * n1); c = Color.Lerp(c, Hx("#ffe9a0"), bright * .7f);
            g = Color.Lerp(f.glow, Hx("#ffcc55"), bright) * (line * (.75f + bright * .8f)) * (r < .07f ? .6f : 1f);
        }
        static float Ring(float r, float r0, float w) => Mathf.Clamp01(1 - Mathf.Abs(r - r0) / w);
        static float Seg(float x, float y, float ax, float ay, float bx, float by, float w)
        {
            float dx = bx - ax, dy = by - ay, l2 = dx * dx + dy * dy; float t = l2 > 0 ? Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / l2) : 0; float px = ax + dx * t - x, py = ay + dy * t - y;
            return Mathf.Clamp01(1 - Mathf.Sqrt(px * px + py * py) / w);
        }
        static float Dot(float x, float y, float cx, float cy, float rad) { float dx = x - cx, dy = y - cy; return Mathf.Clamp01(1 - (Mathf.Sqrt(dx * dx + dy * dy) - rad) / .02f); }
        /// <summary>Zeichen je Planet (Stil in crat, Anzahl in crD). „bright“ = der Teil, der zum nächsten Planeten zeigt (golden).</summary>
        /// <summary>Linien des Zeichens im Einheitskreis (Stil st, Anzahl n). bright = der Teil, der zum nächsten Planeten zeigt.</summary>
        static void GlyphLine(int st, int n, float hx, float hy, out float line, out float bright)
        {
            float r = Mathf.Sqrt(hx * hx + hy * hy), w = .028f; n = Mathf.Max(1, n);
            line = 0; bright = 0; const float A0 = .35f; float ux0 = Mathf.Cos(A0), uy0 = Mathf.Sin(A0);
            switch (st)
            {
                case 0: // Sternkarte: Ringe, n Strahlen mit Punkten
                    line = Mathf.Max(Ring(r, .9f, w), Mathf.Max(Ring(r, .52f, w * .8f), Ring(r, .16f, w))); if (r < .07f) line = 1;
                    for (int s = 0; s < n; s++) { float a0 = s * 2 * Mathf.PI / n + A0, ux = Mathf.Cos(a0), uy = Mathf.Sin(a0); float m = Mathf.Max(Seg(hx, hy, ux * .16f, uy * .16f, ux * .9f, uy * .9f, w * (s == 0 ? 1.1f : .7f)), Dot(hx, hy, ux * .9f, uy * .9f, s == 0 ? .075f : .05f)); line = Mathf.Max(line, m); if (s == 0) bright = Mathf.Max(bright, m); }
                    break;
                case 1: // Dreieck im Kreis, Punkte in den Ecken
                    line = Ring(r, .92f, w); if (r < .06f) line = 1;
                    for (int s = 0; s < 3; s++) { float a = s * 2 * Mathf.PI / 3 + A0, b = (s + 1) * 2 * Mathf.PI / 3 + A0; float m = Seg(hx, hy, Mathf.Cos(a) * .8f, Mathf.Sin(a) * .8f, Mathf.Cos(b) * .8f, Mathf.Sin(b) * .8f, w); line = Mathf.Max(line, m); float dm = Dot(hx, hy, Mathf.Cos(a) * .8f, Mathf.Sin(a) * .8f, .06f); line = Mathf.Max(line, dm); if (s == 0) bright = Mathf.Max(bright, dm); }
                    break;
                case 2: // Wellenschrift: n Wellen im Kreis, die längste endet in einem Pfeil
                    line = Ring(r, .93f, w * .8f);
                    for (int s = 0; s < n; s++) { float yy = -.6f + 1.2f * s / Mathf.Max(1, n - 1), wv = yy + Mathf.Sin(hx * 9f + s * 1.3f) * .05f; if (Mathf.Abs(hx) < Mathf.Sqrt(Mathf.Max(0, .8f - yy * yy))) { float m = Mathf.Clamp01(1 - Mathf.Abs(hy - wv) / (w * .9f)); line = Mathf.Max(line, m); if (s == n / 2) bright = Mathf.Max(bright, m * Mathf.Clamp01(hx * 3)); } }
                    break;
                case 3: // Spirale mit n Windungen
                    {
                        float ang = Mathf.Atan2(hy, hx); if (ang < 0) ang += 2 * Mathf.PI; float pitch = .85f / n;
                        for (int t = 0; t < n + 1; t++) { float rr = (ang / (2 * Mathf.PI) + t) * pitch + .05f; if (rr > .9f) break; line = Mathf.Max(line, Ring(r, rr, w)); }
                        line = Mathf.Max(line, Ring(r, .93f, w * .7f)); float dm = Dot(hx, hy, .9f * ux0, .9f * uy0, .06f); line = Mathf.Max(line, dm); bright = dm; if (r < .06f) line = 1; break;
                    }
                case 4: // Auge (n = 1 oder 2 Augen nebeneinander)
                    for (int e = 0; e < n; e++)
                    {
                        float ox = n == 1 ? 0 : (e == 0 ? -.45f : .45f), sx = n == 1 ? 1f : .5f, ex = (hx - ox) / sx, ey = hy / (n == 1 ? .55f : .3f);
                        float lid = Mathf.Clamp01(1 - Mathf.Abs(Mathf.Sqrt(ex * ex * .8f + ey * ey) - .9f) / (w * 2.2f)); float ir = Mathf.Sqrt((hx - ox) * (hx - ox) + hy * hy);
                        line = Mathf.Max(line, Mathf.Max(lid, Ring(ir, n == 1 ? .3f : .16f, w))); float pu = ir < (n == 1 ? .12f : .07f) ? 1 : 0; line = Mathf.Max(line, pu); if (e == 0) bright = Mathf.Max(bright, pu);
                    }
                    break;
                case 5: // Sechseck mit zwei verschränkten Dreiecken
                    for (int s = 0; s < 6; s++) { float a = s * Mathf.PI / 3, b = (s + 1) * Mathf.PI / 3; line = Mathf.Max(line, Seg(hx, hy, Mathf.Cos(a) * .9f, Mathf.Sin(a) * .9f, Mathf.Cos(b) * .9f, Mathf.Sin(b) * .9f, w)); }
                    for (int s = 0; s < 3; s++) for (int tri = 0; tri < 2; tri++) { float off = tri * Mathf.PI / 3, a = s * 2 * Mathf.PI / 3 + off + Mathf.PI / 6, b = (s + 1) * 2 * Mathf.PI / 3 + off + Mathf.PI / 6; float m = Seg(hx, hy, Mathf.Cos(a) * .72f, Mathf.Sin(a) * .72f, Mathf.Cos(b) * .72f, Mathf.Sin(b) * .72f, w * .8f); line = Mathf.Max(line, m); }
                    { float dm = Dot(hx, hy, 0, 0, .08f); line = Mathf.Max(line, dm); bright = dm; } break;
                case 6: // Perlenkreis: n Punkte, Ring in der Mitte
                    line = Mathf.Max(Ring(r, .8f, w * .6f), Ring(r, .28f, w)); if (r < .06f) line = 1;
                    for (int s = 0; s < n; s++) { float a = s * 2 * Mathf.PI / n + A0; float m = Dot(hx, hy, Mathf.Cos(a) * .8f, Mathf.Sin(a) * .8f, .085f); line = Mathf.Max(line, m); if (s == 0) bright = Mathf.Max(bright, m); }
                    break;
                case 8: // Doppelsichel: zwei Sicheln, Rücken an Rücken zur Mitte, goldener Punkt dazwischen
                    for (int s = 0; s < 2; s++)
                    {
                        float sg = s == 0 ? -1 : 1, ax = sg * .44f, bx = ax + sg * .17f; /* Kreise überlappen die Mitte nicht */
                        float ro = Mathf.Sqrt((hx - ax) * (hx - ax) + hy * hy), ri = Mathf.Sqrt((hx - bx) * (hx - bx) + hy * hy);
                        bool inA = ro < .4f, outB = ri > .33f;
                        if (outB) line = Mathf.Max(line, Ring(ro, .4f, w)); /* äußerer Bogen */
                        if (inA) line = Mathf.Max(line, Ring(ri, .33f, w * .8f)); /* innerer Bogen */
                    }
                    line = Mathf.Max(line, Ring(r, .93f, w * .7f)); { float dm = Dot(hx, hy, 0, 0, .07f); line = Mathf.Max(line, dm); bright = dm; }
                    break;
                default: // 7 Puls: Ringe und zwei Strahlen quer
                    for (int s = 1; s <= n; s++) line = Mathf.Max(line, Ring(r, s * .93f / n, w * (s == n ? 1 : .8f))); if (r < .07f) line = 1;
                    { float m = Mathf.Max(Seg(hx, hy, -.93f * ux0, -.93f * uy0, .93f * ux0, .93f * uy0, w * .9f), Seg(hx, hy, -.93f * uy0, .93f * ux0, .93f * uy0, -.93f * ux0, w * .6f)); line = Mathf.Max(line, m); bright = Mathf.Max(bright, m * Mathf.Clamp01((hx * ux0 + hy * uy0) * 3)); }
                    break;
            }
        }
    }
}
