using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FlexReality.BodyTracking
{
    // Fullscreen tutorial card shown ONCE on scene load. Pauses the game via
    // Time.timeScale = 0 so the player has time to read. Click Start (or press
    // Space) to begin. Webcam inference keeps running because it gates on
    // Time.unscaledTime, so by the time the player hits Start tracking is
    // already warmed up.
    public class TutorialOverlay : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button startButton;

        private bool hasShown;

        private void Awake()
        {
            if (panel == null) panel = (RectTransform)transform;
            if (!hasShown) Show();
        }

        private void OnEnable()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStart);
        }

        private void OnDisable()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStart);
            // Belt-and-braces: never leave the timescale at 0 if this gets disabled mid-pause.
            if (Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = 1f;
        }

        private void Update()
        {
            if (!hasShown) return;
            if (Time.timeScale > 0f) return;
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                OnStart();
        }

        private void Show()
        {
            hasShown = true;
            if (panel != null) panel.gameObject.SetActive(true);
            Time.timeScale = 0f;
        }

        private void OnStart()
        {
            Time.timeScale = 1f;
            if (panel != null) panel.gameObject.SetActive(false);
        }
    }
}
