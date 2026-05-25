using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Translates body gestures into game actions:
    //   HandsForward  → projectile from centre (chest height)
    //   RightHandUp   → projectile from right side
    //   LeftHandUp    → projectile from left side
    //   Jump          → avatar hops (visual only, no scoring)
    public class PlayerAvatarController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BodyGestureDetector gestureDetector;
        [SerializeField] private Transform avatarRoot;

        [Header("Jump (visual only)")]
        [SerializeField] private float jumpHeight = 1.6f;
        [SerializeField] private float jumpDuration = 0.55f;

        [Header("Projectile")]
        [SerializeField] private float projectileSpeed = 20f;
        [SerializeField] private float projectileMaxDistance = 25f;
        [SerializeField] private float projectileVisualRadius = 0.5f;
        [Tooltip("Side offset for left/right hand projectile spawn. Must match ObstacleSpawner.sideTargetOffset.")]
        [SerializeField] private float sideAttackOffset = 1.8f;
        [Tooltip("Height of the projectile spawn point (chest level).")]
        [SerializeField] private float chestHeight = 1.2f;

        private float jumpTimer;
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
                case GestureType.HandsForward:
                    LaunchProjectile(CentreOrigin());
                    break;
                case GestureType.RightHandUp:
                    LaunchProjectile(SideOrigin(+1));
                    break;
                case GestureType.LeftHandUp:
                    LaunchProjectile(SideOrigin(-1));
                    break;
            }
        }

        private void Jump()
        {
            if (jumpTimer <= 0f) jumpTimer = jumpDuration;
        }

        private void Update()
        {
            float jumpY = 0f;
            if (jumpTimer > 0f)
            {
                jumpTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(jumpTimer / jumpDuration);
                jumpY = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            }

            Vector3 target = basePos;
            target.y = basePos.y + jumpY;
            avatarRoot.localPosition = Vector3.Lerp(avatarRoot.localPosition, target,
                Time.deltaTime * 12f);
        }

        // ── Projectile helpers ────────────────────────────────────────────

        private Vector3 CentreOrigin() =>
            avatarRoot.position + Vector3.up * chestHeight;

        private Vector3 SideOrigin(int dir) =>
            avatarRoot.position + avatarRoot.right * dir * sideAttackOffset + Vector3.up * chestHeight;

        private void LaunchProjectile(Vector3 origin)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "AttackProjectile";
            go.transform.position = origin;
            go.transform.forward = avatarRoot.forward;
            go.transform.localScale = Vector3.one * (projectileVisualRadius * 2f);

            // Remove the default non-trigger collider — AttackProjectile adds its own.
            var defaultCol = go.GetComponent<Collider>();
            if (defaultCol != null) Destroy(defaultCol);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.material.color = new Color(1f, 0.88f, 0.1f, 1f); // yellow

            var proj = go.AddComponent<AttackProjectile>();
            proj.speed = projectileSpeed;
            proj.maxDistance = projectileMaxDistance;

            Destroy(go, projectileMaxDistance / Mathf.Max(projectileSpeed, 1f) + 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            if (avatarRoot == null) return;
            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(CentreOrigin(), projectileVisualRadius);
            Gizmos.DrawWireSphere(SideOrigin(+1), projectileVisualRadius);
            Gizmos.DrawWireSphere(SideOrigin(-1), projectileVisualRadius);
        }
    }
}
