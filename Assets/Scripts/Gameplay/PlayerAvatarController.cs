using UnityEngine;

namespace FlexReality.BodyTracking
{
    // Controls a simple 3-lane endless-runner avatar. Lane changes/jumps come
    // from BodyGestureDetector events. Attack hitboxes use a "swing window":
    // when a gesture triggers, the hitbox stays active for a short period and
    // continuously sweeps for obstacles entering the zone. That makes hits
    // forgiving — you don't need frame-perfect timing.
    public class PlayerAvatarController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BodyGestureDetector gestureDetector;
        [SerializeField] private Transform avatarRoot;

        [Header("Lane Movement")]
        [SerializeField] private float laneOffset = 1.6f;
        [SerializeField] private float laneLerpSpeed = 12f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 1.6f;
        [SerializeField] private float jumpDuration = 0.55f;

        [Header("Attack")]
        [SerializeField] private float attackRange = 1.3f;
        [SerializeField] private float forwardAttackOffset = 1.6f;
        [Tooltip("Must match ObstacleSpawner.sideTargetOffset so side hits line up.")]
        [SerializeField] private float sideAttackOffset = 1.8f;
        [SerializeField] private LayerMask obstacleLayer = ~0;
        [Tooltip("How long the attack hitbox stays active after the gesture fires. Bigger = easier to time.")]
        [SerializeField] private float swingDuration = 0.6f;
        [SerializeField] private float attackVisualDuration = 0.18f;

        private int currentLane;
        private float jumpTimer;
        private Vector3 basePos;

        private float forwardSwingUntil;
        private float leftSwingUntil;
        private float rightSwingUntil;

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
            // Body stays planted — we intentionally ignore MoveLeft/MoveRight so
            // body sway in front of the camera doesn't translate to lane shifts.
            // Tracking is hands + jumps only.
            switch (gesture)
            {
                case GestureType.Jump:         Jump(); break;
                case GestureType.HandsForward: StartForwardSwing(); break;
                case GestureType.RightHandUp:  StartSideSwing(+1); break;
                case GestureType.LeftHandUp:   StartSideSwing(-1); break;
            }
        }

        private void ShiftLane(int delta)
        {
            currentLane = Mathf.Clamp(currentLane + delta, -1, 1);
        }

        private void Jump()
        {
            if (jumpTimer <= 0f) jumpTimer = jumpDuration;
        }

        private void StartForwardSwing()
        {
            forwardSwingUntil = Time.time + swingDuration;
            SpawnAttackVisual(ForwardOrigin());
        }

        private void StartSideSwing(int dir)
        {
            if (dir > 0) rightSwingUntil = Time.time + swingDuration;
            else         leftSwingUntil  = Time.time + swingDuration;
            SpawnAttackVisual(SideOrigin(dir));
        }

        private Vector3 ForwardOrigin() =>
            avatarRoot.position + avatarRoot.forward * forwardAttackOffset;

        private Vector3 SideOrigin(int dir) =>
            avatarRoot.position + avatarRoot.right * dir * sideAttackOffset;

        private void Update()
        {
            // --- Motion ---
            Vector3 target = basePos;
            target.x = currentLane * laneOffset;

            float jumpY = 0f;
            if (jumpTimer > 0f)
            {
                jumpTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(jumpTimer / jumpDuration);
                jumpY = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            }
            target.y = basePos.y + jumpY;

            avatarRoot.localPosition = Vector3.Lerp(avatarRoot.localPosition, target, Time.deltaTime * laneLerpSpeed);

            // --- Active attack zones ---
            if (Time.time < forwardSwingUntil) SweepAttack(ForwardOrigin());
            if (Time.time < leftSwingUntil)    SweepAttack(SideOrigin(-1));
            if (Time.time < rightSwingUntil)   SweepAttack(SideOrigin(+1));
        }

        private void SweepAttack(Vector3 origin)
        {
            var hits = Physics.OverlapSphere(origin, attackRange, obstacleLayer);
            for (int i = 0; i < hits.Length; i++)
            {
                var oc = hits[i].GetComponentInParent<ObstacleController>();
                if (oc != null) oc.HitByPlayer();
            }
        }

        private void SpawnAttackVisual(Vector3 origin)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * (attackRange * 2f);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.material.color = new Color(1f, 0.9f, 0.2f, 0.5f);
            Destroy(go, attackVisualDuration);
        }

        private void OnDrawGizmosSelected()
        {
            if (avatarRoot == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(avatarRoot.position + avatarRoot.forward * forwardAttackOffset, attackRange);
            Gizmos.DrawWireSphere(avatarRoot.position + avatarRoot.right * sideAttackOffset, attackRange);
            Gizmos.DrawWireSphere(avatarRoot.position - avatarRoot.right * sideAttackOffset, attackRange);
        }
    }
}
