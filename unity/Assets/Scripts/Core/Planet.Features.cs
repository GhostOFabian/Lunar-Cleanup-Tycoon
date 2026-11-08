// Geländeformen (Fabian 01.11.: „kein Modell – es soll wirklich aussehen, als wäre es vom Planeten“):
// Vulkane, Berge, Eisgrate, Schlothügel, Becken, Herz und Tigerstreifen sind Teil des Bodens –
// sie heben/senken das Gelände (Height) und färben die Bodentextur (Paint, inkl. Leuchten über die Emission).
// Die Plätze sind fest je Planet (gleiche Lage für Boden, Geysire und Entdeckung).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunarCleanup.Core
{
    public class Feature { public Vector3 d, e1, e2; public float rad, h, crat, crD; public int kind; public Color col, col2, glow; public float seed; }
    // kind: 0 Vulkan, 1 Schlothügel (Geysir), 2 Berg/Grat/Eisnadel, 3 Herz (nur Farbe), 4 Becken (leicht vertieft, leuchtet), 5 Leuchtrisse (nur Farbe)

    public partial class PlanetData
    {
        public readonly List<Feature> feats = new List<Feature>();
        readonly Dictionary<int, List<Feature>> cellFeats = new Dictionary<int, List<Feature>>();
        public readonly List<Vector3> geyserSites = new List<Vector3>();
        /// <summary>Krater der Vulkane (Lava-Fontänen, Rauch).</summary>
        public readonly List<Vector3> volcanoSites = new List<Vector3>();
        public bool HasFeatures(int cell) { if (hk == null) InitTerrain(); return cellFeats.ContainsKey(cell); }

        /// <summary>Platz der Entdeckung: ein paar Parzellen vor der Basis, nicht auf einem Außenlager-Platz.</summary>
        public int WonderCell()
        {
            var bc = cells[baseCell]; var dirW = (bc.e1 * .8f + bc.e2 * .6f).normalized; var sites = Game.OutpostSites(pid); int best = -1; float bs = -1e9f;
            foreach (var c in cells) { if (c.IsBase || c.Wet || sites.Contains(c.id) || c.ring < 2 || c.ring > 3) continue; float sc = Vector3.Dot(c.c - bc.c, dirW); if (sc > bs) { bs = sc; best = c.id; } }
            return best;
        }

        void AddFeature(Feature f)
        {
            f.d.Normalize(); feats.Add(f); float lim = f.rad * 1.15f;
            foreach (var c in cells) { if (Vector3.Dot(c.c, f.d) < Mathf.Cos(Mathf.Min(Mathf.PI, (lim + c.H * 1.5f) / R))) continue; if (!cellFeats.TryGetValue(c.id, out var l)) cellFeats[c.id] = l = new List<Feature>(); l.Add(f); }
        }
        Feature F(int kind, Vector3 d, Cell frame, float rad, float h, Color col, Color col2, Color glow, float crat = 0, float crD = 0) => new Feature { kind = kind, d = d, e1 = frame.e1, e2 = frame.e2, rad = rad, h = h, col = col, col2 = col2, glow = glow, crat = crat, crD = crD, seed = (d.x * 91 + d.y * 37 + d.z * 13) % 10 };
        static Color Hx(string h) => GameData.Hex(h);

        /// <summary>Alle Geländeformen des Planeten anlegen (einmal, beim ersten Height-Aufruf).</summary>
        void PlaceFeatures()
        {
            try { PlaceGlyph(); } catch (Exception) { } /* 08.11.: Mond-Sternkarte */
            int wc = -1; try { wc = WonderCell(); } catch (Exception) { }
            if (wc >= 0)
            {
                var cl = cells[wc]; Vector3 At(float x, float y) => ToDir(cl, x, y);
                switch (pid)
                {
                    case "io": AddFeature(F(0, cl.c, cl, 12f, 9.5f, Hx("#2a1f1a"), Hx("#5a3a1c"), Hx("#ff5a14"), .17f, 3f)); break; /* Fontänen an den übrigen Vulkanen: volcanoSites */
                    case "cancri": AddFeature(F(2, cl.c, cl, 13f, 8f, Hx("#cfe8f2"), Hx("#7fa6b8"), Hx("#bff4ff"))); break;
                    case "ganymed": for (int i = -2; i <= 2; i++) AddFeature(F(2, At(i * 3.2f, Mathf.Abs(i) * .8f), cl, 5f - Mathf.Abs(i) * .6f, 3.6f - Mathf.Abs(i) * .7f, Hx("#a8d4f5"), Hx("#5f88b0"), Color.black)); break;
                    case "eris": { var r = new System.Random(5); for (int i = 0; i < 9; i++) AddFeature(F(2, At((float)r.NextDouble() * 9 - 4.5f, (float)r.NextDouble() * 9 - 4.5f), cl, 1.6f + (float)r.NextDouble() * .8f, 2.2f + (float)r.NextDouble() * 2.4f, Hx("#eef6ff"), Hx("#9ab4cc"), Hx("#cfe6ff") * .15f)); break; }
                    case "pluto": AddFeature(F(3, cl.c, cl, 7.5f, 0, Hx("#f4dccb"), Hx("#e8c2a8"), Hx("#ffd8c8") * .35f)); break;
                    case "trappist": { for (int i = 0; i < 4; i++) AddFeature(F(4, At(-4.5f + i * 3, (i % 2) * 2.5f - 1.2f), cl, 1.4f + .3f * i, -.25f, Hx("#0b3a36"), Hx("#164a44"), i % 2 == 0 ? Hx("#3affd0") : Hx("#5ab0ff"))); break; }
                    case "enceladus": AddFeature(F(5, cl.c, cl, 9f, 0, Hx("#dff2ff"), Hx("#9fd0f0"), Hx("#3d93ff"))); break;
                }
                if (pid == "europa" || pid == "enceladus" || pid == "triton" || pid == "gliese")
                {
                    int n = pid == "europa" ? 1 : pid == "gliese" ? 4 : 3; var look = VentLook();
                    for (int i = 0; i < n; i++) AddFeature(F(1, At(-4 + i * 4, (i % 2) * 2 - 1), cl, pid == "europa" ? 3.6f : 2.6f, pid == "europa" ? 1.3f : .8f, look.a, look.b, look.g, .28f, .45f));
                }
            }
            // Fabian 01.11.: „auch an anderen Stellen“ – je nach Planet weitere Geländeformen verteilt (Lava → Vulkane, Säure → Säurekrater, Wasser → heiße Quellen, Eis → Eisdome)
            {
                string lk = GameData.PLiq.TryGetValue(pid, out var l0) ? l0 : null; bool ice = pid == "europa" || pid == "enceladus" || pid == "ganymed" || pid == "triton" || pid == "pluto" || pid == "eris" || pid == "sedna";
                var rnd2 = new System.Random(pid.GetHashCode() ^ 0x2b7); var sites2 = Game.OutpostSites(pid); var used2 = new List<Vector3>(); if (wc >= 0) used2.Add(cells[wc].c);
                int want2 = Mathf.Clamp(N / 300, 3, 9); var cand2 = new List<int>(); foreach (var c in cells) if (!c.IsBase && c.ring >= 3 && !c.Wet && !sites2.Contains(c.id)) cand2.Add(c.id);
                for (int tries = 0; tries < want2 * 10 && used2.Count < want2 + 1 && cand2.Count > 0 && (lk != null || ice); tries++)
                {
                    var cl = cells[cand2[rnd2.Next(cand2.Count)]]; bool near = false; foreach (var u in used2) if (Arc(u, cl.c) < cl.H * 6) { near = true; break; } if (near) continue; used2.Add(cl.c);
                    float sz = .6f + (float)rnd2.NextDouble() * .5f;
                    if (lk == "lava") { AddFeature(F(0, cl.c, cl, 9f * sz, 6.5f * sz, Hx("#2a1f1a"), Hx("#5a3a1c"), Hx("#ff5a14"), .18f, 2.2f * sz)); volcanoSites.Add(cl.c); }
                    else if (lk == "acid") AddFeature(F(4, cl.c, cl, 5.5f * sz, -.7f, Hx("#3a4a10"), Hx("#8a9a30"), Hx("#b8ff2a")));
                    else if (lk == "water") AddFeature(F(4, cl.c, cl, 4.5f * sz, -.5f, Hx("#0b3048"), Hx("#4a7a8a"), Hx("#5ad8ff") * .6f));
                    else if (lk == "methan") AddFeature(F(4, cl.c, cl, 5f * sz, -.4f, Hx("#3a2a18"), Hx("#7a5a30"), Hx("#ffa040") * .25f));
                    else if (ice) AddFeature(F(2, cl.c, cl, 6f * sz, 3.2f * sz, Hx("#eaf6ff"), Hx("#9ab8d4"), Hx("#cfe6ff") * .12f)); // Eisdom
                }
            }
            // Venus und Poltergeist: Säurekrater bei der Entdeckung
            if (wc >= 0 && (pid == "venus" || pid == "poltergeist")) { var cl = cells[wc]; AddFeature(F(4, ToDir(cl, 2.5f, -2f), cl, 5.5f, -.9f, Hx("#2e3a0c"), Hx("#8a9a30"), Hx("#b8ff2a"))); }
            // Geysire überall: kleine Schlothügel abseits der Basis
            {
                var look = VentLook(); var rnd = new System.Random(pid.GetHashCode() ^ 0x5e1); var sites = Game.OutpostSites(pid);
                int want = Mathf.Clamp(N / 40, 12, 45); /* Leistung auf dem Handy: höchstens 45 */ var cand = new List<int>(); foreach (var c in cells) if (!c.IsBase && c.ring >= 2 && !sites.Contains(c.id) && c.id != wc) cand.Add(c.id); /* Fabian 01.11.: über den ganzen Planeten verteilt */ float minD = R * 3.2f / Mathf.Sqrt(want);
                for (int tries = 0; tries < want * 20 && geyserSites.Count < want && cand.Count > 0; tries++)
                {
                    int id = cand[rnd.Next(cand.Count)]; var cl = cells[id]; bool near = false; foreach (var g in geyserSites) if (Arc(g, cl.c) < (tries < want * 12 ? minD : minD * .5f)) { near = true; break; } if (near) continue;
                    float a = (float)rnd.NextDouble() * 6.283f, rr = (float)rnd.NextDouble() * cl.H * .4f; var d = ToDir(cl, Mathf.Cos(a) * rr, Mathf.Sin(a) * rr);
                    geyserSites.Add(d); AddFeature(F(1, d, cl, 2.4f, .55f, look.a, look.b, look.g, .3f, .35f));
                }
            }
        }
        /// <summary>Farben rund um einen Schlot: Ablagerung (a), Rand (b), Glühen (g) – je nach dem, was der Planet ausspuckt.</summary>
        (Color a, Color b, Color g) VentLook()
        {
            if (GameData.PLiq.TryGetValue(pid, out var lk))
                switch (lk) { case "lava": return (Hx("#3a2a18"), Hx("#d8b040"), Hx("#ff6a1a") * .8f); case "acid": return (Hx("#5a6a20"), Hx("#c8e040"), Hx("#a8ff30") * .5f); case "methan": return (Hx("#4a3a28"), Hx("#a88a5a"), Color.black); default: return (Hx("#dfeef8"), Hx("#a8c8e0"), Color.black); }
            switch (pid) { case "mars": return (Hx("#6a3a24"), Hx("#a8603a"), Color.black); case "triton": return (Hx("#2a2424"), Hx("#4a3c3a"), Color.black); case "ganymed": case "pluto": case "eris": case "sedna": return (Hx("#e6f0fa"), Hx("#b0c4d8"), Color.black); default: return (Hx("#3a3a40"), Hx("#7a7a84"), Color.black); }
        }

        /// <summary>Höhe der Geländeformen an dieser Stelle (Meter).</summary>
        float FeatureH(Vector3 dir, int cell)
        {
            if (!cellFeats.TryGetValue(cell, out var l)) return 0; float h = 0;
            foreach (var f in l)
            {
                float cd = Vector3.Dot(dir, f.d); if (cd < .5f) continue; float x = Mathf.Acos(Mathf.Min(1, cd)) * R / f.rad; if (x >= 1.15f) continue;
                float n = Noise2(Vector3.Dot(dir, f.e1) * R * .35f + f.seed, Vector3.Dot(dir, f.e2) * R * .35f); // unregelmäßige Hänge (Rauschen statt Zacken)
                switch (f.kind)
                {
                    case 0: { float s = Mathf.Clamp01(1 - x); h += f.h * Mathf.Pow(s, 1.7f) * (.94f + .12f * n); if (x < f.crat * 1.3f) { float c = Mathf.Clamp01(1 - x / (f.crat * 1.3f)); h -= f.crD * Mathf.SmoothStep(0, 1, c); } break; } // Kegel mit hohlen Flanken und Krater
                    case 1: { float s = Mathf.Clamp01(1 - x * x); h += f.h * s * s; if (x < f.crat) h -= f.crD * (1 - x / f.crat); break; } // flacher Hügel mit Schlot
                    case 2: { float s = Mathf.Clamp01(1 - x); h += f.h * Mathf.Pow(s, 1.35f) * (.85f + .3f * n); break; } // Berg / Eisnadel
                    case 4: { float s = Mathf.Clamp01(1 - x * x); h += f.h * s; break; } // flaches Becken
                }
            }
            return h;
        }

        /// <summary>Farbe der Geländeform (Anteil a) und Leuchten für die Bodentextur.</summary>
        public bool FeaturePaint(int cell, Vector3 dir, out Color col, out float a, out Color glow)
        {
            col = Color.black; a = 0; glow = Color.black; if (hk == null) InitTerrain(); if (!cellFeats.TryGetValue(cell, out var l)) return false; bool any = false;
            foreach (var f in l)
            {
                float cd = Vector3.Dot(dir, f.d); if (cd < .5f) continue; float dist = Mathf.Acos(Mathf.Min(1, cd)) * R, x = dist / f.rad; if (x >= 1.15f) continue;
                float lx = Vector3.Dot(dir - f.d * cd, f.e1) * R, ly = Vector3.Dot(dir - f.d * cd, f.e2) * R, ang = Mathf.Atan2(ly, lx);
                float n1 = Noise2(lx * .7f + f.seed, ly * .7f), n2 = Noise2(lx * 2.3f, ly * 2.3f + f.seed);
                Color c; float k; Color g = Color.black;
                switch (f.kind)
                {
                    case 0:
                        {
                            k = Mathf.Clamp01((1.12f - x) / .25f); c = Color.Lerp(f.col, f.col2, n1 * .6f) * (.75f + .5f * n2); // Basalt
                            float flow = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(ang * 4 + n1 * 5 + f.seed) * .5f + .5f), 22) * Mathf.Clamp01((.85f - x) / .5f) * Mathf.Clamp01((x - f.crat) / .08f); // Lavaströme den Hang hinab
                            float pool = x < f.crat * .8f ? 1 : Mathf.Clamp01(1 - (x - f.crat * .8f) / .04f);
                            float lava = Mathf.Max(flow * (.6f + .4f * n2), pool);
                            c = Color.Lerp(c, Color.Lerp(f.glow, Hx("#ffd060"), n2 * .5f), lava); g = f.glow * lava * (pool > 0 ? .9f : .55f);
                            break;
                        }
                    case 1:
                        {
                            k = Mathf.Clamp01((1.1f - x) / .4f) * .85f; c = Color.Lerp(f.col2, f.col, Mathf.Clamp01(x * 1.4f) + (n2 - .5f) * .3f);
                            if (x < f.crat) { c = Color.Lerp(Hx("#0a0908"), c, x / f.crat * .5f); k = 1; g = f.glow * (1 - x / f.crat); } // dunkles Schlotloch
                            break;
                        }
                    case 2: { k = Mathf.Clamp01((1.1f - x) / .35f); c = Color.Lerp(f.col2, f.col, Mathf.Clamp01(1.1f - x + (n1 - .5f) * .5f)) * (.85f + .3f * n2); if (f.glow.maxColorComponent > 0 && n2 > .82f) g = f.glow * ((n2 - .82f) * 5); break; } // Gestein unten, hell oben, Funkeln
                    case 3:
                        {
                            float hx = lx / f.rad, hy = -ly / f.rad + .25f; float hv = Mathf.Pow(hx * hx + hy * hy - .25f, 3) - hx * hx * hy * hy * hy * 1.2f; // Herzkurve
                            k = Mathf.Clamp01(-hv * 40) * .9f; c = Color.Lerp(f.col, f.col2, n1 * .4f); g = f.glow * k; break;
                        }
                    case 4: { k = Mathf.Clamp01((1.05f - x) / .15f); float inside = Mathf.Clamp01((.85f - x) / .15f); c = Color.Lerp(f.col2, f.col, inside); g = f.glow * inside * (.6f + .4f * n2); c = Color.Lerp(c, f.glow, inside * .45f); break; }
                    case 5:
                        {
                            float line = 0; for (int s = 0; s < 4; s++) { float off = (s - 1.5f) * 2.4f, yy = ly - off - Mathf.Sin(lx * .35f + s) * .6f; line = Mathf.Max(line, Mathf.Clamp01(1 - Mathf.Abs(yy) / .35f)); }
                            line *= Mathf.Clamp01((1 - x) / .3f); k = line * .9f; c = Color.Lerp(f.col2, Hx("#1a3a5a"), .5f); g = f.glow * line * 1.6f; break;
                        }
                    case 6: GlyphPaint(f, lx, ly, n1, out c, out k, out g); break;
                    default: continue;
                }
                if (k <= 0) continue; any = true; col = Color.Lerp(col, c, a <= 0 ? 1 : k); a = Mathf.Max(a, k); glow += g;
            }
            return any;
        }
        static float Noise2(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y); float fx = x - ix, fy = y - iy; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float H(int a, int b) { unchecked { int h = a * 374761393 + b * 668265263; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
            return Mathf.Lerp(Mathf.Lerp(H(ix, iy), H(ix + 1, iy), fx), Mathf.Lerp(H(ix, iy + 1), H(ix + 1, iy + 1), fx), fy);
        }
    }
}
