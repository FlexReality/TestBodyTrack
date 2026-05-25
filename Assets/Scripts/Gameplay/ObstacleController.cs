using TMPro;
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

    // Flies in a straight line toward a target.
    // Math obstacles (Front/Left/Right) carry a number — hitting the correct answer scores,
    // hitting a wrong answer loses a life, missing the correct answer loses a life.
    // Bottom obstacles use the original jump-dodge logic (no math).
    public class ObstacleController : MonoBehaviour
    {
        [Tooltip("Y-position of the player above which a Bottom obstacle counts as dodged.")]
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

        // Math fields
        private int answerValue;
        private bool isMathObstacle;

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

        // Bright palette for number balls — vivid enough to read from 14 units away.
        private static readonly Color[] BallColors =
        {
            new Color(0.95f, 0.25f, 0.25f), // red
            new Color(0.20f, 0.55f, 1.00f), // blue
            new Color(1.00f, 0.75f, 0.05f), // yellow
            new Color(0.20f, 0.82f, 0.30f), // green
            new Color(1.00f, 0.45f, 0.10f), // orange
            new Color(0.75f, 0.20f, 1.00f), // purple
            new Color(0.10f, 0.85f, 0.85f), // cyan
            new Color(1.00f, 0.30f, 0.75f), // pink
        };

        // Call after Launch() to turn this into a math answer obstacle.
        public void SetAsMathAnswer(int value, bool isCorrect)
        {
            answerValue = value;
            isMathObstacle = true;

            // No mesh — just the number floating in the air.
            spinAxis  = Vector3.up;
            spinSpeed = 55f;
            foreach (var mr in GetComponentsInChildren<MeshRenderer>())
                mr.enabled = false;

            var labelObj = new GameObject("AnswerLabel");
            labelObj.transform.SetParent(transform, false);
            labelObj.transform.localPosition = Vector3.zero;
            float parentWorldScale = Mathf.Max(transform.lossyScale.x, 0.001f);
            labelObj.transform.localScale = Vector3.one / parentWorldScale;

            var tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text      = value.ToString();
            tmp.fontSize  = 11f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = BallColors[Random.Range(0, BallColors.Length)];
            tmp.rectTransform.sizeDelta = new Vector2(3f, 3f);
        }

        private void Update()
        {
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);

            elapsed += Time.deltaTime;
            if (consumed) return;

            if (elapsed >= timeToReach)
            {
                consumed = true;

                if (isMathObstacle)
                {
                    bool isCorrectNow = (answerValue == GameSession.Instance?.CurrentQuestion.Answer);
                    if (isCorrectNow)
                    {
                        GameSession.Instance?.RegisterCorrectAnswerMissed();
                        FlashAndDestroy(new Color(0.9f, 0.2f, 0.2f), 0.18f);
                    }
                    else
                    {
                        // Wrong answer reaching player — no penalty.
                        FlashAndDestroy(new Color(0.5f, 0.5f, 0.5f), 0.12f);
                    }
                    return;
                }

                // Bottom obstacles are visual only — jump is cosmetic, no scoring.
                FlashAndDestroy(new Color(0.4f, 0.4f, 0.4f), 0.12f);
            }
        }

        public void HitByPlayer()
        {
            if (consumed) return;
            consumed = true;

            if (isMathObstacle)
            {
                bool isCorrectNow = (answerValue == GameSession.Instance?.CurrentQuestion.Answer);
                if (isCorrectNow)
                {
                    GameSession.Instance.RegisterCorrectHit();
                    SpawnShards(new Color(0.3f, 1f, 0.4f));
                    HitEffects.SpawnShockwave(transform.position, new Color(0.3f, 1f, 0.5f, 0.8f));
                    HitEffects.SpawnScreenFlash(new Color(0.2f, 0.9f, 0.3f));
                    Destroy(gameObject);
                }
                else
                {
                    GameSession.Instance?.RegisterWrongHit();
                    HitEffects.SpawnShockwave(transform.position, new Color(1f, 0.3f, 0.2f, 0.7f));
                    Destroy(gameObject);
                }
                return;
            }

            GameSession.Instance?.RegisterHit();
            FlashAndDestroy(new Color(1f, 0.95f, 0.4f), 0.12f);
        }

        // Spawns 8 small cubes that fly outward then fade — correct-answer burst.
        private void SpawnShards(Color color)
        {
            velocity = Vector3.zero;
            var shardMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            shardMat.color = color;

            for (int i = 0; i < 8; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
                s.name = "Shard";
                Destroy(s.GetComponent<Collider>());
                s.transform.position   = transform.position;
                s.transform.localScale = Vector3.one * Random.Range(0.15f, 0.3f);
                s.GetComponent<MeshRenderer>().sharedMaterial = shardMat;
                var anim = s.AddComponent<ShardAnim>();
                anim.dir = Random.onUnitSphere;
                anim.dir.z = Mathf.Abs(anim.dir.z) * -0.5f; // bias toward camera
                Destroy(s, 0.5f);
            }
        }

        private void FlashAndDestroy(Color flash, float duration)
        {
            var mr = GetComponentInChildren<MeshRenderer>();
            if (mr != null) mr.material.color = flash;
            var anim = gameObject.AddComponent<ObstacleDeathAnim>();
            anim.duration = duration;
            velocity = Vector3.zero;
            Destroy(gameObject, duration);
            enabled = false;
        }
    }

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

    // Flies a shard outward and shrinks it to nothing.
    public class ShardAnim : MonoBehaviour
    {
        public Vector3 dir;
        private float t;
        private Vector3 startScale;
        private const float Duration = 0.45f;
        private const float Speed    = 6f;

        private void Awake() => startScale = transform.localScale;

        private void Update()
        {
            t += Time.deltaTime;
            float k = t / Duration;
            transform.position   += dir * Speed * Time.deltaTime * (1f - k);
            transform.localScale  = Vector3.Lerp(startScale, Vector3.zero, k);
            transform.Rotate(Vector3.one, 400f * Time.deltaTime);
        }
    }
}
