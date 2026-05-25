using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlexReality.BodyTracking
{
    // Score (top-left) and lives hearts (top-right).
    // If labels are not wired in the Inspector they are created automatically.
    public class ScoreLivesUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text livesLabel;
        [SerializeField] private GameSession session;

        private const string Heart      = "<color=#FF4D4D>♥</color>";
        private const string EmptyHeart = "<color=#552222>♥</color>";

        private void Awake()
        {
            var canvas = GetOrCreateCanvas();
            if (scoreLabel == null) scoreLabel = CreateLabel(canvas, "ScoreLabel", TopLeft);
            else TopLeft(scoreLabel.rectTransform);

            if (livesLabel == null) livesLabel = CreateLabel(canvas, "LivesLabel", TopRight);
            else TopRight(livesLabel.rectTransform);
        }

        private void OnEnable()
        {
            if (session == null) session = FindAnyObjectByType<GameSession>();
            if (session != null)
            {
                session.OnScoreChanged += Refresh;
                session.OnLivesChanged += Refresh;
                session.OnRestart      += Refresh;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.OnScoreChanged -= Refresh;
                session.OnLivesChanged -= Refresh;
                session.OnRestart      -= Refresh;
            }
        }

        private void Refresh()
        {
            if (session == null) return;

            if (scoreLabel != null)
                scoreLabel.text = $"Score\n<size=72><b>{session.Score}</b></size>";

            if (livesLabel != null)
            {
                int alive = Mathf.Max(0, session.Lives);
                int total = Mathf.Max(alive, 3);
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < total; i++)
                    sb.Append(i < alive ? Heart : EmptyHeart);
                livesLabel.text = sb.ToString();
            }
        }

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

        private static TMP_Text CreateLabel(Canvas canvas, string name,
            System.Action<RectTransform> applyAnchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas.transform, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 36f;
            tmp.color = Color.white;
            applyAnchor(tmp.rectTransform);
            return tmp;
        }

        // Top-left: score
        private static void TopLeft(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24f, -16f);
            rt.sizeDelta = new Vector2(220f, 110f);
            var tmp = rt.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.alignment = TextAlignmentOptions.TopLeft;
        }

        // Top-right: lives
        private static void TopRight(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-24f, -16f);
            rt.sizeDelta = new Vector2(280f, 80f);
            var tmp = rt.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.alignment = TextAlignmentOptions.TopRight;
                tmp.fontSize = 48f;
            }
        }
    }
}
