using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Subtly drifts the camera based on the tracked player's body position so
    // every little movement gives a cinematic feedback — even though the
    // avatar itself stays planted. Hook this on Main Camera and assign any
    // IBodyTrackingProvider (mock or webcam).
    public class CameraSway : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private MonoBehaviour providerBehaviour;

        [Header("Magnitude (max world-units of drift)")]
        [SerializeField] private float horizontalMagnitude = 0.6f;
        [SerializeField] private float verticalMagnitude = 0.25f;

        [Header("Smoothing")]
        [Tooltip("How quickly the camera chases the target position. Higher = snappier, lower = floatier.")]
        [SerializeField] private float swayLerpSpeed = 4f;

        [Header("Direction")]
        [Tooltip("Flip if camera moves the 'wrong' way relative to your body — depends on mirror setting on the provider.")]
        [SerializeField] private bool invertHorizontal = false;

        private IBodyTrackingProvider provider;
        private Vector3 basePosition;
        private float baselineHipY = -1f;
        private const float JumpYGain = 6f;

        private void Awake()
        {
            provider = providerBehaviour as IBodyTrackingProvider;
            basePosition = transform.position;
        }

        public void SetProvider(IBodyTrackingProvider p) => provider = p;

        private void LateUpdate()
        {
            if (provider == null) return;
            var pose = provider.CurrentPose;
            if (pose == null || !pose.IsTracking) return;

            var lh = pose.Get(BodyKeypoint.LeftHip).Position;
            var rh = pose.Get(BodyKeypoint.RightHip).Position;
            var midHip = (lh + rh) * 0.5f;

            // Lateral: deviation of mid-hip from frame centre [-1..+1].
            float lateralNorm = Mathf.Clamp((midHip.x - 0.5f) * 2f, -1f, 1f);
            if (invertHorizontal) lateralNorm = -lateralNorm;

            // Vertical: detect jumps as a fast hip rise vs a slow baseline.
            if (baselineHipY < 0f) baselineHipY = midHip.y;
            else baselineHipY = Mathf.Lerp(baselineHipY, midHip.y, Time.deltaTime * 0.4f);
            float jumpUpNorm = Mathf.Clamp01((baselineHipY - midHip.y) * JumpYGain);

            Vector3 sway = new Vector3(
                lateralNorm * horizontalMagnitude,
                jumpUpNorm  * verticalMagnitude,
                0f);

            transform.position = Vector3.Lerp(transform.position, basePosition + sway, Time.deltaTime * swayLerpSpeed);
        }
    }
}
