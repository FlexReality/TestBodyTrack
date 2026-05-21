using UnityEngine;
using UnityEngine.InputSystem;

namespace FlexReality.BodyTracking
{
    // Keyboard-driven stand-in for a real webcam tracking provider.
    // Synthesizes a plausible BodyPoseData every frame so the rest of the
    // pipeline (BodyGestureDetector, PlayerAvatarController) is identical
    // whether we're testing from keyboard or running real tracking.
    //
    // Mapping (matches in-game legend):
    //   Space = Jump                       → GREEN low cube
    //   A     = body shifts left in frame  → MoveLeft
    //   D     = body shifts right in frame → MoveRight
    //   W     = BOTH hands up              → RED centre cube
    //   Q     = LEFT  arm out horizontally → BLUE  left cube
    //   E     = RIGHT arm out horizontally → YELLOW right cube
    public class MockBodyTrackingProvider : MonoBehaviour, IBodyTrackingProvider
    {
        [Header("Mock Tuning")]
        [SerializeField] private float jumpDuration = 0.45f;
        [SerializeField] private float jumpUpAmount = 0.10f;        // how much hips move up in normalized space
        [SerializeField] private float lateralOffset = 0.18f;       // body shift left/right in normalized space
        [SerializeField] private float lateralLerpSpeed = 12f;
        [SerializeField] private float handUpAmount = 0.18f;        // wrist Y above shoulder when raised
        [SerializeField] private float armOutExtension = 0.20f;     // how far wrist X extends past shoulder for "arm out"

        private BodyPoseData pose;
        private float jumpTimer;
        private float lateralX;
        private bool bothHandsUp;
        private bool leftArmOut;
        private bool rightArmOut;

        public BodyPoseData CurrentPose => pose;
        public bool IsAvailable => true;

        public void Initialize()
        {
            if (pose == null) pose = new BodyPoseData();
            pose.IsTracking = true;
            BuildPose(); // ensure first read is valid even before Update runs
        }

        public void Shutdown() { }

        private void Awake() => Initialize();

        private void Update()
        {
            ReadKeyboard();
            BuildPose();
        }

        private void ReadKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.spaceKey.wasPressedThisFrame) jumpTimer = jumpDuration;
            if (jumpTimer > 0f) jumpTimer -= Time.deltaTime;

            float targetX = 0f;
            if (kb.aKey.isPressed) targetX = -lateralOffset;
            else if (kb.dKey.isPressed) targetX = lateralOffset;
            lateralX = Mathf.Lerp(lateralX, targetX, Time.deltaTime * lateralLerpSpeed);

            bothHandsUp = kb.wKey.isPressed;
            rightArmOut = kb.eKey.isPressed;
            leftArmOut  = kb.qKey.isPressed;
        }

        private void BuildPose()
        {
            // jump curve: sin over the active window, 0 otherwise
            float jumpUp = 0f;
            if (jumpTimer > 0f)
            {
                float t = 1f - (jumpTimer / jumpDuration);
                jumpUp = Mathf.Sin(t * Mathf.PI) * jumpUpAmount;
            }

            float cx = 0.5f + lateralX;
            float headY     = 0.20f - jumpUp;
            float shoulderY = 0.32f - jumpUp;
            float elbowY    = 0.45f - jumpUp;
            float wristY    = 0.58f - jumpUp;
            float hipY      = 0.58f - jumpUp;
            float kneeY     = 0.78f - jumpUp;
            float ankleY    = 0.96f - jumpUp;

            const float shoulderHalfW = 0.10f;
            const float hipHalfW = 0.08f;

            // Resolve each wrist position by priority:
            //   bothHandsUp ⇒ wrist Y above shoulder, X near shoulder
            //   armOut      ⇒ wrist Y at shoulder, X extended past shoulder outward
            //   default     ⇒ resting next to the body
            float leftShoulderX  = cx - shoulderHalfW;
            float rightShoulderX = cx + shoulderHalfW;

            float lWX, lWY, rWX, rWY;

            if (bothHandsUp)
            {
                lWX = leftShoulderX;
                rWX = rightShoulderX;
                lWY = shoulderY - handUpAmount;
                rWY = shoulderY - handUpAmount;
            }
            else
            {
                if (leftArmOut)
                {
                    lWX = leftShoulderX - armOutExtension;
                    lWY = shoulderY;
                }
                else
                {
                    lWX = leftShoulderX - 0.04f;
                    lWY = wristY;
                }

                if (rightArmOut)
                {
                    rWX = rightShoulderX + armOutExtension;
                    rWY = shoulderY;
                }
                else
                {
                    rWX = rightShoulderX + 0.04f;
                    rWY = wristY;
                }
            }

            pose.IsTracking = true;
            pose.Timestamp = Time.time;

            pose.Set(BodyKeypoint.Head,          new Vector3(cx, headY, 0f));
            pose.Set(BodyKeypoint.LeftShoulder,  new Vector3(cx - shoulderHalfW, shoulderY, 0f));
            pose.Set(BodyKeypoint.RightShoulder, new Vector3(cx + shoulderHalfW, shoulderY, 0f));
            pose.Set(BodyKeypoint.LeftElbow,     new Vector3(cx - shoulderHalfW - 0.02f, elbowY, 0f));
            pose.Set(BodyKeypoint.RightElbow,    new Vector3(cx + shoulderHalfW + 0.02f, elbowY, 0f));
            pose.Set(BodyKeypoint.LeftWrist,     new Vector3(lWX, lWY, 0f));
            pose.Set(BodyKeypoint.RightWrist,    new Vector3(rWX, rWY, 0f));
            pose.Set(BodyKeypoint.LeftHip,       new Vector3(cx - hipHalfW, hipY, 0f));
            pose.Set(BodyKeypoint.RightHip,      new Vector3(cx + hipHalfW, hipY, 0f));
            pose.Set(BodyKeypoint.LeftKnee,      new Vector3(cx - hipHalfW, kneeY, 0f));
            pose.Set(BodyKeypoint.RightKnee,     new Vector3(cx + hipHalfW, kneeY, 0f));
            pose.Set(BodyKeypoint.LeftAnkle,     new Vector3(cx - hipHalfW, ankleY, 0f));
            pose.Set(BodyKeypoint.RightAnkle,    new Vector3(cx + hipHalfW, ankleY, 0f));
        }
    }
}
