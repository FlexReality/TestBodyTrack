using UnityEngine;

namespace FlexReality.BodyTracking
{
    public enum ObstacleDirection
    {
        Front,   // chest-height, requires HandsForward
        Left,    // chest-height from left, requires LeftHandUp
        Right,   // chest-height from right, requires RightHandUp
        Bottom   // low, requires Jump (player must be airborne when it arrives)
    }

    // Flies in a straight line toward a target. If the player destroys it via
    // attack (HitByPlayer) → score. If it reaches the target alive → miss.
    public class ObstacleController : MonoBehaviour
    {
        [Tooltip("Y-position of the player above which a Bottom obstacle counts as 'dodged' instead of a hit.")]
        public float dodgeHeightThreshold = 0.8f;

        public ObstacleDirection Direction { get; private set; }

        private Vector3 velocity;
        private Vector3 target;
        private float timeToReach;
        private float elapsed;
        private Vector3 spinAxis;
        private float spinSpeed;
        private bool consumed;
        private Transform playerRef;

        public void Launch(ObstacleDirection dir, Vector3 vel, Vector3 targetPos, Transform player)
        {
            Direction = dir;
            velocity = vel;
            target = targetPos;
            playerRef = player;
            elapsed = 0f;
            float speed = vel.magnitude;
            float distance = Vector3.Distance(transform.position, targetPos);
            timeToReach = speed > 0.01f ? distance / speed : 8f;

            spinAxis = Random.onUnitSphere;
            spinSpeed = Random.Range(120f, 320f);
        }

        private void Update()
        {
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);

            elapsed += Time.deltaTime;
            if (consumed) return;

            // Reached the player.
            if (elapsed >= timeToReach)
            {
                consumed = true;
                if (Direction == ObstacleDirection.Bottom && playerRef != null
                    && playerRef.position.y > dodgeHeightThreshold)
                {
                    // Player jumped over a low cube → success.
                    GameSession.Instance?.RegisterHit();
                    FlashAndDestroy(new Color(0.4f, 1f, 0.6f), 0.15f);
                }
                else
                {
                    GameSession.Instance?.RegisterMiss();
                    FlashAndDestroy(new Color(0.9f, 0.2f, 0.2f), 0.18f);
                }
            }
        }

        public void HitByPlayer()
        {
            if (consumed) return;
            consumed = true;
            GameSession.Instance?.RegisterHit();
            FlashAndDestroy(new Color(1f, 0.95f, 0.4f), 0.12f);
        }

        private void FlashAndDestroy(Color flash, float duration)
        {
            // Simple visual feedback: pulse color + shrink, then disappear.
            var mr = GetComponentInChildren<MeshRenderer>();
            if (mr != null) mr.material.color = flash;
            // Scale animation handled by tiny coroutine-free trick: detach physics, set destroy timer.
            var anim = gameObject.AddComponent<ObstacleDeathAnim>();
            anim.duration = duration;
            velocity = Vector3.zero;
            Destroy(gameObject, duration);
            enabled = false;
        }
    }

    // Tiny helper to shrink+spin the obstacle during its death animation.
    public class ObstacleDeathAnim : MonoBehaviour
    {
        public float duration = 0.15f;
        private Vector3 startScale;
        private float t;

        private void Awake() => startScale = transform.localScale;

        private void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, k);
            transform.Rotate(Vector3.up, 720f * Time.deltaTime);
        }
    }
}
