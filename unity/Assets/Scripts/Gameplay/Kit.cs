// Kenney-Modelle (CC0, Resources/Kenney): laden, auf Zielgröße bringen und einfärben.
// Fehlt ein Modell, liefern die Funktionen null und Models.cs baut das alte prozedurale Modell.
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public static class Kit
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { Cache.Clear(); MatCache.Clear(); }
        static readonly Dictionary<string, GameObject> Cache = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, Material> MatCache = new Dictionary<string, Material>();
        static Color Hex(string h) => GameData.Hex(h);

        static GameObject Prefab(string path)
        {
            if (Cache.TryGetValue(path, out var p)) return p;
            p = Resources.Load<GameObject>("Kenney/" + path); Cache[path] = p; return p;
        }
        public static bool Has(string path) => Prefab(path) != null;

        /// <summary>Farbe je Kenney-Materialname (Space Kit): weiß, grau, Akzent, dunkel, Gestein.</summary>
        static Material Remap(Material src, Color acc)
        {
            string n = src ? src.name.Replace(" (Instance)", "") : "";
            var tex = src && src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : src && src.HasProperty("_MainTex") ? src.GetTexture("_MainTex") : null;
            string key = n + "|" + (tex ? tex.name : "") + "|" + ColorUtility.ToHtmlStringRGB(acc);
            if (MatCache.TryGetValue(key, out var m)) return m;
            if (tex)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", .35f); m.SetFloat("_Metallic", .15f);
            }
            else
            {
                string l = n.ToLowerInvariant();
                if (l.Contains("metalred") || l.Contains("red")) m = MeshKit.Mat(acc, .35f, .55f);
                else if (l.Contains("metaldark")) m = MeshKit.Mat(Hex("#7d8594"), .7f, .55f);
                else if (l.Contains("metal")) m = MeshKit.Mat(Hex("#e4e8ee"), .25f, .6f);
                else if (l.Contains("dark")) m = MeshKit.Mat(Hex("#2a3039"), .4f, .45f);
                else if (l.Contains("crystal")) m = MeshKit.Mat(Hex("#6ff2d2") * .5f, .1f, .9f, Hex("#6ff2d2") * 1.2f);
                else if (l.Contains("rockdark")) m = MeshKit.Mat(Hex("#4a4642"), 0, .15f);
                else if (l.Contains("rock")) m = MeshKit.Mat(Hex("#7a746c"), 0, .15f);
                else if (l.Contains("glass")) m = MeshKit.Mat(Hex("#7fd8ff") * .35f, .1f, .9f, Hex("#7fd8ff") * .7f);
                else m = MeshKit.Mat(src && src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : Color.gray, .3f, .5f);
            }
            MatCache[key] = m; return m;
        }

        /// <summary>Setzt ein Kenney-Modell unter parent. size = Zieldurchmesser (größte waagrechte Ausdehnung), height = optional Zielhöhe statt dessen.</summary>
        public static GameObject Spawn(string path, Transform parent, float size, Vector3 pos, float yaw, Color acc, float height = -1, string name = null)
        {
            var pf = Prefab(path); if (!pf) return null;
            var holder = new GameObject(name ?? path); holder.transform.SetParent(parent, false); holder.transform.localPosition = pos; holder.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var g = Object.Instantiate(pf); g.name = "Modell"; g.transform.SetParent(holder.transform, false); g.transform.localPosition = Vector3.zero; g.transform.localRotation = pf.transform.localRotation; g.transform.localScale = pf.transform.localScale;
            foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var rs = g.GetComponentsInChildren<Renderer>();
            foreach (var r in rs) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = Remap(ms[i], acc); r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            // Größe und Lage aus den Grenzen im Halter-Raum
            var b = LocalBounds(holder.transform, rs); if (b.size.sqrMagnitude < 1e-8f) return holder;
            float k = height > 0 ? height / Mathf.Max(1e-4f, b.size.y) : size / Mathf.Max(1e-4f, Mathf.Max(b.size.x, b.size.z));
            g.transform.localScale *= k; b = LocalBounds(holder.transform, rs);
            g.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            return holder;
        }
        static Bounds LocalBounds(Transform space, Renderer[] rs)
        {
            bool any = false; var b = new Bounds();
            foreach (var r in rs)
            {
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue; var mb = mf.sharedMesh.bounds; var m = space.worldToLocalMatrix * r.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++) { var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)); var p = m.MultiplyPoint3x4(c); if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p); }
            }
            return b;
        }
        static float Top(GameObject g) { if (!g) return 0; var rs = g.GetComponentsInChildren<Renderer>(); var b = LocalBounds(g.transform.parent, rs); return b.max.y; }

        // ---------------- Gebäude ----------------
        public static Models.BuildingParts Building(string id, float fp, Color acc)
        {
            if (!Has("space/hangar_largeA")) return null;
            var bp = new Models.BuildingParts(); var g = new GameObject(id); bp.root = g; var t = g.transform; float D = fp * 1.9f;
            var pad = MeshKit.Mat(Hex("#4a5060"), .2f, .3f); MeshKit.Cyl(t, pad, new Vector3(0, .06f, 0), fp, fp + .15f, .12f, 32); MeshKit.Cyl(t, MeshKit.Mat(acc, .3f, .55f), new Vector3(0, .13f, 0), fp - .05f, fp, .03f, 32);
            var mint = MeshKit.Glow(Hex("#6ff2d2"), 2.5f); bp.lamp = mint; var y0 = new Vector3(0, .14f, 0);
            switch (id)
            {
                case "hq": Spawn("space/hangar_largeA", t, D, y0, 0, acc); Spawn("space/satelliteDish", t, D * .22f, new Vector3(fp * .45f, Top(t.GetChild(t.childCount - 1).gameObject) + .1f, fp * .2f), 30, acc); break;
                case "roh": Spawn("factory/hopper-high-round", t, D * .48f, new Vector3(-fp * .45f, .14f, 0), 0, acc); Spawn("factory/hopper-high-round", t, D * .48f, new Vector3(fp * .45f, .14f, 0), 0, acc); break;
                case "prod": Spawn("space/hangar_smallA", t, D, y0, 0, acc); break;
                case "u1": Spawn("factory/hopper-square", t, D * .75f, y0, 0, acc); break;
                case "u2": Spawn("factory/hopper-high-square", t, D * .7f, y0, 0, acc); Spawn("space/supports_low", t, D * .4f, new Vector3(fp * .6f, .14f, 0), 0, acc); break;
                case "u3": Spawn("factory/hopper-high-square", t, D * .45f, new Vector3(-fp * .35f, .14f, 0), 0, acc); Spawn("factory/hopper-high-square", t, D * .45f, new Vector3(fp * .35f, .14f, 0), 0, acc); break;
                case "presse":
                    { var mc = Spawn("factory/machine-fortified", t, D * .8f, y0, 0, acc); float top = Top(mc); var p = Spawn("factory/piston-round", t, D * .3f, new Vector3(0, top, 0), 0, acc); if (p) { bp.piston = p.transform; bp.pistonY = top; } }
                    break;
                case "schmelze": Spawn("factory/machine", t, D * .75f, new Vector3(-fp * .15f, .14f, 0), 0, acc); Spawn("space/chimney_detailed", t, 0, new Vector3(fp * .55f, .14f, fp * .3f), 0, acc, fp * 1.6f); break;
                case "raff": Spawn("space/machine_barrelLarge", t, D * .7f, y0, 0, acc); Spawn("space/chimney", t, 0, new Vector3(fp * .6f, .14f, -fp * .4f), 0, acc, fp * 1.3f); break;
                case "energy": Spawn("space/machine_generatorLarge", t, D * .85f, y0, 0, acc); break;
                case "research": { var d = Spawn("space/satelliteDish_large", t, D * .9f, y0, 0, acc); if (d) bp.spin = d.transform; } break;
                case "werkstatt": Spawn("space/hangar_roundA", t, D, y0, 0, acc); break;
                case "hangar": Spawn("space/hangar_roundGlass", t, D, y0, 0, acc); break;
                case "lifter": Spawn("space/hangar_largeB", t, D, y0, 90, acc); break;
                case "rtg": Spawn("space/machine_generator", t, D * .8f, y0, 0, acc); break;
                case "geo": Spawn("space/structure_closed", t, D * .6f, y0, 0, acc); Spawn("space/chimney_detailed", t, 0, new Vector3(fp * .5f, .14f, 0), 0, acc, fp * 1.8f); break;
                case "reactor": Spawn("space/machine_barrelLarge", t, D * .9f, y0, 0, acc); Spawn("space/chimney_detailed", t, 0, new Vector3(fp * .65f, .14f, fp * .5f), 0, acc, fp * 2f); break;
                case "sell":
                case "port":
                    Spawn("space/platform_large", t, D * .95f, y0, 0, acc);
                    { var pl = t.GetChild(t.childCount - 1).gameObject; float top = Top(pl); var r = Rocket(t, fp * (id == "port" ? 2.2f : 1.7f), acc); r.transform.localPosition = new Vector3(0, top, 0); bp.rocket = r.transform; bp.rocketY = top; }
                    break;
                default: Object.Destroy(g); return null;
            }
            MeshKit.Ball(t, mint, new Vector3(fp * .75f, .4f, -fp * .75f), .22f);
            return bp;
        }

        /// <summary>Rakete aus Kenney-Teilen gestapelt: Triebwerk, Tank, Rumpf, Flossen, Spitze.</summary>
        public static GameObject Rocket(Transform parent, float height, Color acc)
        {
            var g = new GameObject("Rakete"); g.transform.SetParent(parent, false);
            float w = height * .32f, y = 0;
            foreach (var (part, hk) in new[] { ("space/rocket_baseA", .36f), ("space/rocket_fuelA", .12f), ("space/rocket_sidesA", .24f), ("space/rocket_topA", .2f) })
            {
                var p = Spawn(part, g.transform, part.EndsWith("baseA") ? w : w * .62f, new Vector3(0, y, 0), 0, acc, height * hk); if (!p) continue; y += height * hk;
            }
            Spawn("space/rocket_finsA", g.transform, w * 1.15f, Vector3.zero, 45, acc, height * .2f);
            return g;
        }

        // ---------------- Fahrzeuge ----------------
        /// <summary>Bodenfahrzeuge: Rover (klein), Bergbaugleiter (Industriesauger) und Frachter (Truck/Spezial) – die Gleiter schweben knapp über dem Boden.</summary>
        public static Models.VehicleParts Vehicle(VehDef T, Color acc)
        {
            if (!Has("space/rover")) return null;
            var vp = new Models.VehicleParts(); var g = new GameObject(T.name); vp.root = g; var t = g.transform; vp.wheels = new Transform[0];
            string m = T.fly ? "space/craft_speederA" : T.key == "staubi" || T.key == "buggy" ? "space/rover" : T.key == "sauger" ? "space/craft_miner" : T.key == "truck" ? "space/craft_cargoA" : "space/craft_cargoB";
            float len = T.key == "staubi" ? 1.3f : T.key == "buggy" ? 2f : T.size == "m" ? 3f : T.size == "l" ? 4.2f : 2.4f;
            Color body = T.key == "staubi" ? Hex("#d9dee6") : T.key == "buggy" ? Hex("#ffbf4d") : T.key == "sauger" ? Hex("#58cdb4") : T.key == "truck" ? Hex("#e2553a") : acc;
            if (T.fly) body = T.key == "schwebe" ? Hex("#6fe0ff") : Hex("#b79cff");
            var model = Spawn(m, t, len, new Vector3(0, T.fly ? 1.1f : m.Contains("craft") ? .35f : 0, 0), 0, body); // Schwebefahrzeuge fliegen über Seen if (!model) { Object.Destroy(g); return null; }
            // Saugdüse vorne (dreht sich beim Reinigen)
            vp.tool = MeshKit.Cyl(t, MeshKit.Mat(Hex("#8a93a3"), .8f, .6f), new Vector3(0, .22f, len * .52f), .16f * len / 2, .16f * len / 2, len * .7f, 12, new Vector3(0, 0, 90)).transform;
            return vp;
        }
        public static Models.DroneParts Drone(Color acc, bool heavy)
        {
            if (!Has("space/craft_speederD")) return null;
            var dp = new Models.DroneParts(); var g = new GameObject(heavy ? "Lastdrohne" : "Drohne"); dp.root = g; var t = g.transform;
            var m = Spawn(heavy ? "space/craft_cargoB" : "space/craft_speederD", t, heavy ? 2.4f : 2.6f, new Vector3(0, -.3f, 0), 0, acc); if (!m) { Object.Destroy(g); return null; }
            dp.rotors = new Transform[0];
            dp.cargo = MeshKit.Box(t, MeshKit.Mat(acc, .3f, .55f), new Vector3(0, -.55f, 0), new Vector3(.7f, .45f, .7f)).transform;
            MeshKit.Ball(t, MeshKit.Glow(Hex("#6ff2d2"), 3f), new Vector3(0, -.35f, .9f), .14f);
            return dp;
        }
        public static Models.BuildingParts Outpost(Color acc)
        {
            if (!Has("factory/hopper-high-square")) return null;
            var bp = new Models.BuildingParts(); var g = new GameObject("Außenlager"); bp.root = g; var t = g.transform;
            MeshKit.Cyl(t, MeshKit.Mat(Hex("#3a414d"), .2f, .35f), new Vector3(0, .06f, 0), 4.2f, 4.4f, .12f, 32);
            Spawn("factory/hopper-high-round", t, 3.2f, new Vector3(-.8f, .12f, 0), 0, acc); Spawn("factory/machine-connection-pipe", t, 1.6f, new Vector3(1.6f, .12f, -2.2f), 0, acc);
            for (int k = 0; k < 2; k++) MeshKit.Box(t, MeshKit.Glow(Hex("#6ff2d2"), 1.8f), new Vector3(2.9f, .14f, -(k - .5f) * 2), new Vector3(1.6f, .04f, .12f));
            bp.lamp = new Material(MeshKit.Glow(Hex("#3fe58a"), 2.5f)); MeshKit.Ball(t, bp.lamp, new Vector3(-.8f, 5.2f, 0), .3f);
            return bp;
        }
        public static Models.BuildingParts Rig(bool bag, Color acc)
        {
            if (!Has("factory/crane")) return null;
            var bp = new Models.BuildingParts(); var g = new GameObject(bag ? "Erzbagger" : "Bohrturm"); bp.root = g; var t = g.transform;
            MeshKit.Cyl(t, MeshKit.Mat(Hex("#3a414d"), .2f, .35f), new Vector3(0, .05f, 0), 3.2f, 3.4f, .1f, 28);
            if (bag) { var m = Spawn("space/craft_miner", t, 5f, new Vector3(0, .3f, 0), 0, acc); if (m) bp.spin = MeshKit.Cyl(t, MeshKit.Mat(Hex("#ffbf4d"), .3f, .5f), new Vector3(0, .9f, 2.6f), .9f, .9f, .3f, 16, new Vector3(90, 0, 0)).transform; }
            else
            {
                Spawn("factory/structure-yellow-tall", t, 2.2f, new Vector3(0, .1f, 0), 0, acc, 6f);
                var p = Spawn("factory/piston-round", t, 1.1f, new Vector3(0, 3.5f, 0), 0, acc); if (p) { bp.piston = p.transform; bp.pistonY = 3.5f; }
                Spawn("factory/hopper-round", t, 1.6f, new Vector3(1.8f, .1f, 0), 0, acc);
            }
            bp.lamp = new Material(MeshKit.Glow(Hex("#3fe58a"), 2.5f)); MeshKit.Ball(t, bp.lamp, new Vector3(0, bag ? 2.6f : 6.4f, 0), .3f);
            return bp;
        }
        public static GameObject Cart(Color acc) { if (!Has("space/rover")) return null; var g = new GameObject("Sammelwagen"); Spawn("space/rover", g.transform, 1.8f, Vector3.zero, 0, Hex("#ffbf4d")); MeshKit.Ball(g.transform, MeshKit.Glow(Hex("#6ff2d2"), 3f), new Vector3(0, 1.2f, 0), .16f); return g; }

        /// <summary>Krater, Felsen und Meteoritenbrocken auf der Oberfläche (nur Deko).</summary>
        public static void Decorate(Transform parent, PlanetData pd, int seed, int count, Color rockTint)
        {
            if (!Has("space/crater")) return; var r = new System.Random(seed); string[] deco = { "space/crater", "space/craterLarge", "space/rock_largeA", "space/rock_largeB", "space/rocks_smallA", "space/rocks_smallB", "space/meteor_detailed", "space/rock_crystals" };
            for (int i = 0; i < count; i++)
            {
                var cl = pd.cells[r.Next(pd.N)]; if (cl.IsBase || cl.ring <= 2) continue;
                float a = (float)r.NextDouble() * Mathf.PI * 2, d = (float)r.NextDouble() * cl.H * .8f; var dir = pd.ToDir(cl, Mathf.Cos(a) * d, Mathf.Sin(a) * d);
                string m = deco[r.Next(deco.Length)]; bool crater = m.Contains("crater"); float s = crater ? 3 + (float)r.NextDouble() * 6 : .6f + (float)r.NextDouble() * 1.6f;
                var go = Spawn(m, parent, s, Vector3.zero, (float)r.NextDouble() * 360, rockTint); if (!go) continue;
                go.transform.position = pd.Surf(dir, -(crater ? .05f : .1f)); go.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(0, (float)r.NextDouble() * 360, 0);
            }
        }
    }
}
