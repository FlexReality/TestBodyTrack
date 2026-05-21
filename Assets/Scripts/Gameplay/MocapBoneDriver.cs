using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Drives a Mecanim Humanoid avatar from a BodyPoseData stream (2D mocap).
    //
    // We project BlazePose's normalized image-space keypoints onto a flat
    // world plane in front of the character, then rotate each upper-body bone
    // so its T-pose extension axis points along the desired world direction
    // (parent keypoint → child keypoint).
    //
    // Crucial detail: every Humanoid rig has different bone-local axes (Mixamo
    // arms typically extend along local +Y, but exporters disagree). We don't
    // guess — at startup we read each bone's CHILD-BONE world position and use
    // (child − parent).normalized as the "down the bone" direction in the rest
    // pose. Quaternion.FromToRotation then aligns that with the live direction.
    [RequireComponent(typeof(Animator))]
    public class MocapBoneDriver : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("Any MonoBehaviour that implements IBodyTrackingProvider.")]
        [SerializeField] private MonoBehaviour providerBehaviour;

        [Header("Projection")]
        [Tooltip("How wide (in world units) the camera's normalized [0..1] range maps to.")]
        [SerializeField] private float worldScale = 2.5f;
        [Tooltip("Where in world space the keypoint (0.5, 0.5) lands. Usually near the character's chest.")]
        [SerializeField] private Vector3 projectionAnchor = new Vector3(0f, 1.4f, 0f);
        [Tooltip("Smoothing on the projected keypoints (0 = none, ~10 = stable).")]
        [SerializeField] private float smoothing = 12f;

        [Header("Confidence")]
        [Tooltip("Keypoints below this visibility are skipped (bone keeps its T-pose).")]
        [SerializeField] private float minConfidence = 0.2f;

        [Header("Drive what")]
        [SerializeField] private bool driveArms = true;
        [Tooltip("Off by default — needs more careful math; enable after arms work.")]
        [SerializeField] private bool driveHead = false;
        [Tooltip("Off by default — needs more careful math; enable after arms work.")]
        [SerializeField] private bool driveChest = false;

        private IBodyTrackingProvider provider;
        private Animator animator;

        private Transform bChest;
        private Transform bHead;
        private Transform bLUpperArm, bLLowerArm, bLHand;
        private Transform bRUpperArm, bRLowerArm, bRHand;

        // Captured at startup, in world space — used to "anchor" the math to
        // each bone's actual rest orientation regardless of rig convention.
        private struct BoneRest
        {
            public Quaternion rotation;        // bone.rotation at rest pose (world)
            public Vector3 extensionDir;       // direction from this bone to its child, world space
            public bool valid;
        }
        private BoneRest restLUpper, restLLower, restRUpper, restRLower;
        private BoneRest restChest, restHead;

        // Smoothed world positions per keypoint.
        private Vector3 wHead, wLSh, wRSh, wLEl, wREl, wLWr, wRWr, wMidHip;
        private bool smoothInitialized;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            provider = providerBehaviour as IBodyTrackingProvider;

            if (animator == null || !animator.isHuman)
            {
                Debug.LogError("[MocapBoneDriver] Requires a Humanoid Animator on this GameObject.", this);
                enabled = false;
                return;
            }

            bChest     = animator.GetBoneTransform(HumanBodyBones.Chest)
                      ?? animator.GetBoneTransform(HumanBodyBones.Spine);
            bHead      = animator.GetBoneTransform(HumanBodyBones.Head);
            bLUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            bLLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            bLHand     = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            bRUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            bRLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            bRHand     = animator.GetBoneTransform(HumanBodyBones.RightHand);

            CaptureRest();
        }

        private void CaptureRest()
        {
            restLUpper = MakeRest(bLUpperArm, bLLowerArm);
            restLLower = MakeRest(bLLowerArm, bLHand);
            restRUpper = MakeRest(bRUpperArm, bRLowerArm);
            restRLower = MakeRest(bRLowerArm, bRHand);

            // Chest "extension" = direction from chest up to neck/head.
            var neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            restChest = MakeRest(bChest, neck != null ? neck : bHead);

            // Head "extension" = upward direction from head; approximate via head transform Y axis.
            if (bHead != null)
            {
                restHead.rotation = bHead.rotation;
                restHead.extensionDir = (bHead.rotation * Vector3.up).normalized;
                restHead.valid = true;
            }
        }

        private static BoneRest MakeRest(Transform bone, Transform child)
        {
            var r = new BoneRest();
            if (bone == null || child == null) return r;
            Vector3 dir = child.position - bone.position;
            if (dir.sqrMagnitude < 1e-8f) return r;
            r.rotation = bone.rotation;
            r.extensionDir = dir.normalized;
            r.valid = true;
            return r;
        }

        public void SetProvider(IBodyTrackingProvider p) => provider = p;

        // Run AFTER the Animator so our overrides win for the frame.
        private void LateUpdate()
        {
            if (provider == null) return;
            var pose = provider.CurrentPose;
            if (pose == null || !pose.IsTracking) return;

            UpdateProjected(pose);

            if (driveArms)
            {
                DriveBoneToDirection(bLUpperArm, restLUpper, (wLEl - wLSh));
                DriveBoneToDirection(bLLowerArm, restLLower, (wLWr - wLEl));
                DriveBoneToDirection(bRUpperArm, restRUpper, (wREl - wRSh));
                DriveBoneToDirection(bRLowerArm, restRLower, (wRWr - wREl));
            }

            if (driveChest && restChest.valid && bChest != null)
            {
                Vector3 midSh = (wLSh + wRSh) * 0.5f;
                DriveBoneToDirection(bChest, restChest, (midSh - wMidHip));
            }

            if (driveHead && restHead.valid && bHead != null)
            {
                Vector3 midSh = (wLSh + wRSh) * 0.5f;
                DriveBoneToDirection(bHead, restHead, (wHead - midSh));
            }
        }

        private void UpdateProjected(BodyPoseData pose)
        {
            Vector3 head = ProjectKp(pose, BodyKeypoint.Head, wHead);
            Vector3 lSh  = ProjectKp(pose, BodyKeypoint.LeftShoulder, wLSh);
            Vector3 rSh  = ProjectKp(pose, BodyKeypoint.RightShoulder, wRSh);
            Vector3 lEl  = ProjectKp(pose, BodyKeypoint.LeftElbow, wLEl);
            Vector3 rEl  = ProjectKp(pose, BodyKeypoint.RightElbow, wREl);
            Vector3 lWr  = ProjectKp(pose, BodyKeypoint.LeftWrist, wLWr);
            Vector3 rWr  = ProjectKp(pose, BodyKeypoint.RightWrist, wRWr);
            Vector3 lHip = ProjectKp(pose, BodyKeypoint.LeftHip, wMidHip);
            Vector3 rHip = ProjectKp(pose, BodyKeypoint.RightHip, wMidHip);
            Vector3 midHip = (lHip + rHip) * 0.5f;

            if (!smoothInitialized)
            {
                wHead = head; wLSh = lSh; wRSh = rSh;
                wLEl = lEl; wREl = rEl; wLWr = lWr; wRWr = rWr;
                wMidHip = midHip;
                smoothInitialized = true;
                return;
            }

            float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            wHead   = Vector3.Lerp(wHead, head, k);
            wLSh    = Vector3.Lerp(wLSh, lSh, k);
            wRSh    = Vector3.Lerp(wRSh, rSh, k);
            wLEl    = Vector3.Lerp(wLEl, lEl, k);
            wREl    = Vector3.Lerp(wREl, rEl, k);
            wLWr    = Vector3.Lerp(wLWr, lWr, k);
            wRWr    = Vector3.Lerp(wRWr, rWr, k);
            wMidHip = Vector3.Lerp(wMidHip, midHip, k);
        }

        private Vector3 ProjectKp(BodyPoseData pose, BodyKeypoint kp, Vector3 fallback)
        {
            var d = pose.Get(kp);
            if (d.Confidence < minConfidence) return fallback;
            return new Vector3(
                (d.Position.x - 0.5f) * worldScale,
                (0.5f - d.Position.y) * worldScale,
                0f
            ) + transform.position + projectionAnchor;
        }

        // Rotates the bone so its rest-pose extension direction lines up with
        // `worldDir`. Preserves the bone's rest roll (twist around the bone
        // axis) because we apply only the minimal rotation that maps one axis
        // onto another.
        private static void DriveBoneToDirection(Transform bone, BoneRest rest, Vector3 worldDir)
        {
            if (bone == null || !rest.valid) return;
            if (worldDir.sqrMagnitude < 1e-6f) return;
            Vector3 target = worldDir.normalized;
            Quaternion delta = Quaternion.FromToRotation(rest.extensionDir, target);
            bone.rotation = delta * rest.rotation;
        }
    }
}
