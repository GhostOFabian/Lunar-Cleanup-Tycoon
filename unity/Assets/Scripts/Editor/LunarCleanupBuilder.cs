// Baut die Spielszene: Kamera, Sonne und das Objekt "Lunar Cleanup" mit GameRoot. Alles Weitere entsteht beim Start per Code.
// Menü: Lunar Cleanup > Build Prototype. Mehrfach ausführen ist ungefährlich.
using System.Linq;
using LunarCleanup.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LunarCleanup.EditorTools
{
    [InitializeOnLoad]
    public static class LunarCleanupBuilder
    {
        const string ScenePath = "Assets/Scenes/LunarCleanup.unity";
        const string Ver = "LunarCleanup.Built.v2";

        static LunarCleanupBuilder()
        {
            if (Application.isBatchMode || EditorPrefs.GetBool(Ver, false)) return;
            EditorApplication.delayCall += () => { EditorPrefs.SetBool(Ver, true); Build(); };
        }

        const string TestKey = "LunarCleanup.TestMode";
        [MenuItem("Lunar Cleanup/Testmodus (eigener Spielstand + Test-Tasten)")]
        static void ToggleTest() { bool on = !EditorPrefs.GetBool(TestKey, false); EditorPrefs.SetBool(TestKey, on); Debug.Log("Lunar Cleanup: Testmodus " + (on ? "AN – getrennter Test-Spielstand, Tasten F5–F10 aktiv" : "AUS – dein normaler Spielstand")); }
        [MenuItem("Lunar Cleanup/Testmodus (eigener Spielstand + Test-Tasten)", true)]
        static bool ToggleTestCheck() { Menu.SetChecked("Lunar Cleanup/Testmodus (eigener Spielstand + Test-Tasten)", EditorPrefs.GetBool(TestKey, false)); return !EditorApplication.isPlaying; }

        [MenuItem("Lunar Cleanup/Build Prototype")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Lunar Cleanup: erst den Play-Modus beenden."); return; }
            if (SceneManager.GetActiveScene().path != ScenePath && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera"; var cam = camGo.AddComponent<Camera>(); camGo.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.02f, .03f, .06f);
            var urp = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime"); if (urp != null) camGo.AddComponent(urp);
            var sun = new GameObject("Sonne").AddComponent<Light>(); sun.type = LightType.Directional; sun.shadows = LightShadows.Soft;
            new GameObject("Lunar Cleanup").AddComponent<GameRoot>();
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList(); list.Insert(0, new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("Lunar Cleanup: Szene gebaut (" + ScenePath + "). Drück auf Play.");
        }
    }
}
