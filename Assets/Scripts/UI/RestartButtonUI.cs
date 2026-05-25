using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlexReality.BodyTracking
{
    // Two persistent buttons on the right edge:
    //   [↺ Recalibrate]  — resets body tracking baseline
    //   [↺ Restart]      — restarts the game session
    public class RestartButtonUI : MonoBehaviour
    {
        [SerializeField] private Button restartButton;
        [SerializeField] private Button calibrateButton;
        [SerializeField] private GameSession session;
        [SerializeField] private BodyGestureDetector gestureDetector;

        private void Awake()
        {
            var canvas = GetOrCreateCanvas();
            if (restartButton == null)
                restartButton = CreateButton(canvas, "RestartButton", "↺  Restart",
                    new Color(0.12f, 0.12f, 0.12f, 0.78f), 0f);
            if (calibrateButton == null)
                calibrateButton = CreateButton(canvas, "CalibrateButton", "⊕  Recalibrate",
                    new Color(0.08f, 0.18f, 0.28f, 0.78f), 60f);
        }

        private void OnEnable()
        {
            if (session == null) session = FindAnyObjectByType<GameSession>();
            if (gestureDetector == null) gestureDetector = FindAnyObjectByType<BodyGestureDetector>();

            if (restartButton != null)  restartButton.onClick.AddListener(OnRestart);
            if (calibrateButton != null) calibrateButton.onClick.AddListener(OnCalibrate);
        }

        private void OnDisable()
        {
            if (restartButton != null)  restartButton.onClick.RemoveListener(OnRestart);
            if (calibrateButton != null) calibrateButton.onClick.RemoveListener(OnCalibrate);
        }

        private void OnRestart()   => session?.Restart();
        private void OnCalibrate() => gestureDetector?.Recalibrate();

        // ── Helpers ────────────────────────────────────────────────────────

        private static Canvas GetOrCreateCanvas()
        {
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.renderMode == RenderMode.ScreenSpaceOverlay) return c;

            var cgo = new GameObject("HUDCanvas");
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        // offsetY: positive = higher on screen. Restart at 0, Calibrate at +60.
        private static Button CreateButton(Canvas canvas, string name, string label,
            Color bgColor, float offsetY)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(canvas.transform, false);

            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-20f, offsetY);
            rt.sizeDelta = new Vector2(160f, 50f);

            var img = btnGo.AddComponent<Image>();
            img.color = bgColor;

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = bgColor;
            colors.highlightedColor = bgColor + new Color(0.15f, 0.15f, 0.15f, 0f);
            colors.pressedColor     = bgColor - new Color(0.1f, 0.1f, 0.1f, 0f);
            btn.colors = colors;
            btn.targetGraphic = img;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }
    }
}
