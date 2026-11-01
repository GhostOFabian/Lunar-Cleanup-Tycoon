// Import der eigenen Modelle (Assets/Resources/LC, FBX aus Blender) und Prüfung gegen Modelle_Quelle/manifest.json.
// Menü „Lunar Cleanup → Eigene Modelle prüfen“: schreibt für jedes Modell Größe, Achsen und Teile in die Konsole
// und meldet Abweichungen (falsche Größe, liegt auf der Seite, Vorderseite verkehrt, fehlende bewegliche Teile).
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LunarCleanup.EditorTools
{
    public class LcModelImport : AssetPostprocessor
    {
        const string Root = "Assets/Resources/LC/";

        void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(Root)) return;
            var mi = (ModelImporter)assetImporter;
            mi.globalScale = 1; mi.useFileScale = true; mi.bakeAxisConversion = true;          // Blender-Export: Meter, Y oben
            mi.importCameras = false; mi.importLights = false; mi.importVisibility = false; mi.importBlendShapes = false;
            mi.animationType = ModelImporterAnimationType.None; mi.importAnimation = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;             // nur für die Namen LC_* – die echten Materialien setzt LcKit
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.isReadable = true;                                                              // Bänder suchen ihre Anschlüsse an den echten Gebäudewänden (Dreiecke lesen)
            mi.addCollider = false; mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.importNormals = ModelImporterNormals.Import; mi.importTangents = ModelImporterTangents.None;
        }

        /// <summary>App-Icon (Assets/Icons): volle Auflösung, keine Kompression, keine Transparenz – sonst lehnt Apple das Icon ab.</summary>
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith("Assets/Icons/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default; ti.mipmapEnabled = false; ti.npotScale = TextureImporterNPOTScale.None;
            ti.alphaSource = TextureImporterAlphaSource.None; ti.alphaIsTransparency = false; ti.maxTextureSize = 1024; ti.textureCompression = TextureImporterCompression.Uncompressed;
        }

        [System.Serializable] class Man { public List<Mdl> modelle; }
        [System.Serializable] class Mdl { public string id, gruppe; public Size groesse_m; public List<Teil> teile; }
        [System.Serializable] class Size { public float breite, tiefe, hoehe; }
        [System.Serializable] class Teil { public string name; }

        [MenuItem("Lunar Cleanup/Eigene Modelle prüfen")]
        public static void Check()
        {
            string mp = Path.Combine(Application.dataPath, "../Modelle_Quelle/manifest.json");
            if (!File.Exists(mp)) { Debug.LogWarning("Lunar Cleanup: Modelle_Quelle/manifest.json fehlt."); return; }
            var man = JsonUtility.FromJson<Man>(File.ReadAllText(mp)); int ok = 0, bad = 0, miss = 0; var sb = new StringBuilder();
            foreach (var m in man.modelle)
            {
                string path = Root + m.gruppe + "/" + m.id + ".fbx"; var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!go) { miss++; sb.AppendLine("FEHLT  " + path); continue; }
                var inst = Object.Instantiate(go); bool any = false; var b = new Bounds();
                foreach (var r in inst.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                var issues = new List<string>(); var info = new List<string>(); var g = m.groesse_m;
                if (any)
                {
                    if (Mathf.Abs(b.size.y - g.hoehe) > Mathf.Max(.08f, g.hoehe * .08f)) issues.Add("Höhe " + b.size.y.ToString("0.00") + " statt " + g.hoehe.ToString("0.00") + (Mathf.Abs(b.size.z - g.hoehe) < g.hoehe * .1f ? " (liegt auf der Seite?)" : ""));
                    if (Mathf.Abs(b.size.x - g.breite) > Mathf.Max(.08f, g.breite * .08f) && Mathf.Abs(b.size.x - g.tiefe) > Mathf.Max(.08f, g.tiefe * .08f)) issues.Add("Breite " + b.size.x.ToString("0.00") + " statt " + g.breite.ToString("0.00"));
                    if (b.min.y < -.1f) info.Add("ragt " + (-b.min.y).ToString("0.00") + " m in den Boden (eingegraben)");
                }
                // Vorderseite: Werkzeug bzw. Teil vorne muss bei +Z liegen
                var tool = Find(inst.transform, "_Werkzeug"); if (tool && tool.position.z < -.05f) info.Add("Vorderseite −Z (LcKit dreht Fahrzeuge um 180°)");
                foreach (var t in m.teile) if (!t.name.EndsWith("_Koerper") && !Find(inst.transform, t.name)) issues.Add("Teil fehlt: " + t.name);
                Object.DestroyImmediate(inst);
                if (issues.Count == 0) { ok++; if (info.Count > 0) sb.AppendLine("ok     " + m.gruppe + "/" + m.id + ": " + string.Join("; ", info)); } else { bad++; sb.AppendLine("PRÜFEN " + m.gruppe + "/" + m.id + ": " + string.Join("; ", issues.Concat(info))); }
            }
            string head = "Lunar Cleanup: Eigene Modelle – " + ok + " in Ordnung, " + bad + " prüfen, " + miss + " fehlen";
            Debug.Log(head + (sb.Length > 0 ? "\n" + sb : ""));
            File.WriteAllText(Path.Combine(Application.dataPath, "../Modelle_Quelle/pruefung.txt"), head + "\n" + sb);
        }
        static Transform Find(Transform t, string suffix) { foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name.EndsWith(suffix)) return c; return null; }
    }
}
