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

        // Call after Launch() to turn this into a math answer obstacle.
        public void SetAsMathAnswer(int value, bool isCorrect)
        {
            answerValue = value;
            isMathObstacle = true;

            // Slow Y-axis carousel so the number stays readable.
            spinAxis = Vector3.up;
            spinSpeed = 50f;

            var labelObj = new GameObject("AnswerLabel");
            labelObj.transform.SetParent(transform, false);
            labelObj.transform.localPosition = Vector3.zero;

            var tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text = value.ToString();
            tmp.fontSize = 4f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(1.2f, 1.2f);
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

                // Original jump-dodge logic for Bottom obstacles.
                if (Direction == ObstacleDirection.Bottom && playerRef != null
                    && playerRef.position.y > dodgeHeightThreshold)
                {
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

            if (isMathObstacle)
            {
                bool isCorrectNow = (answerValue == GameSession.Instance?.CurrentQuestion.Answer);
                if (isCorrectNow)
                {
                    GameSession.Instance.RegisterCorrectHit();
                    FlashAndDestroy(new Color(0.3f, 1f, 0.4f), 0.2f);
                }
                else
                {
                    GameSession.Instance?.RegisterWrongHit();
                    FlashAndDestroy(new Color(0.9f, 0.2f, 0.2f), 0.2f);
                }
                return;
            }

            GameSession.Instance?.RegisterHit();
            FlashAndDestroy(new Color(1f, 0.95f, 0.4f), 0.12f);
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
}
