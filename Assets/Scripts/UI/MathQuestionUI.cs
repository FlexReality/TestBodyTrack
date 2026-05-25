using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlexReality.BodyTracking
{
    // Shows the current math question ("3 + 5 = ?") in the HUD.
    // If questionLabel is not wired in the Inspector, creates its own
    // Canvas + TMP_Text automatically so no scene setup is required.
    public class MathQuestionUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text questionLabel;
        [SerializeField] private GameSession session;

        private void Awake()
        {
            if (questionLabel == null)
                questionLabel = CreateLabel();
        }

        private void OnEnable()
        {
            if (session == null) session = FindAnyObjectByType<GameSession>();
            if (session != null)
            {
                session.OnQuestionChanged += Refresh;
                session.OnRestart         += Refresh;
                session.OnGameOver        += Hide;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.OnQuestionChanged -= Refresh;
                session.OnRestart         -= Refresh;
                session.OnGameOver        -= Hide;
            }
        }

        private void Refresh()
        {
            if (questionLabel == null || session == null) return;
            questionLabel.gameObject.SetActive(true);
            questionLabel.text = session.CurrentQuestion.Display;
        }

        private void Hide()
        {
            if (questionLabel != null) questionLabel.gameObject.SetActive(false);
        }

        private TMP_Text CreateLabel()
        {
            // Reuse an existing Screen Space canvas if one is available.
            Canvas canvas = null;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                { canvas = c; break; }
            }

            if (canvas == null)
            {
                var cgo = new GameObject("MathQuestionCanvas");
                canvas = cgo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cgo.AddComponent<CanvasScaler>();
                cgo.AddComponent<GraphicRaycaster>();
            }

            var labelGo = new GameObject("QuestionLabel");
            labelGo.transform.SetParent(canvas.transform, false);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 90f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            // Top-center of screen.
            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -10f);
            rt.sizeDelta = new Vector2(0f, 120f);

            return tmp;
        }
    }
}
