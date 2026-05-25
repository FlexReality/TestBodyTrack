using TMPro;
using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Displays the current math question (e.g. "3 + 5 = ?") in the HUD.
    // Wire up: drag a TMP_Text label into questionLabel in the Inspector.
    public class MathQuestionUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text questionLabel;
        [SerializeField] private GameSession session;

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
    }
}
