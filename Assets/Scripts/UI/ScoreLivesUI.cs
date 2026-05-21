using TMPro;
using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Top-center HUD: score on the left half, lives (hearts) on the right.
    // Subscribes to GameSession events; pulses on changes.
    public class ScoreLivesUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text livesLabel;
        [SerializeField] private GameSession session;

        private const string Heart = "<color=#ff4d4d>♥</color>";
        private const string EmptyHeart = "<color=#552222>♥</color>";

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
            if (scoreLabel != null) scoreLabel.text = $"Score: {session.Score}";
            if (livesLabel != null)
            {
                int alive = Mathf.Max(0, session.Lives);
                int total = Mathf.Max(alive, 3);
                var sb = new System.Text.StringBuilder("Lives: ");
                for (int i = 0; i < total; i++) sb.Append(i < alive ? Heart : EmptyHeart);
                livesLabel.text = sb.ToString();
            }
        }
    }
}
