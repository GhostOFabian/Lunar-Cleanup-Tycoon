// Bausteine für die Oberfläche im Stil des Browser-Spiels (Glas-Paneele, Gold-Knöpfe, Mint-Akzent, Oxanium/Atkinson).
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LunarCleanup.UI
{
    public static class Pal
    {
        public static readonly Color Void = C("#070b16"), Glass = C("#0c1222", .86f), GlassHi = C("#11192d", .97f), Line = new Color(.67f, .75f, .92f, .16f),
            Ink = C("#eef2fb"), Ink2 = C("#a3aec6"), Ink3 = C("#6f7a93"), Credit = C("#ffbf4d"), CreditInk = C("#2a1a00"), Clean = C("#6ff2d2"), Energy = C("#7fb4ff"), Warn = C("#ff6b5b"), Prod = C("#d7c4ff");
        public static Color C(string h, float a = 1) { ColorUtility.TryParseHtmlString(h, out var c); c.a = a; return c; }
    }

    public static class UiKit
    {
        public static TMP_FontAsset Display, DisplaySemi, Body, BodyBold;
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        public static void Init()
        {
            if (Display) return;
            Display = Font("Fonts/Oxanium-Bold"); DisplaySemi = Font("Fonts/Oxanium-SemiBold") ?? Display; Body = Font("Fonts/AtkinsonHyperlegible-Regular"); BodyBold = Font("Fonts/AtkinsonHyperlegible-Bold") ?? Body;
            if (!Body) Body = Display; if (!Display) Display = TMP_Settings.defaultFontAsset;
            if (Display && Body && Display != Body) { if (Display.fallbackFontAssetTable == null) Display.fallbackFontAssetTable = new List<TMP_FontAsset>(); if (!Display.fallbackFontAssetTable.Contains(Body)) Display.fallbackFontAssetTable.Add(Body); }
            if (!Display) Debug.LogWarning("Lunar Cleanup: Schriften nicht gefunden (Resources/Fonts)");
        }
        static TMP_FontAsset Font(string path)
        {
            var f = Resources.Load<Font>(path); if (!f) return null;
            var fa = TMP_FontAsset.CreateFontAsset(f, 64, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fa) fa.name = f.name; return fa;
        }

        // ---------- Sprites ----------
        /// <summary>Abgerundetes Rechteck (9-Slice), optional mit Rand.</summary>
        public static Sprite Round(int radius, bool border = true)
        {
            string k = "r" + radius + border; if (Sprites.TryGetValue(k, out var s)) return s;
            int S = radius * 2 + 4; var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float cx = Mathf.Clamp(x + .5f, radius, S - radius), cy = Mathf.Clamp(y + .5f, radius, S - radius);
                    float d = radius - new Vector2(x + .5f - cx, y + .5f - cy).magnitude; float a = Mathf.Clamp01(d + .5f);
                    float edge = border ? Mathf.Clamp01(1.5f - Mathf.Abs(d - .75f)) : 0;
                    byte w = (byte)Mathf.Lerp(255, 255, 0); px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    if (border && d < 1.6f && a > 0) px[y * S + x] = new Color32(255, 255, 255, (byte)(Mathf.Max(a * .999f, edge) * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius + 1, radius + 1, radius + 1, radius + 1));
            Sprites[k] = s; return s;
        }
        public static Sprite Circle(Color inner, Color outer, int S = 64, Vector2? hl = null)
        {
            string k = "c" + ColorUtility.ToHtmlStringRGBA(inner) + ColorUtility.ToHtmlStringRGBA(outer); if (Sprites.TryGetValue(k, out var s)) return s;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false); var px = new Color32[S * S]; var h = hl ?? new Vector2(.34f, .7f);
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    var p = new Vector2((x + .5f) / S, (y + .5f) / S); float d = (p - new Vector2(.5f, .5f)).magnitude * 2; float a = Mathf.Clamp01((1 - d) * S * .5f);
                    float g = Mathf.Clamp01((p - h).magnitude / .72f); var c = Color.Lerp(inner, outer, g); c.a *= a; px[y * S + x] = c;
                }
            tex.SetPixels32(px); tex.Apply(); s = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f)); Sprites[k] = s; return s;
        }

        /// <summary>Linien-Icons wie im Browser (24er Raster, Linienstärke 1.8).</summary>
        public static Sprite Icon(string name)
        {
            if (Sprites.TryGetValue("i" + name, out var s)) return s;
            int S = 96; float k = S / 24f, lw = 1.9f * k; var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear }; var a = new float[S * S];
            void Seg(float x0, float y0, float x1, float y1)
            {
                Vector2 A = new Vector2(x0 * k, S - y0 * k), B = new Vector2(x1 * k, S - y1 * k);
                for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                    {
                        var p = new Vector2(x + .5f, y + .5f); var ab = B - A; float t = Mathf.Clamp01(Vector2.Dot(p - A, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude)); float d = (A + ab * t - p).magnitude;
                        a[y * S + x] = Mathf.Max(a[y * S + x], Mathf.Clamp01(lw / 2 - d + .5f));
                    }
            }
            void Poly(params float[] p) { for (int i = 0; i + 3 < p.Length; i += 2) Seg(p[i], p[i + 1], p[i + 2], p[i + 3]); }
            void Circ(float cx, float cy, float r, float a0 = 0, float a1 = 360) { int n = 28; for (int i = 0; i < n; i++) { float t0 = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad, t1 = Mathf.Lerp(a0, a1, (i + 1) / (float)n) * Mathf.Deg2Rad; Seg(cx + Mathf.Cos(t0) * r, cy + Mathf.Sin(t0) * r, cx + Mathf.Cos(t1) * r, cy + Mathf.Sin(t1) * r); } }
            switch (name)
            {
                case "clean": Poly(12, 3, 12, 7); Poly(12, 17, 12, 21); Poly(3, 12, 7, 12); Poly(17, 12, 21, 12); Poly(5.6f, 5.6f, 8.4f, 8.4f); Poly(15.6f, 15.6f, 18.4f, 18.4f); Poly(5.6f, 18.4f, 8.4f, 15.6f); Poly(15.6f, 8.4f, 18.4f, 5.6f); break;
                case "car": Poly(3, 15, 4.5f, 10, 7, 8, 17, 8, 19.5f, 10, 21, 15, 21, 17, 3, 17, 3, 15); Circ(7.5f, 17.5f, 1.8f); Circ(16.5f, 17.5f, 1.8f); break;
                case "factory": Poly(3, 20, 3, 11, 8, 14, 8, 11, 13, 14, 13, 6, 17, 6, 17, 4, 20, 4, 20, 20, 3, 20); Poly(7, 17, 9, 17); Poly(12, 17, 14, 17); break;
                case "home": Poly(4, 11, 12, 4, 20, 11); Poly(6, 9.5f, 6, 20, 18, 20, 18, 9.5f); Poly(10, 20, 10, 14, 14, 14, 14, 20); break;
                case "up": Poly(12, 20, 12, 5); Poly(6, 11, 12, 5, 18, 11); Poly(5, 21, 19, 21); break;
                case "planet": Circ(12, 12, 5.5f); Poly(2.5f, 15.5f, 7, 13.4f); Poly(17, 10.6f, 21.5f, 8.5f); Circ(12, 12, 9.6f, 200, 340); break;
                case "goals": Poly(8, 4, 16, 4, 16, 6, 8, 6, 8, 4); Poly(6, 5, 5, 5, 5, 21, 19, 21, 19, 5, 18, 5); Poly(8.5f, 13, 11, 15.5f, 15.5f, 10.5f); break;
                case "gear": Circ(12, 12, 3.2f); Circ(12, 12, 7.2f); for (int i = 0; i < 8; i++) { float t = i * 45 * Mathf.Deg2Rad; Seg(12 + Mathf.Cos(t) * 7.2f, 12 + Mathf.Sin(t) * 7.2f, 12 + Mathf.Cos(t) * 9.6f, 12 + Mathf.Sin(t) * 9.6f); } break;
                case "bucket": Poly(6, 8, 18, 8, 16.6f, 19.2f, 15, 21, 9, 21, 7.4f, 19.2f, 6, 8); Circ(12, 8, 3, 180, 360); break;
                case "globe": Circ(12, 12, 9); Poly(3, 12, 21, 12); Circ(12, 12, 9, 0, 0); Poly(12, 3, 12, 21); Circ(7.5f, 12, 9, -30, 30); Circ(16.5f, 12, 9, 150, 210); break;
                case "close": Poly(6, 6, 18, 18); Poly(18, 6, 6, 18); break;
                case "rocket": Poly(12, 3, 15, 8, 15, 16, 9, 16, 9, 8, 12, 3); Poly(9, 13, 6, 17, 9, 17); Poly(15, 13, 18, 17, 15, 17); Poly(11, 19, 11, 21); Poly(13, 19, 13, 21); break;
            }
            var px = new Color32[S * S]; for (int i = 0; i < a.Length; i++) px[i] = new Color32(255, 255, 255, (byte)(a[i] * 255));
            tex.SetPixels32(px); tex.Apply(); s = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f)); Sprites["i" + name] = s; return s;
        }

        // ---------- Elemente ----------
        public static RectTransform Rt(Transform parent, string name)
        {
            var g = new GameObject(name, typeof(RectTransform)); g.transform.SetParent(parent, false); return (RectTransform)g.transform;
        }
        public static RectTransform Anchor(this RectTransform rt, float ax0, float ay0, float ax1, float ay1, float l = 0, float b = 0, float r = 0, float t = 0)
        { rt.anchorMin = new Vector2(ax0, ay0); rt.anchorMax = new Vector2(ax1, ay1); rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t); return rt; }

        public static Image Glass(Transform parent, string name, Color? col = null, int radius = 16)
        {
            var rt = Rt(parent, name); var img = rt.gameObject.AddComponent<Image>(); img.sprite = Round(radius); img.type = Image.Type.Sliced; img.color = col ?? Pal.Glass; return img;
        }
        public static TextMeshProUGUI Text(Transform parent, string name, string txt, float size, Color col, bool display = false, FontStyles st = FontStyles.Normal, TextAlignmentOptions al = TextAlignmentOptions.Left)
        {
            var rt = Rt(parent, name); var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = display ? Display : Body; t.fontSize = size; t.color = col; t.text = txt; t.fontStyle = st; t.alignment = al;
            t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false; t.overflowMode = TextOverflowModes.Truncate; return t;
        }
        public static Image Img(Transform parent, string name, Sprite s, Color col)
        {
            var rt = Rt(parent, name); var i = rt.gameObject.AddComponent<Image>(); i.sprite = s; i.color = col; i.raycastTarget = false; i.preserveAspect = true; return i;
        }
        public static Button Btn(Transform parent, string name, Action onClick, Color bg, int radius = 12)
        {
            var img = Glass(parent, name, bg, radius); img.raycastTarget = true; var b = img.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            var cb = b.colors; cb.normalColor = Color.white; cb.highlightedColor = new Color(1, 1, 1, .92f); cb.pressedColor = new Color(.8f, .8f, .8f, 1); cb.disabledColor = new Color(1, 1, 1, .35f); cb.fadeDuration = .06f; b.colors = cb;
            b.onClick.AddListener(() => onClick?.Invoke()); return b;
        }
        public static RectTransform Bar(Transform parent, string name, Color col, out RectTransform fill, float h = 5)
        {
            var bg = Glass(parent, name, new Color(1, 1, 1, .08f), 3); var f = Glass(bg.transform, "Füllung", col, 3); fill = f.rectTransform; fill.Anchor(0, 0, 0, 1); return bg.rectTransform;
        }
        public static void SetFill(RectTransform fill, float v) { v = Mathf.Clamp01(v); fill.anchorMax = new Vector2(v, 1); }
        public static LayoutElement LE(Component c, float minH = -1, float prefH = -1, float flexW = -1, float prefW = -1)
        { var le = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>(); if (minH >= 0) le.minHeight = minH; if (prefH >= 0) le.preferredHeight = prefH; if (flexW >= 0) le.flexibleWidth = flexW; if (prefW >= 0) le.preferredWidth = prefW; return le; }
        public static T Add<T>(Component c) where T : Component => c.gameObject.GetComponent<T>() ?? c.gameObject.AddComponent<T>();
    }
}
