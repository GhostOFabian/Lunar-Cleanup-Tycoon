// Baut die komplette Spielszene für LunarCleanupGame automatisch auf:
// Kamera, Licht, Spielobjekt, Canvas (1080 x 1920), EventSystem (Input System), Texte und Knöpfe – alles verdrahtet.
// Menü: Lunar Cleanup > Build Prototype. Läuft beim ersten Öffnen einmal von selbst, wenn die Szene noch fehlt.
// Mehrfach ausführen ist ungefährlich: die Szene wird jedes Mal neu erzeugt.

using System;
using System.Linq;
using System.Reflection;
using LunarCleanup.Core;
using LunarCleanup.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LunarCleanup.EditorTools
{
    [InitializeOnLoad]
    public static class LunarCleanupBuilder
    {
        const string ScenePath = "Assets/Scenes/LunarCleanup.unity";

        static LunarCleanupBuilder()
        {
            // nur einmal automatisch, danach über das Menü
            if (Application.isBatchMode || EditorPrefs.GetBool("LunarCleanup.AutoBuilt", false)) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            EditorApplication.delayCall += () => { EditorPrefs.SetBool("LunarCleanup.AutoBuilt", true); Build(); };
        }

        [MenuItem("Lunar Cleanup/Build Prototype")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Lunar Cleanup: erst den Play-Modus beenden."); return; }
            if (Resources.Load<TMP_Settings>("TMP Settings") == null && ImportTmpEssentials()) return; // Build läuft nach dem Import weiter
            BuildScene();
        }

        // TextMeshPro braucht seine Grundschrift ("TMP Essential Resources"). Fehlt sie, wird sie importiert.
        static bool ImportTmpEssentials()
        {
            Type importer = TypeCache.GetTypesDerivedFrom<object>().FirstOrDefault(t => t.FullName == "TMPro.TMP_PackageResourceImporter");
            MethodInfo m = importer?.GetMethod("ImportResources", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(bool), typeof(bool), typeof(bool) }, null);
            if (m == null) { Debug.LogWarning("Lunar Cleanup: Bitte einmal Window > TextMeshPro > Import TMP Essential Resources ausführen und danach Lunar Cleanup > Build Prototype."); return false; }
            AssetDatabase.ImportPackageCallback done = null;
            done = name => { AssetDatabase.importPackageCompleted -= done; EditorApplication.delayCall += BuildScene; };
            AssetDatabase.importPackageCompleted += done;
            m.Invoke(null, new object[] { true, false, false });
            Debug.Log("Lunar Cleanup: TMP Essential Resources werden importiert, danach wird die Szene gebaut.");
            return true;
        }

        static void BuildScene()
        {
            // die eigene, erzeugte Szene wird ohnehin neu gebaut – nur bei anderen Szenen nach dem Speichern fragen
            if (SceneManager.GetActiveScene().path != ScenePath && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Kamera schräg von oben auf das 24 x 24 m große Feld
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f);
            cam.fieldOfView = 60f; camGo.transform.position = new Vector3(0, 40, -13f); camGo.transform.rotation = Quaternion.Euler(70, 0, 0);
            camGo.AddComponent<AudioListener>();
            var urpCam = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpCam != null) camGo.AddComponent(urpCam);

            var lightGo = new GameObject("Sonne"); var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.2f; light.color = new Color(1f, 0.96f, 0.9f); light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

            var gameGo = new GameObject("Game"); var game = gameGo.AddComponent<LunarCleanupGame>();
            game.mainCamera = cam;

            // UI
            var canvasGo = new GameObject("Canvas", typeof(RectTransform)); var canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var esGo = new GameObject("EventSystem"); esGo.AddComponent<EventSystem>(); esGo.AddComponent<InputSystemUIInputModule>();

            var safe = Rect(canvasGo.transform, "SafeArea", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            var top = Panel(safe, "Oben", new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -300), new Vector2(-24, -24));
            game.creditsText = Text(top, "Credits", "0 Cr", 64, TextAlignmentOptions.Left, new Vector2(0, .5f), new Vector2(.5f, 1), new Color(1f, .78f, .3f));
            game.progressText = Text(top, "Fortschritt", "", 32, TextAlignmentOptions.Right, new Vector2(.5f, .5f), new Vector2(1, 1), Color.white);
            game.handText = Text(top, "Behälter", "", 34, TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(.33f, .5f), Color.white);
            game.rohText = Text(top, "Rohstofflager", "", 34, TextAlignmentOptions.Center, new Vector2(.33f, 0), new Vector2(.62f, .5f), Color.white);
            game.prodText = Text(top, "Produkte", "", 30, TextAlignmentOptions.Right, new Vector2(.62f, 0), new Vector2(1, .5f), Color.white);

            var msgRt = Rect(safe, "Meldung", new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -400), new Vector2(-24, -310));
            game.messageText = AddText(msgRt.gameObject, "", 38, TextAlignmentOptions.Center, new Color(.55f, 1f, .8f));

            var bottom = Panel(safe, "Knöpfe", new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 24), new Vector2(-24, 640));
            var grid = bottom.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(20, 20, 20, 20); grid.spacing = new Vector2(16, 16); grid.cellSize = new Vector2(484, 132);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
            game.emptyHandButton = Button(bottom, "Behälter leeren", new Color(.15f, .55f, .45f));
            game.launchRocketButton = Button(bottom, "Rakete starten", new Color(.85f, .45f, .2f));
            game.buyVehicleButton = Button(bottom, "Buggy kaufen", new Color(.25f, .35f, .6f));
            game.buildPressButton = Button(bottom, "Staubpresse bauen", new Color(.25f, .35f, .6f));
            game.upgradePowerButton = Button(bottom, "Turbo-Saugkraft", new Color(.3f, .3f, .38f));
            game.upgradeRohButton = Button(bottom, "Größeres Rohstofflager", new Color(.3f, .3f, .38f));
            game.upgradeVehicleButton = Button(bottom, "Doppelbürsten", new Color(.3f, .3f, .38f));

            EditorUtility.SetDirty(game);
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            Selection.activeGameObject = gameGo;
            Debug.Log("Lunar Cleanup: Szene gebaut und gespeichert (" + ScenePath + "). Drück auf Play.");
        }

        // ---------- kleine UI-Helfer ----------
        static RectTransform Rect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax; return rt;
        }

        static RectTransform Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var rt = Rect(parent, name, aMin, aMax, oMin, oMax);
            var img = rt.gameObject.AddComponent<Image>(); img.color = new Color(.06f, .08f, .13f, .82f);
            return rt;
        }

        static TMP_Text Text(Transform parent, string name, string text, float size, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Color col)
        {
            var rt = Rect(parent, name, aMin, aMax, new Vector2(28, 8), new Vector2(-28, -8));
            return AddText(rt.gameObject, text, size, align, col);
        }

        static TMP_Text AddText(GameObject go, string text, float size, TextAlignmentOptions align, Color col)
        {
            var t = go.AddComponent<TextMeshProUGUI>(); t.text = text; t.fontSize = size; t.alignment = align; t.color = col;
            t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false; return t;
        }

        static Button Button(Transform parent, string label, Color col)
        {
            var go = new GameObject(label, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>(); img.color = col;
            var b = go.AddComponent<Button>(); b.targetGraphic = img;
            var colors = b.colors; colors.disabledColor = new Color(.55f, .55f, .55f, .55f); b.colors = colors;
            var lr = Rect(go.transform, "Text", Vector2.zero, Vector2.one, new Vector2(12, 6), new Vector2(-12, -6));
            AddText(lr.gameObject, label, 34, TextAlignmentOptions.Center, Color.white);
            return b;
        }
    }
}
