// Weltraum-Hintergrund: pro Planet eine eigene, prozedural erzeugte Panorama-Himmelskugel
// (Nebel in Planetenfarben, Milchstraßenband, farbige Sterne, helle Sterne mit Schein, ferne Galaxie).
// Keine Downloads nötig. Ergebnis wird je Planet zwischengespeichert.
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public static class SpaceSky
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { Cache.Clear(); }
        static readonly Dictionary<string, (Material mat, Color neb1, Color neb2, Vector3 nebDir)> Cache = new Dictionary<string, (Material, Color, Color, Vector3)>();

        /// <summary>Liefert Himmel-Material sowie die beiden Nebelfarben und die Richtung des hellsten Nebels (für das farbige Sternlicht).</summary>
        public static (Material mat, Color neb1, Color neb2, Vector3 nebDir) For(string pid, PlanetDef P)
        {
            if (Cache.TryGetValue(pid, out var c)) return c;
            int idx = System.Array.IndexOf(GameData.POrder, pid); var rnd = new System.Random(911 + idx * 7717);
            // Farbpalette je Planet: Farbton aus Planetenfarbe, dazu ein Komplementärton – jeder Himmel sieht anders aus
            Color.RGBToHSV(P.col0, out float h0, out _, out _);
            float hA = Mathf.Repeat(h0 + .52f + idx * .071f, 1), hB = Mathf.Repeat(hA + .1f + (float)rnd.NextDouble() * .2f, 1);
            Color n1 = Color.HSVToRGB(hA, .75f, 1), n2 = Color.HSVToRGB(hB, .65f, 1);
            /* Fabian 01.11.: jeder Himmel deutlich anders – feste Nebelfarben und -stärke je Planet */
            float nebK = 1f, bandS = 1f;
            switch (pid)
            {
                case "moon": n1 = GameData.Hex("#3d6dff"); n2 = GameData.Hex("#9a5cff"); nebK = .8f; break;
                case "mars": n1 = GameData.Hex("#ff5a3a"); n2 = GameData.Hex("#ffb05a"); nebK = 1.3f; break;
                case "europa": n1 = GameData.Hex("#2fd0ff"); n2 = GameData.Hex("#4a7bff"); nebK = 1.4f; break;
                case "titan": n1 = GameData.Hex("#ff9a2a"); n2 = GameData.Hex("#ffd36a"); nebK = .9f; bandS = .5f; break;
                case "io": n1 = GameData.Hex("#ffe03a"); n2 = GameData.Hex("#ff6a2a"); nebK = 1.4f; break;
                case "venus": n1 = GameData.Hex("#ffcf7a"); n2 = GameData.Hex("#ff8a4a"); nebK = 1.1f; bandS = .3f; break;
                case "ganymed": n1 = GameData.Hex("#6a8aff"); n2 = GameData.Hex("#c0a0ff"); nebK = 1.1f; break;
                case "enceladus": n1 = GameData.Hex("#9af2ff"); n2 = GameData.Hex("#ffffff"); nebK = 1.2f; break;
                case "triton": n1 = GameData.Hex("#ff7ab0"); n2 = GameData.Hex("#7a9aff"); nebK = 1.5f; break;
                case "pluto": n1 = GameData.Hex("#ffb38a"); n2 = GameData.Hex("#8a6aff"); nebK = 1.2f; break;
                case "eris": n1 = GameData.Hex("#b0c8ff"); n2 = GameData.Hex("#e8eeff"); nebK = .9f; bandS = 1.6f; break;
                case "sedna": n1 = GameData.Hex("#ff3a4a"); n2 = GameData.Hex("#ff9a7a"); nebK = 1.5f; break;
                case "proxima": n1 = GameData.Hex("#ff4a2a"); n2 = GameData.Hex("#ffd04a"); nebK = 1.6f; break;
                case "trappist": n1 = GameData.Hex("#3affd0"); n2 = GameData.Hex("#3a8aff"); nebK = 1.6f; break;
                case "kepler": n1 = GameData.Hex("#6aff6a"); n2 = GameData.Hex("#3ad0ff"); nebK = 1.5f; break;
                case "cancri": n1 = GameData.Hex("#ff7a2a"); n2 = GameData.Hex("#ffffff"); nebK = 1.3f; break;
                case "gliese": n1 = GameData.Hex("#7affff"); n2 = GameData.Hex("#b07aff"); nebK = 1.4f; break;
                case "poltergeist": n1 = GameData.Hex("#5affb0"); n2 = GameData.Hex("#d04aff"); nebK = 1.8f; break;
            }
            int W = SystemInfo.systemMemorySize > 3000 ? 2048 : 1024, H = W / 2; var px = new Color32[W * H];
            var band = new Vector3((float)rnd.NextDouble() - .5f, (float)rnd.NextDouble() - .5f, (float)rnd.NextDouble() - .5f).normalized; // Achse des Milchstraßenbands
            var nebC = new Vector3((float)rnd.NextDouble() - .5f, (float)rnd.NextDouble() - .5f, (float)rnd.NextDouble() - .5f).normalized; // Zentrum des hellsten Nebels
            var gal = new Vector3((float)rnd.NextDouble() - .5f, (float)rnd.NextDouble() - .5f, (float)rnd.NextDouble() - .5f).normalized;
            float seed = (float)rnd.NextDouble() * 100; Color bg = new Color(.006f, .008f, .018f);
            var img = new Color[W * H];
            System.Threading.Tasks.Parallel.For(0, H / 2, yy => // Nebel in halber Auflösung, auf allen Prozessorkernen
            {
                int y = yy * 2;
                float lat = (y + 1f) / H * Mathf.PI - Mathf.PI / 2, cl = Mathf.Cos(lat), sl = Mathf.Sin(lat);
                for (int x = 0; x < W; x += 2)
                {
                    float lon = (x + 1f) / W * Mathf.PI * 2; var d = new Vector3(cl * Mathf.Cos(lon), sl, cl * Mathf.Sin(lon));
                    float bandK = Mathf.Exp(-Mathf.Pow(Vector3.Dot(d, band) / .22f, 2));
                    float n = Fbm(d * 2.2f + Vector3.one * seed, 5), m = Fbm(d * 4.5f - Vector3.one * seed, 4);
                    float neb = Mathf.Clamp01(n * 1.6f - .55f) * (.35f + .65f * Mathf.Clamp01(Vector3.Dot(d, nebC) * .8f + .5f));
                    float dust = Mathf.Clamp01(m * 1.4f - .6f);
                    var col = bg + n1 * (neb * .42f * nebK) + n2 * (Mathf.Clamp01(m * 1.5f - .7f) * neb * .6f * nebK) + new Color(.55f, .5f, .62f) * (bandK * bandS * (.06f + .1f * n)) - new Color(.02f, .02f, .02f) * dust * bandK;
                    float gk = Mathf.Exp(-(1 - Vector3.Dot(d, gal)) * 900); col += new Color(.9f, .8f, 1f) * gk * .55f; // ferne Galaxie
                    img[y * W + x] = col; img[y * W + x + 1] = col; img[(y + 1) * W + x] = col; img[(y + 1) * W + x + 1] = col;
                }
            });
            // Sterne: viele kleine, im Band dichter; einige große mit Schein
            int nStars = W * H / 180;
            for (int i = 0; i < nStars; i++)
            {
                var d = new Vector3((float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1).normalized;
                float bandK = Mathf.Exp(-Mathf.Pow(Vector3.Dot(d, band) / .25f, 2)); if (rnd.NextDouble() > .35f + .65f * bandK) continue;
                float b = Mathf.Pow((float)rnd.NextDouble(), 6) * 2.2f + .12f; float t = (float)rnd.NextDouble();
                var sc = t < .15f ? new Color(.65f, .78f, 1f) : t < .3f ? new Color(1f, .82f, .62f) : t < .34f ? new Color(1f, .6f, .55f) : Color.white;
                Plot(img, W, H, d, sc * b, b > 1.2f ? 2 : 0);
            }
            for (int i = 0; i < 45; i++) { var d = new Vector3((float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1).normalized; Plot(img, W, H, d, Color.Lerp(Color.white, i % 2 == 0 ? n1 : n2, .3f) * 2.5f, 4); }
            for (int i = 0; i < px.Length; i++) { var c0 = img[i]; px[i] = new Color32((byte)(Mathf.Clamp01(c0.r) * 255), (byte)(Mathf.Clamp01(c0.g) * 255), (byte)(Mathf.Clamp01(c0.b) * 255), 255); }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapModeU = TextureWrapMode.Repeat, wrapModeV = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, name = "Himmel " + pid };
            tex.SetPixels32(px); tex.Apply(true, true);
            var sh = Shader.Find("Skybox/Panoramic"); var mat = new Material(sh); mat.SetTexture("_MainTex", tex); mat.SetFloat("_Mapping", 1); mat.SetFloat("_ImageType", 0); mat.SetFloat("_Exposure", 1.15f);
            c = (mat, n1, n2, nebC); Cache[pid] = c; return c;
        }

        static void Plot(Color[] img, int W, int H, Vector3 d, Color c, int r)
        {
            float lat = Mathf.Asin(Mathf.Clamp(d.y, -1, 1)), lon = Mathf.Atan2(d.z, d.x); if (lon < 0) lon += Mathf.PI * 2;
            int cx = Mathf.Clamp((int)(lon / (Mathf.PI * 2) * W), 0, W - 1), cy = Mathf.Clamp((int)((lat + Mathf.PI / 2) / Mathf.PI * H), 0, H - 1);
            float sx = 1 / Mathf.Max(.2f, Mathf.Cos(lat)); // am Pol breiter, damit die Sterne rund bleiben
            for (int dy = -r - 1; dy <= r + 1; dy++) for (int dx = -(int)((r + 1) * sx); dx <= (int)((r + 1) * sx); dx++)
                {
                    int x = (cx + dx + W) % W, y = cy + dy; if (y < 0 || y >= H) continue; float dd = Mathf.Sqrt(dx * dx / (sx * sx) + dy * dy);
                    float k = r == 0 ? (dd < .5f ? 1 : 0) : Mathf.Exp(-dd * dd / (r * .45f)) + (dx == 0 || dy == 0 ? .25f * Mathf.Exp(-dd / (r * 1.2f)) : 0);
                    img[y * W + x] += c * k;
                }
        }

        // ---------- Rauschen (3D, nahtlos auf der Kugel) ----------
        static float Hash(int x, int y, int z) { unchecked { int h = x * 374761393 + y * 668265263 + z * 1274126177; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffffff) / (float)0xffffff; } }
        static float Noise(Vector3 p)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y), z = Mathf.FloorToInt(p.z); float fx = p.x - x, fy = p.y - y, fz = p.z - z;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy); fz = fz * fz * (3 - 2 * fz);
            float a = Mathf.Lerp(Hash(x, y, z), Hash(x + 1, y, z), fx), b = Mathf.Lerp(Hash(x, y + 1, z), Hash(x + 1, y + 1, z), fx);
            float c = Mathf.Lerp(Hash(x, y, z + 1), Hash(x + 1, y, z + 1), fx), d = Mathf.Lerp(Hash(x, y + 1, z + 1), Hash(x + 1, y + 1, z + 1), fx);
            return Mathf.Lerp(Mathf.Lerp(a, b, fy), Mathf.Lerp(c, d, fy), fz);
        }
        static float Fbm(Vector3 p, int oct) { float s = 0, a = .5f, n = 0; for (int i = 0; i < oct; i++) { s += Noise(p) * a; n += a; p *= 2.03f; a *= .5f; } return s / n; }
    }
}
