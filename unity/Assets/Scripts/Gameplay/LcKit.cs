// Eigene 3D-Modelle (Fabians Blender-Skript lunar_models.py, Resources/LC): laden, Materialien nach Namen (LC_*) auf URP übertragen
// und die beweglichen Teile (<id>_Rakete, _Kolben, _Dreh, _Rad_n, _Werkzeug, _Rotor_n, _Ladung, _Licht) an die vorhandenen Animationen hängen.
// Reihenfolge in Models.cs: LcKit → Kit (Kenney, liegt jetzt im Archiv) → prozedurales Modell. Fehlt ein LC-Modell, liefern die Funktionen null.
// Quelle der Modelle und Maße: Modelle_Quelle/LIESMICH.txt und manifest.json (1 Einheit = 1 m, Ursprung Mitte am Boden, Vorderseite +Z).
using System.Collections.Generic;
using System.Linq;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public static class LcKit
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { Cache.Clear(); MatCache.Clear(); }
        static readonly Dictionary<string, GameObject> Cache = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, Material> MatCache = new Dictionary<string, Material>();
        static Color Hex(string h) => GameData.Hex(h);

        static GameObject Prefab(string path)
        {
            if (Cache.TryGetValue(path, out var p)) return p;
            p = Resources.Load<GameObject>("LC/" + path); Cache[path] = p; return p;
        }
        public static bool Has(string path) => Prefab(path) != null;

        // ---------- Materialien: Werte 1:1 aus materialien.mtl (Kd Farbe, Ke Leuchten, Pr Rauheit, Pm Metall) ----------
        readonly struct P { public readonly Color c, e; public readonly float rough, metal; public P(float r, float g, float b, float er, float eg, float eb, float pr, float pm) { c = new Color(r, g, b); e = new Color(er, eg, eb); rough = pr; metal = pm; } }
        static readonly Dictionary<string, P> Pal = new Dictionary<string, P>
        {
            { "LC_akzent", new P(1f, 0.749f, 0.302f, 0, 0, 0, 0.45f, 0.3f) },
            { "LC_akzent2", new P(0.3451f, 0.8039f, 0.7059f, 0, 0, 0, 0.45f, 0.3f) },
            { "LC_band", new P(0.1373f, 0.1529f, 0.1804f, 0, 0, 0, 0.75f, 0.1f) },
            { "LC_dunkel", new P(0.2275f, 0.2549f, 0.3137f, 0, 0, 0, 0.45f, 0.5f) },
            { "LC_eis", new P(0.749f, 0.9098f, 1f, 0, 0, 0, 0.2f, 0.1f) },
            { "LC_fels", new P(0.5412f, 0.5137f, 0.4706f, 0, 0, 0, 0.9f, 0) },
            { "LC_felsdkl", new P(0.302f, 0.2863f, 0.2627f, 0, 0, 0, 0.9f, 0) },
            { "LC_glas", new P(0.498f, 0.8471f, 1f, 0.0747f, 0.1271f, 0.15f, 0.05f, 0.1f) },
            { "LC_glut", new P(1f, 0.4784f, 0.102f, 1f, 0.4784f, 0.102f, 0.4f, 0) },
            { "LC_gummi", new P(0.102f, 0.1137f, 0.1333f, 0, 0, 0, 0.85f, 0) },
            { "LC_hell", new P(0.7686f, 0.8f, 0.8471f, 0, 0, 0, 0.5f, 0.2f) },
            { "LC_kaltlicht", new P(0.9176f, 0.9569f, 1f, 0.9176f, 0.9569f, 1f, 0.3f, 0) },
            { "LC_kiste1", new P(0.851f, 0.6431f, 0.2549f, 0, 0, 0, 0.7f, 0.1f) },
            { "LC_kiste2", new P(0.3569f, 0.5608f, 0.851f, 0, 0, 0, 0.5f, 0.3f) },
            { "LC_kiste3", new P(0.6039f, 0.8196f, 0.7686f, 0, 0, 0, 0.3f, 0.2f) },
            { "LC_kometeis", new P(0.8745f, 0.9373f, 1f, 0.039f, 0.0539f, 0.0625f, 0.25f, 0.05f) },
            { "LC_kristall", new P(0.7255f, 0.549f, 1f, 0.4534f, 0.3431f, 0.625f, 0.1f, 0.1f) },
            { "LC_kupfer", new P(0.7216f, 0.451f, 0.2f, 0, 0, 0, 0.35f, 0.9f) },
            { "LC_lampe", new P(1f, 0.9412f, 0.7843f, 1f, 0.9412f, 0.7843f, 0.3f, 0) },
            { "LC_lavastein", new P(0.1647f, 0.1412f, 0.1333f, 0, 0, 0, 0.85f, 0) },
            { "LC_mint", new P(0.4353f, 0.949f, 0.8235f, 0.4353f, 0.949f, 0.8235f, 0.3f, 0) },
            { "LC_orange", new P(1f, 0.451f, 0.102f, 0, 0, 0, 0.45f, 0.3f) },
            { "LC_p_algenoel", new P(0.2902f, 0.8157f, 0.4784f, 0, 0, 0, 0.45f, 0.25f) },
            { "LC_p_barren", new P(0.9333f, 0.949f, 0.9647f, 0, 0, 0, 0.3f, 0.9f) },
            { "LC_p_basaltfaser", new P(0.4784f, 0.3765f, 0.3137f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_biokristall", new P(0.4784f, 1f, 0.7843f, 0.1435f, 0.3f, 0.2353f, 0.08f, 0.1f) },
            { "LC_p_biosprit", new P(0.7843f, 0.8157f, 0.2902f, 0, 0, 0, 0.45f, 0.25f) },
            { "LC_p_bitumen", new P(0.3529f, 0.2902f, 0.1882f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_brennelement", new P(0.7843f, 1f, 0.6039f, 0.2941f, 0.375f, 0.2265f, 0.3f, 0.9f) },
            { "LC_p_brennstab", new P(0.4784f, 1f, 0.4784f, 0.1794f, 0.375f, 0.1794f, 0.4f, 0.2f) },
            { "LC_p_chemie", new P(0.6078f, 0.8275f, 0.3529f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_diamant", new P(1f, 1f, 1f, 0.3f, 0.3f, 0.3f, 0.08f, 0.1f) },
            { "LC_p_druckeis", new P(0.6588f, 0.8157f, 0.9333f, 0.0312f, 0.0431f, 0.05f, 0.12f, 0.05f) },
            { "LC_p_eisblock", new P(0.8118f, 0.9098f, 1f, 0.0312f, 0.0431f, 0.05f, 0.12f, 0.05f) },
            { "LC_p_eiskern", new P(0.8784f, 0.9098f, 1f, 0.0312f, 0.0431f, 0.05f, 0.12f, 0.05f) },
            { "LC_p_elektrolyt", new P(0.7529f, 0.9412f, 1f, 0, 0, 0, 0.45f, 0.25f) },
            { "LC_p_frostglas", new P(0.8745f, 0.9569f, 1f, 0.2624f, 0.2871f, 0.3f, 0.08f, 0.1f) },
            { "LC_p_graphen", new P(0.1647f, 0.1804f, 0.2039f, 0, 0, 0, 0.3f, 0.9f) },
            { "LC_p_graphit", new P(0.2275f, 0.2118f, 0.2039f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_hitzekachel", new P(0.9098f, 0.8157f, 0.6902f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_hitzelack", new P(1f, 0.6902f, 0.4392f, 0, 0, 0, 0.45f, 0.25f) },
            { "LC_p_isotop", new P(0.5412f, 1f, 0.3529f, 0.2029f, 0.375f, 0.1324f, 0.4f, 0.2f) },
            { "LC_p_kohlefaser", new P(0.2275f, 0.2275f, 0.2275f, 0, 0, 0, 0.3f, 0.9f) },
            { "LC_p_kondensat", new P(0.6588f, 0.9098f, 0.9412f, 0, 0, 0, 0.45f, 0.25f) },
            { "LC_p_kristall", new P(0.7255f, 0.549f, 1f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_kryolinse", new P(0.6235f, 0.8784f, 1f, 0.1871f, 0.2635f, 0.3f, 0.08f, 0.1f) },
            { "LC_p_kryostahl", new P(0.7529f, 0.8157f, 0.9098f, 0, 0, 0, 0.3f, 0.9f) },
            { "LC_p_kuehlmittel", new P(0.498f, 0.8784f, 0.7843f, 0, 0, 0, 0.45f, 0.25f) },
            { "LC_p_lavaziegel", new P(0.4157f, 0.2275f, 0.1647f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_magnetkristall", new P(0.5608f, 0.6902f, 1f, 0.1682f, 0.2071f, 0.3f, 0.08f, 0.1f) },
            { "LC_p_magnetrohling", new P(0.4157f, 0.4784f, 0.6039f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_marsstahl", new P(0.7843f, 0.4392f, 0.2902f, 0, 0, 0, 0.3f, 0.9f) },
            { "LC_p_metall", new P(0.6039f, 0.6392f, 0.6706f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_metallBrick", new P(0.4902f, 0.5294f, 0.5647f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_methankristall", new P(0.9412f, 0.9098f, 0.8784f, 0.2824f, 0.2729f, 0.2635f, 0.08f, 0.1f) },
            { "LC_p_methanol", new P(1f, 0.8118f, 0.4157f, 0, 0, 0, 0.45f, 0.25f) },
            { "LC_p_ozeaneis", new P(0.749f, 0.8941f, 0.9569f, 0.0312f, 0.0431f, 0.05f, 0.12f, 0.05f) },
            { "LC_p_pellet", new P(0.6039f, 1f, 0.3529f, 0.2265f, 0.375f, 0.1324f, 0.4f, 0.2f) },
            { "LC_p_polymer", new P(0.4353f, 0.949f, 0.8235f, 0.1632f, 0.3559f, 0.3088f, 0.4f, 0.2f) },
            { "LC_p_reaktorkern", new P(0.4902f, 1f, 0.4157f, 0.1838f, 0.375f, 0.1559f, 0.3f, 0.9f) },
            { "LC_p_reinkristall", new P(0.8863f, 0.8314f, 1f, 0.2659f, 0.2494f, 0.3f, 0.08f, 0.1f) },
            { "LC_p_reinwasser", new P(0.749f, 0.9098f, 1f, 0, 0, 0, 0.45f, 0.25f) },
            { "LC_p_rohdiamant", new P(0.9098f, 0.9098f, 0.9412f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_rotharz", new P(0.6275f, 0.251f, 0.1882f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_salzblock", new P(0.9412f, 0.9255f, 0.8784f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_schwefelsaeure", new P(0.949f, 0.8863f, 0.2353f, 0, 0, 0, 0.45f, 0.25f) },
            { "LC_p_schwefelstahl", new P(0.8784f, 0.8157f, 0.3765f, 0, 0, 0, 0.3f, 0.9f) },
            { "LC_p_sednit", new P(0.8471f, 0.4157f, 0.2902f, 0, 0, 0, 0.3f, 0.9f) },
            { "LC_p_staub", new P(0.6431f, 0.6118f, 0.5569f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_staubBrick", new P(0.8118f, 0.7647f, 0.6784f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_stickstoffblock", new P(0.9412f, 0.8471f, 0.8784f, 0.0312f, 0.0431f, 0.05f, 0.12f, 0.05f) },
            { "LC_p_strahlstahl", new P(0.6039f, 0.6588f, 0.6039f, 0, 0, 0, 0.3f, 0.9f) },
            { "LC_p_superionik", new P(0.4784f, 0.8471f, 1f, 0.1435f, 0.2541f, 0.3f, 0.08f, 0.1f) },
            { "LC_p_supraleiter", new P(0.6235f, 0.7686f, 1f, 0, 0, 0, 0.3f, 0.9f) },
            { "LC_p_tholinharz", new P(0.7529f, 0.4784f, 0.251f, 0, 0, 0, 0.45f, 0.25f) },
            { "LC_p_tholinziegel", new P(0.6039f, 0.4157f, 0.2902f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_titanleg", new P(0.8471f, 0.8157f, 0.7529f, 0, 0, 0, 0.3f, 0.9f) },
            { "LC_p_torfblock", new P(0.2902f, 0.2275f, 0.1412f, 0, 0, 0, 0.75f, 0.05f) },
            { "LC_p_treibstoff", new P(1f, 0.6039f, 0.2902f, 0, 0, 0, 0.45f, 0.25f) },
            { "LC_platte", new P(0.2902f, 0.3137f, 0.3765f, 0, 0, 0, 0.7f, 0.2f) },
            { "LC_rost", new P(0.5412f, 0.2902f, 0.1647f, 0, 0, 0, 0.8f, 0.4f) },
            { "LC_rot", new P(0.8863f, 0.3333f, 0.2275f, 0, 0, 0, 0.45f, 0.3f) },
            { "LC_russ", new P(0.0824f, 0.0745f, 0.0588f, 0, 0, 0, 0.95f, 0) },
            { "LC_schleim", new P(0.498f, 0.749f, 0.2275f, 0.0498f, 0.0749f, 0.0227f, 0.2f, 0) },
            { "LC_schwefel", new P(0.9098f, 0.8157f, 0.251f, 0, 0, 0, 0.7f, 0) },
            { "LC_solar", new P(0.149f, 0.2667f, 0.498f, 0, 0, 0, 0.15f, 0.7f) },
            { "LC_stahl", new P(0.5412f, 0.5765f, 0.6392f, 0, 0, 0, 0.35f, 0.85f) },
            { "LC_visier", new P(1f, 0.702f, 0.2784f, 0, 0, 0, 0.1f, 0.9f) },
            { "LC_warn", new P(1f, 0.3529f, 0.2902f, 1f, 0.3529f, 0.2902f, 0.3f, 0) },
            { "LC_weiss", new P(0.8745f, 0.898f, 0.9333f, 0, 0, 0, 0.55f, 0.1f) },
        };
        /// <summary>Material zum Blender-Namen. LC_akzent = Planeten-/Fahrzeugfarbe, LC_akzent2 = zweite Akzentfarbe; Leuchtmaterialien bekommen HDR-Leuchten fürs Bloom.</summary>
        /// <summary>Ein Material aus Fabians Palette (LC_stahl, LC_dunkel …) für selbst gebaute Teile, damit sie zu den Modellen passen.</summary>
        public static Material PalMat(string name, Color acc) { var tmp = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; return Remap(tmp, acc, acc); }
        public static bool HasBelt => Has("Foerderband/band_stuetze");
        static Material Remap(Material src, Color acc, Color acc2)
        {
            string n = src ? src.name.Replace(" (Instance)", "") : ""; int dot = n.IndexOf('.'); if (dot > 0) n = n.Substring(0, dot); // „LC_stahl.001“ → „LC_stahl“
            string key = n + "|" + ColorUtility.ToHtmlStringRGB(acc) + ColorUtility.ToHtmlStringRGB(acc2);
            if (MatCache.TryGetValue(key, out var m)) return m;
            if (Pal.TryGetValue(n, out var p))
            {
                Color c = n == "LC_akzent" ? acc : n == "LC_akzent2" ? acc2 : p.c; float sm = 1 - p.rough;
                float ek = Mathf.Max(p.e.r, Mathf.Max(p.e.g, p.e.b));
                if (ek > .5f) m = MeshKit.Mat(c * .4f, 0, sm, p.e * 2.4f);            // Lampen, Glut, Warnlicht: kräftig leuchtend
                else if (ek > .005f) m = MeshKit.Mat(c, p.metal, sm, p.e * 2.2f);       // Glas, Kristalle, Eis: sanfter Schimmer
                else m = MeshKit.Mat(c, p.metal, sm);
            }
            else m = MeshKit.Mat(src && src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src && src.HasProperty("_Color") ? src.GetColor("_Color") : Color.gray, .3f, .5f);
            m.name = n; MatCache[key] = m; return m;
        }
        static bool Glows(Material m) => m && m.IsKeywordEnabled("_EMISSION") && m.name != "LC_glas" && !m.name.StartsWith("LC_p_") && m.name != "LC_kristall" && m.name != "LC_kometeis" && m.name != "LC_schleim";

        /// <summary>Setzt ein LC-Modell unter parent (Maßstab 1 = Meter wie im Spiel).</summary>
        public static GameObject Spawn(string path, Transform parent, Vector3 pos, float yaw, Color acc, Color? acc2 = null, float scale = 1)
        {
            var pf = Prefab(path); if (!pf) return null;
            var g = Object.Instantiate(pf); g.name = path; g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localRotation = Quaternion.Euler(0, yaw, 0) * pf.transform.localRotation; g.transform.localScale = pf.transform.localScale * scale;
            foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                var ms = r.sharedMaterials; bool allGlow = ms.Length > 0;
                for (int i = 0; i < ms.Length; i++) { ms[i] = Remap(ms[i], acc, acc2 ?? acc); allGlow &= Glows(ms[i]); }
                r.sharedMaterials = ms; r.shadowCastingMode = allGlow ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On; // Lampen werfen keinen Schatten
            }
            return g;
        }

        // ---------- Gebackene Modelle (ein Mesh, je Material ein Teilmesh) für GPU-Instancing ----------
        public class Baked { public Mesh mesh; public Material[] mats; public Vector3 size; }
        static readonly Dictionary<string, Baked> BakeCache = new Dictionary<string, Baked>();
        public static Baked Bake(string path)
        {
            if (BakeCache.TryGetValue(path, out var bk)) return bk;
            var g = Spawn(path, null, Vector3.zero, 0, Hex("#ffbf4d")); if (!g) { BakeCache[path] = null; return null; }
            var groups = new Dictionary<Material, List<CombineInstance>>(); var order = new List<Material>(); var w2l = g.transform.worldToLocalMatrix;
            foreach (var mf in g.GetComponentsInChildren<MeshFilter>())
            {
                var r = mf.GetComponent<MeshRenderer>(); if (!r || !mf.sharedMesh || !mf.sharedMesh.isReadable) continue; var ms = r.sharedMaterials;
                for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
                {
                    var m = ms[Mathf.Min(s, ms.Length - 1)]; if (!groups.TryGetValue(m, out var l)) { l = new List<CombineInstance>(); groups[m] = l; order.Add(m); }
                    l.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = s, transform = w2l * mf.transform.localToWorldMatrix });
                }
            }
            Object.Destroy(g); if (order.Count == 0) { BakeCache[path] = null; return null; }
            var parts = new List<CombineInstance>();
            foreach (var m in order) { var pm = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; pm.CombineMeshes(groups[m].ToArray(), true, true); parts.Add(new CombineInstance { mesh = pm, transform = Matrix4x4.identity }); }
            var mesh = new Mesh { name = path, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.CombineMeshes(parts.ToArray(), false, false); mesh.RecalculateBounds();
            foreach (var p in parts) Object.Destroy(p.mesh);
            var mats = order.Select(m => { var c = new Material(m); c.enableInstancing = true; return c; }).ToArray();
            bk = new Baked { mesh = mesh, mats = mats, size = mesh.bounds.size }; BakeCache[path] = bk; return bk;
        }
        /// <summary>Warenmodell für ein Produkt (Browser-Schlüssel), null wenn es keins gibt.</summary>
        public static Baked Ware(string key) => key != null && Has("Produkte/ware_" + key) ? Bake("Produkte/ware_" + key) : null;
        /// <summary>Außenlager (Silo, Andockplätze, Pumpenhaus) in Spielgröße.</summary>
        public static Models.BuildingParts Outpost(Color acc)
        {
            if (!Has("Welt/aussenlager")) return null; var bp = new Models.BuildingParts(); var g = new GameObject("Außenlager"); bp.root = g;
            Spawn("Welt/aussenlager", g.transform, Vector3.zero, 0, acc, null, 1.65f);
            bp.lamp = new Material(MeshKit.Glow(Hex("#3fe58a"), 2.5f)); var top = Top(g.transform, g); MeshKit.Ball(g.transform, bp.lamp, new Vector3(0, top + .35f, 0), .32f); return bp;
        }
        /// <summary>Abkippgrube vor einem Andockplatz der Entladeanlage (Ausrichtung: Schurre zeigt zur Anlage).</summary>
        public static bool Pit(Transform t, Vector3 pos, Vector3 toCenter)
        {
            if (!Has("Welt/grube")) return false; float yaw = Mathf.Atan2(toCenter.x, toCenter.z) * Mathf.Rad2Deg;
            var g = Spawn("Welt/grube", t, pos, 0, Hex("#ffbf4d"), null, .9f); g.transform.localRotation = Quaternion.Euler(0, yaw, 0) * g.transform.localRotation; return true;
        }
        /// <summary>Astronaut als ganzes Modell (ohne eigene Beine – er wippt beim Laufen).</summary>
        public static GameObject Astronaut(Transform parent) => Has("Welt/astronaut") ? Spawn("Welt/astronaut", parent, Vector3.zero, 0, Hex("#ffbf4d"), null, .74f) : null;

        /// <summary>Bodenteil-Modelle je Schmutzart (Browser-Schlüssel deb).</summary>
        public static Baked Debris(string deb, int variant)
        {
            string b; int n;
            switch (deb) { case "scrap": b = "schrott"; n = 3; break; case "sat": b = "satellit"; n = 2; break; case "crystal": b = "kristall"; n = 2; break; case "sulfur": b = "schwefel"; n = 2; break;
                case "barrel": b = "fass"; n = 2; break; case "ice": b = "eis"; n = 2; break; case "lava": b = "lava"; n = 2; break; case "goo": b = "schleim"; n = 2; break; default: b = "fels"; n = 3; break; }
            return Bake("Schmutz/schmutz_" + b + "_" + (1 + Mathf.Abs(variant) % n));
        }

        // ---------- bewegliche Teile ----------
        static Transform Part(GameObject g, string suffix)
        {
            foreach (var t in g.GetComponentsInChildren<Transform>(true)) if (t != g.transform && t.name.EndsWith(suffix)) return t;
            return null;
        }
        static List<Transform> Parts(GameObject g, string infix)
        {
            var l = new List<Transform>(); foreach (var t in g.GetComponentsInChildren<Transform>(true)) if (t != g.transform && t.name.Contains(infix)) l.Add(t);
            l.Sort((a, b) => string.CompareOrdinal(a.name, b.name)); return l;
        }
        /// <summary>Legt ein Drehteil in einen Halter am Drehpunkt des Teils. Die Halter-Achse localAxis zeigt entlang worldAxis –
        /// so drehen die vorhandenen Animationen (Rotate um up bzw. forward) jedes Teil um seine richtige Achse.</summary>
        static Transform Spinner(Transform part, Vector3 worldAxis, Vector3 localAxis)
        {
            var h = new GameObject(part.name + "_Achse").transform; h.SetParent(part.parent, false);
            h.position = part.position; h.rotation = Quaternion.FromToRotation(localAxis, worldAxis.normalized);
            part.SetParent(h, true); return h;
        }
        /// <summary>Drehachse aus der Form: Scheibe (Rad, Bürste, Schaufelrad) → dünnste Achse, Walze (Fräse) → längste Achse, sonst senkrecht.</summary>
        static Vector3 AxisOf(Transform part, Vector3 fallback)
        {
            var mf = part.GetComponentInChildren<MeshFilter>(); if (!mf || !mf.sharedMesh) return fallback;
            var e = Vector3.Scale(mf.sharedMesh.bounds.extents, mf.transform.lossyScale); e = new Vector3(Mathf.Abs(e.x), Mathf.Abs(e.y), Mathf.Abs(e.z));
            int mn = e.x <= e.y && e.x <= e.z ? 0 : e.y <= e.z ? 1 : 2, mx = e.x >= e.y && e.x >= e.z ? 0 : e.y >= e.z ? 1 : 2, md = 3 - mn - mx;
            Vector3 Ax(int i) => mf.transform.TransformDirection(i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward);
            if (e[mn] < e[md] * .6f) return Ax(mn);
            if (e[mx] > e[md] * 1.6f) return Ax(mx);
            return fallback;
        }
        /// <summary>Oberkante des Modells im Raum von space.</summary>
        static float Top(Transform space, GameObject g)
        {
            float top = 0; foreach (var r in g.GetComponentsInChildren<Renderer>()) { var b = r.bounds; top = Mathf.Max(top, space.InverseTransformPoint(b.center + Vector3.up * b.extents.y).y); }
            return top;
        }

        // ---------- Gebäude ----------
        public static Models.BuildingParts Building(string id, float fp, Color acc, Color acc2)
        {
            string path = "Gebaeude/" + id; if (!Has(path)) return null;
            var bp = new Models.BuildingParts(); var g = new GameObject(id); bp.root = g; var t = g.transform;
            var m = Spawn(path, t, Vector3.zero, 0, acc, acc2); bp.lamp = MeshKit.Glow(Hex("#6ff2d2"), 2.5f);
            var rk = Part(m, "_Rakete"); if (rk) { rk.SetParent(t, true); rk.localRotation = Quaternion.identity; rk.localPosition = new Vector3(0, rk.localPosition.y, 0); bp.rocket = rk; bp.rocketY = rk.localPosition.y; }
            var kb = Part(m, "_Kolben"); if (kb) { var h = new GameObject("Kolbenhalter").transform; h.SetParent(t, false); kb.SetParent(h, true); bp.piston = h; bp.pistonY = 0; } // Halter fährt auf/ab, der Kolben behält seine Lage
            var dr = Part(m, "_Dreh"); if (dr) bp.spin = Spinner(dr, t.up, Vector3.up); // Schüssel, Radar, Kran: dreht sich um die Hochachse
            return bp;
        }

        // ---------- Bohrturm und Erzbagger ----------
        public static Models.BuildingParts Rig(bool bag, Color acc)
        {
            string path = "Anlagen/" + (bag ? "bagger" : "rig"); if (!Has(path)) return null;
            var bp = new Models.BuildingParts(); var g = new GameObject(bag ? "Erzbagger" : "Bohrturm"); bp.root = g; var t = g.transform;
            var m = Spawn(path, t, Vector3.zero, bag ? FrontYaw : 0, acc); bp.lamp = new Material(MeshKit.Glow(Hex("#3fe58a"), 2.5f));
            var dr = Part(m, "_Dreh");
            if (dr) { if (bag) bp.spin = Spinner(dr, AxisOf(dr, t.right), Vector3.forward); else bp.spinUp = Spinner(dr, t.up, Vector3.up); } // Schaufelrad dreht um seine Achse, Bohrgestänge um die Hochachse
            var lamp = MeshKit.Ball(t, bp.lamp, new Vector3(0, Top(t, m) + .3f, 0), .3f); lamp.name = "Statuslampe"; // Farbe setzt WorldView (läuft/Störung/wartet)
            var lr = lamp.GetComponent<MeshRenderer>(); if (lr) lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return bp;
        }

        // ---------- Fahrzeuge, Suchtrupp, Drohnen ----------
        /// <summary>Erkennungsfarbe je Fahrzeugtyp (LC_akzent) – wie bisher, damit man die Typen im Gewimmel unterscheidet.</summary>
        static Color BodyOf(VehDef T, Color acc) => T.key == "staubi" ? Hex("#d9dee6") : T.key == "buggy" ? Hex("#ffbf4d") : T.key == "sauger" ? Hex("#58cdb4") : T.key == "truck" ? Hex("#e2553a") :
            T.key == "schwebe" ? Hex("#6fe0ff") : T.key == "magnetgleiter" ? Hex("#b79cff") : acc;
        /// <summary>Der FBX-Export dreht die Modelle um 180° (Vorderseite −Z, „Eigene Modelle prüfen“ meldet es). Hier ausgeglichen, damit Fahrzeuge vorwärts fahren.
        /// Wird in Blender mit FRONT_FLIP = True neu exportiert, hier 0 eintragen.</summary>
        const float FrontYaw = 180;
        public static Models.VehicleParts Vehicle(VehDef T, Color acc) => VehicleAt("Fahrzeuge/" + T.key, T.name, BodyOf(T, acc), T.fly);
        public static Models.VehicleParts Team(Color acc) => VehicleAt("Fahrzeuge/suchtrupp", "Suchtrupp", acc, false);
        static Models.VehicleParts VehicleAt(string path, string name, Color body, bool fly)
        {
            if (!Has(path)) return null;
            var vp = new Models.VehicleParts(); var g = new GameObject(name); vp.root = g; var t = g.transform;
            var m = Spawn(path, t, new Vector3(0, fly ? .6f : 0, 0), FrontYaw, body); // Schwebefahrzeuge gleiten knapp über dem Boden (auch über Seen)
            var ws = new List<Transform>(); foreach (var w in Parts(m, "_Rad_")) ws.Add(Spinner(w, t.right, Vector3.up)); vp.wheels = ws.ToArray(); // Räder drehen um die Querachse
            var tool = Part(m, "_Werkzeug"); if (tool) vp.tool = Spinner(tool, AxisOf(tool, t.up), Vector3.up);
            return vp;
        }
        public static Models.DroneParts Drone(Color acc, bool heavy)
        {
            string path = "Drohnen/" + (heavy ? "lastdrohne" : "drohne"); if (!Has(path)) return null;
            var dp = new Models.DroneParts(); var g = new GameObject(heavy ? "Lastdrohne" : "Drohne"); dp.root = g; var t = g.transform;
            var m = Spawn(path, t, new Vector3(0, heavy ? -.8f : -.4f, 0), 0, acc); // Drehpunkt der Drohne etwa in der Mitte (wie bisher)
            var rs = new List<Transform>(); foreach (var r in Parts(m, "_Rotor_")) rs.Add(Spinner(r, t.up, Vector3.up)); dp.rotors = rs.ToArray();
            dp.cargo = Part(m, "_Ladung");
            return dp;
        }

        // ---------- Welt und Ereignisse ----------
        public static GameObject Cart() { if (!Has("Welt/sammelwagen")) return null; var g = new GameObject("Sammelwagen"); Spawn("Welt/sammelwagen", g.transform, Vector3.zero, FrontYaw, Hex("#ffbf4d")); return g; }
        /// <summary>Bauplatz für ein noch nicht gebautes Gebäude (Modell passt auf Grundkreis 2,5 m – wird auf fp skaliert).</summary>
        public static GameObject Site(float fp) { if (!Has("Welt/bauplatz")) return null; var g = new GameObject("Bauplatz"); Spawn("Welt/bauplatz", g.transform, Vector3.zero, 0, Hex("#ffbf4d"), null, Mathf.Clamp(fp / 2.5f, .5f, 1.6f)); return g; }
        /// <summary>Ampelkopf (Gehäuse + Lichtkugel). Die Kugel bekommt das Material lamp, dessen Farbe das Spiel umschaltet.</summary>
        public static GameObject AmpelHead(Transform parent, Vector3 pos, Material lamp)
        {
            if (!Has("Welt/ampel")) return null; var m = Spawn("Welt/ampel", parent, pos, 0, Hex("#ffbf4d"));
            var li = Part(m, "_Licht"); if (li) foreach (var r in li.GetComponentsInChildren<Renderer>()) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = lamp; r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            return m;
        }
        public static GameObject Event(string id, Transform parent) => Has("Ereignisse/" + id) ? Spawn("Ereignisse/" + id, parent, Vector3.zero, 0, Hex("#ffbf4d")) : null;

        /// <summary>Felsen und Krater auf der Oberfläche (nur Deko). Liefert false, wenn die Modelle fehlen.</summary>
        public static bool Decorate(Transform parent, PlanetData pd, int seed, int count)
        {
            if (!Has("Welt/deko_fels_1")) return false; var r = new System.Random(seed); string[] deco = { "Welt/deko_fels_1", "Welt/deko_fels_2", "Welt/deko_fels_3", "Welt/deko_fels_1", "Welt/deko_fels_2", "Ereignisse/meteorit" }; /* deko_krater raus (Fabian 01.11.): Ringwall ohne ausgehobenen Boden sah wie ein Reifen aus */
            for (int i = 0; i < count; i++)
            {
                var cl = pd.cells[r.Next(pd.N)]; if (cl.IsBase || cl.ring <= 2) continue;
                float a = (float)r.NextDouble() * Mathf.PI * 2, d = (float)r.NextDouble() * cl.H * .8f; var dir = pd.ToDir(cl, Mathf.Cos(a) * d, Mathf.Sin(a) * d);
                string m = deco[r.Next(deco.Length)]; bool crater = m.EndsWith("krater"); float s = crater ? .7f + (float)r.NextDouble() * 1.3f : .25f + (float)r.NextDouble() * .55f;
                var go = Spawn(m, parent, Vector3.zero, 0, Color.gray, null, s); if (!go) continue;
                go.transform.position = pd.Surf(dir, -(crater ? .05f : .1f)); go.transform.rotation = Quaternion.FromToRotation(Vector3.up, pd.SurfNormal(dir)) * Quaternion.Euler(0, (float)r.NextDouble() * 360, 0);
            }
            return true;
        }
    }
}
