// Fabian 08.11.: Mond-Geheimnis – eine leuchtende Sternkarte im Boden (Geländeform kind 6, nur Farbe und Leuchten,
// unter Dreck unsichtbar). Liegt gegenüber der Entdeckung, mitten im Dreck, gut sichtbar groß.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public partial class PlanetData
    {
        List<int> glyphCells;
        /// <summary>Parzellen, auf denen die Sternkarte liegt (ohne Basis und Wasser).</summary>
        public List<int> GlyphCells
        {
            get
            {
                if (glyphCells != null) return glyphCells; if (hk == null) InitTerrain(); glyphCells = new List<int>();
                var f = feats.FirstOrDefault(x => x.kind == 6); if (f == null) return glyphCells;
                foreach (var c in cells) { if (c.IsBase || c.Wet) continue; float d = Mathf.Acos(Mathf.Clamp(Vector3.Dot(c.c.normalized, f.d), -1, 1)) * R; if (d < f.rad * .97f) glyphCells.Add(c.id); }
                return glyphCells;
            }
        }
        public bool GlyphAt(int cell) => GlyphCells.Contains(cell);
        void PlaceGlyph()
        {
            if (!Game.Glyphs.TryGetValue(pid, out var gs)) return;
            var bc = cells[baseCell]; var away = -(bc.e1 * .8f + bc.e2 * .6f).normalized; int maxRing = cells.Max(c => c.ring);
            int best = -1; float bs = -1e9f; int want = Mathf.Clamp(maxRing / 2, 3, 6);
            for (int tries = 0; tries < 3 && best < 0; tries++, want--)
                foreach (var c in cells) { if (c.IsBase || c.Wet || c.ring != want || cellFeats.ContainsKey(c.id)) continue; float sc = Vector3.Dot(c.c - bc.c, away); if (sc > bs) { bs = sc; best = c.id; } }
            if (best < 0) return; var cl = cells[best];
            AddFeature(new Feature { kind = 6, d = cl.c, e1 = cl.e1, e2 = cl.e2, rad = cl.H * 2f * 3.1f, col = Hx(gs.col), col2 = Hx(gs.col2), glow = Hx(gs.glow), crat = gs.style, crD = gs.n, seed = 7 });
        }
        static float Ring(float r, float r0, float w) => Mathf.Clamp01(1 - Mathf.Abs(r - r0) / w);
        static float Seg(float x, float y, float ax, float ay, float bx, float by, float w)
        {
            float dx = bx - ax, dy = by - ay, l2 = dx * dx + dy * dy; float t = l2 > 0 ? Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / l2) : 0; float px = ax + dx * t - x, py = ay + dy * t - y;
            return Mathf.Clamp01(1 - Mathf.Sqrt(px * px + py * py) / w);
        }
        static float Dot(float x, float y, float cx, float cy, float rad) { float dx = x - cx, dy = y - cy; return Mathf.Clamp01(1 - (Mathf.Sqrt(dx * dx + dy * dy) - rad) / .02f); }
        /// <summary>Zeichen je Planet (Stil in crat, Anzahl in crD). „bright“ = der Teil, der zum nächsten Planeten zeigt (golden).</summary>
        static void GlyphPaint(Feature f, float lx, float ly, float n1, out Color c, out float k, out Color g)
        {
            float hx = lx / f.rad, hy = ly / f.rad, r = Mathf.Sqrt(hx * hx + hy * hy), w = .028f; int st = Mathf.RoundToInt(f.crat), n = Mathf.Max(1, Mathf.RoundToInt(f.crD));
            float line = 0, bright = 0; const float A0 = .35f; float ux0 = Mathf.Cos(A0), uy0 = Mathf.Sin(A0);
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
                default: // 7 Puls: Ringe und zwei Strahlen quer
                    for (int s = 1; s <= n; s++) line = Mathf.Max(line, Ring(r, s * .93f / n, w * (s == n ? 1 : .8f))); if (r < .07f) line = 1;
                    { float m = Mathf.Max(Seg(hx, hy, -.93f * ux0, -.93f * uy0, .93f * ux0, .93f * uy0, w * .9f), Seg(hx, hy, -.93f * uy0, .93f * ux0, .93f * uy0, -.93f * ux0, w * .6f)); line = Mathf.Max(line, m); bright = Mathf.Max(bright, m * Mathf.Clamp01((hx * ux0 + hy * uy0) * 3)); }
                    break;
            }
            line *= Mathf.Clamp01((1.02f - r) / .05f); bright *= line > 0 ? 1 : 0;
            k = line * .95f; c = Color.Lerp(f.col2, f.col, .5f + .5f * n1); c = Color.Lerp(c, Hx("#ffe9a0"), bright * .7f);
            g = Color.Lerp(f.glow, Hx("#ffcc55"), bright) * (line * (.75f + bright * .8f)) * (r < .07f ? .6f : 1f);
        }
    }
}
