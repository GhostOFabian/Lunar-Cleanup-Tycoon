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
        /// <summary>Öffentliche Fassung für GitHub Pages: ohne Testmodus, gzip-komprimiert mit Entpacken im Browser (GitHub setzt keine Kompressions-Header).</summary>
        [MenuItem("Lunar Cleanup/Browser-Version für GitHub bauen")]
        public static void BuildGitHub() => Run(false, true);

        static void Run(bool start, bool pub = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Lunar Cleanup: erst den Play-Modus beenden."); return; }
            MakeShaderKeep(); IosBuild.PcQualityOnPhone();
            PlayerSettings.bundleVersion = IosBuild.Version; IosBuild.WriteBuildInfo(); // gleiche Versionsnummer wie auf dem iPhone
            PlayerSettings.WebGL.compressionFormat = pub ? WebGLCompressionFormat.Gzip : WebGLCompressionFormat.Disabled; // unkomprimiert läuft auf jedem einfachen Webserver
            PlayerSettings.WebGL.decompressionFallback = pub; // GitHub Pages: der Browser entpackt selbst
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            SetCodeOptimization(pub ? "DiskSize" : null); /* Fabian 11.11.: Seite lud sehr langsam – „DiskSize“ macht den Code kleiner und schneller als „BuildTimes“ */ /* der erste Release-Versuch mit voller Optimierung hat Unity abstürzen lassen */
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.maximumMemorySize = 4096; /* Fabian 02.11.: „OOM“ beim Riesenmeteor mit voll ausgebauten Planeten – 2 GB reichten nicht */
            PlayerSettings.defaultWebScreenWidth = 540; PlayerSettings.defaultWebScreenHeight = 960;
            PlayerSettings.runInBackground = true;
            PlayerSettings.stripEngineCode = false; // sonst fehlen z. B. Collider-Klassen (Meldung „SphereCollider doesn't exist“)
            // Ein Paket im Projekt benutzt embind (Fehler "undefined symbol _embind_register_class") – Bibliothek beim Linken mitnehmen
            PlayerSettings.WebGL.emscriptenArgs = "-lembind -sERROR_ON_UNDEFINED_SYMBOLS=0"; // KI-Paket (nur Editor) bringt embind-Code mit, den das Spiel nicht benutzt
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL) EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            var opt = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/LunarCleanup.unity" }, locationPathName = pub ? "Builds/WebGL-GitHub" : "Builds/WebGL", target = BuildTarget.WebGL,
                options = (pub ? BuildOptions.None : BuildOptions.Development) | (start ? BuildOptions.AutoRunPlayer : BuildOptions.None)
            };
            var rep = BuildPipeline.BuildPlayer(opt);
            Debug.Log("Lunar Cleanup: Browser-Version " + (rep.summary.result == BuildResult.Succeeded ? "fertig: " + Path.GetFullPath(opt.locationPathName) + " (" + (rep.summary.totalSize / 1048576f).ToString("0.0") + " MB)" : "FEHLGESCHLAGEN: " + rep.summary.result));
        }

        /// <summary>WebGL-Code-Optimierung per Reflexion setzen (UnityEditor.WebGL.UserBuildSettings.codeOptimization), damit es auch ohne direkte Modul-Referenz kompiliert.</summary>
        static void SetCodeOptimization(string name)
        {
            if (name == null) return;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("UnityEditor.WebGL.UserBuildSettings"); if (t == null) continue;
                var p = t.GetProperty("codeOptimization", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static); if (p == null) continue;
                try { p.SetValue(null, System.Enum.Parse(p.PropertyType, name)); Debug.Log("Lunar Cleanup: WebGL-Optimierung = " + name); } catch (System.Exception e) { Debug.LogWarning("Lunar Cleanup: Optimierung nicht gesetzt: " + e.Message); }
                return;
            }
            Debug.LogWarning("Lunar Cleanup: WebGL-Optimierung nicht gefunden (Standard bleibt).");
        }

        /// <summary>Materialien in Resources: dadurch sind Lit/Unlit/Partikel/Himmel samt der benutzten Schlüsselwörter im Build.</summary>
        internal static void MakeShaderKeep()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(Keep)) AssetDatabase.CreateFolder("Assets/Resources", "ShaderKeep");
            /* Fabian 01.11.: Unity prüft die Materialien beim Neustart neu und wirft _EMISSION raus, wenn „EmissiveIsBlack“ gesetzt ist – dann fehlten im Build alle Leucht-Varianten, der Boden verlor Relief und Detail. Darum GI-Flag immer auf None. */
            void Save(Material m, string n) { if (m.IsKeywordEnabled("_EMISSION")) m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; var p = Keep + "/" + n + ".mat"; if (AssetDatabase.LoadAssetAtPath<Material>(p)) AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(m, p); }
            var lit = Shader.Find("Universal Render Pipeline/Lit"); var pu = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (lit)
            {
                Save(new Material(lit), "Lit");
                /* GPU-Instancing-Varianten behalten: Waren auf den Bändern, Bodenteile und Kristalle werden mit DrawMeshInstanced gezeichnet – ohne diese Variante fehlen sie im Build */
                var li = new Material(lit) { enableInstancing = true }; Save(li, "LitInstanced");
                var lie = new Material(lit) { enableInstancing = true }; lie.EnableKeyword("_EMISSION"); lie.SetColor("_EmissionColor", Color.white); Save(lie, "LitInstancedEmission");
                var e = new Material(lit); e.EnableKeyword("_EMISSION"); e.SetColor("_EmissionColor", Color.white); Save(e, "LitEmission");
                var t = new Material(lit); MeshKit.MakeTransparent(t, false); Save(t, "LitTransparent");
                var ta = new Material(lit); MeshKit.MakeTransparent(ta, true); Save(ta, "LitAdditive");
                var te = new Material(lit); MeshKit.MakeTransparent(te, false); te.EnableKeyword("_EMISSION"); te.SetColor("_EmissionColor", Color.white); Save(te, "LitTransparentEmission");
                /* Planetenboden: Lit mit Detailtextur. Ohne zugewiesene Detailtextur entfernt Unity das Schlüsselwort beim Speichern – dann fehlt die Variante im Build und der Boden wird dunkel und flach */
                var dtPath = Keep + "/DetailTex.asset"; var dt = AssetDatabase.LoadAssetAtPath<Texture2D>(dtPath); if (!dt) { dt = new Texture2D(8, 8, TextureFormat.RGBA32, true); var px = new Color[64]; for (int i = 0; i < 64; i++) px[i] = new Color(.5f, .5f, .5f, 1); dt.SetPixels(px); dt.Apply(); AssetDatabase.CreateAsset(dt, dtPath); }
                var d = new Material(lit); d.SetTexture("_DetailAlbedoMap", dt); d.SetFloat("_DetailAlbedoMapScale", 1f); d.EnableKeyword("_DETAIL_MULX2"); Save(d, "LitDetail");
                /* Planetenboden mit Reliefkarte (kleine Krater/Steine): Lit + Normal Map + Detailtextur */
                var nmPath = Keep + "/NormalTex.asset"; var nt = AssetDatabase.LoadAssetAtPath<Texture2D>(nmPath); if (!nt) { nt = new Texture2D(8, 8, TextureFormat.RGBA32, true, true); var px = new Color[64]; for (int i = 0; i < 64; i++) px[i] = new Color(.5f, .5f, 1, 1); nt.SetPixels(px); nt.Apply(); AssetDatabase.CreateAsset(nt, nmPath); }
                var dn = new Material(lit); dn.SetTexture("_DetailAlbedoMap", dt); dn.SetFloat("_DetailAlbedoMapScale", 1f); dn.EnableKeyword("_DETAIL_MULX2"); dn.SetTexture("_BumpMap", nt); dn.EnableKeyword("_NORMALMAP"); Save(dn, "LitDetailNormal");
                var dne = new Material(dn); dne.EnableKeyword("_EMISSION"); dne.SetTexture("_EmissionMap", dt); dne.SetColor("_EmissionColor", Color.white); Save(dne, "LitDetailNormalEmission"); /* Planetenboden mit leuchtenden grünen Rändern */
            }
            if (pu) { var a = new Material(pu); MeshKit.MakeTransparent(a, true); Save(a, "ParticlesAdditive"); var b = new Material(pu); MeshKit.MakeTransparent(b, false); Save(b, "ParticlesAlpha"); Save(new Material(pu), "Particles"); }
            foreach (var (n, s) in new[] { ("Unlit", "Universal Render Pipeline/Unlit"), ("Sky", "Skybox/Panoramic"), ("Sprites", "Sprites/Default"), ("UnlitColor", "Unlit/Color") }) { var sh = Shader.Find(s); if (sh) Save(new Material(sh), n); }
            KeepVariants(lit);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Fabian 01.11.: Unity prüft Materialien beim Neustart nach und streicht dabei Schlüsselwörter (_EMISSION, _DETAIL_MULX2) aus den Platzhalter-Materialien.
        /// Ohne diese Varianten zeichnete das Handy den Boden ohne Relief und Detail. Eine Variantenliste (in den vorgeladenen Shadern) hält sie fest – unabhängig von den Materialien.</summary>
        static void KeepVariants(Shader lit)
        {
            if (!lit) return; var svc = new ShaderVariantCollection(); int n = 0;
            void V(params string[] kw) { try { svc.Add(new ShaderVariantCollection.ShaderVariant(lit, UnityEngine.Rendering.PassType.ScriptableRenderPipeline, kw)); n++; } catch (System.Exception e) { Debug.Log("Lunar Cleanup: Variante " + string.Join(" ", kw) + " – " + e.Message); } }
            V("_NORMALMAP", "_DETAIL_MULX2", "_EMISSION"); V("_NORMALMAP", "_DETAIL_MULX2"); V("_DETAIL_MULX2"); V("_EMISSION"); V("_EMISSION", "INSTANCING_ON"); V("INSTANCING_ON");
            V("_SURFACE_TYPE_TRANSPARENT", "_ALPHAPREMULTIPLY_ON", "_EMISSION"); V("_SURFACE_TYPE_TRANSPARENT", "_EMISSION"); V("_SURFACE_TYPE_TRANSPARENT", "_ALPHAPREMULTIPLY_ON");
            var path = Keep + "/Varianten.shadervariants"; if (AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path)) AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(svc, path);
            var gsA = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset"); if (gsA == null || gsA.Length == 0) return; var so = new SerializedObject(gsA[0]); var pre = so.FindProperty("m_PreloadedShaders");
            for (int i = pre.arraySize - 1; i >= 0; i--) if (pre.GetArrayElementAtIndex(i).objectReferenceValue == null) pre.DeleteArrayElementAtIndex(i);
            pre.InsertArrayElementAtIndex(pre.arraySize); pre.GetArrayElementAtIndex(pre.arraySize - 1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path); so.ApplyModifiedProperties();
            Debug.Log("Lunar Cleanup: " + n + " Shader-Varianten festgehalten");
        }
    }
}
