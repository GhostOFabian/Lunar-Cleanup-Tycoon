// Menü „Lunar Cleanup > Browser-Version bauen und starten“: baut die Unity-Version als WebGL-Seite (Development-Build mit Testmodus)
// nach Builds/WebGL und öffnet sie über Unitys lokalen Webserver im Standard-Browser.
// Vorher werden Platzhalter-Materialien angelegt, damit alle per Code benutzten Shader samt Varianten im Build landen.
using System.IO;
using LunarCleanup.Gameplay;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LunarCleanup.EditorTools
{
    public static class BrowserBuild
    {
        const string Keep = "Assets/Resources/ShaderKeep";

        [MenuItem("Lunar Cleanup/Browser-Version bauen und starten")]
        public static void BuildAndRun() => Run(true);
        [MenuItem("Lunar Cleanup/Browser-Version nur bauen")]
        public static void BuildOnly() => Run(false);

        static void Run(bool start)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Lunar Cleanup: erst den Play-Modus beenden."); return; }
            MakeShaderKeep();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // läuft so auf jedem einfachen Webserver (auch GitHub Pages)
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.defaultWebScreenWidth = 540; PlayerSettings.defaultWebScreenHeight = 960;
            PlayerSettings.runInBackground = true;
            PlayerSettings.stripEngineCode = false; // sonst fehlen z. B. Collider-Klassen (Meldung „SphereCollider doesn't exist“)
            // Ein Paket im Projekt benutzt embind (Fehler "undefined symbol _embind_register_class") – Bibliothek beim Linken mitnehmen
            PlayerSettings.WebGL.emscriptenArgs = "-lembind -sERROR_ON_UNDEFINED_SYMBOLS=0"; // KI-Paket (nur Editor) bringt embind-Code mit, den das Spiel nicht benutzt
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL) EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            var opt = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/LunarCleanup.unity" }, locationPathName = "Builds/WebGL", target = BuildTarget.WebGL,
                options = BuildOptions.Development | (start ? BuildOptions.AutoRunPlayer : BuildOptions.None)
            };
            var rep = BuildPipeline.BuildPlayer(opt);
            Debug.Log("Lunar Cleanup: Browser-Version " + (rep.summary.result == BuildResult.Succeeded ? "fertig: " + Path.GetFullPath("Builds/WebGL") + " (" + (rep.summary.totalSize / 1048576f).ToString("0.0") + " MB)" : "FEHLGESCHLAGEN: " + rep.summary.result));
        }

        /// <summary>Materialien in Resources: dadurch sind Lit/Unlit/Partikel/Himmel samt der benutzten Schlüsselwörter im Build.</summary>
        static void MakeShaderKeep()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(Keep)) AssetDatabase.CreateFolder("Assets/Resources", "ShaderKeep");
            void Save(Material m, string n) { var p = Keep + "/" + n + ".mat"; if (AssetDatabase.LoadAssetAtPath<Material>(p)) AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(m, p); }
            var lit = Shader.Find("Universal Render Pipeline/Lit"); var pu = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (lit)
            {
                Save(new Material(lit), "Lit");
                var e = new Material(lit); e.EnableKeyword("_EMISSION"); e.SetColor("_EmissionColor", Color.white); Save(e, "LitEmission");
                var t = new Material(lit); MeshKit.MakeTransparent(t, false); Save(t, "LitTransparent");
                var ta = new Material(lit); MeshKit.MakeTransparent(ta, true); Save(ta, "LitAdditive");
                var te = new Material(lit); MeshKit.MakeTransparent(te, false); te.EnableKeyword("_EMISSION"); Save(te, "LitTransparentEmission");
                var d = new Material(lit); d.EnableKeyword("_DETAIL_MULX2"); Save(d, "LitDetail");
            }
            if (pu) { var a = new Material(pu); MeshKit.MakeTransparent(a, true); Save(a, "ParticlesAdditive"); var b = new Material(pu); MeshKit.MakeTransparent(b, false); Save(b, "ParticlesAlpha"); Save(new Material(pu), "Particles"); }
            foreach (var (n, s) in new[] { ("Unlit", "Universal Render Pipeline/Unlit"), ("Sky", "Skybox/Panoramic"), ("Sprites", "Sprites/Default"), ("UnlitColor", "Unlit/Color") }) { var sh = Shader.Find(s); if (sh) Save(new Material(sh), n); }
            AssetDatabase.SaveAssets();
        }
    }
}
