using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlexReality.BodyTracking
{
    public class FpsCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text tmpLabel;
        [SerializeField] private Text legacyLabel;
        [SerializeField] private float refreshInterval = 0.25f;

        private float accum;
        private int frames;
        private float timer;
        private float lastFps;

        private void Update()
        {
            accum += Time.unscaledDeltaTime;
            frames++;
            timer += Time.unscaledDeltaTime;
            if (timer < refreshInterval) return;

            lastFps = frames / accum;
            timer = 0f;
            frames = 0;
            accum = 0f;

            string text = $"FPS: {lastFps:0.}";
            if (tmpLabel != null) tmpLabel.text = text;
            if (legacyLabel != null) legacyLabel.text = text;
        }
    }
}
