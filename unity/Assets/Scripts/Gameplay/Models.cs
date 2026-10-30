// Gebäude- und Fahrzeugmodelle, per Code gebaut. Gleiche Form-Idee wie im Browser, aber mit mehr Details:
// Kanten, Paneele, Fenster, Antennen, Leuchten und Planeten-Akzentfarbe.
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public static class Models
    {
        // Grundmaterialien
        static Material M(Color c, float met = .25f, float sm = .5f) => MeshKit.Mat(c, met, sm);
        static Color Hex(string h) => GameData.Hex(h);

        public class BuildingParts { public GameObject root; public Transform piston, rocket, spin; public Material lamp; }

        /// <summary>Gebäude in Modellkoordinaten: x = Plan-x, z = −Plan-y, y nach oben. Grundfläche ~ Radius fp.</summary>
        public static BuildingParts Building(string id, float fp, Color acc, Color acc2)
        {
            var bp = new BuildingParts(); var g = new GameObject(id); bp.root = g; var t = g.transform;
            Material white = M(Hex("#dfe5ee"), .1f, .55f), light = M(Hex("#c4ccd8"), .2f, .5f), dark = M(Hex("#3a4150"), .5f, .45f), steel = M(Hex("#8a93a3"), .8f, .6f),
                pad = M(Hex("#4a5060"), .2f, .3f), A = M(acc, .3f, .55f), A2 = M(acc2, .3f, .55f), glass = MeshKit.Mat(Hex("#7fd8ff") * .35f, .1f, .9f, Hex("#7fd8ff") * .9f),
                warm = MeshKit.Glow(Hex("#ffd38a"), 2.2f), mint = MeshKit.Glow(Hex("#6ff2d2"), 2.5f), red = MeshKit.Glow(Hex("#ff5a4a"), 3f);
            bp.lamp = mint;
            // Bodenplatte für jedes Gebäude
            MeshKit.Cyl(t, pad, new Vector3(0, .08f, 0), fp, fp + .15f, .16f, 32);
            MeshKit.Cyl(t, A, new Vector3(0, .17f, 0), fp - .05f, fp, .04f, 32);
            switch (id)
            {
                case "hq":
                    MeshKit.Box(t, white, new Vector3(0, 1.3f, 0), new Vector3(4.6f, 2.2f, 3.2f));
                    MeshKit.Box(t, light, new Vector3(0, 2.5f, 0), new Vector3(4.8f, .25f, 3.4f));
                    MeshKit.Box(t, A, new Vector3(0, 1.3f, -1.62f), new Vector3(4.62f, .3f, .04f));
                    for (int i = 0; i < 4; i++) MeshKit.Box(t, glass, new Vector3(-1.6f + i * 1.07f, 1.7f, -1.62f), new Vector3(.7f, .55f, .05f));
                    MeshKit.Box(t, dark, new Vector3(1.6f, .85f, -1.62f), new Vector3(1.2f, 1.3f, .06f));
                    MeshKit.Ball(t, light, new Vector3(-1.2f, 2.6f, .3f), 2.2f);
                    MeshKit.Cyl(t, steel, new Vector3(1.6f, 3.4f, .8f), .05f, .07f, 1.8f, 8);
                    MeshKit.Ball(t, red, new Vector3(1.6f, 4.35f, .8f), .22f);
                    for (int i = 0; i < 2; i++) { var p = MeshKit.Box(t, M(Hex("#25407a"), .6f, .8f), new Vector3(-1.1f + i * 2.2f, 3.05f, -.6f), new Vector3(1.9f, .06f, 1.1f), new Vector3(-22, 0, 0)); }
                    break;
                case "roh":
                    for (int i = 0; i < 2; i++)
                    {
                        float x = -1.2f + i * 2.4f;
                        MeshKit.Cyl(t, white, new Vector3(x, 2.1f, 0), 1.25f, 1.25f, 3.8f, 28);
                        MeshKit.Cyl(t, light, new Vector3(x, 4.35f, 0), .35f, 1.3f, .7f, 28);
                        for (int k = 0; k < 3; k++) MeshKit.Cyl(t, A, new Vector3(x, .9f + k * 1.2f, 0), 1.29f, 1.29f, .1f, 28);
                    }
                    MeshKit.Box(t, steel, new Vector3(0, 4.6f, 0), new Vector3(2.6f, .12f, .5f));
                    MeshKit.Box(t, dark, new Vector3(0, .7f, -1.4f), new Vector3(1.4f, 1.2f, .5f));
                    break;
                case "sell":
                    MeshKit.Cyl(t, dark, new Vector3(0, .4f, 0), 2.6f, 2.8f, .5f, 32);
                    MeshKit.Cyl(t, A, new Vector3(0, .68f, 0), 1.6f, 1.6f, .06f, 32);
                    for (int i = 0; i < 4; i++) MeshKit.Box(t, steel, new Vector3(-2.1f, 2.6f, -.6f + i * .4f), new Vector3(.12f, 4.4f, .12f));
                    MeshKit.Box(t, steel, new Vector3(-2.1f, 4.9f, 0), new Vector3(.2f, .2f, 1.6f));
                    MeshKit.Box(t, steel, new Vector3(-1.4f, 4.2f, 0), new Vector3(1.4f, .1f, .2f));
                    var rk = new GameObject("Rakete").transform; rk.SetParent(t, false); rk.localPosition = new Vector3(0, .7f, 0); bp.rocket = rk;
                    MeshKit.Cyl(rk, white, new Vector3(0, 2f, 0), .55f, .6f, 3.6f, 24);
                    MeshKit.Cyl(rk, A, new Vector3(0, 4.3f, 0), .02f, .55f, 1.1f, 24);
                    MeshKit.Cyl(rk, dark, new Vector3(0, .1f, 0), .5f, .35f, .4f, 16);
                    for (int i = 0; i < 4; i++) MeshKit.Box(rk, A2, Quaternion.Euler(0, i * 90, 0) * new Vector3(.62f, .55f, 0), new Vector3(.4f, 1f, .06f), new Vector3(0, i * 90, 0));
                    MeshKit.Box(rk, glass, new Vector3(0, 3.1f, -.56f), new Vector3(.3f, .3f, .05f));
                    break;
                case "prod":
                    MeshKit.Box(t, light, new Vector3(0, .25f, 0), new Vector3(4.4f, .3f, 3.2f));
                    for (int s = 0; s < 3; s++)
                    {
                        float y = .9f + s * 1.1f; MeshKit.Box(t, steel, new Vector3(0, y - .45f, 0), new Vector3(4.2f, .08f, 3f));
                        for (int k = 0; k < 5; k++) MeshKit.Box(t, M(Color.Lerp(acc, Hex("#cfc3ad"), (k + s) % 3 / 2f), .1f, .4f), new Vector3(-1.6f + k * .8f, y - .1f, (k % 2) * .6f - .3f), new Vector3(.6f, .55f, .9f));
                    }
                    for (int i = 0; i < 4; i++) MeshKit.Box(t, dark, new Vector3(i < 2 ? -2.1f : 2.1f, 2f, i % 2 == 0 ? -1.5f : 1.5f), new Vector3(.12f, 3.6f, .12f));
                    MeshKit.Box(t, A, new Vector3(0, 3.85f, 0), new Vector3(4.5f, .14f, 3.3f));
                    break;
                case "u1": case "u2": case "u3":
                    {
                        float s = id == "u1" ? .85f : id == "u2" ? 1f : 1.2f;
                        MeshKit.Box(t, dark, new Vector3(-.4f * s, .9f * s, 0), new Vector3(2.4f * s, 1.6f * s, 2.8f * s));
                        MeshKit.Cyl(t, A, new Vector3(-.4f * s, 2.3f * s, 0), 1.5f * s, .6f * s, 1.3f * s, 4, new Vector3(0, 45, 0));
                        MeshKit.Box(t, M(Hex("#ffbf4d"), .2f, .4f), new Vector3(1.4f * s, .25f, 0), new Vector3(1.6f * s, .1f, 2.6f * s));
                        for (int k = 0; k < 5; k++) MeshKit.Box(t, dark, new Vector3(1.4f * s, .31f, -1.1f * s + k * .55f * s), new Vector3(1.62f * s, .02f, .22f * s));
                        MeshKit.Ball(t, mint, new Vector3(.85f * s, 1.9f * s, 1.2f * s), .25f);
                        break;
                    }
                case "presse":
                    MeshKit.Box(t, dark, new Vector3(0, .5f, 0), new Vector3(3f, .8f, 2.4f));
                    for (int i = 0; i < 4; i++) MeshKit.Box(t, steel, new Vector3(i < 2 ? -1.2f : 1.2f, 1.9f, i % 2 == 0 ? -.9f : .9f), new Vector3(.25f, 2.2f, .25f));
                    MeshKit.Box(t, A, new Vector3(0, 3.1f, 0), new Vector3(3f, .5f, 2.4f));
                    var pist = MeshKit.Box(t, steel, new Vector3(0, 2.2f, 0), new Vector3(1.9f, .35f, 1.5f)); bp.piston = pist.transform;
                    MeshKit.Cyl(t, light, new Vector3(0, 2.7f, 0), .3f, .3f, .8f, 12);
                    break;
                case "energy":
                    for (int r = 0; r < 2; r++) for (int c = 0; c < 3; c++)
                        {
                            var st0 = MeshKit.Cyl(t, steel, new Vector3(-2.2f + c * 2.2f, .7f, -1.2f + r * 2.4f), .06f, .06f, 1.2f, 6);
                            MeshKit.Box(t, M(Hex("#1f3a78"), .7f, .85f), new Vector3(-2.2f + c * 2.2f, 1.35f, -1.2f + r * 2.4f), new Vector3(2f, .06f, 1.6f), new Vector3(-25, 0, 0));
                        }
                    MeshKit.Box(t, white, new Vector3(0, .7f, 2.9f), new Vector3(1.6f, 1.2f, .9f));
                    MeshKit.Ball(t, MeshKit.Glow(Hex("#7fb4ff"), 2.4f), new Vector3(0, 1.4f, 2.9f), .3f);
                    break;
                case "werkstatt":
                    MeshKit.Part(t, MeshKit.ArchMesh(2.3f, 4.2f), light, new Vector3(0, .2f, 0), Vector3.one);
                    MeshKit.Box(t, dark, new Vector3(0, 1.1f, -2.12f), new Vector3(2.6f, 1.9f, .05f));
                    for (int k = 0; k < 6; k++) MeshKit.Box(t, M(Hex("#ffbf4d"), .2f, .4f), new Vector3(0, .35f + k * .32f, -2.15f), new Vector3(2.6f, .05f, .02f));
                    MeshKit.Ball(t, warm, new Vector3(0, 2.4f, -2.2f), .25f);
                    MeshKit.Box(t, A, new Vector3(0, 2.45f, 0), new Vector3(.3f, .12f, 4.2f));
                    break;
                case "schmelze":
                    MeshKit.Box(t, dark, new Vector3(0, 1.1f, 0), new Vector3(2.8f, 2f, 2.4f));
                    MeshKit.Cyl(t, steel, new Vector3(.8f, 3.2f, .6f), .35f, .45f, 2.4f, 16);
                    MeshKit.Box(t, MeshKit.Glow(Hex("#ff8a3a"), 3.5f), new Vector3(0, 1f, -1.22f), new Vector3(1.2f, .6f, .05f));
                    MeshKit.Box(t, A, new Vector3(0, 2.15f, 0), new Vector3(2.9f, .15f, 2.5f));
                    break;
                case "research":
                    MeshKit.Cyl(t, white, new Vector3(0, .8f, 0), 2f, 2.2f, 1.3f, 28);
                    MeshKit.Ball(t, glass, new Vector3(0, 1.45f, 0), 3f);
                    MeshKit.Cyl(t, steel, new Vector3(1.6f, 2.8f, 1f), .05f, .05f, 1.6f, 6);
                    var dish = MeshKit.Cyl(t, light, new Vector3(1.6f, 3.6f, 1f), .9f, .2f, .3f, 20, new Vector3(30, 0, 0)); bp.spin = dish.transform;
                    break;
                case "raff":
                    MeshKit.Cyl(t, white, new Vector3(0, 2.6f, 0), .9f, 1.1f, 4.8f, 20);
                    for (int k = 0; k < 3; k++) MeshKit.Cyl(t, mint, new Vector3(0, 1.4f + k * 1.3f, 0), 1.25f, 1.25f, .12f, 24);
                    MeshKit.Cyl(t, steel, new Vector3(1.4f, 1.6f, 0), .3f, .3f, 2.8f, 12);
                    break;
                case "hangar": case "lifter":
                    MeshKit.Cyl(t, dark, new Vector3(0, .3f, 0), fp - .3f, fp - .2f, .3f, 32);
                    MeshKit.Box(t, M(Hex("#ffbf4d"), .2f, .4f), new Vector3(0, .47f, 0), new Vector3(2.4f, .02f, .3f));
                    MeshKit.Box(t, M(Hex("#ffbf4d"), .2f, .4f), new Vector3(-1.05f, .47f, 0), new Vector3(.3f, .02f, 2.2f));
                    MeshKit.Box(t, M(Hex("#ffbf4d"), .2f, .4f), new Vector3(1.05f, .47f, 0), new Vector3(.3f, .02f, 2.2f));
                    for (int k = 0; k < 8; k++) MeshKit.Ball(t, k % 2 == 0 ? mint : warm, Quaternion.Euler(0, k * 45, 0) * new Vector3(fp - .6f, .5f, 0), .18f);
                    break;
                case "rtg":
                    MeshKit.Cyl(t, steel, new Vector3(0, 1.1f, 0), .7f, .9f, 1.9f, 16);
                    for (int k = 0; k < 8; k++) MeshKit.Box(t, dark, Quaternion.Euler(0, k * 45, 0) * new Vector3(.9f, 1.1f, 0), new Vector3(.5f, 1.6f, .06f), new Vector3(0, k * 45, 0));
                    MeshKit.Ball(t, MeshKit.Glow(Hex("#ffbf4d"), 3f), new Vector3(0, 2.2f, 0), .4f);
                    break;
                case "geo": case "reactor":
                    MeshKit.Cyl(t, white, new Vector3(0, 1.8f, 0), 1.3f, 2f, 3.4f, 28);
                    MeshKit.Cyl(t, light, new Vector3(0, 3.6f, 0), 1.35f, 1.3f, .2f, 28);
                    MeshKit.Box(t, dark, new Vector3(1.8f, .8f, 0), new Vector3(1.2f, 1.4f, 1.6f));
                    MeshKit.Ball(t, id == "reactor" ? MeshKit.Glow(Hex("#7fe0ff"), 3f) : MeshKit.Glow(Hex("#ff8a3a"), 3f), new Vector3(0, 3.8f, 0), .5f);
                    break;
                case "port":
                    MeshKit.Cyl(t, dark, new Vector3(0, .35f, 0), fp - .2f, fp - .1f, .4f, 32);
                    MeshKit.Cyl(t, white, new Vector3(-1.4f, 3f, 1.2f), .35f, .5f, 5.4f, 16);
                    MeshKit.Box(t, steel, new Vector3(-.4f, 5.4f, 1.2f), new Vector3(2.2f, .2f, .2f));
                    MeshKit.Box(t, A, new Vector3(1.2f, .9f, -1f), new Vector3(1.6f, 1f, 1.6f));
                    break;
                default:
                    MeshKit.Box(t, white, new Vector3(0, 1f, 0), new Vector3(fp * 1.2f, 1.8f, fp * 1.2f));
                    MeshKit.Box(t, A, new Vector3(0, 2f, 0), new Vector3(fp * 1.25f, .2f, fp * 1.25f));
                    break;
            }
            return bp;
        }

        /// <summary>Gelber Bauplatz für ein noch nicht gebautes Gebäude.</summary>
        /// <summary>Außenlager: Silo mit Trichter, zwei Andockplätze, Pumpenhaus mit Pipeline-Anschluss.</summary>
        public static BuildingParts Outpost(Color acc)
        {
            var bp = new BuildingParts(); var g = new GameObject("Außenlager"); bp.root = g; var t = g.transform;
            Material W = M(Hex("#d9dee6"), .35f, .55f), D = M(Hex("#2b313c"), .3f, .4f), A = M(acc, .3f, .55f), Pad = M(Hex("#3a414d"), .2f, .35f);
            MeshKit.Cyl(t, Pad, new Vector3(0, .06f, 0), 4.2f, 4.4f, .12f, 32);
            MeshKit.Cyl(t, W, new Vector3(-.8f, 2.4f, 0), 1.5f, 1.5f, 3.6f, 24); MeshKit.Cyl(t, A, new Vector3(-.8f, 4.4f, 0), .4f, 1.55f, .5f, 24);
            MeshKit.Cyl(t, D, new Vector3(-.8f, .45f, 0), .7f, 1.4f, .8f, 24);
            for (int i = 0; i < 4; i++) MeshKit.Box(t, D, new Vector3(-.8f + (i % 2 == 0 ? -1.2f : 1.2f), .9f, i < 2 ? -1.2f : 1.2f), new Vector3(.16f, 1.8f, .16f));
            MeshKit.Box(t, A, new Vector3(1.6f, .7f, -2.2f), new Vector3(1.6f, 1.2f, 1.4f)); MeshKit.Cyl(t, D, new Vector3(1.6f, 1.6f, -2.2f), .25f, .25f, .6f, 10);
            for (int k = 0; k < 2; k++) { MeshKit.Box(t, MeshKit.Glow(Hex("#6ff2d2"), 1.8f), new Vector3(2.9f, .14f, -(k - .5f) * 2), new Vector3(1.6f, .04f, .12f)); }
            bp.lamp = MeshKit.Glow(Hex("#3fe58a"), 2.5f); MeshKit.Ball(t, bp.lamp == null ? A : bp.lamp, new Vector3(-.8f, 4.85f, 0), .32f);
            return bp;
        }

        /// <summary>Bohrturm (Gittermast mit Pumpenkopf) oder Erzbagger (Schaufelrad auf Raupen).</summary>
        public static BuildingParts Rig(bool bag, Color acc)
        {
            var bp = new BuildingParts(); var g = new GameObject(bag ? "Erzbagger" : "Bohrturm"); bp.root = g; var t = g.transform;
            Material D = M(Hex("#2b313c"), .4f, .4f), S = M(Hex("#8a93a3"), .7f, .5f), A = M(acc, .3f, .55f), Pad = M(Hex("#3a414d"), .2f, .35f);
            MeshKit.Cyl(t, Pad, new Vector3(0, .05f, 0), 3.2f, 3.4f, .1f, 28);
            if (!bag)
            {
                for (int i = 0; i < 4; i++) { float a = (45 + i * 90) * Mathf.Deg2Rad; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); MeshKit.Box(t, S, o * .75f + new Vector3(0, 3, 0), new Vector3(.14f, 6.1f, .14f), new Vector3(-o.z * 7, 0, o.x * 7)); }
                for (int k = 1; k < 6; k++) MeshKit.Box(t, S, new Vector3(0, k * 1.1f, 0), new Vector3(1.6f - k * .14f, .08f, 1.6f - k * .14f), new Vector3(0, 45, 0));
                MeshKit.Box(t, A, new Vector3(1.6f, .7f, 0), new Vector3(1.4f, 1.2f, 1.6f));
                bp.piston = MeshKit.Box(t, D, new Vector3(0, 3.5f, 0), new Vector3(.5f, 1.6f, .5f)).transform;
                MeshKit.Cyl(t, M(Hex("#ffbf4d"), .3f, .5f), new Vector3(-1.4f, .6f, 0), .7f, .7f, 1.1f, 16);
            }
            else
            {
                MeshKit.Box(t, D, new Vector3(0, .45f, -.9f), new Vector3(3.6f, .8f, .9f)); MeshKit.Box(t, D, new Vector3(0, .45f, .9f), new Vector3(3.6f, .8f, .9f));
                MeshKit.Box(t, A, new Vector3(-.3f, 1.5f, 0), new Vector3(2.6f, 1.4f, 2.2f));
                MeshKit.Box(t, S, new Vector3(1.8f, 2.4f, 0), new Vector3(2.6f, .3f, .4f), new Vector3(0, 0, 28));
                var wheel = new GameObject("Schaufelrad").transform; wheel.SetParent(t, false); wheel.localPosition = new Vector3(3.1f, 3.0f, 0); bp.spin = wheel;
                MeshKit.Cyl(wheel, S, Vector3.zero, 1.3f, 1.3f, .35f, 20, new Vector3(90, 0, 0));
                for (int k = 0; k < 8; k++) MeshKit.Box(wheel, M(Hex("#ffbf4d"), .3f, .5f), Quaternion.Euler(0, 0, k * 45) * new Vector3(1.4f, 0, 0), new Vector3(.5f, .4f, .6f), new Vector3(0, 0, k * 45));
            }
            bp.lamp = new Material(MeshKit.Glow(Hex("#3fe58a"), 2.5f)); MeshKit.Ball(t, bp.lamp, new Vector3(0, bag ? 2.4f : 6.3f, 0), .3f);
            return bp;
        }

        public static GameObject Site(float fp)
        {
            var g = new GameObject("Bauplatz"); var t = g.transform; var gold = MeshKit.Glow(Hex("#ffbf4d"), 1.6f);
            for (int k = 0; k < 12; k++) MeshKit.Box(t, gold, Quaternion.Euler(0, k * 30, 0) * new Vector3(Mathf.Min(fp, 2.2f), .06f, 0), new Vector3(.18f, .06f, .9f), new Vector3(0, k * 30, 0));
            return g;
        }

        public class VehicleParts { public GameObject root; public Transform[] wheels; public Transform tool; public Light head; public Material headMat; }

        public static VehicleParts Vehicle(VehDef T, Color acc)
        {
            var vp = new VehicleParts(); var g = new GameObject(T.name); vp.root = g; var t = g.transform;
            float s = T.size == "s" ? .8f : T.size == "m" ? 1.15f : 1.55f; if (T.key == "staubi") s = .6f;
            Color body = T.key == "staubi" ? Hex("#d9dee6") : T.key == "buggy" ? Hex("#ffbf4d") : T.key == "sauger" ? Hex("#58cdb4") : T.key == "truck" ? Hex("#e2553a") : acc;
            Material B = M(body, .35f, .6f), D = M(Hex("#2b313c"), .3f, .4f), W = M(Hex("#1a1d22"), .1f, .3f), G = MeshKit.Mat(Hex("#9fd8ff") * .3f, .1f, .95f, Hex("#9fd8ff") * .4f), L = MeshKit.Glow(Hex("#fff0c8"), 3f), R = MeshKit.Glow(Hex("#ff5a4a"), 2f);
            if (T.key == "staubi")
            {
                MeshKit.Cyl(t, B, new Vector3(0, .35f * s, 0), .9f * s, .95f * s, .4f * s, 24);
                MeshKit.Cyl(t, D, new Vector3(0, .58f * s, 0), .4f * s, .5f * s, .12f * s, 16);
                MeshKit.Ball(t, L, new Vector3(0, .4f * s, .9f * s), .18f);
                vp.wheels = new Transform[0]; return vp;
            }
            MeshKit.Box(t, D, new Vector3(0, .55f * s, 0), new Vector3(1.5f * s, .35f * s, 2.4f * s));
            MeshKit.Box(t, B, new Vector3(0, .95f * s, -.25f * s), new Vector3(1.45f * s, .55f * s, 1.7f * s));
            MeshKit.Box(t, B, new Vector3(0, 1.3f * s, .55f * s), new Vector3(1.2f * s, .55f * s, .8f * s));
            MeshKit.Box(t, G, new Vector3(0, 1.35f * s, .96f * s), new Vector3(1.05f * s, .35f * s, .04f));
            MeshKit.Box(t, D, new Vector3(0, .5f * s, 1.3f * s), new Vector3(1.6f * s, .25f * s, .35f * s));
            vp.tool = MeshKit.Cyl(t, M(Hex("#8a93a3"), .8f, .6f), new Vector3(0, .35f * s, 1.45f * s), .2f * s, .2f * s, 1.5f * s, 12, new Vector3(0, 0, 90)).transform;
            MeshKit.Ball(t, L, new Vector3(-.45f * s, .95f * s, 1.2f * s), .16f * s); MeshKit.Ball(t, L, new Vector3(.45f * s, .95f * s, 1.2f * s), .16f * s);
            MeshKit.Ball(t, R, new Vector3(-.55f * s, .8f * s, -1.2f * s), .12f * s); MeshKit.Ball(t, R, new Vector3(.55f * s, .8f * s, -1.2f * s), .12f * s);
            MeshKit.Cyl(t, D, new Vector3(.4f * s, 1.75f * s, -.6f * s), .02f, .03f, .6f * s, 6);
            vp.wheels = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var w = MeshKit.Cyl(t, W, new Vector3((i % 2 == 0 ? -.8f : .8f) * s, .35f * s, (i < 2 ? .75f : -.75f) * s), .38f * s, .38f * s, .3f * s, 16, new Vector3(0, 0, 90));
                vp.wheels[i] = w.transform;
            }
            return vp;
        }

        public class DroneParts { public GameObject root; public Transform[] rotors; public Transform cargo; }
        /// <summary>Transportdrohne: flacher Körper, vier Ausleger mit Rotoren, Frachtkorb darunter, Positionslichter.</summary>
        public static DroneParts Drone(Color acc)
        {
            var dp = new DroneParts(); var g = new GameObject("Drohne"); dp.root = g; var t = g.transform;
            Material B = M(Hex("#e6eaf0"), .4f, .65f), D = M(Hex("#2b313c"), .3f, .4f), A = M(acc, .3f, .55f), P = MeshKit.Mat(Hex("#20242b"), .1f, .3f);
            MeshKit.Box(t, B, Vector3.zero, new Vector3(1.3f, .32f, 1.3f), new Vector3(0, 45, 0));
            MeshKit.Ball(t, A, new Vector3(0, .2f, 0), .7f);
            dp.rotors = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                float a = (45 + i * 90) * Mathf.Deg2Rad; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                MeshKit.Box(t, D, o * .85f, new Vector3(.14f, .1f, 1.2f), new Vector3(0, 90 - (45 + i * 90), 0));
                MeshKit.Cyl(t, D, o * 1.45f + new Vector3(0, .12f, 0), .14f, .16f, .25f, 10);
                var r = new GameObject("Rotor").transform; r.SetParent(t, false); r.localPosition = o * 1.45f + new Vector3(0, .28f, 0);
                MeshKit.Box(r, P, Vector3.zero, new Vector3(1.5f, .03f, .14f)); MeshKit.Box(r, P, Vector3.zero, new Vector3(.14f, .03f, 1.5f));
                dp.rotors[i] = r;
                MeshKit.Ball(t, MeshKit.Glow(i < 2 ? Hex("#6ff2d2") : Hex("#ff5a4a"), 3f), o * 1.45f + new Vector3(0, -.05f, 0), .12f);
            }
            dp.cargo = MeshKit.Box(t, A, new Vector3(0, -.45f, 0), new Vector3(.8f, .5f, .8f)).transform;
            MeshKit.Ball(t, MeshKit.Glow(Hex("#fff0c8"), 2.5f), new Vector3(0, -.2f, .62f), .14f);
            return dp;
        }

        public static GameObject Cart()
        {
            var g = new GameObject("Sammelwagen"); var t = g.transform;
            Material Y = M(Hex("#ffbf4d"), .3f, .55f), D = M(Hex("#2b313c"), .3f, .4f), L = MeshKit.Glow(Hex("#6ff2d2"), 3f);
            MeshKit.Box(t, D, new Vector3(0, .4f, 0), new Vector3(1.1f, .25f, 1.6f));
            MeshKit.Cyl(t, Y, new Vector3(0, .85f, -.15f), .6f, .45f, .7f, 16);
            MeshKit.Ball(t, L, new Vector3(0, .6f, .8f), .16f);
            for (int i = 0; i < 4; i++) MeshKit.Cyl(t, M(Hex("#1a1d22"), .1f, .3f), new Vector3(i % 2 == 0 ? -.55f : .55f, .25f, i < 2 ? .5f : -.5f), .25f, .25f, .2f, 12, new Vector3(0, 0, 90));
            return g;
        }
    }
}
