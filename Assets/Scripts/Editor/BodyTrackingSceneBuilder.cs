using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FlexReality.BodyTracking.EditorTools
{
    // One-click scene builder. Drop into a fresh scene with:
    //   Tools → Body Tracking → Build MVP Scene
    // Creates the full playable MVP: player avatar (with clear front/back),
    // floor with lane lines and direction markers, tracking pipeline, spawner,
    // game session (score/lives/restart), and the full UI stack.
    public static class BodyTrackingSceneBuilder
    {
        private const string MenuRoot = "Tools/Body Tracking/";

        private static readonly Color ColorFront  = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color ColorLeft   = new Color(0.3f, 0.6f, 0.9f);
        private static readonly Color ColorRight  = new Color(0.9f, 0.7f, 0.3f);
        private static readonly Color ColorBottom = new Color(0.4f, 0.9f, 0.4f);

        [MenuItem(MenuRoot + "Build MVP Scene (Mock)")]
        public static void BuildMockScene() => BuildScene(useWebcam: false);

        [MenuItem(MenuRoot + "Build MVP Scene (Webcam)")]
        public static void BuildWebcamScene() => BuildScene(useWebcam: true);

        private static void BuildScene(bool useWebcam)
        {
            if (!EditorUtility.DisplayDialog(
                    "Build MVP Scene",
                    "This will create a new scene and replace any unsaved scene in the editor. Continue?",
                    "Build", "Cancel"))
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // ----- Camera -----
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 3.2f, -4.5f);
                mainCam.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
                mainCam.backgroundColor = new Color(0.10f, 0.12f, 0.16f);
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.fieldOfView = 50f;
            }

            // ----- Floor + orientation aids -----
            BuildFloor();

            // ----- Player (with clear front/back) -----
            var player = BuildPlayer(out var avatar);

            // ----- Tracking -----
            var tracking = new GameObject("Tracking");
            MonoBehaviour provider;
            if (useWebcam)
            {
                var webcam = tracking.AddComponent<WebcamBodyTrackingProvider>();
                provider = webcam;
                Debug.LogWarning("[SceneBuilder] Webcam scene built. Assign your BlazePose ModelAsset on the 'Tracking' GameObject.");
            }
            else
            {
                provider = tracking.AddComponent<MockBodyTrackingProvider>();
            }
            var detector = tracking.AddComponent<BodyGestureDetector>();
            SetPrivate(detector, "providerBehaviour", provider);
            SetPrivate(avatar, "gestureDetector", detector);

            // Subtle camera sway driven by the tracked player's body.
            if (mainCam != null)
            {
                var sway = mainCam.gameObject.AddComponent<CameraSway>();
                SetPrivate(sway, "providerBehaviour", provider);
            }

            // ----- Game session -----
            var sessionGo = new GameObject("GameSession");
            var session = sessionGo.AddComponent<GameSession>();

            // ----- Audio (background music + hit/miss SFX) -----
            var audioGo = new GameObject("AudioManager");
            audioGo.AddComponent<AudioManager>();

            // ----- Spawner -----
            var spawnerGo = new GameObject("Spawner");
            var spawner = spawnerGo.AddComponent<ObstacleSpawner>();
            SetPrivate(spawner, "playerTarget", player.transform);

            // ----- GameController (bootstrap provider lifecycle) -----
            var gcGo = new GameObject("GameController");
            var gc = gcGo.AddComponent<GameController>();
            SetPrivate(gc, "providerBehaviour", provider);
            SetPrivate(gc, "gestureDetector", detector);
            SetPrivate(gc, "spawner", spawner);

            // ----- Canvas + UI -----
            BuildCanvas(useWebcam, provider, detector, session);

            EditorSceneManager.MarkSceneDirty(scene);
            string scenesFolder = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesFolder)) AssetDatabase.CreateFolder("Assets", "Scenes");
            string path = useWebcam ? "Assets/Scenes/MVP_Webcam.unity" : "Assets/Scenes/MVP_Mock.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[SceneBuilder] Saved scene to {path}");
        }

        // -----------------------------------------------------------------------
        // Floor
        // -----------------------------------------------------------------------
        private static void BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(2f, 1f, 6f);
            floor.transform.position = new Vector3(0f, 0f, 5f);
            var floorMR = floor.GetComponent<MeshRenderer>();
            if (floorMR != null) floorMR.sharedMaterial.color = new Color(0.22f, 0.24f, 0.28f);

            // Lane lines: three long thin cubes running into the distance.
            for (int i = -1; i <= 1; i++)
                CreateMarker($"LaneLine_{i}", new Vector3(i * 1.6f, 0.01f, 5f),
                    new Vector3(0.05f, 0.01f, 12f), new Color(0.4f, 0.42f, 0.46f));

            // Player start ring (just a flat square highlight where the avatar stands).
            CreateMarker("PlayerStart", new Vector3(0f, 0.015f, 0f),
                new Vector3(2.2f, 0.01f, 1.8f), new Color(0.15f, 0.18f, 0.22f));

            // Coloured target pads on the ground at the X-positions where each
            // obstacle TYPE will land. So the player sees "the blue cube will
            // hit this blue patch — I need to react with the LEFT-hand gesture".
            const float padLen = 1.6f;
            CreateMarker("Pad_Front",  new Vector3( 0f,    0.02f, 0.5f), new Vector3(1.2f, 0.02f, padLen), ColorFront);
            CreateMarker("Pad_Left",   new Vector3(-1.8f,  0.02f, 0.5f), new Vector3(1.2f, 0.02f, padLen), ColorLeft);
            CreateMarker("Pad_Right",  new Vector3( 1.8f,  0.02f, 0.5f), new Vector3(1.2f, 0.02f, padLen), ColorRight);
            CreateMarker("Pad_Bottom", new Vector3( 0f,    0.025f, -0.4f), new Vector3(1.2f, 0.02f, 0.6f), ColorBottom);

            // Single "you are facing this way" arrow ahead of the player.
            CreateArrow("Arrow_Forward", new Vector3(0f, 0.03f, 2.5f), Vector3.forward, new Color(1f, 0.85f, 0.2f));
        }

        private static void CreateMarker(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) SetUniqueColor(mr, color);
        }

        // A flat triangle-ish arrow built from two stretched cubes.
        private static void CreateArrow(string name, Vector3 pos, Vector3 dir, Color color)
        {
            var root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            // shaft
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(shaft.GetComponent<Collider>());
            shaft.transform.SetParent(root.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0f, 0f);
            shaft.transform.localScale = new Vector3(0.18f, 0.02f, 1.0f);
            SetUniqueColor(shaft.GetComponent<MeshRenderer>(), color);
            // head left
            var headL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(headL.GetComponent<Collider>());
            headL.transform.SetParent(root.transform, false);
            headL.transform.localPosition = new Vector3(-0.18f, 0f, 0.4f);
            headL.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
            headL.transform.localScale = new Vector3(0.18f, 0.02f, 0.45f);
            SetUniqueColor(headL.GetComponent<MeshRenderer>(), color);
            // head right
            var headR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(headR.GetComponent<Collider>());
            headR.transform.SetParent(root.transform, false);
            headR.transform.localPosition = new Vector3(0.18f, 0f, 0.4f);
            headR.transform.localRotation = Quaternion.Euler(0f, -35f, 0f);
            headR.transform.localScale = new Vector3(0.18f, 0.02f, 0.45f);
            SetUniqueColor(headR.GetComponent<MeshRenderer>(), color);
        }

        // -----------------------------------------------------------------------
        // Player
        // -----------------------------------------------------------------------
        private static GameObject BuildPlayer(out PlayerAvatarController avatar)
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0f, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            SetUniqueColor(body.GetComponent<MeshRenderer>(), new Color(0.85f, 0.85f, 0.9f));

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(player.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            head.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            SetUniqueColor(head.GetComponent<MeshRenderer>(), new Color(0.9f, 0.9f, 0.95f));

            // Nose — small cube on the FRONT face of the head (z+). Yellow.
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            nose.transform.SetParent(head.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            nose.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            SetUniqueColor(nose.GetComponent<MeshRenderer>(), new Color(1f, 0.85f, 0.2f));
            Object.DestroyImmediate(nose.GetComponent<Collider>());

            // Chest strip — colored band on the FRONT of the body. Same yellow.
            var chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.name = "ChestStripe";
            chest.transform.SetParent(body.transform, false);
            chest.transform.localPosition = new Vector3(0f, 0.1f, 0.45f);
            chest.transform.localScale = new Vector3(0.7f, 0.3f, 0.15f);
            SetUniqueColor(chest.GetComponent<MeshRenderer>(), new Color(1f, 0.85f, 0.2f));
            Object.DestroyImmediate(chest.GetComponent<Collider>());

            // Back marker — small dark stripe on the BACK so you instantly know
            // which way the player is facing from any camera angle.
            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "BackStripe";
            back.transform.SetParent(body.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.1f, -0.45f);
            back.transform.localScale = new Vector3(0.7f, 0.3f, 0.15f);
            SetUniqueColor(back.GetComponent<MeshRenderer>(), new Color(0.25f, 0.25f, 0.3f));
            Object.DestroyImmediate(back.GetComponent<Collider>());

            avatar = player.AddComponent<PlayerAvatarController>();
            SetPrivate(avatar, "avatarRoot", player.transform);
            return player;
        }

        // -----------------------------------------------------------------------
        // Canvas + UI
        // -----------------------------------------------------------------------
        private static void BuildCanvas(bool useWebcam, MonoBehaviour provider,
            BodyGestureDetector detector, GameSession session)
        {
            var canvasGo = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            // Score (top-center, large) + Lives (right under it).
            var scoreLabel = CreateTmp(canvas.transform, "ScoreLabel", "Score: 0",
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                pivot: new Vector2(0.5f, 1f), anchored: new Vector2(0f, -20f),
                size: new Vector2(600f, 80f), fontSize: 56, alignment: TextAlignmentOptions.Center);
            var livesLabel = CreateTmp(canvas.transform, "LivesLabel", "Lives: ♥♥♥",
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                pivot: new Vector2(0.5f, 1f), anchored: new Vector2(0f, -100f),
                size: new Vector2(600f, 50f), fontSize: 36, alignment: TextAlignmentOptions.Center);
            livesLabel.richText = true;
            var scoreLivesGo = new GameObject("ScoreLivesUI");
            scoreLivesGo.transform.SetParent(canvas.transform, false);
            var sl = scoreLivesGo.AddComponent<ScoreLivesUI>();
            SetPrivate(sl, "scoreLabel", scoreLabel);
            SetPrivate(sl, "livesLabel", livesLabel);
            SetPrivate(sl, "session", session);

            // Gesture label (top-left)
            var gestureLabel = CreateTmp(canvas.transform, "GestureLabel", "Gesture: None",
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f), anchored: new Vector2(20f, -20f),
                size: new Vector2(700f, 60f), fontSize: 32, alignment: TextAlignmentOptions.TopLeft);
            var debugUiGo = new GameObject("DebugGestureUI");
            debugUiGo.transform.SetParent(canvas.transform, false);
            var debugUI = debugUiGo.AddComponent<DebugGestureUI>();
            SetPrivate(debugUI, "detector", detector);
            SetPrivate(debugUI, "tmpLabel", gestureLabel);

            // FPS label (top-right)
            var fpsLabel = CreateTmp(canvas.transform, "FpsLabel", "FPS: --",
                anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f), anchored: new Vector2(-20f, -20f),
                size: new Vector2(250f, 60f), fontSize: 32, alignment: TextAlignmentOptions.TopRight);
            var fpsGo = new GameObject("FpsCounter");
            fpsGo.transform.SetParent(canvas.transform, false);
            var fps = fpsGo.AddComponent<FpsCounter>();
            SetPrivate(fps, "tmpLabel", fpsLabel);

            // Pose overlay (bottom-left preview)
            if (useWebcam)
            {
                var overlayGo = new GameObject("PoseOverlay",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                overlayGo.transform.SetParent(canvas.transform, false);
                var orRT = (RectTransform)overlayGo.transform;
                orRT.anchorMin = orRT.anchorMax = new Vector2(0f, 0f);
                orRT.pivot = new Vector2(0f, 0f);
                orRT.anchoredPosition = new Vector2(20f, 20f);
                orRT.sizeDelta = new Vector2(280f, 280f);
                var raw = overlayGo.GetComponent<RawImage>();
                raw.color = Color.white;

                var overlay = overlayGo.AddComponent<PoseOverlayUI>();
                SetPrivate(overlay, "providerBehaviour", provider);
                SetPrivate(overlay, "webcamProvider", provider);
                SetPrivate(overlay, "overlayRoot", orRT);
                SetPrivate(overlay, "webcamView", raw);
            }

            // Big legend at the bottom-center — "how to kill each cube" cheat sheet.
            var legend = CreateTmp(canvas.transform, "Legend",
                "<size=42><b>How to kill the cubes</b></size>\n" +
                "<color=#e54d4d>■ RED (center)</color>   →  <b>BOTH hands UP</b>            (key W)\n" +
                "<color=#4d99e5>■ BLUE (left)</color>    →  <b>LEFT arm out to the side</b>  (key Q)\n" +
                "<color=#e5b34d>■ YELLOW (right)</color> →  <b>RIGHT arm out to the side</b> (key E)\n" +
                "<color=#66e566>■ GREEN (low)</color>    →  <b>JUMP</b>                       (key Space)",
                anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(0.5f, 0f),
                pivot: new Vector2(0.5f, 0f), anchored: new Vector2(0f, 20f),
                size: new Vector2(1200f, 260f), fontSize: 28, alignment: TextAlignmentOptions.Center);
            legend.richText = true;

            // Game over panel.
            BuildGameOverPanel(canvas.transform, session);
        }

        private static void BuildTutorialPanel(Transform canvas)
        {
            // Dim background covering whole screen.
            var panelGo = new GameObject("TutorialPanel",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelGo.transform.SetParent(canvas, false);
            var panelRT = (RectTransform)panelGo.transform;
            panelRT.anchorMin = Vector2.zero; panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
            panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

            // Card container in the middle.
            var cardGo = new GameObject("Card",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardGo.transform.SetParent(panelRT, false);
            var cardRT = (RectTransform)cardGo.transform;
            cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(1200f, 850f);
            cardGo.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);

            // Title.
            CreateTmp(cardRT, "Title", "HOW TO PLAY",
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                pivot: new Vector2(0.5f, 1f), anchored: new Vector2(0f, -30f),
                size: new Vector2(1100f, 90f), fontSize: 64, alignment: TextAlignmentOptions.Center);

            CreateTmp(cardRT, "Sub", "A coloured cube will fly towards you. React with the matching gesture:",
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                pivot: new Vector2(0.5f, 1f), anchored: new Vector2(0f, -120f),
                size: new Vector2(1100f, 50f), fontSize: 28, alignment: TextAlignmentOptions.Center);

            // 4 rows of: [colored cube icon] [description] [key hint]
            BuildTutorialRow(cardRT, -200f, ColorFront,  "RED — flies at your chest",         "Push BOTH hands forward",            "key: W");
            BuildTutorialRow(cardRT, -290f, ColorLeft,   "BLUE — comes towards your LEFT",    "Raise your LEFT hand up",            "key: Q");
            BuildTutorialRow(cardRT, -380f, ColorRight,  "YELLOW — comes towards your RIGHT", "Raise your RIGHT hand up",           "key: E");
            BuildTutorialRow(cardRT, -470f, ColorBottom, "GREEN — low, slides under you",     "JUMP over it",                       "key: Space");

            // Tip
            CreateTmp(cardRT, "Tip",
                "<color=#a0e0ff>Tip:</color> the floor pads in front of you show where each colour will land — read them ahead of time.",
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                pivot: new Vector2(0.5f, 1f), anchored: new Vector2(0f, -570f),
                size: new Vector2(1100f, 50f), fontSize: 22, alignment: TextAlignmentOptions.Center)
                .richText = true;

            // Start button.
            var btnGo = new GameObject("StartButton",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(cardRT, false);
            var btnRT = (RectTransform)btnGo.transform;
            btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0f);
            btnRT.pivot = new Vector2(0.5f, 0f);
            btnRT.anchoredPosition = new Vector2(0f, 50f);
            btnRT.sizeDelta = new Vector2(420f, 110f);
            btnGo.GetComponent<Image>().color = new Color(0.3f, 0.7f, 0.4f);
            var btn = btnGo.GetComponent<Button>();

            CreateTmp(btnRT, "Label", "START  (or press Space)",
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                pivot: new Vector2(0.5f, 0.5f), anchored: Vector2.zero,
                size: Vector2.zero, fontSize: 36, alignment: TextAlignmentOptions.Center);

            // Tutorial controller on its own host GameObject so it survives panel deactivation.
            var hostGo = new GameObject("TutorialOverlay");
            hostGo.transform.SetParent(canvas, false);
            var tut = hostGo.AddComponent<TutorialOverlay>();
            SetPrivate(tut, "panel", panelRT);
            SetPrivate(tut, "startButton", btn);
        }

        private static void BuildTutorialRow(Transform parent, float y, Color cubeColor,
            string what, string howTo, string keyHint)
        {
            // Colored cube icon (just a square Image).
            var iconGo = new GameObject("Icon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(parent, false);
            var iconRT = (RectTransform)iconGo.transform;
            iconRT.anchorMin = iconRT.anchorMax = new Vector2(0f, 1f);
            iconRT.pivot = new Vector2(0f, 1f);
            iconRT.anchoredPosition = new Vector2(50f, y);
            iconRT.sizeDelta = new Vector2(70f, 70f);
            iconGo.GetComponent<Image>().color = cubeColor;

            // "What" text (which cube)
            CreateTmp(parent, "What", what,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f), anchored: new Vector2(140f, y - 5f),
                size: new Vector2(450f, 40f), fontSize: 26, alignment: TextAlignmentOptions.MidlineLeft);

            // "How to" text — bigger, the actual gesture
            CreateTmp(parent, "HowTo", "→  " + howTo,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f), anchored: new Vector2(600f, y - 5f),
                size: new Vector2(450f, 40f), fontSize: 30, alignment: TextAlignmentOptions.MidlineLeft);

            // Key hint
            CreateTmp(parent, "Key", "<color=#888>" + keyHint + "</color>",
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f), anchored: new Vector2(1060f, y - 5f),
                size: new Vector2(150f, 40f), fontSize: 22, alignment: TextAlignmentOptions.MidlineRight)
                .richText = true;
        }

        private static void BuildGameOverPanel(Transform canvas, GameSession session)
        {
            var panelGo = new GameObject("GameOverPanel",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelGo.transform.SetParent(canvas, false);
            var panelRT = (RectTransform)panelGo.transform;
            panelRT.anchorMin = Vector2.zero; panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
            var bg = panelGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);

            var title = CreateTmp(panelRT, "Title", "GAME OVER",
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f), anchored: new Vector2(0f, 120f),
                size: new Vector2(900f, 140f), fontSize: 110, alignment: TextAlignmentOptions.Center);
            title.color = new Color(1f, 0.4f, 0.4f);

            var final = CreateTmp(panelRT, "FinalScore", "Final score: 0",
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f), anchored: new Vector2(0f, 0f),
                size: new Vector2(900f, 100f), fontSize: 44, alignment: TextAlignmentOptions.Center);

            // Restart button.
            var btnGo = new GameObject("RestartButton",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(panelRT, false);
            var btnRT = (RectTransform)btnGo.transform;
            btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0.5f);
            btnRT.pivot = new Vector2(0.5f, 0.5f);
            btnRT.anchoredPosition = new Vector2(0f, -150f);
            btnRT.sizeDelta = new Vector2(320f, 90f);
            btnGo.GetComponent<Image>().color = new Color(0.3f, 0.55f, 0.85f);
            var btn = btnGo.GetComponent<Button>();

            var btnLabel = CreateTmp(btnRT, "Label", "Restart (Space)",
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                pivot: new Vector2(0.5f, 0.5f), anchored: Vector2.zero,
                size: Vector2.zero, fontSize: 36, alignment: TextAlignmentOptions.Center);
            ((RectTransform)btnLabel.transform).offsetMin = Vector2.zero;
            ((RectTransform)btnLabel.transform).offsetMax = Vector2.zero;

            var goUiGo = new GameObject("GameOverUI");
            goUiGo.transform.SetParent(canvas, false);
            var goUi = goUiGo.AddComponent<GameOverUI>();
            SetPrivate(goUi, "session", session);
            SetPrivate(goUi, "panel", panelRT);
            SetPrivate(goUi, "titleLabel", title);
            SetPrivate(goUi, "finalScoreLabel", final);
            SetPrivate(goUi, "restartButton", btn);

            panelGo.SetActive(false);
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------
        private static TMP_Text CreateTmp(Transform parent, string name, string initialText,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchored,
            Vector2 size, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = initialText;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        // Assigns a fresh colored material without leaking — `.material` getter
        // in edit mode creates a stray instance and prints an error.
        private static void SetUniqueColor(MeshRenderer mr, Color color)
        {
            if (mr == null) return;
            var shader = (mr.sharedMaterial != null && mr.sharedMaterial.shader != null)
                ? mr.sharedMaterial.shader
                : Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader) { name = $"Tint_{ColorUtility.ToHtmlStringRGB(color)}" };
            mat.color = color;
            mr.sharedMaterial = mat;
        }

        private static void SetPrivate(Object target, string fieldName, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneBuilder] Field '{fieldName}' not found on {target.GetType().Name}. Skipping.");
                return;
            }
            if (value is Object unityObj) prop.objectReferenceValue = unityObj;
            else prop.boxedValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
