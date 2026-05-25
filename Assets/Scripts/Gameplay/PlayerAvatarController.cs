using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Lane movement follows body lean (hip position) — continuous, no gesture needed.
    // Shooting is triggered by hand gestures (LeftHandUp / RightHandUp / HandsForward).
    // Jump stays as a visual-only hop.
    public class PlayerAvatarController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BodyGestureDetector gestureDetector;
        [SerializeField] private Transform avatarRoot;

        [Header("Lane movement")]
        [SerializeField] private float laneOffset = 1.8f;
        [SerializeField] private float laneLerpSpeed = 8f;

        [Header("Jump (visual only)")]
        [SerializeField] private float jumpHeight = 1.6f;
        [SerializeField] private float jumpDuration = 0.55f;

        [Header("Projectile")]
        [SerializeField] private float projectileSpeed = 20f;
        [SerializeField] private float projectileMaxDistance = 25f;
        [SerializeField] private float projectileVisualRadius = 0.5f;
        [Tooltip("Height of the projectile spawn point (chest level).")]
        [SerializeField] private float chestHeight = 1.2f;
        [Tooltip("Seconds before the next shot is allowed.")]
        [SerializeField] private float shootCooldown = 1.2f;

        private int currentLane;   // -1 left | 0 centre | 1 right
        private float jumpTimer;
        private float lastShotTime = -99f;
        private Vector3 basePos;

        private void Awake()
        {
            if (avatarRoot == null) avatarRoot = transform;
            basePos = avatarRoot.localPosition;
        }

        private void OnEnable()
        {
            if (gestureDetector != null) gestureDetector.OnGestureTriggered += HandleGesture;
        }

        private void OnDisable()
        {
            if (gestureDetector != null) gestureDetector.OnGestureTriggered -= HandleGesture;
        }

        private void HandleGesture(GestureType gesture)
        {
            switch (gesture)
            {
                case GestureType.Jump:
                    Jump();
                    break;
                // Hand gestures shoot from whatever lane the body is currently in.
                case GestureType.LeftHandUp:
                case GestureType.RightHandUp:
                case GestureType.HandsForward:
                    if (Time.time - lastShotTime >= shootCooldown)
                    {
                        lastShotTime = Time.time;
                        LaunchProjectile(avatarRoot.position + Vector3.up * chestHeight);
                    }
                    break;
            }
        }

        private void Jump()
        {
            if (jumpTimer <= 0f) jumpTimer = jumpDuration;
        }

        private void Update()
        {
            // Continuous lane from body lean — read flags every frame.
            if (gestureDetector != null)
            {
                var flags = gestureDetector.CurrentGestures;
                if ((flags & GestureFlags.MoveLeft) != 0)       currentLane = -1;
                else if ((flags & GestureFlags.MoveRight) != 0) currentLane =  1;
                else                                             currentLane =  0;
            }

            // Lane lerp
            Vector3 target = basePos;
            target.x = currentLane * laneOffset;

            // Jump arc
            float jumpY = 0f;
            if (jumpTimer > 0f)
            {
                jumpTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(jumpTimer / jumpDuration);
                jumpY = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            }
            target.y = basePos.y + jumpY;

            avatarRoot.localPosition = Vector3.Lerp(avatarRoot.localPosition, target,
                Time.deltaTime * laneLerpSpeed);
        }

        private void LaunchProjectile(Vector3 origin)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "AttackProjectile";
            go.transform.position = origin;
            go.transform.forward = avatarRoot.forward;
            go.transform.localScale = Vector3.one * (projectileVisualRadius * 2f);

            // Remove default non-trigger collider — AttackProjectile adds its own.
            var defaultCol = go.GetComponent<Collider>();
            if (defaultCol != null) Destroy(defaultCol);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.material.color = new Color(1f, 0.88f, 0.1f, 1f);

            var proj = go.AddComponent<AttackProjectile>();
            proj.speed = projectileSpeed;
            proj.maxDistance = projectileMaxDistance;

            Destroy(go, projectileMaxDistance / Mathf.Max(projectileSpeed, 1f) + 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            if (avatarRoot == null) return;
            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(avatarRoot.position + Vector3.up * chestHeight,
                projectileVisualRadius);
        }
    }
}
