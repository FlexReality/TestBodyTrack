using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Adds procedural leg-swing animation to any Humanoid avatar.
    // Detects movement automatically from world-space velocity.
    // Runs in LateUpdate so it overrides whatever the Animator plays (or doesn't play).
    [RequireComponent(typeof(Animator))]
    public class ProceduralLegAnimator : MonoBehaviour
    {
        [Header("Swing")]
        [Tooltip("Max swing angle in degrees forward/back.")]
        [SerializeField] private float amplitude = 35f;
        [Tooltip("Steps per second at full speed.")]
        [SerializeField] private float frequency = 6f;
        [Tooltip("How quickly the legs blend in/out when starting or stopping.")]
        [SerializeField] private float blendSpeed = 6f;

        [Header("Velocity detection")]
        [Tooltip("World-units/sec above which legs start swinging.")]
        [SerializeField] private float moveThreshold = 0.15f;

        private Animator anim;

        private Transform leftUpper,  rightUpper;
        private Transform leftLower,  rightLower;

        private Quaternion leftUpperRest,  rightUpperRest;
        private Quaternion leftLowerRest,  rightLowerRest;

        private float phase;
        private float blendWeight;   // 0 = idle, 1 = full swing
        private Vector3 lastPos;

        private void Awake()
        {
            anim = GetComponent<Animator>();
            if (anim == null || !anim.isHuman) { enabled = false; return; }

            leftUpper  = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            rightUpper = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            leftLower  = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            rightLower = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);

            if (leftUpper)  leftUpperRest  = leftUpper.localRotation;
            if (rightUpper) rightUpperRest = rightUpper.localRotation;
            if (leftLower)  leftLowerRest  = leftLower.localRotation;
            if (rightLower) rightLowerRest = rightLower.localRotation;

            lastPos = transform.position;
        }

        private void LateUpdate()
        {
            // Measure velocity from root movement.
            float velocity = (transform.position - lastPos).magnitude / Time.deltaTime;
            lastPos = transform.position;

            bool moving = velocity > moveThreshold;
            float targetWeight = moving ? 1f : 0f;
            blendWeight = Mathf.MoveTowards(blendWeight, targetWeight, Time.deltaTime * blendSpeed);

            if (blendWeight < 0.001f)
            {
                // Reset to rest pose smoothly.
                if (leftUpper)  leftUpper.localRotation  = Quaternion.Lerp(leftUpper.localRotation,  leftUpperRest,  Time.deltaTime * blendSpeed);
                if (rightUpper) rightUpper.localRotation = Quaternion.Lerp(rightUpper.localRotation, rightUpperRest, Time.deltaTime * blendSpeed);
                if (leftLower)  leftLower.localRotation  = Quaternion.Lerp(leftLower.localRotation,  leftLowerRest,  Time.deltaTime * blendSpeed);
                if (rightLower) rightLower.localRotation = Quaternion.Lerp(rightLower.localRotation, rightLowerRest, Time.deltaTime * blendSpeed);
                phase = 0f;
                return;
            }

            phase += Time.deltaTime * frequency * Mathf.PI * 2f;

            float swing = Mathf.Sin(phase) * amplitude * blendWeight;

            // Upper legs swing forward/back alternately.
            if (leftUpper)
                leftUpper.localRotation  = leftUpperRest  * Quaternion.Euler( swing, 0f, 0f);
            if (rightUpper)
                rightUpper.localRotation = rightUpperRest * Quaternion.Euler(-swing, 0f, 0f);

            // Lower legs (knee) bend slightly when the leg swings back.
            float leftKnee  = Mathf.Max(0f,  swing) * 0.6f;
            float rightKnee = Mathf.Max(0f, -swing) * 0.6f;
            if (leftLower)
                leftLower.localRotation  = leftLowerRest  * Quaternion.Euler(leftKnee,  0f, 0f);
            if (rightLower)
                rightLower.localRotation = rightLowerRest * Quaternion.Euler(rightKnee, 0f, 0f);
        }
    }
}
