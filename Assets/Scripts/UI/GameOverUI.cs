using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FlexReality.BodyTracking
{
    // Fullscreen-ish dim panel that appears on game over with final score and a
    // Restart button. Pressing Space (or R) also restarts. We do NOT block
    // body tracking; the camera keeps running so the next round can start
    // immediately.
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text finalScoreLabel;
        [SerializeField] private Button restartButton;

        private void OnEnable()
        {
            if (session == null) session = FindAnyObjectByType<GameSession>();
            if (session != null)
            {
                session.OnGameOver += Show;
                session.OnRestart  += Hide;
            }
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
            Hide();
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.OnGameOver -= Show;
                session.OnRestart  -= Hide;
            }
            if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        private void Update()
        {
            if (session == null || session.IsAlive) return;
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame))
                OnRestartClicked();
        }

        private void OnRestartClicked() => session?.Restart();

        private void Show()
        {
            if (panel != null) panel.gameObject.SetActive(true);
            if (titleLabel != null) titleLabel.text = "GAME OVER";
            if (finalScoreLabel != null && session != null)
                finalScoreLabel.text = $"Final score: {session.Score}\nLasted: {session.Elapsed:0.0}s";
        }

        private void Hide()
        {
            if (panel != null) panel.gameObject.SetActive(false);
        }
    }
}
