using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FlexReality.BodyTracking
{
    // Renders the live webcam feed in a corner of the screen with detected
    // keypoints overlaid as dots. Pure UGUI — no shaders, no extra packages.
    // The whole point is to make "is tracking working?" answerable at a glance.
    public class PoseOverlayUI : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private MonoBehaviour providerBehaviour;  // IBodyTrackingProvider
        [SerializeField] private WebcamBodyTrackingProvider webcamProvider; // optional, for live feed

        [Header("Layout")]
        [SerializeField] private RectTransform overlayRoot;
        [SerializeField] private RawImage webcamView;
        [SerializeField] private Color dotColor = new Color(0.2f, 1f, 0.4f, 1f);
        [SerializeField] private float dotSize = 10f;
        [SerializeField] private float minConfidence = 0.2f;

        private IBodyTrackingProvider provider;
        private readonly List<RectTransform> dots = new List<RectTransform>();

        private void Awake()
        {
            provider = providerBehaviour as IBodyTrackingProvider;
        }

        private void Start()
        {
            EnsureDots();
            if (webcamView != null && webcamProvider != null && webcamProvider.WebcamTexture != null)
            {
                webcamView.texture = webcamProvider.WebcamTexture;
            }
        }

        private void Update()
        {
            // Late-bind the webcam preview once it actually starts.
            if (webcamView != null && webcamProvider != null && webcamView.texture == null)
            {
                if (webcamProvider.WebcamTexture != null) webcamView.texture = webcamProvider.WebcamTexture;
            }

            if (provider == null || overlayRoot == null) return;
            var pose = provider.CurrentPose;
            if (pose == null) return;

            var rect = overlayRoot.rect;
            for (int i = 0; i < (int)BodyKeypoint.Count; i++)
            {
                var dot = dots[i];
                var kp = pose.Keypoints[i];
                if (kp.Confidence < minConfidence)
                {
                    dot.gameObject.SetActive(false);
                    continue;
                }
                dot.gameObject.SetActive(true);

                // Normalized (x,y) with y=0 at top → flip to UGUI's y=0 at bottom.
                float px = kp.Position.x * rect.width;
                float py = (1f - kp.Position.y) * rect.height;
                dot.anchoredPosition = new Vector2(px - rect.width * 0.5f, py - rect.height * 0.5f);
            }
        }

        private void EnsureDots()
        {
            if (overlayRoot == null) return;
            int count = (int)BodyKeypoint.Count;
            while (dots.Count < count)
            {
                var go = new GameObject($"Dot_{(BodyKeypoint)dots.Count}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(overlayRoot, false);
                var img = go.GetComponent<Image>();
                img.color = dotColor;
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(dotSize, dotSize);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                dots.Add(rt);
            }
        }
    }
}
