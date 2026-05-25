using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlexReality.BodyTracking
{
    // Persistent restart button on the right edge — always visible, not just on game over.
    // Auto-creates itself if not wired in the Inspector.
    public class RestartButtonUI : MonoBehaviour
    {
        [SerializeField] private Button restartButton;
        [SerializeField] private GameSession session;

        private void Awake()
        {
            if (restartButton == null)
                restartButton = CreateButton();
        }

        private void OnEnable()
        {
            if (session == null) session = FindAnyObjectByType<GameSession>();
            if (restartButton != null)
                restartButton.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            if (restartButton != null)
                restartButton.onClick.RemoveListener(OnClick);
        }

        private void OnClick() => session?.Restart();

        private Button CreateButton()
        {
            Canvas canvas = null;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = c; break; }

            if (canvas == null)
            {
                var cgo = new GameObject("HUDCanvas");
                canvas = cgo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cgo.AddComponent<CanvasScaler>();
                cgo.AddComponent<GraphicRaycaster>();
            }

            // Button root
            var btnGo = new GameObject("RestartButton");
            btnGo.transform.SetParent(canvas.transform, false);

            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-20f, 0f);
            rt.sizeDelta = new Vector2(110f, 50f);

            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.75f);

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = new Color(0.15f, 0.15f, 0.15f, 0.75f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
            colors.pressedColor     = new Color(0.05f, 0.05f, 0.05f, 1f);
            btn.colors = colors;
            btn.targetGraphic = img;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "↺ Restart";
            tmp.fontSize = 20f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }
    }
}
