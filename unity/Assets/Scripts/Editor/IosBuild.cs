// Menü „Lunar Cleanup > iPhone-Version …“: richtet die iOS-Einstellungen ein und baut ein Xcode-Projekt nach Builds/iOS.
// „bauen und aufs iPhone“ öffnet danach Xcode und startet das Spiel auf dem per Kabel verbundenen iPhone.
// Voraussetzungen: Unity-Modul „iOS Build Support“ (Unity Hub), Xcode, eine Apple-ID in Xcode (Einstellungen > Accounts).
// Mit kostenloser Apple-ID läuft die App 7 Tage auf dem eigenen iPhone; TestFlight/App Store/Game Center brauchen das Apple Developer Program.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LunarCleanup.EditorTools
{
    public static class IosBuild
    {
        const string BundleId = "com.fabian.lunarcleanup";
        const string OutDir = "Builds/iOS";
        const string IconPath = "Assets/Icons/AppIcon.png"; // 1024 × 1024, ohne Transparenz – iOS rundet die Ecken selbst
        internal const string Version = "0.5"; // Fabian 04.11.: einheitlich überall (iPhone, Browser, Webseite); 0.5 = Hard Mode, Planetenkassen, Perks (ab Build 53). Buildnummer zählt bei jedem iPhone-Build hoch
        internal const string TeamKey = "LunarCleanup.AppleTeamId";

        [MenuItem("Lunar Cleanup/iPhone-Version bauen und aufs iPhone")]
        public static void BuildAndRun() => Run(true, true);
        [MenuItem("Lunar Cleanup/iPhone-Version nur bauen (Xcode-Projekt)")]
        public static void BuildOnly() => Run(false, true);
        [MenuItem("Lunar Cleanup/iPhone-Version für TestFlight bauen (ohne Testmodus)")]
        public static void BuildRelease() => Run(false, false);
        [MenuItem("Lunar Cleanup/iPhone-Version für den Simulator bauen (Mac)")]
        public static void BuildSim() => Run(false, true, true);
        [MenuItem("Lunar Cleanup/iPhone: Apple-Team-ID eintragen …")]
        public static void SetTeam() => TeamWindow.Open();

        static void Run(bool start, bool dev, bool sim = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Lunar Cleanup: erst den Play-Modus beenden."); return; }
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                EditorUtility.DisplayDialog("iOS Build Support fehlt",
                    "In dieser Unity-Version ist das Modul „iOS Build Support“ nicht installiert.\n\nUnity Hub > Installs > 6000.6.7f1 > Zahnrad > Add modules > „iOS Build Support“ anhaken, installieren und Unity neu starten.", "OK");
                return;
            }
            Apply(); PlayerSettings.iOS.sdkVersion = sim ? iOSSdkVersion.SimulatorSDK : iOSSdkVersion.DeviceSDK;
            string outDir = sim ? OutDir + "-Simulator" : OutDir;
            if (sim) PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64; // Mac mit Apple-Chip
            BrowserBuild.MakeShaderKeep(); // gleiche Platzhalter-Materialien wie im Browser, sonst fehlen Shader-Varianten (dunkler Boden, kein Himmel)
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS) EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

            var opt = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/LunarCleanup.unity" }, locationPathName = outDir, target = BuildTarget.iOS,
                options = (dev ? BuildOptions.Development : BuildOptions.None) | (start ? BuildOptions.AutoRunPlayer : BuildOptions.None)
                    | (System.IO.Directory.Exists(System.IO.Path.Combine(outDir, "Unity-iPhone.xcodeproj")) ? BuildOptions.AcceptExternalModificationsToPlayer : BuildOptions.None) /* Fabian 13.11.: Xcode-Ordner nur aktualisieren statt löschen – sonst verschwindet das Projekt aus „Zuletzt geöffnet“ und Einstellungen in Xcode gehen verloren */
            };
            var rep = BuildPipeline.BuildPlayer(opt);
            bool ok = rep.summary.result == BuildResult.Succeeded;
            Debug.Log("Lunar Cleanup: iPhone-Version " + (ok ? "fertig: " + Path.GetFullPath(outDir) + (sim ? " – für den Simulator" : "") + " (Build " + PlayerSettings.iOS.buildNumber + ")" : "FEHLGESCHLAGEN: " + rep.summary.result + " – Details in der Konsole oben"));
            if (ok && !start) EditorUtility.RevealInFinder(Path.Combine(outDir, "Unity-iPhone.xcodeproj"));
        }

        /// <summary>Alle iOS-Einstellungen an einer Stelle. Mehrfach ausführen ist ungefährlich.</summary>
        [MenuItem("Lunar Cleanup/iPhone: nur Einstellungen setzen")]
        /// <summary>Build-Nummer ins Spiel schreiben (Resources/Data/build.txt), damit Einstellungen und Startmenü „Version 0.5 · Build 53“ zeigen.</summary>
        internal static void WriteBuildInfo()
        {
            File.WriteAllText("Assets/Resources/Data/build.txt", PlayerSettings.iOS.buildNumber); AssetDatabase.ImportAsset("Assets/Resources/Data/build.txt");
        }
        /// <summary>Die Qualitätsstufe „PC“ war für iPhone und WebGL ausgeschlossen – dann fehlt dort die Umgebungsverdeckung, der Boden wirkt flach. Ausschluss entfernen (mehrfach ausführen ist ungefährlich).</summary>
        public static void PcQualityOnPhone()
        {
            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset"); if (qs == null || qs.Length == 0) return; var so = new SerializedObject(qs[0]); var lv = so.FindProperty("m_QualitySettings"); bool ch = false;
            for (int i = 0; i < lv.arraySize; i++)
            {
                var l = lv.GetArrayElementAtIndex(i); if (l.FindPropertyRelative("name").stringValue != "PC") continue; var ex = l.FindPropertyRelative("excludedTargetPlatforms");
                /* Build 31: wieder wie bis Build 29 – die PC-Stufe ließ das iPhone schwarz; die eigentliche Ursache für den flachen Boden waren fehlende Shader-Varianten (siehe BrowserBuild.MakeShaderKeep) */
                foreach (var want in new[] { "iPhone", "WebGL" }) { bool has = false; for (int k = 0; k < ex.arraySize; k++) if (ex.GetArrayElementAtIndex(k).stringValue == want) has = true; if (!has) { ex.InsertArrayElementAtIndex(ex.arraySize); ex.GetArrayElementAtIndex(ex.arraySize - 1).stringValue = want; ch = true; } }
            }
            if (ch) { so.ApplyModifiedProperties(); AssetDatabase.SaveAssets(); Debug.Log("Lunar Cleanup: PC-Grafikstufe wieder nur für den PC"); }
        }
        public static void Apply()
        {
            PcQualityOnPhone(); /* Fabian 01.11.: die PC-Grafikstufe (SSAO, weiche Schatten) gehört auch ins iPhone- und Browser-Paket */
            var ios = NamedBuildTarget.iOS;
            PlayerSettings.companyName = "Fabian";
            PlayerSettings.productName = "Lunar Cleanup Tycoon";
            PlayerSettings.bundleVersion = Version; // sichtbare Versionsnummer (App Store/TestFlight)
            SetIcon();
            PlayerSettings.SetApplicationIdentifier(ios, BundleId);
            PlayerSettings.SetScriptingBackend(ios, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = false; // Collider u. a. werden nur per Code erzeugt – nicht wegschneiden lassen

            // Hochformat wie im Browser (540 × 960)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.iOS.requiresFullScreen = true;

            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.appInBackgroundBehavior = iOSAppInBackgroundBehavior.Suspend;

            // Signieren: automatisch über das Team aus Xcode
            var team = EditorPrefs.GetString(TeamKey, "RULPUY392S");
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            if (team.Length > 0) PlayerSettings.iOS.appleDeveloperTeamID = team;

            // Jede neue Fassung braucht eine höhere Build-Nummer (TestFlight lehnt doppelte ab)
            int.TryParse(PlayerSettings.iOS.buildNumber, out var n);
            PlayerSettings.iOS.buildNumber = (n + 1).ToString(); WriteBuildInfo();

            AssetDatabase.SaveAssets();
            Debug.Log("Lunar Cleanup: iOS-Einstellungen gesetzt (" + BundleId + ", Hochformat, iOS 15+, Team " + (team.Length > 0 ? team : "– noch keins, in Xcode wählen") + ").");
        }

        /// <summary>App-Icon für alle Größen und Arten (Homescreen, Spotlight, Einstellungen, App Store 1024) aus einer Datei.</summary>
        static void SetIcon()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath); if (!tex) { Debug.LogWarning("Lunar Cleanup: " + IconPath + " fehlt – App bekommt das Unity-Standardsymbol."); return; }
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { tex }, IconKind.Any); // Standard-Icon (auch für Android/Browser)
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
            {
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
                foreach (var ic in icons) { var t = new Texture2D[Mathf.Max(1, ic.maxLayerCount)]; for (int i = 0; i < t.Length; i++) t[i] = tex; ic.SetTextures(t); }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, icons);
            }
        }

        class TeamWindow : EditorWindow
        {
            string id;
            public static void Open() { var w = GetWindow<TeamWindow>(true, "Apple-Team-ID"); w.minSize = w.maxSize = new Vector2(420, 130); w.id = EditorPrefs.GetString(TeamKey, "RULPUY392S"); }
            void OnGUI()
            {
                EditorGUILayout.HelpBox("Die Team-ID (10 Zeichen, z. B. AB12CD34EF) steht in Xcode > Einstellungen > Accounts > dein Team, oder auf developer.apple.com > Account > Membership.", MessageType.Info);
                id = EditorGUILayout.TextField("Team-ID", id ?? "").Trim().ToUpperInvariant();
                if (GUILayout.Button("Speichern")) { EditorPrefs.SetString(TeamKey, id); PlayerSettings.iOS.appleDeveloperTeamID = id; Close(); }
            }
        }
    }

#if UNITY_IOS
    /// <summary>Nach dem Bau: Info.plist ergänzen (keine eigene Verschlüsselung → keine Exportfrage bei jedem TestFlight-Upload).</summary>
    public static class IosPostBuild
    {
        [UnityEditor.Callbacks.PostProcessBuild(100)]
        static void Post(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new UnityEditor.iOS.Xcode.PlistDocument(); plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.WriteToFile(plistPath);
            StoreCapabilities(path);
        }
        /// <summary>Fabian 09.11.: App-Store-Dienste vorbereitet. Nur wenn „LC_STORE“ in den iOS-Scripting-Defines steht:
        /// Game Center, iCloud (für den Spielstand im Konto) und „Mit Apple anmelden“ ins Xcode-Projekt eintragen.</summary>
        static void StoreCapabilities(string path)
        {
            var defs = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.iOS);
            if (!defs.Split(';').Contains("LC_STORE")) return;
            string proj = UnityEditor.iOS.Xcode.PBXProject.GetPBXProjectPath(path);
            var pbx = new UnityEditor.iOS.Xcode.PBXProject(); pbx.ReadFromFile(proj);
            string main = pbx.GetUnityMainTargetGuid(), fw = pbx.GetUnityFrameworkTargetGuid();
            pbx.AddFrameworkToProject(fw, "GameKit.framework", false); pbx.AddFrameworkToProject(fw, "AuthenticationServices.framework", true);
            pbx.WriteToFile(proj);
            var cap = new UnityEditor.iOS.Xcode.ProjectCapabilityManager(proj, "Unity-iPhone/lunarcleanup.entitlements", null, main);
            cap.AddGameCenter(); cap.AddSignInWithApple();
            cap.AddiCloud(true, true, false, true, new string[0]); /* Schlüssel-Wert-Speicher + Dokumente für GKSavedGame */
            cap.WriteToFile();
            Debug.Log("Lunar Cleanup: App-Store-Dienste ins Xcode-Projekt eingetragen (Game Center, iCloud, Mit Apple anmelden).");
        }
    }
#endif
}
