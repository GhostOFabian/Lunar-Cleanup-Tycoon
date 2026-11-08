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
        public bool GlyphAt(int cell) => pid == "moon" && GlyphCells.Contains(cell);
        void PlaceGlyph()
        {
            if (pid != "moon") return;
            var bc = cells[baseCell]; var away = -(bc.e1 * .8f + bc.e2 * .6f).normalized; int maxRing = cells.Max(c => c.ring);
            int best = -1; float bs = -1e9f; int want = Mathf.Clamp(maxRing / 2, 3, 6);
            foreach (var c in cells) { if (c.IsBase || c.Wet || c.ring != want) continue; float sc = Vector3.Dot(c.c - bc.c, away); if (sc > bs) { bs = sc; best = c.id; } }
            if (best < 0) return; var cl = cells[best];
            AddFeature(new Feature { kind = 6, d = cl.c, e1 = cl.e1, e2 = cl.e2, rad = cl.H * 2f * 3.1f, col = Hx("#9ff6ff"), col2 = Hx("#55c8ff"), glow = Hx("#38e8ff"), seed = 7 });
        }
        /// <summary>Sternkarte: zwei Ringe, Mitte, sechs Strahlen mit Planeten-Punkten; der Strahl zum Mars leuchtet heller.</summary>
        static void GlyphPaint(Feature f, float lx, float ly, float n1, out Color c, out float k, out Color g)
        {
            float hx = lx / f.rad, hy = ly / f.rad, r = Mathf.Sqrt(hx * hx + hy * hy), w = .028f;
            float line = Mathf.Max(Mathf.Clamp01(1 - Mathf.Abs(r - .9f) / w), Mathf.Clamp01(1 - Mathf.Abs(r - .52f) / (w * .8f)), Mathf.Clamp01(1 - Mathf.Abs(r - .16f) / w));
            if (r < .07f) line = 1; float bright = 0;
            for (int s = 0; s < 6; s++)
            {
                float a0 = s * Mathf.PI / 3 + .35f, ux = Mathf.Cos(a0), uy = Mathf.Sin(a0); float along = hx * ux + hy * uy, perp = Mathf.Abs(-hx * uy + hy * ux);
                float sp = along > .16f && along < .9f ? Mathf.Clamp01(1 - perp / (w * (s == 0 ? 1.1f : .7f))) : 0;
                float dx = hx - ux * .9f, dy = hy - uy * .9f, dd = Mathf.Sqrt(dx * dx + dy * dy); float dot = Mathf.Clamp01(1 - (dd - (s == 0 ? .075f : .05f)) / .02f);
                float m = Mathf.Max(sp, dot); if (m > line) line = m; if (s == 0) bright = Mathf.Max(bright, m);
            }
            line *= Mathf.Clamp01((1.02f - r) / .05f);
            k = line * .95f; c = Color.Lerp(f.col2, f.col, .5f + .5f * n1); c = Color.Lerp(c, Hx("#ffe9a0"), bright * .7f);
            g = (Color.Lerp(f.glow, Hx("#ffcc55"), bright) * (line * (1.2f + bright * 1.2f)));
        }
    }
}
