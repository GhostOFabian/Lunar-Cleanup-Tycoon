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
