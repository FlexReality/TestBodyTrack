using System;
using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Reads BodyPoseData from any IBodyTrackingProvider and converts it to
    // discrete gestures + a continuous flags bitmask. Tuned for MediaPipe-style
    // normalized coordinates (x,y in [0,1] with y growing downwards).
    public class BodyGestureDetector : MonoBehaviour
    {
        [Header("Provider")]
        [Tooltip("Any MonoBehaviour that implements IBodyTrackingProvider (e.g. MockBodyTrackingProvider).")]
        [SerializeField] private MonoBehaviour providerBehaviour;

        [Header("Jump")]
        [SerializeField] private float jumpYDelta = 0.04f;      // hips moved this far above baseline (normalized)
        [SerializeField] private float jumpCooldown = 0.5f;
        [SerializeField] private float baselineLerpSpeed = 0.5f;

        [Header("Lateral")]
        [SerializeField] private float lateralThreshold = 0.08f; // body center offset from frame center

        [Header("Hands")]
        [Tooltip("Wrist Y must be at least this much above the shoulder Y to count as 'up'. Used for BothHandsUp.")]
        [SerializeField] private float handUpMargin = 0.04f;
        [Tooltip("Wrist X must be at least this far horizontally past the shoulder X to count as 'arm out to the side'.")]
        [SerializeField] private float armSideMinExtension = 0.12f;
        [Tooltip("Wrist Y must stay within this band around the shoulder Y to count as a horizontal 'arm out' gesture (not pointing up/down).")]
        [SerializeField] private float armSideYBand = 0.10f;

        public GestureFlags CurrentGestures { get; private set; }
        public event Action<GestureType> OnGestureTriggered;   // fires once when gesture activates
        public event Action<GestureFlags> OnGesturesUpdated;   // fires every frame with the current mask

        private IBodyTrackingProvider provider;
        private float baselineHipY = -1f;
        private float lastJumpTime = -10f;
        private GestureFlags previousFlags;
        private bool wasTracking;

        private void Awake()
        {
            provider = providerBehaviour as IBodyTrackingProvider;
            if (provider == null)
            {
                Debug.LogError("[BodyGestureDetector] providerBehaviour must implement IBodyTrackingProvider", this);
            }
        }

        public void SetProvider(IBodyTrackingProvider newProvider)
        {
            provider = newProvider;
            baselineHipY = -1f;
            previousFlags = GestureFlags.None;
        }

        // Reset all calibration state — call when a new player steps in or
        // tracking looks off. Jump baseline re-learns within a few frames.
        public void Recalibrate()
        {
            baselineHipY = -1f;
            previousFlags = GestureFlags.None;
            wasTracking = false;
        }

        private void Update()
        {
            if (provider == null) return;
            var pose = provider.CurrentPose;
            if (pose == null || !pose.IsTracking)
            {
                wasTracking = false;
                return;
            }

            // New person entered the frame — reset calibration so their body
            // proportions don't inherit the previous player's jump baseline.
            if (!wasTracking)
            {
                baselineHipY = -1f;
                previousFlags = GestureFlags.None;
                wasTracking = true;
            }

            var flags = Evaluate(pose);
            CurrentGestures = flags;
            OnGesturesUpdated?.Invoke(flags);

            EmitIfNewlySet(flags, previousFlags, GestureFlags.Jump,         GestureType.Jump);
            EmitIfNewlySet(flags, previousFlags, GestureFlags.MoveLeft,     GestureType.MoveLeft);
            EmitIfNewlySet(flags, previousFlags, GestureFlags.MoveRight,    GestureType.MoveRight);
            EmitIfNewlySet(flags, previousFlags, GestureFlags.HandsForward, GestureType.HandsForward);
            EmitIfNewlySet(flags, previousFlags, GestureFlags.RightHandUp,  GestureType.RightHandUp);
            EmitIfNewlySet(flags, previousFlags, GestureFlags.LeftHandUp,   GestureType.LeftHandUp);

            previousFlags = flags;
        }

        private GestureFlags Evaluate(BodyPoseData pose)
        {
            var flags = GestureFlags.None;

            var lHip = pose.Get(BodyKeypoint.LeftHip).Position;
            var rHip = pose.Get(BodyKeypoint.RightHip).Position;
            var midHip = (lHip + rHip) * 0.5f;

            // Track a slow-moving baseline so the player can stand at slightly
            // different heights; jumps are detected as a fast rise above it.
            if (baselineHipY < 0f) baselineHipY = midHip.y;
            else baselineHipY = Mathf.Lerp(baselineHipY, midHip.y, Time.deltaTime * baselineLerpSpeed);

            if ((baselineHipY - midHip.y) > jumpYDelta && Time.time - lastJumpTime > jumpCooldown)
            {
                flags |= GestureFlags.Jump;
                lastJumpTime = Time.time;
            }

            float lateral = midHip.x - 0.5f;
            if (lateral < -lateralThreshold) flags |= GestureFlags.MoveLeft;
            else if (lateral > lateralThreshold) flags |= GestureFlags.MoveRight;

            var lWrist    = pose.Get(BodyKeypoint.LeftWrist).Position;
            var rWrist    = pose.Get(BodyKeypoint.RightWrist).Position;
            var lShoulder = pose.Get(BodyKeypoint.LeftShoulder).Position;
            var rShoulder = pose.Get(BodyKeypoint.RightShoulder).Position;

            // Y grows downward in image space — "up" means smaller y.
            // GestureFlags.HandsForward is *semantically* now "BothHandsUp" —
            // both wrists raised clearly above their shoulders. Used for the
            // RED centre cube.
            bool leftUp  = lWrist.y < lShoulder.y - handUpMargin;
            bool rightUp = rWrist.y < rShoulder.y - handUpMargin;
            if (leftUp && rightUp)
                flags |= GestureFlags.HandsForward;

            // LeftHandUp / RightHandUp are *semantically* now "arm extended
            // horizontally to the side" (used for BLUE and YELLOW side cubes).
            //   • wrist X is far past the shoulder X in the matching direction
            //   • wrist Y stays in a band around the shoulder Y (not raised up,
            //     not hanging down)
            //
            // Image-space X grows to the right; the player's LEFT side in
            // screen space has SMALLER x. After the mirror+swap in the webcam
            // provider, our LeftShoulder/LeftWrist are guaranteed to be on the
            // visual LEFT of the screen, matching the player's perceived left.
            bool leftArmHoriz =
                (lShoulder.x - lWrist.x) > armSideMinExtension &&
                Mathf.Abs(lWrist.y - lShoulder.y) < armSideYBand &&
                !leftUp;
            bool rightArmHoriz =
                (rWrist.x - rShoulder.x) > armSideMinExtension &&
                Mathf.Abs(rWrist.y - rShoulder.y) < armSideYBand &&
                !rightUp;
            if (leftArmHoriz)  flags |= GestureFlags.LeftHandUp;
            if (rightArmHoriz) flags |= GestureFlags.RightHandUp;

            return flags;
        }

        private void EmitIfNewlySet(GestureFlags now, GestureFlags before, GestureFlags mask, GestureType type)
        {
            if ((now & mask) != 0 && (before & mask) == 0)
                OnGestureTriggered?.Invoke(type);
        }
    }
}
